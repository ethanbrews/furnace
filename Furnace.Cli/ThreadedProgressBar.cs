using System.Threading.Channels;
using Furnace.Tasks;
using Spectre.Console;

namespace Furnace.Cli;

using LabelledProgressPair = Tuple<string, double>;

public class ThreadedProgressBar
{
    private readonly Channel<LabelledProgressPair> _channel;

    private readonly Dictionary<string, ProgressTask> _progressMapping;

    public ThreadedProgressBar()
    {
        _progressMapping = new Dictionary<string, ProgressTask>();
        _channel = Channel.CreateUnbounded<LabelledProgressPair>(new UnboundedChannelOptions{
            SingleWriter = false,
            SingleReader = true
        });
    }

    public async void ReportProgress(Runnable runnable, double progress) =>
        await _channel.Writer.WriteAsync(Tuple.Create(runnable.Label, progress));

    public void Finish() => _channel.Writer.Complete();

    private ProgressTask GetProgressTaskForLabel(ProgressContext ctx, string label)
    {
        if (_progressMapping.TryGetValue(label, out var value))
            return value;

        var progress = ctx.AddTask(label);
        _progressMapping[label] = progress;
        return progress;
    }

    public async Task DisplayProgressAsync()
    {
        await AnsiConsole.Progress()
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),       
                new ElapsedTimeColumn(),      
                new SpinnerColumn()
            )
            .StartAsync(async ctx =>
            {
                while (await _channel.Reader.WaitToReadAsync())
                {
                    while (_channel.Reader.TryRead(out var data))
                    {
                        var progress = GetProgressTaskForLabel(ctx, data.Item1);
                        if (!progress.IsStarted) progress.StartTask();
                        if (data.Item2 == 1.0 && !progress.IsFinished) progress.StopTask();
                        if (data.Item2 < 0)
                            progress.IsIndeterminate = true;
                        else
                            progress.Increment(data.Item2);
                    }
                }
            });
    }
}