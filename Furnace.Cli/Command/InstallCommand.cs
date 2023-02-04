using Furnace.Log;
using Furnace.Modrinth;
using Spectre.Console;

namespace Furnace.Cli.Command;

public static class InstallCommand
{
    public static async Task InstallPack(string? packId, string? versionId, string? minecraftVersion, bool verbose)
    {
        // Pack ID is required.
        packId ??= AnsiConsole.Ask<string>("Enter the pack id.");

        PackInstallTask installer;

        if (versionId != null && minecraftVersion != null)
        {
            AnsiConsole.Write("Only specify a version ID or a minecraft version - not both");
            return;
        }
        
        if (versionId != null)
            installer = PackInstallTask.InstallPackVersion(Program.RootDirectory, packId, versionId);
        else if (minecraftVersion != null)
            installer = PackInstallTask.InstallForMinecraftVersion(Program.RootDirectory, packId, minecraftVersion);
        else
            installer = PackInstallTask.InstallLatest(Program.RootDirectory, packId);

        if (verbose)
        {
            LogManager.Level = LoggingLevel.Debug;
            await installer.RunAsync(CancellationToken.None);
        }
        else
        {
            LogManager.Level = LoggingLevel.NeverLog;
            await AnsiConsole.Status()
                .StartAsync($"Installing modrinth pack {packId}...", async ctx => 
                {
                    ctx.Spinner(Spinner.Known.Star);
                    ctx.SpinnerStyle(Style.Parse("green"));
                    await installer.RunAsync(CancellationToken.None);
                });
        }
    }
}