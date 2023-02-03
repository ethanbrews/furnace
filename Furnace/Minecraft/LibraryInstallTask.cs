using System.Runtime.InteropServices;
using Furnace.Log;
using Furnace.Minecraft.Data.GameManifest;
using Furnace.Tasks;
using Furnace.Utility.Extension;

namespace Furnace.Minecraft;

public class LibraryInstallTask : Runnable
{
    private readonly GameManifest _gameManifest;
    private readonly DirectoryInfo _gameDir;

    public LibraryInstallTask(GameManifest gameManifest, DirectoryInfo gameDir)
    {
        _gameManifest = gameManifest;
        _gameDir = gameDir;
    }
    
    public override async Task RunAsync(ReportProgress? progress, CancellationToken ct)
    {
        var log = LogManager.GetLogger($"Installer {_gameManifest.Id} (Libraries)");
        log.I("Installing libraries");
        var libDir = _gameDir.CreateSubdirectory("libraries");
        
        var scheduledTaskQueue = new SharedResourceQueue<HttpClient>(1, _ => new HttpClient());

        foreach (var library in _gameManifest.Libraries)
        {
            if (library.SystemMeetsRules)
            {
                await scheduledTaskQueue.Enqueue(client => new FileDownloadTask(
                    client,
                    library.Downloads.Artifact.Url,
                    libDir.GetFileInfo(library.Downloads.Artifact.Path)
                ).RunAsync(ct));
            }
        }

        log.I($"Scheduling {scheduledTaskQueue.ItemsInQueue} library files for download");
        var totalTasks = (double)scheduledTaskQueue.ItemsInQueue;
        scheduledTaskQueue.OnTaskCompleted += (_, n) => progress?.Invoke(this, n/totalTasks);
        await scheduledTaskQueue.RunAsync(ct);
        log.I("Library files are installed");
    }
}