using System.Collections.Specialized;
using System.ComponentModel;
using Furnace.Runnable;

namespace Furnace.Cli.ConsoleTool;

public class MultiProgress
{
    public MultiProgress()
    {
        _currentlyTracking = new List<ObservableProgress>();
        _lastDrawLength = 0;
        foreach (var progress in ProgressListener.Instance.TrackedProgress)
        {
            Track(progress);
        }
        ProgressListener.Instance.TrackedProgress.CollectionChanged += TrackedProgressOnCollectionChanged;
    }

    private List<ObservableProgress> _currentlyTracking;
    private int _lastDrawLength;

    private void TrackedProgressOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var oldItem in e.OldItems ?? Array.Empty<object>())
        {
            if (oldItem is ObservableProgress oldProgress)
            {
                oldProgress.PropertyChanged -= ProgressOnPropertyChanged;
            }
        }
        foreach (var newItem in e.NewItems ?? Array.Empty<object>())
        {
            if (newItem is ObservableProgress newProgress)
            {
                Track(newProgress);
            }
        }
    }

    private void ProgressOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Redraw();
    }

    private void Track(ObservableProgress progress)
    {
        _currentlyTracking.Add(progress);
        progress.PropertyChanged += ProgressOnPropertyChanged;
    }

    private void Redraw()
    {
        var (_, top) = Console.GetCursorPosition();
        Console.SetCursorPosition(0, top-_lastDrawLength);
        foreach (var progress in _currentlyTracking)
        {
            Console.WriteLine($"{progress.Tag} = {progress.Progress}");
        }
    }
}