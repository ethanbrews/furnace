using Furnace.Lib.Web;

namespace Furnace.Lib.Forge;

public class ForgeInstallTask : Runnable.Runnable
{
    private const string ForgeInstallerUrl =
        "https://maven.minecraftforge.net/net/minecraftforge/forge/{0}-{1}/forge-{0}-{1}-installer.jar";

    private readonly string _minecraftVersion;
    private readonly string _forgeVersion;
    
    public ForgeInstallTask(string minecraftVersion, string forgeVersion)
    {
        _minecraftVersion = minecraftVersion;
        _forgeVersion = forgeVersion;
    }
    
    public override async Task RunAsync(CancellationToken ct)
    {
        Logger.I($"Installing forge {_minecraftVersion}-{_forgeVersion}");
        
        // The index file is bundled with the installer... Download to extract a single json file :/

        //await WebService.GetJson<Furnace.Forge.Data.ForgeManifest.ForgeManifest>(new Uri(""), ct);
    }

    public override string Tag => "Forge installer";
}