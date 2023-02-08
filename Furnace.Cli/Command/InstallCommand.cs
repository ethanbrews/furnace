using Furnace.Log;
using Furnace.Tasks;
using Furnace.Tasks.Progress;
using Spectre.Console;

namespace Furnace.Cli.Command;

public static class InstallCommand
{
    public static async Task InstallAsync(string? id)
    {
        var progressBar = new ThreadedProgressBar();
        var installTask = Modrinth.PackInstallTask.InstallLatest(
            Program.RootDirectory,
            id
        );
        var progressTask = Task.Run(progressBar.DisplayProgressAsync);
        await installTask.RunAsync(progressBar.ReportProgress, CancellationToken.None);
        progressBar.Finish();
        await progressTask;
    }
}