using Furnace.Log;

namespace Furnace.Runnable;

public abstract class Runnable
{
    protected Logger Logger;
    protected IProgress<double> Progress;
    public abstract Task RunAsync(CancellationToken ct);

    public async Task RunAsync() => await RunAsync(CancellationToken.None);

    protected Runnable()
    {
        Logger = Logger.GetLogger();
        Progress = ProgressListener.Instance.GetProgress("Runnable");
    }

    public abstract string Tag { get; }
}