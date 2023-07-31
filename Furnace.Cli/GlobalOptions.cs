using System.CommandLine;

namespace Furnace.Cli;

public static class GlobalOptions
{
    public static readonly Option<bool> ForceOption = new(new[]{ "--force", "-f" }, () => false, "Perform destructive operation without confirmation.");
    public static readonly Option<bool> ServerOption = new(new[] { "--server", "-s" }, () => false, "Target the minecraft server.");
    public static readonly Option<bool> DebugOutputOption = new(new[] { "-d", "--debug" }, () => false, "Show debug output.");
    public static readonly Option<bool> NoInputOption = new(new[] { "--no-input"}, () => false, "Don't ask for interactive input and instead exit with an error.");

    public static readonly Option<bool> JsonOption =
        new(new[] { "--json" }, () => false, "Output the results to stdin using JSON format.") { IsHidden = true };
    public static readonly Argument<string?> PackIdArgument = new("packId", () => null, "The modrinth pack id.");
    public static readonly Argument<string?> UuidArgument = new("uuid", () => null, "The uuid to target the operation towards.");

    public static void RegisterGlobalOptions(RootCommand rootCommand)
    {
        rootCommand.AddGlobalOption(ForceOption);
        rootCommand.AddGlobalOption(ServerOption);
        rootCommand.AddGlobalOption(DebugOutputOption);
        rootCommand.AddGlobalOption(NoInputOption);
        rootCommand.AddGlobalOption(JsonOption);
    }
}