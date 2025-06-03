using System;
using System.Threading;
using System.Threading.Tasks;
using PeanutButter.Utils;

namespace tailscaler;

public class Tailscale
{
    private readonly SemaphoreSlim _lck;
    public EventHandler<EventArgs> OnConnecting { get; set; }
    public EventHandler<EventArgs> OnConnected { get; set; }
    public EventHandler<EventArgs> OnDisconnected { get; set; }

    public Tailscale()
    {
        _lck = new SemaphoreSlim(1);
    }

    public bool IsTailscaleUp()
    {
        using var io = ProcessIO
            .Start("tailscale", "status");
        io.WaitForExit();
        return io.ExitCode == 0;
    }

    public void TakeDownTailScale()
    {
        using var _ = new AutoLocker(_lck);
        using var io = ProcessIO
            .WithPassthroughIo()
            .Start("tailscale", "down");
        io.WaitForExit();
        OnDisconnected(this, EventArgs.Empty);
    }

    public void BringUpTailScale()
    {
        Task.Run(
            () =>
            {
                using var _ = new AutoLocker(_lck);
                OnConnecting(this, EventArgs.Empty);
                var currentUser = Environment.GetEnvironmentVariable("USER");
                var opString = string.IsNullOrWhiteSpace(currentUser)
                    ? ""
                    : $"--operator={currentUser}";
                var wasInteractive = false;
                using var io = ProcessIO
                    .WithStdErrReceiver(
                        s =>
                        {
                            wasInteractive = true;
                            var trimmed = s.Trim();
                            Console.WriteLine($"stderr: {trimmed}");
                            if (trimmed.StartsWith("https://login.tailscale.com"))
                            {
                                Console.WriteLine($"Opening link: {s}");
                                using var sub = ProcessIO.Start("xdg-open", trimmed);
                                sub.WaitForExit();
                                return;
                            }

                            if (trimmed.Equals("success.", StringComparison.OrdinalIgnoreCase))
                            {
                                OnConnected(this, EventArgs.Empty);
                            }
                        }
                    )
                    .WithStdOutReceiver(
                        s =>
                        {
                            Console.WriteLine($"stdout: {s}");
                        }
                    )
                    .Start("tailscale", "up", opString, "--accept-routes");
                // TODO: open up the web ui with 'tailscale web' when there's a challenge
                /* tailscale emits
                 * To authenticate, visit:

                    https://login.tailscale.com/a/abc0123456789
                      >>> and waits here <<<
                   Success.
                      >>> emitted after login <<<
                 */
                io.WaitForExit();

                // When tailscale can just connect, without
                // login, we get no output, just a success status code
                if (!wasInteractive && io.ExitCode == 0)
                {
                    OnConnected(this, EventArgs.Empty);
                }
            }
        );
    }
}
