using System.Diagnostics;
using System.Threading.Channels;

namespace Furnace.Tasks;

public class SharedResourceQueue<T> : Runnable
{
    private readonly Channel<Func<T, Task>> _workQueue;
    private readonly T[] _sharedResources;
    private readonly bool[] _sharedResourcesBusy;
    private readonly SemaphoreSlim _sharedResourceLock;
    private readonly SemaphoreSlim _sharedResourceSignal;

    public SharedResourceQueue(int number, Func<int, T> resourceFactory)
    {
        _workQueue = Channel.CreateUnbounded<Func<T, Task>>();
        _sharedResources = new T[number];
        _sharedResourcesBusy = new bool[number];
        _sharedResourceLock = new SemaphoreSlim(1, 1);
        _sharedResourceSignal = new SemaphoreSlim(number, number);
        for (var i = 0; i < number; i++)
        {
            _sharedResources[i] = resourceFactory.Invoke(i);
            _sharedResourcesBusy[i] = false;
        }
    }

    public int ItemsInQueue => _workQueue.Reader.Count;

    public async Task Enqueue(Func<T, Task> action)
    {
        await _workQueue.Writer.WriteAsync(action);
    }

    private int IndexOfNextFreeResource()
    {
        for (var i = 0; i < _sharedResourcesBusy.Length; i++)
        {
            if (_sharedResourcesBusy[i] == false)
                return i;
        }
        throw new UnreachableException("This method should not be called until the lock is released");
    }

    public override async Task RunAsync(CancellationToken ct)
    {
        _workQueue.Writer.Complete();
        while (await _workQueue.Reader.WaitToReadAsync(ct))
        {
            if (!_workQueue.Reader.TryRead(out var action)) return;
            
            await _sharedResourceSignal.WaitAsync(ct);
            await _sharedResourceLock.WaitAsync(ct);
            
            var index = IndexOfNextFreeResource();
            _sharedResourcesBusy[index] = true;
            _ = Task.Run(async () =>
            {
                await action.Invoke(_sharedResources[index]);
                await _sharedResourceLock.WaitAsync(ct);
                _sharedResourcesBusy[index] = false;
                _sharedResourceLock.Release();
                _sharedResourceSignal.Release();
            }, ct);
            _sharedResourceLock.Release();
        }
    }
}