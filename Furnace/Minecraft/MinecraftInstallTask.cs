using Furnace.Log;
using Furnace.Minecraft.Data;
using Furnace.Minecraft.Data.GameManifest;
using Furnace.Tasks;
using Furnace.Utility.Extension;

namespace Furnace.Minecraft;

public class MinecraftInstallTask : Runnable
{
    private readonly string _versionName;
    private readonly DirectoryInfo _rootDir;
    private const string VersionManifestUri = "https://piston-meta.mojang.com/mc/game/version_manifest_v2.json";
    private readonly GameInstallType _installType;
    
    private MinecraftInstallTask(string versionName, DirectoryInfo rootDirectory, GameInstallType installType)
    {
        _versionName = versionName;
        _rootDir = rootDirectory;
        _installType = installType;
    }

    public static MinecraftInstallTask InstallLatest(DirectoryInfo rootDirectory, GameInstallType installType) =>
        new MinecraftInstallTask("release", rootDirectory, installType);
    
    public static MinecraftInstallTask InstallLatestSnapshot(DirectoryInfo rootDirectory, GameInstallType installType) =>
        new MinecraftInstallTask("snapshot", rootDirectory, installType);
    
    public static MinecraftInstallTask InstallSpecificVersion(string version, DirectoryInfo rootDirectory, GameInstallType installType) =>
        new MinecraftInstallTask(version, rootDirectory, installType);
    
    private async Task<IEnumerable<Task>> ScheduleGameFilesInstall(HttpClient client, GameManifest manifest)
    {
        var downloadTasks = new List<Task>();
        var gameDir = _rootDir.CreateSubdirectory($"minecraft/versions/{manifest.Id}");
        if (_installType == GameInstallType.Client)
        {
            downloadTasks.Add(new FileDownloadTask(
                client, manifest.Downloads.Client.Url, gameDir.GetFileInfo("client.jar")
            ).RunAsync());
            
            downloadTasks.Add(new FileDownloadTask(
                client, manifest.Downloads.ClientMappings.Url, gameDir.GetFileInfo("client.txt")
            ).RunAsync());
        }
        else
        {
            downloadTasks.Add(new FileDownloadTask(
                client, manifest.Downloads.Server.Url, gameDir.GetFileInfo("server.jar")
            ).RunAsync());
            
            downloadTasks.Add(new FileDownloadTask(
                client, manifest.Downloads.ServerMappings.Url, gameDir.GetFileInfo("server.txt")
            ).RunAsync());
        }
        // Add logging mappings
        var logDir = _rootDir.CreateSubdirectory("minecraft/assets/log_configs/");
        
        downloadTasks.Add(new FileDownloadTask(
            client, manifest.Logging.Client.File.Url, logDir.GetFileInfo(manifest.Logging.Client.File.Id)
        ).RunAsync());

        var manifestFile = gameDir.GetFileInfo("manifest.json");
        await using var fs = manifestFile.OpenWrite();
        await using var writer = new StreamWriter(fs);
        await writer.WriteAsync(manifest.ToJson());

        return downloadTasks;
    }

    public override async Task RunAsync(ReportProgress? progress, CancellationToken ct)
    {
        var log = new Logger($"MinecraftInstaller({_versionName})");
        progress?.Invoke(this, 0.0);
        log.I($"Installing minecraft {_versionName}");
        log.D("Getting version information");
        var client = new HttpClient();

        var allVersions =
            await HttpJsonGetTask.Create<Data.VersionManifest.VersionManifest>(client, new Uri(VersionManifestUri)).RunAsync(ct);

        var targetVersion = allVersions.Versions.First(x => x.Id == _versionName switch
        {
            "release" => allVersions.Latest.Release,
            "snapshot" => allVersions.Latest.Snapshot,
            _ => _versionName
        });
        progress?.Invoke(this, 0.1);
        
        log.I($"Selected minecraft {targetVersion.Id} for install");

        log.D("Getting game manifest information");
        var gameManifest = await HttpJsonGetTask.Create<GameManifest>(client, targetVersion.Url).RunAsync(ct);
        
        log.D("Creating local files");
        var dir = _rootDir.CreateSubdirectory("minecraft");
        dir.Create();
        progress?.Invoke(this, 0.3);
        var parallelTasks = new List<Task>();

        if (_installType == GameInstallType.Client)
        {
            log.D("Scheduling game assets installation");
            parallelTasks.Add(new AssetInstallTask(client, gameManifest, dir).RunAsync(ct));
        }
        
        log.D("Scheduling game libraries installation");
        parallelTasks.Add(new LibraryInstallTask(gameManifest, dir).RunAsync(ct));

        log.D("Scheduling game JAR and logging config installation");
        parallelTasks.AddRange(await ScheduleGameFilesInstall(client, gameManifest));
        progress?.Invoke(this, 0.5);
        await Task.WhenAll(parallelTasks);
        progress?.Invoke(this, 1.0);
    }
}