using System;
using System.Linq;
using PeanutButter.Utils;

namespace tailscaler;

public class WebInterfaceWrapper : IDisposable
{
    private readonly SlidingWindow<string> _stdErr;
    private static IProcessIO _io;
    private string _url;

    public WebInterfaceWrapper()
    {
        _stdErr = new SlidingWindow<string>(100);
        StartWebInterface();
    }

    public void Show()
    {
        if (string.IsNullOrEmpty(_url))
        {
            _url = GrokUrlFromLogs();
            Console.WriteLine($"Web interface url: {_url}");
        }

        if (string.IsNullOrEmpty(_url))
        {
            return;
        }

        Launch(_url);
    }

    private void Launch(
        string url
    )
    {
        using var io = ProcessIO.Start("xdg-open", url);
        io.WaitForExit();
    }

    private string GrokUrlFromLogs()
    {
        var fallback = "";
        foreach (var item in _stdErr)
        {
            if (item.Contains("starting tailscaled web client"))
            {
                return item.Split(" ", StringSplitOptions.RemoveEmptyEntries).Last();
            }

            if (item.Contains("web server running on"))
            {
                fallback = item.Split(" ", StringSplitOptions.RemoveEmptyEntries)
                    .Last();
            }
        }

        return fallback;
    }

    private void StartWebInterface()
    {
        if (_io?.HasExited ?? true)
        {
            _stdErr.Clear();
            _io = ProcessIO
                .WithStdErrReceiver(_stdErr.Add)
                .Start("tailscale", "web");
        }
    }

    public void Dispose()
    {
        // TODO release managed resources here
        _io?.Kill();
        _io?.Dispose();
        _io = null;
    }
}
