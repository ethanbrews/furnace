using System.Diagnostics;
using System.Runtime.InteropServices;
using Furnace.Lib.Auth;
using Furnace.Lib.Fabric;
using Furnace.Lib.Minecraft;
using Furnace.Lib.Minecraft.Data;
using Furnace.Lib.Modrinth.Data;
using Furnace.Lib.Utility;
using Furnace.Lib.Utility.Extension;

namespace Furnace.Lib.Modrinth;

public class PackLaunchTask : Runnable.Runnable
{
    public override string Tag => $"Modrinth launch ({_packId})";
    
    private readonly string _packId;
    private readonly DirectoryInfo _rootDir;
    private readonly GameInstallType _runType;
    private readonly PackLaunchAction _packLaunchAction;
    
    public PackLaunchTask(string packId, DirectoryInfo rootDirectory, GameInstallType runType, PackLaunchAction launchAction)
    {
        _packId = packId;
        _rootDir = rootDirectory;
        _runType = runType;
        _packLaunchAction = launchAction;
    }

    private async Task<MinecraftCommandBuilder> GetVanillaCommandAsync(
        string minecraftVersionName,
        UserProfile auth,
        CancellationToken ct
    )
    {
        var minecraftManifest = await _rootDir
            .GetFileInfo($"minecraft/versions/{minecraftVersionName}/manifest.json")
            .ReadAsync<Furnace.Minecraft.Data.GameManifest.GameManifest>(ct);
        
        var vanillaBuilder = new MinecraftCommandBuilder(minecraftManifest, auth)
        {
            NativesDirectory = FileUtil.CreateUniqueTempDirectory(),
            RootDirectory = _rootDir,
            GameDirectory = _rootDir.CreateSubdirectory($"Instances/{_packId}")
        };
        
        Logger.D($"Natives directory is {vanillaBuilder.NativesDirectory.FullName}");
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
            new FileInfo(lib).CopyTo(vanillaBuilder.NativesDirectory.GetFileInfo(libName).FullName, true);
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
        var fabricMeta = Furnace.Fabric.Data.FabricLoaderMeta.FabricLoaderMeta.FromJson(
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
                FabricInstallTask.LibraryNameToPath(lib.Name)
            ));
        }

        builder.MainClass = _runType == GameInstallType.Client
            ? fabricMeta.LauncherMeta.MainClass.Client
            : fabricMeta.LauncherMeta.MainClass.Server;
        
        
        return builder;
    }
    
    private async Task WriteCommandToFileAsync(FileInfo file, MinecraftCommandBuilder builder)
    {
        await using var fs = file.OpenWrite();
        await using var writer = new StreamWriter(fs);
        await writer.WriteAsync(builder.Build());

    }
    
    private static async Task ExecuteBatchFileAsync(FileInfo file, DirectoryInfo workingDirectory)
    {
        var process = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"cmd /C \"{file.FullName}\"",
                UseShellExecute = false,
                CreateNoWindow = false,
                WorkingDirectory = workingDirectory.FullName
            }
        };
        process.Start();
        await process.WaitForExitAsync();
    }
    
    public override async Task RunAsync(CancellationToken ct)
    {
        var profileManager = await UserProfileManager.LoadProfilesAsync(_rootDir);
        var profile = profileManager.SelectedProfile;
        ArgumentNullException.ThrowIfNull(profile);
        
        // Read Instances/(PackId)/modrinth.index.json
        var packInfo = await _rootDir
            .GetFileInfo($"Instances/{_packId}/modrinth.index.json")
            .ReadAsync<Furnace.Modrinth.Data.PackIndex.PackIndex>(ct);

        var minecraftVersion = packInfo.Dependencies.Minecraft;
        var fabricVersion = packInfo.Dependencies.FabricLoader;
        
        var auth = (await UserProfileManager.LoadProfilesAsync(_rootDir)).SelectedProfile;
        
        ArgumentNullException.ThrowIfNull(auth);
        var builder = await GetVanillaCommandAsync(minecraftVersion, auth, ct);
        builder = await OverwriteFabricDetailsAsync(builder, fabricVersion, ct);
        builder.RootDirectory = _rootDir.CreateSubdirectory($"Instances/{_packId}");
        
        FileInfo file;
        switch (_packLaunchAction)
        {
            case PackLaunchAction.GenerateScript:
                file = builder.RootDirectory.GetFileInfo("start.bat");
                await WriteCommandToFileAsync(file, builder);
                Logger.I($"Start script written to {file}");
                break;
            case PackLaunchAction.Launch:
                file = new FileInfo(Path.GetTempFileName()+".bat");
                await WriteCommandToFileAsync(file, builder);
                await ExecuteBatchFileAsync(file, builder.RootDirectory);
                Logger.I("Cleaning up...");
                file.Delete();
                break;
            default:
                throw new UnreachableException();
        }
        
    }
}