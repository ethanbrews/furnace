using Furnace.Fabric.Data.FabricLoaderMeta;
using Furnace.Log;
using Furnace.Minecraft;
using Furnace.Minecraft.Data;
using Furnace.Tasks;
using Furnace.Utility.Extension;

namespace Furnace.Fabric;

public class FabricInstallTask : Runnable
{
    private readonly string _gameVersion;
    private readonly string _fabricVersion;
    private const string FabricLoaderMetaUrl = "https://meta.fabricmc.net/v2/versions/loader/{0}/{1}";
    private const string FabricMetaRootUrl = "https://maven.fabricmc.net/";
    private readonly GameInstallType _installType;
    private readonly DirectoryInfo _rootDir;

    private FabricInstallTask(string gameVersion, string fabricVersion, Minecraft.Data.GameInstallType installType, DirectoryInfo rootDir)
    {
        _gameVersion = gameVersion;
        _fabricVersion = fabricVersion;
        _installType = installType;
        _rootDir = rootDir;
    }

    public static FabricInstallTask SpecificVersion(string gameVersion, string fabricVersion, Minecraft.Data.GameInstallType installType, DirectoryInfo rootDir) =>
        new(gameVersion, fabricVersion, installType, rootDir);

    public static string LibraryNameToPath(string name)
    {
        var split = name.Split(":");
        var packageName = split[0];
        var jarName = split[1];
        var versionName = split[2];
        return $"{packageName.Replace(".", "/")}/{jarName}/{versionName}/{jarName}-{versionName}.jar";
    }

    private static async Task<Task> InstallLibrariesAsync(IEnumerable<Library> libraries, DirectoryInfo libraryDir, CancellationToken ct)
    {
        var sharedQueue = new SharedResourceQueue<HttpClient>(1, _ => new HttpClient());
        foreach (var lib in libraries)
        {
            var path = LibraryNameToPath(lib.Name);
            var uriBuilder = new UriBuilder(lib.Url)
            {
                Path = path
            };

            await sharedQueue.Enqueue(client => new FileDownloadTask(client, uriBuilder.Uri, libraryDir.GetFileInfo(path)).RunAsync(ct));
        }

        return sharedQueue.RunAsync(ct);
    }

    public override async Task RunAsync(CancellationToken ct)
    {
        var log = LogManager.GetLogger($"Installing Fabric {_gameVersion}/{_fabricVersion}");
        log.I("Installing fabric");
        var client = new HttpClient();
        var loaderMetaUri = new Uri(string.Format(FabricLoaderMetaUrl, _gameVersion, _fabricVersion));
        var fabricMeta = await HttpJsonGetTask.Create<FabricLoaderMeta>(client, loaderMetaUri).RunAsync(ct);
        var libraryDir = _rootDir.CreateSubdirectory("minecraft/libraries");
        log.D("Scheduling common library installation");
        var commonLibraryTask = await InstallLibrariesAsync(fabricMeta.LauncherMeta.Libraries.Common, libraryDir , ct);
        log.D($"Scheduling sided library installation (installType={_installType})");
        var sidedLibraryTask = await InstallLibrariesAsync(_installType == GameInstallType.Client ? 
                fabricMeta.LauncherMeta.Libraries.Client : fabricMeta.LauncherMeta.Libraries.Server, libraryDir , ct);

        var fabricDirectory = _rootDir.CreateSubdirectory($"minecraft/fabric/");
        
        log.D("Installing fabric loader");
        var loaderUriBuilder = new UriBuilder(FabricMetaRootUrl)
        {
            Path = LibraryNameToPath(fabricMeta.Loader.Maven)
        };
        await new FileDownloadTask(
            client, 
            loaderUriBuilder.Uri, 
            fabricDirectory
                .CreateSubdirectory($"loader/{fabricMeta.Loader.Version}")
                .GetFileInfo($"fabric-loader-{fabricMeta.Loader.Version}.jar")
        ).RunAsync(ct);

        log.D("Installing fabric intermediary");
        var intermediaryUriBuilder = new UriBuilder(FabricMetaRootUrl)
        {
            Path = LibraryNameToPath(fabricMeta.Intermediary.Maven)
        };
        await new FileDownloadTask(
            client, 
            intermediaryUriBuilder.Uri, 
            fabricDirectory
                .CreateSubdirectory($"intermediary/{fabricMeta.Intermediary.Version}")
                .GetFileInfo($"fabric-intermediary-{fabricMeta.Intermediary.Version}.jar")
        ).RunAsync(ct);
        
        
        log.D("Writing fabric metadata...");
        await using (var fs = fabricDirectory
                         .GetFileInfo(
                             $"loader/{fabricMeta.Loader.Version}/fabric-meta-{fabricMeta.Loader.Version}.json")
                         .OpenWrite())
        {
            await using (var writer = new StreamWriter(fs))
            {
                await writer.WriteAsync(fabricMeta.ToJson());
            }
        }

        await commonLibraryTask;
        await sidedLibraryTask;
        log.I("Fabric is installed!");
    }
}