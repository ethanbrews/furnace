using Furnace.Auth;
using Furnace.Minecraft;
using Furnace.Tasks;
using Furnace.Utility;
using Furnace.Utility.Extension;

namespace Furnace.Modrinth;

public class PackLaunchTask : Runnable
{
    private readonly string _packId;
    private readonly string _userUuid;
    private readonly DirectoryInfo _rootDir;
    
    public PackLaunchTask(string packId, string userUuid, DirectoryInfo rootDirectory)
    {
        _packId = packId;
        _userUuid = userUuid;
        _rootDir = rootDirectory;
    }
    
    public override async Task RunAsync(CancellationToken ct)
    {
        var profileManager = await UserProfileManager.LoadProfilesAsync(_rootDir);
        var profile = profileManager.SelectedProfile;
        ArgumentNullException.ThrowIfNull(profile);
        
        // Read Instances/(PackId)/modrinth.index.json
        var packInfo = await JsonFileReader.Read<Data.PackIndex.PackIndex>(
            _rootDir.GetFileInfo($"Instances/{_packId}/modrinth.index.json") 
        ).RunAsync(ct);

        // Discover dependencies: mcV, fbV
        var minecraftVersion = packInfo.Dependencies.Minecraft;
        var fabricVersion = packInfo.Dependencies.FabricLoader;

        //Load minecraft/versions/mcV/manifest.json
        var minecraftManifest = await JsonFileReader.Read<Furnace.Minecraft.Data.GameManifest.GameManifest>(
            _rootDir.GetFileInfo($"minecraft/versions/{minecraftVersion}/manifest.json") 
        ).RunAsync(ct);

        var auth = (await UserProfileManager.LoadProfilesAsync(_rootDir)).SelectedProfile;
        ArgumentNullException.ThrowIfNull(auth);


        var vanillaBuilder = new MinecraftCommandBuilder(minecraftManifest, auth)
        {
            NativesDirectory = FileUtil.CreateUniqueTempDirectory(),
            RootDirectory = _rootDir,
            GameDirectory = _rootDir.CreateSubdirectory($"Instances/{_packId}")
        };
    }
}