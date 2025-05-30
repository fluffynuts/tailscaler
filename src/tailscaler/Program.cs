using System.Threading;
using tailscaler;
using Monitor = tailscaler.Monitor;

var source = new CancellationTokenSource();
var tailscale = new Tailscale();
var icon = new TrayIcon(tailscale, source);
using var monitor = new Monitor();
monitor.Start(source, tailscale, icon.Connect, icon);
icon.Show();

// var asm = typeof(Program).Assembly;
// var connectedIcon = FindIcon("connected");
// var disconnectedIcon = FindIcon("disconnected");
//
// using var web = new WebInterfaceWrapper();
// var connect = new MenuItem("(starting up)")
// {
//     Click = (
//         s,
//         e
//     ) =>
//     {
//         var menuItem = s as MenuItem;
//         menuItem!.IsDisabled = true;
//         try
//         {
//             if (IsTailscaleUp())
//             {
//                 TakeDownTailScale();
//             }
//             else
//             {
//                 BringUpTailScale();
//             }
//         }
//         finally
//         {
//             menuItem.IsDisabled = false;
//         }
//     }
// };
// var status = new MenuItem("status")
// {
//     Click = (
//         s,
//         e
//     ) =>
//     {
//         web.Show();
//     }
// };
// var divider = new MenuItem("-");
// var exit = new MenuItem("Exit")
// {
//     Click = (
//         s,
//         e
//     ) =>
//     {
//         source.Cancel();
//     }
// };
// var menu = new List<MenuItem>()
// {
//     connect,
//     status,
//     divider,
//     exit
// };
// var icon = NotifyIcon.Create(
//     connectedIcon,
//     menu
// );
//
// var tailscale = new Tailscale();
// var icon = new TrayIcon(tailscale, source);
// using var monitor = new Monitor();
// monitor.Start(source, tailscale, connect, icon);
//
// var t = new Thread(
//     () =>
//     {
//         do
//         {
//             var isTailscaleUp = IsTailscaleUp();
//             connect.Text = isTailscaleUp
//                 ? "disconnect"
//                 : "connect";
//             var iconPath = isTailscaleUp
//                 ? FindIcon("connected")
//                 : FindIcon("disconnected");
//             if (icon.IconPath != iconPath)
//             {
//                 icon.IconPath = iconPath;
//             }
//
//             SleepUntil(NextSecond());
//         } while (!source.IsCancellationRequested);
//
//         source.Cancel();
//         Console.WriteLine("Exiting");
//     }
// );
// t.Start();
//
// icon.Show(source.Token);
// t.Join();
//
// bool IsTailscaleUp()
// {
//     using var io = ProcessIO
//         .Start("tailscale", "status");
//     io.WaitForExit();
//     return io.ExitCode == 0;
// }
//
// void TakeDownTailScale()
// {
//     using var io = ProcessIO
//         .WithPassthroughIo()
//         .Start("tailscale", "down");
//     io.WaitForExit();
// }
//
// void BringUpTailScale()
// {
//     Task.Run(
//         () =>
//         {
//             using var io = ProcessIO
//                 .WithStdErrReceiver(
//                     s =>
//                     {
//                         var trimmed = s.Trim();
//                         if (s.StartsWith("https://login.tailscale.com"))
//                         {
//                             using var sub = ProcessIO.Start("xdg-open", trimmed);
//                             sub.WaitForExit();
//                         }
//                     }
//                 )
//                 .WithStdOutReceiver(
//                     s =>
//                     {
//                         Console.WriteLine($"stdout: {s}");
//                     }
//                 )
//                 .Start("tailscale", "up", "--operator=davydm", "--accept-routes");
//             // TODO: open up the web ui with 'tailscale web' when there's a challenge
//             /* tailscale emits
//              * To authenticate, visit:
//
//                 https://login.tailscale.com/a/abc0123456789
//                   >>> and waits here <<<
//                Success.
//                   >>> emitted after login <<<
//              */
//             io.WaitForExit();
//         }
//     );
// }
//
// void Sleep(
//     int millis
// )
// {
//     var until = DateTime.Now.AddMilliseconds(millis);
//     while (!source.IsCancellationRequested && DateTime.Now < until)
//     {
//         var left = until - DateTime.Now;
//         var toSleep = left > TimeSpan.FromMilliseconds(100)
//             ? TimeSpan.FromMilliseconds(100)
//             : left;
//         Thread.Sleep(toSleep);
//     }
// }
//
// void SleepUntil(
//     DateTime dt
// )
// {
//     var delta = dt - DateTime.Now;
//     Sleep((int) delta.TotalMilliseconds);
// }
//
// string FindIcon(
//     string withName
// )
// {
//     var asm = typeof(Program).Assembly;
//     var container = Path.GetDirectoryName(new Uri(asm.Location).LocalPath);
//     return Path.Combine(container, "img", $"{withName}.png");
// }
//
// DateTime NextSecond()
// {
//     return DateTime.Now.TruncateMilliseconds().AddSeconds(1);
// }
//
// void Spin(
//     CancellationToken cancellationToken
// )
// {
// }
