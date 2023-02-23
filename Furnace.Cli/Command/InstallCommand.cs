using Furnace.Log;
using Furnace.Modrinth;
using Furnace.Utility.Extension;
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
            Logger.RegisterHandler(new ConsoleLoggingHandler(LoggingLevel.Debug));
        
        await installer.RunAsync(CancellationToken.None);
    }

    public static async Task DeletePackAsync(string? packId, bool force)
    {
        packId ??= LaunchPack.AskForPackId("Which pack should be deleted?");
        var targetDirectory = Program.RootDirectory.CreateSubdirectory($"Instances/{packId}");
        string text;
        await using (var stream = targetDirectory.GetFileInfo("modrinth.index.json").OpenRead())
        {
            using var reader = new StreamReader(stream);
            text = await reader.ReadToEndAsync();
        }

        var index = Modrinth.Data.PackIndex.PackIndex.FromJson(text);
        
        if (!force && !AnsiConsole.Confirm($"Delete the pack: {index.Name}? ({packId})", false))
        {
            AnsiConsole.MarkupLine("Cancelling Operation.");
            return;
        }

        targetDirectory.Delete(true);
        AnsiConsole.MarkupLine($"Deleted {index.Name} ({packId})");
    }
}