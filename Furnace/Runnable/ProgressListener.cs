using System.Collections.ObjectModel;
using Furnace.Log;

namespace Furnace.Runnable;

using IProgress = IProgress<double>;
using Progress = Progress<double>;

public class ProgressListener
{
    private static readonly Logger Logger = Logger.GetLogger();
    
    private static readonly Lazy<ProgressListener> Lazy
        = new(() => new ProgressListener());

    public static ProgressListener Instance
        => Lazy.Value;

    private ProgressListener()
    {
        TrackedProgress = new ObservableCollection<ObservableProgress>();
    }

    public ObservableCollection<ObservableProgress> TrackedProgress { get; }

    private ObservableProgress? ProgressByLabel(string label) =>
        TrackedProgress.FirstOrDefault(x => x.Tag == label);

    public IProgress GetProgress(string tag)
    {
        var progress = new Progress();
        progress.ProgressChanged += OnProgressChanged;
        return progress;
    }

    private void OnProgressChanged(object? sender, double e)
    {
        var tag = (sender as Runnable)?.Tag;
        if (tag is null)
        {
            Logger.W($"Received an OnProgressChanged callback from an object that was not a {nameof(Runnable)}");
            return;
        }

        var pair = ProgressByLabel(tag);
        if (pair is {}) // !null check
            pair.Progress = e;
        
        pair = new ObservableProgress
        {
            Tag = tag,
            Progress = e
        };
        TrackedProgress.Add(pair);
    }
}