namespace Furnace.Runnable;

using IProgress = IProgress<double>;
using Progress = Progress<double>;

public class ProgressListener
{
    private static readonly Lazy<ProgressListener> Lazy
        = new Lazy<ProgressListener>(() => new ProgressListener());

    public static ProgressListener Instance
        => Lazy.Value;

    private ProgressListener()
    {
        _trackedProgress = new List<IProgress>();
    }

    private readonly List<IProgress> _trackedProgress;

    public IProgress GetProgress(string tag)
    {
        var progress = new Progress();
        _trackedProgress.Add(progress);
        return progress;
    }
}