using Furnace.Fabric;
using Furnace.Log;
using Furnace.Minecraft;
using Furnace.Minecraft.Data;
using Furnace.Tasks;
using Furnace.Utility;
using Furnace.Utility.Extension;

namespace Furnace.Modrinth;

public class PackInstallTask : Runnable
{
    private const string ModrinthVersionListUri = "https://api.modrinth.com/v2/project/{0}/version";
    private const string MrPackIndexFileName = "modrinth.index.json";

    private readonly string _packId;
    private readonly Func<Data.ProjectVersionList.ProjectVersion, bool> _viableCandidateTest;
    private readonly DirectoryInfo _rootDirectory;

    private PackInstallTask(DirectoryInfo rootDirectory, string packId, string? minecraftVersion, string? versionId)
    {
        static bool MatchOne(string? required, string? actual) => required == null || required == actual;

        static bool MatchMany(string? required, IEnumerable<string?> actual) => required == null || actual.Any(
            x => x != null && x == required);
        
        _packId = packId;
        _viableCandidateTest = v =>
            MatchOne(versionId, v.Id) && MatchMany(minecraftVersion, v.GameVersions);

        _rootDirectory = rootDirectory;
    }

    public static PackInstallTask InstallLatest(DirectoryInfo rootDirectory, string packId) => 
        new PackInstallTask(rootDirectory, packId, null, null);

    public static PackInstallTask InstallForMinecraftVersion(DirectoryInfo rootDirectory, string packId, string mcVersion) => 
        new PackInstallTask(rootDirectory, packId, mcVersion, null);
    
    public static PackInstallTask InstallPackVersion(DirectoryInfo rootDirectory, string packId, string packVersion) => 
        new PackInstallTask(rootDirectory, packId, null, packVersion);



    public override async Task RunAsync(ReportProgress? progress, CancellationToken ct)
    {
        var logger = LogManager.GetLogger();
        var client = new HttpClient();
        
        logger.D("Getting Pack Data");
        var allVersions = await HttpJsonGetTask.Create(
            client,
            new Uri(string.Format(ModrinthVersionListUri, _packId)),
            Data.ProjectVersionList.ProjectVersion.FromJson
        ).RunAsync(ct);

        var candidateVersions = allVersions
            .Where(_viableCandidateTest)
            .OrderByDescending(v => v.DatePublished)
            .ToList();

        Data.ProjectVersionList.ProjectVersion selectedVersion;
        try
        {
            selectedVersion = candidateVersions.First(v => v.Featured);
        }
        catch (InvalidOperationException)
        {
            selectedVersion = candidateVersions.First();
        }
        
        logger.I($"Selected valid candidate for installation: {selectedVersion.Id}");
        
        var packZip = new FileInfo(Path.GetTempFileName());
        // Downloading the mr-pack file. The files list may contain other mirrors to try on failure.
        // TODO: Allow mirrors in `FileDownloadTask`
        await new FileDownloadTask(client, selectedVersion.Files[0].Url, packZip).RunAsync(ct);
        var extractDirectory = FileUtil.CreateUniqueTempDirectory();
        System.IO.Compression.ZipFile.ExtractToDirectory(packZip.FullName, extractDirectory.FullName);
        var indexFile = extractDirectory.GetFiles().First(x => x.Name == MrPackIndexFileName);
        var indexData = Data.PackIndex.PackIndex.FromJson(
            await new StreamReader(indexFile.OpenRead()).ReadToEndAsync(ct)
        );

        logger.I($"Beginning Installation...");
        var installDir = _rootDirectory.CreateSubdirectory($"Instances/{_packId}");
        indexFile.CopyTo(installDir.GetFileInfo(MrPackIndexFileName).FullName, true);
        
        var sharedResourceQ = new SharedResourceQueue<HttpClient>(2, i => new HttpClient());
        foreach (var file in indexData.Files)
        {
            await sharedResourceQ.Enqueue(
                c => new FileDownloadTask(c, file.Downloads[0], installDir.GetFileInfo(file.Path)).RunAsync(ct)
            );
        }

        var parallelTasks = new List<Task>();
        logger.I($"Starting download of {sharedResourceQ.ItemsInQueue} items.");
        var totalTasks = (double)sharedResourceQ.ItemsInQueue;
        sharedResourceQ.OnTaskCompleted += (_, n) => progress?.Invoke(this, n/totalTasks);
        await sharedResourceQ.RunAsync(ct);
        
        logger.I($"Installing dependency: Minecraft({indexData.Dependencies.Minecraft})");
        parallelTasks.Add(MinecraftInstallTask.InstallSpecificVersion(
            indexData.Dependencies.Minecraft,
            _rootDirectory,
            GameInstallType.Client
        ).RunAsync(progress, ct));
        
        logger.I($"Installing dependency: FabricLoader({indexData.Dependencies.FabricLoader})");
        parallelTasks.Add(FabricInstallTask.SpecificVersion(
            indexData.Dependencies.Minecraft,
            indexData.Dependencies.FabricLoader,
            GameInstallType.Client,
            _rootDirectory
        ).RunAsync(progress, ct));
        
        
        await Task.WhenAll(parallelTasks);
        logger.I($"Installation completed");
    }
}