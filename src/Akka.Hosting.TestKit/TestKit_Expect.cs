using System;
using System.Threading;
using System.Threading.Tasks;

namespace Akka.Hosting.TestKit;

public abstract partial class TestKit
{
    public new T ExpectMsg<T>(
        TimeSpan? duration = null,
        string? hint = null,
        CancellationToken cancellationToken = default)
    {
        return AwaitReadyThen(() => base.ExpectMsgAsync<T>(duration, hint, cancellationToken)).GetAwaiter().GetResult();
    }

    public new ValueTask<T> ExpectMsgAsync<T>(
        TimeSpan? duration = null, 
        string? hint = null,
        CancellationToken cancellationToken = default)
    {
        return AwaitReadyThen(() => base.ExpectMsgAsync<T>(duration, hint, cancellationToken));
    }
}