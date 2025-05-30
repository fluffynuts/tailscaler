using System;
using System.Threading;
using PeanutButter.Utils;

namespace tailscaler;

public class TimeUtil
{
    public DateTime NextSecond()
    {
        return DateTime.Now.TruncateMilliseconds().AddSeconds(1);
    }

    public bool Sleep(
        int millis,
        CancellationTokenSource source
    )
    {
        var until = DateTime.Now.AddMilliseconds(millis);
        while (!source.IsCancellationRequested && DateTime.Now < until)
        {
            var left = until - DateTime.Now;
            var toSleep = left > TimeSpan.FromMilliseconds(100)
                ? TimeSpan.FromMilliseconds(100)
                : left;
            Thread.Sleep(toSleep);
        }
        return !source.IsCancellationRequested;
    }

    public void SleepUntil(
        DateTime dt,
        CancellationTokenSource source
    )
    {
        var delta = dt - DateTime.Now;
        Sleep((int) delta.TotalMilliseconds, source);
    }
}
