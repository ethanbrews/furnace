using Furnace.Log;
using Furnace.Minecraft.Data.AssetManifest;
using Furnace.Tasks;
using Furnace.Utility.Extension;

namespace Furnace.Minecraft;

public class AssetInstallTask : Runnable
{
    private readonly Data.GameManifest.GameManifest _gameManifest;
    private readonly DirectoryInfo _directoryInfo;
    private const string AssetUrl = "https://resources.download.minecraft.net/{0}/{1}";
    private readonly HttpClient _httpClient;

    public AssetInstallTask(HttpClient httpClient, Data.GameManifest.GameManifest gameManifest, DirectoryInfo gameDir)
    {
        _gameManifest = gameManifest;
        _directoryInfo = gameDir;
        _httpClient = httpClient;
    }
    
    public override async Task RunAsync(ReportProgress? progress, CancellationToken ct)
    {
        var log = new Logger($"MinecraftAssetInstaller({_gameManifest.Id})");
        progress?.Invoke(this, 0.0);
        
        log.I($"Installing minecraft {_gameManifest.Id} assets");
        log.D("Getting asset index");

        var assetIndex = await HttpJsonGetTask
            .Create<AssetManifest>(_httpClient, _gameManifest.AssetIndex.Url).RunAsync(ct);

        log.D("Creating local files");
        var assetsDir = _directoryInfo.CreateSubdirectory("assets");
        var objectsDir = assetsDir.CreateSubdirectory("objects");
        var assetsIndexDir = assetsDir.CreateSubdirectory("indexes");
        
        log.D("Scheduling files for download");

        var scheduledTaskQueue = new SharedResourceQueue<HttpClient>(1, _ => new HttpClient());

        foreach (var asset in assetIndex.Objects.Values)
        {
            await scheduledTaskQueue.Enqueue(client => new FileDownloadTask(
                client,
                new Uri(string.Format(AssetUrl, asset.Hash[..2], asset.Hash)),
                objectsDir.GetFileInfo($"{asset.Hash[..2]}/{asset.Hash}")
            ).RunAsync(ct));
        }
        
        await using (var stream = assetsIndexDir.GetFileInfo($"{_gameManifest.Id}.json").OpenWrite())
        {
            await using (var streamWriter = new StreamWriter(stream))
            {
                await streamWriter.WriteAsync(assetIndex.ToJson());
            }
        }

        log.I($"Scheduled {scheduledTaskQueue.ItemsInQueue} assets for install");
        var totalTasks = (double)scheduledTaskQueue.ItemsInQueue;
        scheduledTaskQueue.OnTaskCompleted += (_, n) => progress?.Invoke(this, n/totalTasks);
        await scheduledTaskQueue.RunAsync(ct);
        log.I($"All assets are installed!");
    }
}