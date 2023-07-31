using System.CommandLine;

namespace Furnace.Cli.Command;

public interface ICommand
{ 
    void Register(RootCommand rootCommand);
}