using Furnace.Fabric.Data.FabricLoaderMeta;
using Furnace.Minecraft.Data;
using Furnace.Utility.Extension;
using Furnace.Web;

namespace Furnace.Fabric;

public class FabricInstallTask : Runnable.Runnable
{
    public override string Tag => $"Minecraft libraries install ({_fabricVersion} - {_gameVersion})";
    private readonly string _gameVersion;
    private readonly string _fabricVersion;
    private const string FabricLoaderMetaUrl = "https://meta.fabricmc.net/v2/versions/loader/{0}/{1}";
    private const string FabricMetaRootUrl = "https://maven.fabricmc.net/";
    private readonly GameInstallType _installType;
    private readonly DirectoryInfo _rootDir;

    private FabricInstallTask(string gameVersion, string fabricVersion, GameInstallType installType, DirectoryInfo rootDir)
    {
        _gameVersion = gameVersion;
        _fabricVersion = fabricVersion;
        _installType = installType;
        _rootDir = rootDir;
    }

    public static FabricInstallTask SpecificVersion(string gameVersion, string fabricVersion, GameInstallType installType, DirectoryInfo rootDir) =>
        new(gameVersion, fabricVersion, installType, rootDir);

    public static string LibraryNameToPath(string name)
    {
        var split = name.Split(":");
        var packageName = split[0];
        var jarName = split[1];
        var versionName = split[2];
        return $"{packageName.Replace(".", "/")}/{jarName}/{versionName}/{jarName}-{versionName}.jar";
    }

    private static async Task InstallLibrariesAsync(IEnumerable<Library> libraries, DirectoryInfo libraryDir, CancellationToken ct)
    {
        await Parallel.ForEachAsync(libraries, ct, async (lib, token) =>
        {
            var path = LibraryNameToPath(lib.Name);
            var uriBuilder = new UriBuilder(lib.Url)
            {
                Path = path
            };
            await WebService.DownloadFileAsync(uriBuilder.Uri, libraryDir.GetFileInfo(path), token);
        });
    }

    public override async Task RunAsync(CancellationToken ct)
    {
        Logger.I("Installing fabric");
        var loaderMetaUri = new Uri(string.Format(FabricLoaderMetaUrl, _gameVersion, _fabricVersion));
        var fabricMeta = await WebService.GetJson<FabricLoaderMeta>(loaderMetaUri, ct);
        var libraryDir = _rootDir.CreateSubdirectory("minecraft/libraries");
        Logger.D("Scheduling common library installation");
        var commonLibraryTask = InstallLibrariesAsync(fabricMeta.LauncherMeta.Libraries.Common, libraryDir , ct);
        Logger.D($"Scheduling sided library installation (installType={_installType})");
        var sidedLibraryTask = InstallLibrariesAsync(_installType == GameInstallType.Client ? 
                fabricMeta.LauncherMeta.Libraries.Client : fabricMeta.LauncherMeta.Libraries.Server, libraryDir , ct);

        var fabricDirectory = _rootDir.CreateSubdirectory($"minecraft/fabric/");
        
        Logger.D("Installing fabric loader");
        var loaderUriBuilder = new UriBuilder(FabricMetaRootUrl)
        {
            Path = LibraryNameToPath(fabricMeta.Loader.Maven)
        };
        await WebService.DownloadFileAsync(
            loaderUriBuilder.Uri, 
            fabricDirectory
                .CreateSubdirectory($"loader/{fabricMeta.Loader.Version}")
                .GetFileInfo($"fabric-loader-{fabricMeta.Loader.Version}.jar"),
            ct
        );

        Logger.D("Installing fabric intermediary");
        var intermediaryUriBuilder = new UriBuilder(FabricMetaRootUrl)
        {
            Path = LibraryNameToPath(fabricMeta.Intermediary.Maven)
        };
        
        await WebService.DownloadFileAsync(
            intermediaryUriBuilder.Uri, 
            fabricDirectory
                .CreateSubdirectory($"intermediary/{fabricMeta.Intermediary.Version}")
                .GetFileInfo($"fabric-intermediary-{fabricMeta.Intermediary.Version}.jar"),
            ct
        );


        Logger.D("Writing fabric metadata...");
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
        Logger.I("Fabric is installed!");
    }
}