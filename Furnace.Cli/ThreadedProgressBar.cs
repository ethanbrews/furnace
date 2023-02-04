using System.Threading.Channels;
using Furnace.Tasks;
using Spectre.Console;

namespace Furnace.Cli;

using LabelledProgressPair = Tuple<string, double>;

public class ThreadedProgressBar
{
    private readonly Queue<LabelledProgressPair> _q;
    private readonly SemaphoreSlim _lock;
    private readonly SemaphoreSlim _signal;
    private bool _finished;

    private readonly Dictionary<string, ProgressTask> _progressMapping;

    public ThreadedProgressBar()
    {
        _progressMapping = new Dictionary<string, ProgressTask>();
        _q = new Queue<LabelledProgressPair>();
        _lock = new SemaphoreSlim(1, 1);
        _signal = new SemaphoreSlim(0);
        _finished = false;
    }

    public void ReportProgress(Runnable runnable, double progress)
    {
        _lock.Wait();
        _q.Enqueue(Tuple.Create(runnable.Label, progress));
        _signal.Release();
        _lock.Release();
    }

    public void Finish()
    {
        
        _lock.Wait();
        _finished = true;
        _signal.Release();
        _lock.Release();
    } 

    private ProgressTask GetProgressTaskForLabel(ProgressContext ctx, string label)
    {
        if (_progressMapping.TryGetValue(label, out var value))
            return value;

        var progress = ctx.AddTask(label);
        _progressMapping[label] = progress;
        return progress;
    }

    public void DisplayProgressAsync()
    {
        AnsiConsole.Progress()
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),       
                new ElapsedTimeColumn(),      
                new SpinnerColumn()
            )
            .Start(ctx =>
            {
                while (true)
                {
                    _signal.Wait();
                    if (_finished) return;
                    _lock.Wait();
                    var data = _q.Dequeue();
                    _lock.Release();
                    
                    var progress = GetProgressTaskForLabel(ctx, data.Item1);
                    if (!progress.IsStarted) progress.StartTask();
                    if (data.Item2 == 1.0 && !progress.IsFinished) progress.StopTask();
                    if (data.Item2 < 0)
                        progress.IsIndeterminate = true;
                    else
                        progress.Value(data.Item2);
                }
            });
    }
}