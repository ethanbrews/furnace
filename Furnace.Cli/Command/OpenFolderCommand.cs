using System.Diagnostics;

namespace Furnace.Cli.Command;

public static class OpenFolderCommand
{
    public static void OpenFolder(string? name, bool promptIfBlank)
    {
        var d = Program.RootDirectory;
        if (string.IsNullOrEmpty(name) && promptIfBlank)
        {
            var packId = LaunchPack.AskForPackId("Select pack");
            d = d.CreateSubdirectory(packId);
        }

        Process.Start("explorer.exe", d.FullName);
    }
}