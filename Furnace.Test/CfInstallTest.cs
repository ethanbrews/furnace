using Furnace.Lib.Logging;
using Furnace.Lib.Minecraft.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furnace.Test
{
    internal class CfInstallTest
    {
        private Lib.CurseForge.CFPackInstallTask? _installer;

        [SetUp]
        public void Setup()
        {
            Logger.RegisterHandler(new ConsoleLoggingHandler(LoggingLevel.Debug));
            //https://www.curseforge.com/api/v1/mods/452013/files/4646900/download
            _installer = new Lib.CurseForge.CFPackInstallTask(Furnace.Cli.Program.TestsRootDirectory, "452013", "4646900");
        }

        [Test]
        public async Task Test1()
        {
            await _installer!.RunAsync(CancellationToken.None);
        }
    }
}
