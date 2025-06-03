using System.Threading;
using tailscaler;
using Monitor = tailscaler.Monitor;

var source = new CancellationTokenSource();
var tailscale = new Tailscale();
var icon = new TrayIcon(tailscale, source);
using var monitor = new Monitor();
monitor.Start(source, tailscale, icon.Connect, icon);
icon.Show();
