using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Akka.Hosting.TestKit.Internals;

// Hosting.TestKit internal utility
internal sealed class TestKitThread : IDisposable
{
    private readonly Thread _thread;
    private readonly BlockingCollection<Action> _queue = new();
    private readonly ManualResetEventSlim _started = new();

    public TestKitThread(string name)
    {
        _thread = new Thread(() =>
        {
            // Give this thread its own SC so CTD / message pumps can bind here
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            _started.Set();

            foreach (var work in _queue.GetConsumingEnumerable())
            {
                try { work(); } catch { /* let caller observe via TCS */ }
            }
        })
        {
            IsBackground = true, 
            Name = name
        };
        _thread.Start();
        _started.Wait();
    }

    public Task InvokeAsync(Action a)
    {
        var tcs = new TaskCompletionSource<Done>(TaskCreationOptions.RunContinuationsAsynchronously);
        _queue.Add(() =>
        {
            try
            {
                a();
                tcs.SetResult(Done.Instance);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }

    public Task<T> InvokeAsync<T>(Func<T> f)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _queue.Add(() =>
        {
            try { tcs.SetResult(f()); }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task;
    }

    public void Dispose() => _queue.CompleteAdding();
}
