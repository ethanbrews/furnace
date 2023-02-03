using System.Runtime.InteropServices;
using Furnace.Auth;
using Furnace.Log;
using Furnace.Minecraft;
using Furnace.Minecraft.Data;
using Furnace.Minecraft.Data.GameManifest;
using Furnace.Tasks;
using Furnace.Utility;
using Furnace.Utility.Extension;

namespace Furnace.Modrinth;

public class PackLaunchTask : Runnable
{
    private readonly string _packId;
    private readonly DirectoryInfo _rootDir;
    private readonly Logger _logger;
    private readonly GameInstallType _runType;
    
    public PackLaunchTask(string packId, DirectoryInfo rootDirectory, GameInstallType runType)
    {
        _packId = packId;
        _rootDir = rootDirectory;
        _logger = LogManager.GetLogger();
        _runType = runType;
    }

    private async Task<MinecraftCommandBuilder> GetVanillaCommandAsync(
        string minecraftVersionName,
        UserProfile auth,
        CancellationToken ct
    )
    {
        var minecraftManifest = await JsonFileReader.Read<Furnace.Minecraft.Data.GameManifest.GameManifest>(
            _rootDir.GetFileInfo($"minecraft/versions/{minecraftVersionName}/manifest.json") 
        ).RunAsync(ct);
        
        var vanillaBuilder = new MinecraftCommandBuilder(minecraftManifest, auth)
        {
            NativesDirectory = FileUtil.CreateUniqueTempDirectory(),
            RootDirectory = _rootDir,
            GameDirectory = _rootDir.CreateSubdirectory($"Instances/{_packId}")
        };
        
        _logger.D($"Natives directory is {vanillaBuilder.NativesDirectory.FullName}");
        var nativesIntermediaryDirectory = FileUtil.CreateUniqueTempDirectory();
        var librariesDir = _rootDir.CreateSubdirectory("minecraft/libraries");
        foreach(var library in minecraftManifest.Libraries)
        {
            if (!library.SystemMeetsRules || !library.Name.ToLower().Contains("native")) continue;
            var libraryFile = librariesDir.GetFileInfo(library.Downloads.Artifact.Path);
            System.IO.Compression.ZipFile.ExtractToDirectory(libraryFile.FullName, nativesIntermediaryDirectory.FullName, null, true);
        }

        var possibleArchitectures = new[] { "x86", "x64" };

        var thisArchitecture = RuntimeInformation.ProcessArchitecture switch 
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            _ => "x64"
        };

        foreach (var lib in Directory.GetFiles(nativesIntermediaryDirectory.FullName, "*.*", SearchOption.AllDirectories)
                     .Where(file => file.ToLower().EndsWith("dll") && (!possibleArchitectures.Any(arch => file.ToLower().Contains(arch)) || file.ToLower().Contains(thisArchitecture))))
        {
            var libName = Path.GetFileName(lib);
            new FileInfo(lib).CopyTo(vanillaBuilder.NativesDirectory.GetFileInfo(libName).FullName);
        }

        foreach (var lib in minecraftManifest.Libraries)
        {
            if (lib.SystemMeetsRules)
            {
                vanillaBuilder.ClassPathList.Add(_rootDir.GetFileInfo(Path.Join("minecraft/libraries/", lib.Downloads.Artifact.Path)));
            }
        }

        vanillaBuilder.ClassPathList.Add(_rootDir.GetFileInfo(Path.Join("minecraft/versions", minecraftManifest.Id, "client.jar")));

        return vanillaBuilder;
    }

    private async Task<MinecraftCommandBuilder> OverwriteFabricDetailsAsync(MinecraftCommandBuilder builder,
        string fabricVersion, CancellationToken ct)
    {
        var minecraftDirectory = _rootDir.CreateSubdirectory("minecraft");
        var fabricDirectory = minecraftDirectory.CreateSubdirectory("fabric");
        await using var fs = fabricDirectory.GetFileInfo(
                $"loader/{fabricVersion}/fabric-meta-{fabricVersion}.json")
            .OpenRead();
        var fabricMeta = Fabric.Data.FabricLoaderMeta.FabricLoaderMeta.FromJson(
            await new StreamReader(fs).ReadToEndAsync(ct)
        );
        
        builder.ClassPathList.Add(fabricDirectory
            .CreateSubdirectory($"loader/{fabricMeta.Loader.Version}")
            .GetFileInfo($"fabric-loader-{fabricMeta.Loader.Version}.jar"));
        
        builder.ClassPathList.Add(fabricDirectory
            .CreateSubdirectory($"intermediary/{fabricMeta.Intermediary.Version}")
            .GetFileInfo($"fabric-intermediary-{fabricMeta.Intermediary.Version}.jar"));

        var libs = fabricMeta.LauncherMeta.Libraries.Common.Concat(_runType == GameInstallType.Client
            ? fabricMeta.LauncherMeta.Libraries.Client
            : fabricMeta.LauncherMeta.Libraries.Server).ToList();

        var libraryDirectory = minecraftDirectory.CreateSubdirectory("libraries");
        foreach (var lib in libs)
        {
            builder.ClassPathList.Add(libraryDirectory.GetFileInfo(
                Fabric.FabricInstallTask.LibraryNameToPath(lib.Name)
            ));
        }

        builder.MainClass = _runType == GameInstallType.Client
            ? fabricMeta.LauncherMeta.MainClass.Client
            : fabricMeta.LauncherMeta.MainClass.Server;
        
        
        return builder;
    }
    
    private async Task WriteToStartFileAsync(MinecraftCommandBuilder builder)
    {
        var startBat = _rootDir.GetFileInfo("start.bat");
        await using var fs = startBat.OpenWrite();
        await using var writer = new StreamWriter(fs);
        await writer.WriteAsync(builder.Build());

    }
    
    public override async Task RunAsync(ReportProgress? progress, CancellationToken ct)
    {
        var profileManager = await UserProfileManager.LoadProfilesAsync(_rootDir);
        var profile = profileManager.SelectedProfile;
        ArgumentNullException.ThrowIfNull(profile);
        
        // Read Instances/(PackId)/modrinth.index.json
        var packInfo = await JsonFileReader.Read<Data.PackIndex.PackIndex>(
            _rootDir.GetFileInfo($"Instances/{_packId}/modrinth.index.json") 
        ).RunAsync(ct);
        
        var minecraftVersion = packInfo.Dependencies.Minecraft;
        var fabricVersion = packInfo.Dependencies.FabricLoader;
        
        var auth = (await UserProfileManager.LoadProfilesAsync(_rootDir)).SelectedProfile;
        
        ArgumentNullException.ThrowIfNull(auth);
        var builder = await GetVanillaCommandAsync(minecraftVersion, auth, ct);
        builder = await OverwriteFabricDetailsAsync(builder, fabricVersion, ct);

        await WriteToStartFileAsync(builder);
    }
}