using Furnace.Lib.Minecraft.Data;

namespace Furnace.Test;

public class MinecraftInstallTest
{
    private Lib.Minecraft.MinecraftInstallTask? _installer;
    
    [SetUp]
    public void Setup()
    {
        _installer = Lib.Minecraft.MinecraftInstallTask.InstallLatest(new DirectoryInfo("minecraft"), GameInstallType.Client);
    }

    [Test]
    public async Task Test1()
    {
        await _installer!.RunAsync(CancellationToken.None);
    }
}