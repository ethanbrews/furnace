using Furnace.Lib.Log;

namespace Furnace.Lib.Runnable;

public abstract class Runnable
{
    protected readonly Logger Logger;

    public abstract Task RunAsync(CancellationToken ct);

    public async Task RunAsync() => await RunAsync(CancellationToken.None);

    protected Runnable()
    {
        Logger = Logger.GetLogger();
    }

    public abstract string Tag { get; }
}