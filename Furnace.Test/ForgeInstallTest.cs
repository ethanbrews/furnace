using Furnace.Lib.Logging;

namespace Furnace.Test;

public class ForgeInstallTest
{
    private Lib.Forge.ForgeInstallTask? _installer;
    
    [SetUp]
    public void Setup()
    {
        Logger.RegisterHandler(new ConsoleLoggingHandler(LoggingLevel.Debug));
        _installer = new Lib.Forge.ForgeInstallTask(Furnace.Cli.Program.TestsRootDirectory, "1.20.1", "47.1.43");
    }

    [Test]
    public async Task Test1()
    {
        await _installer!.RunAsync(CancellationToken.None);
    }
}