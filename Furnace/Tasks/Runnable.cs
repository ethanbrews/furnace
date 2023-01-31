namespace Furnace.Tasks;

public abstract class Runnable
{
    public abstract Task RunAsync(CancellationToken ct);

    public async Task RunAsync() => await RunAsync(CancellationToken.None);
    public static async Task RunActionAsync(Func<CancellationToken, Task> func, CancellationToken ct)
    {
        await Task.Run(() => new RunnableAction(func).RunAsync(ct), ct);
    }
    
    public static async Task RunActionAsync(Func<CancellationToken, Task> func) =>
        await RunActionAsync(func, CancellationToken.None);

    public static async Task RunManyActionsAsync(CancellationToken ct, params Func<CancellationToken, Task>[] fs)
    {
        var tasks = fs.Select(f => RunActionAsync(f, ct));
        await Task.WhenAll(tasks);
    }

    public static async Task RunManyActionsAsync(params Func<CancellationToken, Task>[] fs) =>
        await RunManyActionsAsync(CancellationToken.None, fs);
    
    private class RunnableAction : Runnable
    {
        private readonly Func<CancellationToken, Task> _action;
        public RunnableAction(Func<CancellationToken, Task> action)
        {
            _action = action;
        }

        public override async Task RunAsync(CancellationToken ct) => await _action.Invoke(ct);
    }
}