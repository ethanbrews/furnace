using Furnace.Log;
using Furnace.Tasks;
using Spectre.Console;

namespace Furnace.Cli.Command;

public static class InstallCommand
{
    public static async Task InstallAsync(string? id)
    {
        LogManager.Level = LoggingLevel.NeverLog;
        var progressBar = new ThreadedProgressBar();
        var installTask = Modrinth.PackInstallTask.InstallLatest(
            Program.RootDirectory,
            id
        );

        var progressTask = progressBar.DisplayProgressAsync();
        await installTask.RunAsync(progressBar.ReportProgress, CancellationToken.None);
        progressBar.Finish();
        await progressTask;
    }
}