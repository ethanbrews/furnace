using Furnace.Lib.Utility.Extension;
using Furnace.Lib.Web;
using Furnace.Minecraft.Data.GameManifest;

namespace Furnace.Lib.Minecraft;

public class LibraryInstallTask : Runnable.Runnable
{
    public override string Tag => $"Minecraft libraries install ({_gameManifest.Id})";
    private readonly GameManifest _gameManifest;
    private readonly DirectoryInfo _gameDir;

    public LibraryInstallTask(GameManifest gameManifest, DirectoryInfo gameDir)
    {
        _gameManifest = gameManifest;
        _gameDir = gameDir;
    }
    
    public override async Task RunAsync(CancellationToken ct)
    {
        Logger.I("Installing libraries");
        var libDir = _gameDir.CreateSubdirectory("libraries");
        
        Logger.D("Downloading libraries");
        await Parallel.ForEachAsync(_gameManifest.Libraries, ct, async (library, token) =>
        {
            if (library.SystemMeetsRules)
            {
                await WebService.DownloadFileAsync(
                    library.Downloads.Artifact.Url,
                    libDir.GetFileInfo(library.Downloads.Artifact.Path),
                    token
                );
            }
        });
        
        Logger.I("Library files are installed");
    }
}