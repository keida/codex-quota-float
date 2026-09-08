using System.Threading;

namespace QuotaFloat.Wpf.Services;

public sealed class SingleInstanceService : IDisposable
{
    public const string MutexName = "Local\\QuoteFloat.Wpf.QF009.SingleInstance";
    public const string ActivationEventName = "Local\\QuoteFloat.Wpf.QF009.Activate";

    private readonly Mutex mutex;
    private readonly EventWaitHandle? activationEvent;
    private readonly CancellationTokenSource lifetime = new();
    private readonly Task? listener;
    private static int processOwnsMutex;
    private bool ownsMutex;
    private bool disposed;

    private SingleInstanceService(Mutex mutex, EventWaitHandle? activationEvent, bool ownsMutex, Action activate)
    {
        this.mutex = mutex;
        this.activationEvent = activationEvent;
        this.ownsMutex = ownsMutex;
        if (ownsMutex)
        {
            listener = Task.Run(() => ListenAsync(activate));
        }
    }

    public bool IsPrimary => ownsMutex;

    public static SingleInstanceService Acquire(Action activate)
    {
        var activation = new EventWaitHandle(false, EventResetMode.AutoReset, ActivationEventName);
        var mutex = new Mutex(false, MutexName);
        var ownsMutex = false;
        if (Volatile.Read(ref processOwnsMutex) == 0)
        {
            try
            {
                ownsMutex = mutex.WaitOne(0);
            }
            catch (AbandonedMutexException)
            {
                ownsMutex = true;
            }

            if (ownsMutex && Interlocked.CompareExchange(ref processOwnsMutex, 1, 0) != 0)
            {
                mutex.ReleaseMutex();
                ownsMutex = false;
            }
        }

        if (!ownsMutex)
        {
            activation.Set();
            activation.Dispose();
            return new SingleInstanceService(mutex, null, false, activate);
        }

        return new SingleInstanceService(mutex, activation, true, activate);
    }

    private async Task ListenAsync(Action activate)
    {
        var activation = activationEvent;
        if (activation is null) return;

        while (!lifetime.IsCancellationRequested)
        {
            try
            {
                if (activation.WaitOne(250)) activate();
                await Task.Yield();
            }
            catch (ObjectDisposedException) { break; }
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        lifetime.Cancel();
        listener?.Wait(TimeSpan.FromSeconds(1));
        activationEvent?.Dispose();
        if (ownsMutex)
        {
            try { mutex.ReleaseMutex(); } catch (ApplicationException) { }
            Interlocked.Exchange(ref processOwnsMutex, 0);
            ownsMutex = false;
        }
        mutex.Dispose();
        lifetime.Dispose();
    }
}
