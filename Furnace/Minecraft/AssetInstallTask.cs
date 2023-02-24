using Furnace.Log;
using Furnace.Minecraft.Data.AssetManifest;
using Furnace.Utility.Extension;
using Furnace.Web;

namespace Furnace.Minecraft;

public class AssetInstallTask : Runnable.Runnable
{
    public override string Tag => $"Minecraft assets install ({_gameManifest.Id})";
    private readonly Data.GameManifest.GameManifest _gameManifest;
    private readonly DirectoryInfo _directoryInfo;
    private const string AssetUrl = "https://resources.download.minecraft.net/{0}/{1}";

    public AssetInstallTask(Data.GameManifest.GameManifest gameManifest, DirectoryInfo gameDir)
    {
        _gameManifest = gameManifest;
        _directoryInfo = gameDir;
    }
    
    public override async Task RunAsync(CancellationToken ct)
    {
        Logger.I($"Installing minecraft {_gameManifest.Id} assets");
        Logger.D("Getting asset index");

        var assetIndex = await WebService.GetJson<AssetManifest>(_gameManifest.AssetIndex.Url, ct);

        Logger.D("Creating local files");
        var assetsDir = _directoryInfo.CreateSubdirectory("assets");
        var objectsDir = assetsDir.CreateSubdirectory("objects");
        var assetsIndexDir = assetsDir.CreateSubdirectory("indexes");

        Logger.D("Downloading assets.");
        await Parallel.ForEachAsync(assetIndex.Objects.Values, ct, async (asset, token) =>
        {
            await WebService.DownloadFileAsync(
                new Uri(string.Format(AssetUrl, asset.Hash[..2], asset.Hash)),
                objectsDir.GetFileInfo($"{asset.Hash[..2]}/{asset.Hash}"),
                token
            );
        });

        Logger.D("Writing asset index.");
        await using var stream = assetsIndexDir.GetFileInfo($"{_gameManifest.Id}.json").OpenWrite();
        await using var streamWriter = new StreamWriter(stream);
        await streamWriter.WriteAsync(assetIndex.ToJson());
        Logger.I("All assets are installed!");
    }
}