namespace Furnace.Tasks.Progress;

public interface IAsyncProgressReporting
{
    public IProgress<double> Progress { get; }
}