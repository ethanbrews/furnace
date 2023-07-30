using Furnace.Lib.Fabric;
using Furnace.Lib.Minecraft;
using Furnace.Lib.Minecraft.Data;
using Furnace.Lib.Utility;
using Furnace.Lib.Utility.Extension;
using Furnace.Lib.Web;

namespace Furnace.Lib.CurseForge;

public class CFPackInstallTask : Runnable.Runnable
{
    public override string Tag => $"Modrinth install ({_packId})";

    private const string CfModUri = "https://www.curseforge.com/api/v1/mods/{0}/files/{1}/download";
    private const string CFManifest ="manifest.json";

    private readonly string _packId;
    private readonly string _fileId;
    private readonly DirectoryInfo _rootDirectory;

    private CFPackInstallTask(DirectoryInfo rootDirectory, string packId,string fileId, string? minecraftVersion, string? versionId)
    {
        static bool MatchOne(string? required, string? actual) => required == null || required == actual;

        static bool MatchMany(string? required, IEnumerable<string?> actual) => required == null || actual.Any(
            x => x != null && x == required);
        
        _packId = packId;
        _fileId = fileId;

        _rootDirectory = rootDirectory;
    }




    public override async Task RunAsync(CancellationToken ct)
    {
        
        var packZip = new FileInfo(Path.GetTempFileName());
        // Downloading the mr-pack file. The files list may contain other mirrors to try on failure.
        // TODO: Allow mirrors in `FileDownloadTask`

        await WebService.DownloadFileAsync(string.Format(CfModUri,-_packId,_fileId), packZip, ct);
        var extractDirectory = FileUtil.CreateUniqueTempDirectory();
        System.IO.Compression.ZipFile.ExtractToDirectory(packZip.FullName, extractDirectory.FullName);
        var indexFile = extractDirectory.GetFiles().First(x => x.Name == CFManifest);
        var indexData = Furnace.Lib.CurseForge.data.CfManifestFormat.fromJson(
            await new StreamReader(indexFile.OpenRead()).ReadToEndAsync(ct)
        );

        Logger.I($"Installing {indexData.name} {indexData.version}");
		
        var installDir = _rootDirectory.CreateSubdirectory($"Instances/{_packId}");
        indexFile.CopyTo(installDir.GetFileInfo(CfManifest).FullName, true);
        
        await Parallel.ForEachAsync(indexData.Files, ct, async (file, token) =>
        {
			
            await WebService.DownloadFileAsync(string.Format(CfModUri,_packId,_fileId), installDir.GetFileInfo("mods/"+file.fileId+".jar"), token);
        });

        Logger.I($"Installing dependency: Minecraft({indexData.Dependencies.Minecraft})");
        var minecraftTask = MinecraftInstallTask.InstallSpecificVersion(
            indexData.Dependencies.Minecraft,
            _rootDirectory,
            GameInstallType.Client
        ).RunAsync(ct);
        
        Logger.I($"Installing dependency: FabricLoader({indexData.Dependencies.FabricLoader})");
        var fabricTask = FabricInstallTask.SpecificVersion(
            indexData.Dependencies.Minecraft,
            indexData.Dependencies.FabricLoader,
            GameInstallType.Client,
            _rootDirectory
        ).RunAsync(ct);


        await fabricTask;
        await minecraftTask;
        Logger.I($"Installation completed");
    }
}