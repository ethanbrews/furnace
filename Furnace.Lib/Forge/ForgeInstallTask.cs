using Furnace.Lib.Utility;
using Furnace.Lib.Utility.Extension;
using Furnace.Lib.Web;

namespace Furnace.Lib.Forge;

public class ForgeInstallTask : Runnable.Runnable
{
    private const string ForgeInstallerUrl =
        "https://maven.minecraftforge.net/net/minecraftforge/forge/{0}-{1}/forge-{0}-{1}-installer.jar";
    
    private const string ForgeIndexFileName = "install_profile.json";

    private readonly string _minecraftVersion;
    private readonly string _forgeVersion;
    private readonly DirectoryInfo _installDir;
    
    public ForgeInstallTask(DirectoryInfo rootDir, string minecraftVersion, string forgeVersion)
    {
        _minecraftVersion = minecraftVersion;
        _forgeVersion = forgeVersion;
        _installDir = rootDir.CreateSubdirectory("minecraft");
    }
    
    public override async Task RunAsync(CancellationToken ct)
    {
        Logger.I($"Installing forge {_minecraftVersion}-{_forgeVersion}");
        
        // The index file is bundled with the installer... Download to extract a single json file :/

        var installerJar = new FileInfo(Path.GetTempFileName());
        await WebService.DownloadFileAsync(
            new Uri(string.Format(ForgeInstallerUrl, _minecraftVersion, _forgeVersion)),
            installerJar,
            ct);
        
        var extractDirectory = FileUtil.CreateUniqueTempDirectory();
        System.IO.Compression.ZipFile.ExtractToDirectory(installerJar.FullName, extractDirectory.FullName);
        var indexFile = extractDirectory.GetFiles().First(x => x.Name == ForgeIndexFileName);

        var forgeManifest =
            Data.ForgeManifest.ForgeManifest.FromJson(await new StreamReader(indexFile.OpenRead()).ReadToEndAsync(ct));
        
        Logger.I($"Installing forge {_minecraftVersion}-{_forgeVersion} (libraries)");
        foreach (var lib in forgeManifest.Libraries)
        {
            Logger.D("Installing library: " + lib.Downloads.Artifact.Url);
            await WebService.DownloadFileAsync(lib.Downloads.Artifact.Url,
                _installDir.GetFileInfo(lib.Downloads.Artifact.Path), ct);
        }
    }

    public override string Tag => "Forge installer";
}