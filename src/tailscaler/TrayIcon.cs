using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NotificationIcon.NET;
using PeanutButter.Utils;

namespace tailscaler;

public class TrayIcon : IDisposable
{
    public MenuItem Exit { get; private set; }
    public MenuItem Divider { get; private set; }
    public MenuItem Status { get; private set; }
    public MenuItem Connect { get; private set; }
    private readonly string _connectedIcon;
    private readonly string _disconnectedIcon;
    private CircularList<string> _spinnerPaths;
    private WebInterfaceWrapper _web;
    private readonly CancellationTokenSource _source;

    public string IconPath
    {
        get => NotifyIcon?.IconPath;
        set
        {
            var icon = NotifyIcon;
            if (icon is null)
            {
                return;
            }

            icon.IconPath = value;
        }
    }


    public TrayIcon(
        Tailscale tailscale,
        CancellationTokenSource source
    )
    {
        _source = source;
        _connectedIcon = FindIcon("connected");
        _disconnectedIcon = FindIcon("disconnected");
        _spinnerPaths = FindSpinners();
        _web = new WebInterfaceWrapper();

        SetupConnectMenuItem(tailscale);
        SetupStatusMenuItem();
        Divider = new MenuItem("-");
        SetupExitMenuItem();
        Menu =
        [
            Connect,
            Status,
            Divider,
            Exit
        ];
        NotifyIcon = NotifyIcon.Create(
            _disconnectedIcon,
            Menu
        );

        tailscale.OnConnected += SetConnectedIcon;
        tailscale.OnDisconnected += SetDisconnectedIcon;
        tailscale.OnConnecting = StartSpinner;
    }

    private CancellationTokenSource _spinning;
    private Thread _spinningThread;

    private void StartSpinner(
        object sender,
        EventArgs e
    )
    {
        if (_spinning is not null)
        {
            return; // already spinning
        }

        _spinning = new CancellationTokenSource();

        _spinningThread = new Thread(
            () =>
            {
                try
                {
                    var timeUtil = new TimeUtil();
                    while (!_spinning.IsCancellationRequested)
                    {
                        foreach (var item in _spinnerPaths)
                        {
                            IconPath = item;
                            if (!timeUtil.Sleep(500, _spinning))
                            {
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Spinner died: {ex.Message}");
                    _spinning.Cancel();
                }
            }
        );
        _spinningThread.Start();
    }

    private void StopSpinner()
    {
        _spinning?.Cancel();
        _spinningThread?.Join();
        _spinning = null;
        _spinningThread = null;
    }

    private void SetDisconnectedIcon(
        object sender,
        EventArgs e
    )
    {
        StopSpinner();
        IconPath = _disconnectedIcon;
    }

    private void SetConnectedIcon(
        object sender,
        EventArgs e
    )
    {
        StopSpinner();
        IconPath = _connectedIcon;
    }

    public void Show()
    {
        NotifyIcon.Show(_source.Token);
    }

    public NotifyIcon NotifyIcon { get; private set; }

    public List<MenuItem> Menu { get; private set; }

    private void SetupExitMenuItem()
    {
        Exit = new MenuItem("Exit")
        {
            Click = (
                s,
                e
            ) =>
            {
                _source.Cancel();
            }
        };
    }

    private void SetupStatusMenuItem()
    {
        Status = new MenuItem("status")
        {
            Click = (
                s,
                e
            ) =>
            {
                _web.Show();
            }
        };
    }

    private void SetupConnectMenuItem(
        Tailscale tailscale
    )
    {
        Connect = new MenuItem("(starting up)")
        {
            Click = (
                s,
                e
            ) =>
            {
                var menuItem = s as MenuItem;
                menuItem!.IsDisabled = true;
                try
                {
                    if (tailscale.IsTailscaleUp())
                    {
                        tailscale.TakeDownTailScale();
                    }
                    else
                    {
                        tailscale.BringUpTailScale();
                    }
                }
                finally
                {
                    menuItem.IsDisabled = false;
                }
            }
        };
    }


    public string FindIcon(
        string withName
    )
    {
        return Path.Combine(ContainerPath, "img", $"{withName}.png");
    }

    private CircularList<string> FindSpinners()
    {
        var collected = new List<string>();
        foreach (var f in Directory.EnumerateFiles(Path.Combine(ContainerPath, "img")))
        {
            var baseName = Path.GetFileName(f);
            if (baseName.StartsWith("spin-"))
            {
                collected.Add(f);
            }
        }

        collected.Sort();
        return new CircularList<string>(collected.ToArray());
    }

    private string ContainerPath =>
        _containerPath ??= FindContainerPath();

    private string FindContainerPath()
    {
        var asm = typeof(Program).Assembly;
        return Path.GetDirectoryName(new Uri(asm.Location).LocalPath);
    }

    private string _containerPath;

    public void Dispose()
    {
        _web?.Dispose();
        _web = null;
    }
}
