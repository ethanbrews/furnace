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

    public CFPackInstallTask(DirectoryInfo rootDirectory, string packId,string fileId)
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

        Uri packUri = new Uri(string.Format(CfModUri, _packId, _fileId));
        await WebService.DownloadFileAsync(packUri, packZip, ct); ;
        var extractDirectory = FileUtil.CreateUniqueTempDirectory();
        System.IO.Compression.ZipFile.ExtractToDirectory(packZip.FullName, extractDirectory.FullName);
        var indexFile = extractDirectory.GetFiles().First(x => x.Name == CFManifest);
        var overridesFolder = extractDirectory.GetDirectories().First(x => x.Name == "overrides");
        var indexData = Furnace.Lib.CurseForge.data.CfManifestFormat.CfManifestFormat.FromJson(
            await new StreamReader(indexFile.OpenRead()).ReadToEndAsync(ct)
        );
        

        Logger.I($"Installing {indexData.Name} {indexData.Version}");
		
        var installDir = _rootDirectory.CreateSubdirectory($"Instances/{_packId}");
        indexFile.CopyTo(installDir.GetFileInfo(CFManifest).FullName, true);
        await FileUtil.CopyDirectoryAsync(overridesFolder, installDir);


        await Parallel.ForEachAsync(indexData.Files, ct, async (file, token) =>
        {
            Uri modUri = new Uri(string.Format(CfModUri, file.ProjectId, file.FileId));
            Logger.I($"looking at: {modUri.AbsoluteUri}");
            try
            {
                await WebService.DownloadFileAsync(modUri, installDir.GetFileInfo("mods/" + file.FileId + ".jar"), token);
            }
            catch (Exception e) 
            {
                Logger.W($"Project with id: {file.ProjectId} likely no longer exists on CurseForge (google 'curseforge.com {file.ProjectId}' to see what it is");
                
            }
            
        });



        Logger.I($"Installing dependency: Minecraft({indexData.Minecraft.Version})");
        var minecraftTask = MinecraftInstallTask.InstallSpecificVersion(
            indexData.Minecraft.Version,
            _rootDirectory,
            GameInstallType.Client
        ).RunAsync(ct);

        //TODO: check the consistency of this, I assume forge works like this better.
        string loaderVersion = indexData.Minecraft.ModLoaders[0].Id.Replace("fabric-", "");

        Logger.I($"Installing dependency: FabricLoader({loaderVersion})");
        var fabricTask = FabricInstallTask.SpecificVersion(
            indexData.Minecraft.Version,
            loaderVersion,
            GameInstallType.Client,
            _rootDirectory
        ).RunAsync(ct);


        await fabricTask;
        await minecraftTask;
        Logger.I($"Installation completed");
        
    }
}