using System.CommandLine;

namespace Furnace.Cli.Command;

public abstract class CliCommand
{ 
    public abstract void Register(RootCommand rootCommand);

    protected static bool ThrowNoInputException(bool noInput, string name)
    {
        if (noInput)
            throw new ArgumentNullException("Required argument " + name + " is not set.");
        return false;
    }
}