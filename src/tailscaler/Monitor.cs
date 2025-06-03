using System;
using System.Threading;
using NotificationIcon.NET;

namespace tailscaler;

public class Monitor : IDisposable
{
    private Thread _monitorThread;
    private CancellationTokenSource _cancellationTokenSource;
    private bool _paused;

    public void Start(
        CancellationTokenSource source,
        Tailscale tailscale,
        MenuItem connect,
        TrayIcon trayIcon
    )
    {
        _cancellationTokenSource = source;
        _monitorThread = new Thread(
            () =>
            {
                var timeUtil = new TimeUtil();
                do
                {
                    var isTailscaleUp = tailscale.IsTailscaleUp();
                    connect.Text = isTailscaleUp
                        ? "disconnect"
                        : "connect";
                    var iconPath = isTailscaleUp
                        ? trayIcon.FindIcon("connected")
                        : trayIcon.FindIcon("disconnected");
                    if (trayIcon.IconPath != iconPath)
                    {
                        trayIcon.IconPath = iconPath;
                    }

                    timeUtil.SleepUntil(timeUtil.NextSecond(), source);
                } while (!source.IsCancellationRequested);

                Console.WriteLine("Exiting");
            }
        );
        _monitorThread.Start();
    }

    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = null;
        _monitorThread?.Join();
        _monitorThread = null;
    }

    public void Dispose()
    {
        Stop();
    }
}
