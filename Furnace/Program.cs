using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Furnace.Auth.Microsoft;
using Furnace.Fabric;
using Furnace.Log;
using Furnace.Minecraft;
using Furnace.Minecraft.Data;
using Furnace.Modrinth;
using Furnace.Tasks;
using Furnace.Utility;
using Furnace.Utility.Extension;

LogManager.Level = LoggingLevel.Trace;
var logger = LogManager.GetLogger();
var cts = new CancellationTokenSource();

logger.I("Starting...");
var rootDirectory = new DirectoryInfo("data");
var packInstallTask = PackInstallTask.InstallLatest(rootDirectory, "1KVo5zza");
var fabricTask = FabricInstallTask.SpecificVersion("1.19.3", "0.14.13", GameInstallType.Client, rootDirectory);
var minecraftInstallTask = MinecraftInstallTask.InstallLatest(rootDirectory, GameInstallType.Client);

// LAUNCH (PackId)
const string packId = "1KVo5zza";


var auth = await new MicrosoftAuth().AuthenticateAsync();
Console.WriteLine("Authenticated!");

var builder = new MinecraftCommandBuilder(minecraftManifest, auth)
{
    NativesDirectory = FileUtil.CreateUniqueTempDirectory(),
    RootDirectory = rootDirectory,
    GameDirectory = rootDirectory.CreateSubdirectory("Instances/Example")
};
logger.D($"Natives directory is {builder.NativesDirectory.FullName}");
var nativesIntermediaryDirectory = FileUtil.CreateUniqueTempDirectory();
var librariesDir = rootDirectory.CreateSubdirectory("minecraft/libraries");
foreach(var library in minecraftManifest.Libraries) {
    if (library.SystemMeetsRules && library.Name.ToLower().Contains("native"))
    {
        var libraryFile = librariesDir.GetFileInfo(library.Downloads.Artifact.Path);
        System.IO.Compression.ZipFile.ExtractToDirectory(libraryFile.FullName, nativesIntermediaryDirectory.FullName, null, true);
    }
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
    new FileInfo(lib).CopyTo(builder.NativesDirectory.GetFileInfo(libName).FullName);
}

foreach (var lib in minecraftManifest.Libraries)
{
    if (lib.SystemMeetsRules)
    {
        builder.ClassPathList.Add(rootDirectory.GetFileInfo(Path.Join("minecraft/libraries/", lib.Downloads.Artifact.Path)));
    }
}

builder.ClassPathList.Add(rootDirectory.GetFileInfo(Path.Join("minecraft/versions", minecraftVersion, "client.jar")));

var built = builder.Build();
logger.I(built);

var startBat = rootDirectory.GetFileInfo("start.bat");
await using var fs = startBat.OpenWrite();
await using var writer = new StreamWriter(fs);
await writer.WriteAsync(built);



// // Load libraries, assetVersion, other stuff?
// // Load minecraft/fabric/loader/fbV/fabric-meta-fbV.json
/*var fabricManifest = await JsonFileReader.Read<Furnace.Fabric.Data.FabricLoaderMeta.FabricLoaderMeta>(
    rootDirectory.GetFileInfo($"minecraft/fabric/loader/{fabricVersion}/fabric-meta-{fabricVersion}.json") 
).RunAsync(cts.Token);*/

// // Discover: List of libraries, intermediary, loader, mainClass
