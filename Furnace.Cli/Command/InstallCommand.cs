using System.CommandLine;
using Furnace.Cli.ConsoleTool;
using Furnace.Lib.Logging;
using Furnace.Lib.Minecraft;
using Furnace.Lib.Minecraft.Data;
using Furnace.Lib.Modrinth;
using Furnace.Lib.Utility.Extension;
using Spectre.Console;

namespace Furnace.Cli.Command;

public class InstallCommand : ICommand
{

    public void Register(RootCommand rootCommand)
    {
        var minecraftVersionOption = new Option<string?>("--minecraft-version", () => null, "The target minecraft version.");
        var modrinthVersionOption = new Option<string?>("--pack-version", () => null, "The target pack version id.");
        var installCommand = new System.CommandLine.Command("install", "Install a modrinth pack.");
        installCommand.AddOption(modrinthVersionOption);
        installCommand.AddOption(minecraftVersionOption);
        installCommand.AddOption(GlobalOptions.NoInputOption);
        installCommand.AddArgument(GlobalOptions.PackIdArgument);
        installCommand.SetHandler(InstallPack, GlobalOptions.PackIdArgument, modrinthVersionOption, minecraftVersionOption, GlobalOptions.DebugOutputOption, GlobalOptions.NoInputOption);
        rootCommand.AddCommand(installCommand);
        
        var deleteCommand = new System.CommandLine.Command("delete", "Delete an installed modrinth pack.");
        deleteCommand.AddArgument(GlobalOptions.PackIdArgument);
        deleteCommand.AddOption(GlobalOptions.ForceOption);
        deleteCommand.AddOption(GlobalOptions.NoInputOption);
        deleteCommand.SetHandler(DeletePackAsync, GlobalOptions.PackIdArgument, GlobalOptions.ForceOption, GlobalOptions.NoInputOption);
        rootCommand.AddCommand(deleteCommand);
    }

    private static async Task InstallPack(string? packId, string? versionId, string? minecraftVersion, bool verbose, bool noInput)
    {
        // Pack ID is required.
        packId ??= noInput ? 
            throw new ArgumentNullException("Required argument " + GlobalOptions.PackIdArgument.Name + " is not set.") : 
            AnsiConsole.Ask<string>("Enter the pack id.");

        PackInstallTask installer;

        // Cannot specify potentially conflicting minecraft and version ids...
        if (versionId != null && minecraftVersion != null)
        {
            AnsiConsole.WriteLine("Only specify a version ID or a minecraft version - not both");
            return;
        }
        
        // Create the installer based on the given arguments
        if (versionId != null)
            installer = PackInstallTask.InstallPackVersion(Program.RootDirectory, packId, versionId);
        else if (minecraftVersion != null)
            installer = PackInstallTask.InstallForMinecraftVersion(Program.RootDirectory, packId, minecraftVersion);
        else
            installer = PackInstallTask.InstallLatest(Program.RootDirectory, packId);

        Logger.RegisterHandler(new ConsoleLoggingHandler(verbose ? LoggingLevel.Debug : LoggingLevel.Info));
        await installer.RunAsync(CancellationToken.None);
        AnsiConsole.WriteLine("Installation completed successfully.");
    }

    private static async Task DeletePackAsync(string? packId, bool force, bool noInput)
    {
        packId ??= noInput ? 
            throw new ArgumentNullException("Required argument " + GlobalOptions.PackIdArgument.Name + " is not set.") : 
            LaunchCommand.AskForPackId("Which pack should be deleted?");
        
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