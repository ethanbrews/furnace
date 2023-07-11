using Furnace.Lib.Minecraft.Data;
using Furnace.Lib.Utility.Extension;
using Furnace.Lib.Web;
using Furnace.Minecraft.Data.GameManifest;

namespace Furnace.Lib.Minecraft;

public class MinecraftInstallTask : Runnable.Runnable
{
    public override string Tag => $"Minecraft install ({_versionName})";
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
        new("release", rootDirectory, installType);
    
    public static MinecraftInstallTask InstallLatestSnapshot(DirectoryInfo rootDirectory, GameInstallType installType) =>
        new("snapshot", rootDirectory, installType);
    
    public static MinecraftInstallTask InstallSpecificVersion(string version, DirectoryInfo rootDirectory, GameInstallType installType) =>
        new(version, rootDirectory, installType);
    
    private async Task DownloadGameFilesAsync(GameManifest manifest, CancellationToken ct)
    {
        var downloadTasks = new List<Task>();
        var gameDir = _rootDir.CreateSubdirectory($"minecraft/versions/{manifest.Id}");
        if (_installType == GameInstallType.Client)
        {
            downloadTasks.Add(WebService.DownloadFileAsync(manifest.Downloads.Client.Url, gameDir.GetFileInfo("client.jar"), ct));
            downloadTasks.Add(WebService.DownloadFileAsync(manifest.Downloads.ClientMappings.Url, gameDir.GetFileInfo("client.txt"), ct));
        }
        else
        {
            downloadTasks.Add(WebService.DownloadFileAsync(manifest.Downloads.Server.Url, gameDir.GetFileInfo("server.jar"), ct));
            downloadTasks.Add(WebService.DownloadFileAsync(manifest.Downloads.ServerMappings.Url, gameDir.GetFileInfo("server.txt"), ct));
        }
        // Add logging mappings
        var logDir = _rootDir.CreateSubdirectory("minecraft/assets/log_configs/");
        
        downloadTasks.Add(WebService.DownloadFileAsync(manifest.Logging.Client.File.Url, logDir.GetFileInfo(manifest.Logging.Client.File.Id), ct));

        var manifestFile = gameDir.GetFileInfo("manifest.json");
        await using var fs = manifestFile.OpenWrite();
        await using var writer = new StreamWriter(fs);
        await writer.WriteAsync(manifest.ToJson());

        await Task.WhenAll(downloadTasks);
    }

    public override async Task RunAsync(CancellationToken ct)
    {
        Logger.I($"Installing minecraft {_versionName}");
        Logger.D("Getting version information");

        var allVersions =
            await WebService.GetJson<Furnace.Minecraft.Data.VersionManifest.VersionManifest>(new Uri(VersionManifestUri), ct);

        var targetVersion = allVersions.Versions.First(x => x.Id == _versionName switch
        {
            "release" => allVersions.Latest.Release,
            "snapshot" => allVersions.Latest.Snapshot,
            _ => _versionName
        });
        
        Logger.I($"Selected minecraft {targetVersion.Id} for install");

        Logger.D("Getting game manifest information");
        var gameManifest = await WebService.GetJson<GameManifest>(targetVersion.Url, ct);
        
        Logger.D("Creating local files");
        var dir = _rootDir.CreateSubdirectory("minecraft");
        dir.Create();

        var parallelTasks = new List<Task>();

        if (_installType == GameInstallType.Client)
        {
            Logger.D("Scheduling game assets installation");
            parallelTasks.Add(new AssetInstallTask(gameManifest, dir).RunAsync(ct));
        }
        Logger.D("Scheduling game libraries installation");
        parallelTasks.Add(new LibraryInstallTask(gameManifest, dir).RunAsync(ct));

        Logger.D("Scheduling game JAR and logging config installation");
        parallelTasks.Add(DownloadGameFilesAsync(gameManifest, ct));

        await Task.WhenAll(parallelTasks);
    }
}