﻿using Furnace.Lib.Fabric;
using Furnace.Lib.Minecraft;
using Furnace.Lib.Minecraft.Data;
using Furnace.Lib.Utility;
using Furnace.Lib.Utility.Extension;
using Furnace.Lib.Web;

namespace Furnace.Lib.CurseForge;

public class CFPackInstallTask : Runnable.Runnable
{
    public override string Tag => $"Modrinth install ({_packId})";

    private const string ModrinthVersionListUri = "https://www.curseforge.com/api/v1/mods/{0}/files/{1}/download";
    private const string MrPackIndexFileName = "modrinth.index.json";

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

    public static CFPackInstallTask InstallLatest(DirectoryInfo rootDirectory, string packId) =>
        new(rootDirectory, packId, null, null);

    public static CFPackInstallTask InstallForMinecraftVersion(DirectoryInfo rootDirectory, string packId,
        string mcVersion) => new(rootDirectory, packId, mcVersion, null);
    
    public static CFPackInstallTask InstallPackVersion(DirectoryInfo rootDirectory, string packId, string packVersion) =>
        new(rootDirectory, packId, null, packVersion);



    public override async Task RunAsync(CancellationToken ct)
    {
        
        Logger.D("Getting Pack Data");
        var allVersions = await WebService.GetJson(
            new Uri(string.Format(ModrinthVersionListUri, _packId)),
            Furnace.Modrinth.Data.ProjectVersionList.ProjectVersion.FromJson,
            ct
        );

        

        
        
        Logger.I($"Selected valid candidate for installation: {selectedVersion.Id}");
        
        var packZip = new FileInfo(Path.GetTempFileName());
        // Downloading the mr-pack file. The files list may contain other mirrors to try on failure.
        // TODO: Allow mirrors in `FileDownloadTask`
        await WebService.DownloadFileAsync(selectedVersion.Files[0].Url, packZip, ct);
        var extractDirectory = FileUtil.CreateUniqueTempDirectory();
        System.IO.Compression.ZipFile.ExtractToDirectory(packZip.FullName, extractDirectory.FullName);
        var indexFile = extractDirectory.GetFiles().First(x => x.Name == MrPackIndexFileName);
        var indexData = Furnace.Modrinth.Data.PackIndex.PackIndex.FromJson(
            await new StreamReader(indexFile.OpenRead()).ReadToEndAsync(ct)
        );

        Logger.I($"Installing {indexData.Name} {indexData.VersionId}");
        var installDir = _rootDirectory.CreateSubdirectory($"Instances/{_packId}");
        indexFile.CopyTo(installDir.GetFileInfo(MrPackIndexFileName).FullName, true);
        
        await Parallel.ForEachAsync(indexData.Files, ct, async (file, token) =>
        {
            await WebService.DownloadFileAsync(file.Downloads[0], installDir.GetFileInfo(file.Path), token);
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