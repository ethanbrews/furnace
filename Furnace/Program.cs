using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.IO;
using Furnace.Command;
using Furnace.Log;

public static class Program
{
    internal static readonly DirectoryInfo RootDirectory = new("data");

    private static async Task<int> RunCommandAsync(string[] args)
    {
        var forceOption = new Option<bool>("force", () => false, "Perform destructive operation without confirmation.");
        var selectNewUserOption = new Option<bool>("select", () => true, "Select the newly added user account as the default");
        var userUuidArgument = new Argument<string?>("uuid", () => null, "The user to target the operation towards.");

        var verboseOption = new Option<bool>("verbose", () => false, "Show verbose output");
        var throwErrorsOption = new Option<bool>("debug-throw-errors", () => false, "Throw errors, used to debug the furnace application")
        {
            IsHidden = true
        };
        
        var rootCommand = new RootCommand(description: "Install and launch modrinth packs with fabric support.");
        rootCommand.AddGlobalOption(verboseOption);
        rootCommand.AddGlobalOption(throwErrorsOption);
        
        var usersCommand = new Command("users", "add, delete and select user accounts.");
        var usersAddCommand = new Command("add", "Add a new user.");
        usersAddCommand.AddOption(selectNewUserOption);
        usersAddCommand.SetHandler(async select => await UserCommand.AddUserAsync(select), selectNewUserOption);
        var usersDeleteCommand = new Command("delete", "Delete the specified user.");
        usersDeleteCommand.AddOption(forceOption);
        usersDeleteCommand.AddArgument(userUuidArgument);
        usersDeleteCommand.SetHandler(async (uuid, force) => await UserCommand.DeleteUserAsync(uuid, force), userUuidArgument, forceOption);
        var userSelectCommand = new Command("select", "Select a new user as default.");
        userSelectCommand.AddArgument(userUuidArgument);
        userSelectCommand.SetHandler(async uuid => await UserCommand.SetUserSelectedAsync(uuid), userUuidArgument);
        var userListCommand = new Command("list", "List all user accounts that are logged in.");
        userListCommand.SetHandler(async verbose => await UserCommand.ListUsersAsync(verbose), verboseOption);

        usersCommand.AddCommand(usersAddCommand);
        usersCommand.AddCommand(userSelectCommand);
        usersCommand.AddCommand(usersDeleteCommand);
        usersCommand.AddCommand(userListCommand);
        rootCommand.AddCommand(usersCommand);
        
        var parser = new CommandLineBuilder(rootCommand)
            .UseVersionOption()
            .UseHelp()
            .UseEnvironmentVariableDirective()
            .UseParseDirective()
            .UseSuggestDirective()
            .RegisterWithDotnetSuggest()
            .UseTypoCorrections()
            .UseParseErrorReporting()
            .CancelOnProcessTermination()
            .Build();
        
        return await parser.InvokeAsync(args);
        
    }
    
    public static async Task<int> Main(params string[] args)
    {
        LogManager.Level = LoggingLevel.NeverLog;
        var logger = LogManager.GetLogger();
        var cts = new CancellationTokenSource();
        
        try
        {
            return await RunCommandAsync(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        return 1;
    }
}
