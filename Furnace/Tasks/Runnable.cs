namespace Furnace.Tasks;
public delegate void ReportProgress(Runnable runnable, double progress);

public abstract class Runnable
{
    public abstract Task RunAsync(ReportProgress? progress, CancellationToken ct);

    public async Task RunAsync(CancellationToken ct) => await RunAsync(null, ct);
    
    public async Task RunAsync() => await RunAsync(null, CancellationToken.None);

    public string Label => "Unlabelled";

}