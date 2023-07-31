using System.CommandLine;
using Furnace.Lib.Auth;
using Microsoft.Identity.Client;
using Spectre.Console;

namespace Furnace.Cli.Command;

public class UserCommand : ICommand
{
    private static UserProfile PromptForUser(string prompt, UserProfileManager profileManager)
    {
        var username = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(prompt)
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more users)[/]")
                .AddChoices(profileManager.Profiles.Select(x => x.Username)));
        return profileManager.Profiles.First(x => x.Username == username);
    }

    private static UserProfile GetUserOrThrow(string usernameOrUuid, UserProfileManager profileManager)
    {
        var result =
            profileManager.Profiles.FirstOrDefault(x => x.Username == usernameOrUuid || x.Uuid == usernameOrUuid);
        if (result == null)
            throw new ArgumentException("The supplied username or UUID did not match any user profiles.");
        return result;
    }
    
    private static async Task AddUserAsync(bool noInput)
    {
        if (noInput)
            throw new NotImplementedException("Adding users without the browser is not supported in this release.");
        
        Console.WriteLine("Please sign in using the browser.");
        try
        {
            var profileManager = await UserProfileManager.LoadProfilesAsync(Program.RootDirectory);
            var profile = await profileManager.SignInWithMicrosoftAsync();
            await profileManager.WriteProfilesAsync();
            if (profile.IsDemoUser)
            {
                Console.WriteLine("This profile is in demo mode with limited playtime and other restrictions. Purchase the full game on minecraft.net!");
            }
            Console.WriteLine($"Signed in as {profile.Username}!");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: Something went wrong! Please check your Internet connection, or register your account on Xbox Live first.\nDetails: {e.Message}");
        }

    }

    private static async Task SetUserSelectedAsync(string? usernameOrUuid)
    {
        var profileManager = await UserProfileManager.LoadProfilesAsync(Program.RootDirectory);
        if (usernameOrUuid == null)
        {
            if (profileManager.Profiles.Count == 0)
                throw new ArgumentOutOfRangeException(nameof(profileManager), "No users have been added yet!");

            var selectedUserName = PromptForUser("Select a new default user account.", profileManager);
            profileManager.ChangeSelectedProfile(selectedUserName);
        }
        else
        {
            var profile = profileManager.Profiles.First(x => x.Uuid == usernameOrUuid || x.Username == usernameOrUuid);
            profileManager.ChangeSelectedProfile(profile);
        }
        await profileManager.WriteProfilesAsync();
        Console.WriteLine($"Selected {profileManager.SelectedProfile?.Username} as the default profile!");
    }

    private static async Task DeleteUserAsync(string? usernameOrUuid, bool force)
    {
        var profileManager = await UserProfileManager.LoadProfilesAsync(Program.RootDirectory);
        
        if (profileManager.Profiles.Count == 0)
            throw new ArgumentOutOfRangeException(nameof(profileManager), "No users have been added yet!");

        var profile = usernameOrUuid == null ? 
            PromptForUser("Select a user to delete.", profileManager) : 
            GetUserOrThrow(usernameOrUuid, profileManager);

        if (!force && !AnsiConsole.Confirm($"Delete user: {profile.Username}? ({profile.Uuid})", false))
        {
            Console.WriteLine("Cancelling operation.");
            return;
        }

        Console.WriteLine(profileManager.DeleteProfile(profile)
            ? $"Deleted {profile.Username} from saved user accounts."
            : $"An error occured removing {profile.Username} from saved user accounts.");

        await profileManager.WriteProfilesAsync();
    }

    private static async Task ListUsersAsync(bool verbose)
    {
        var profileManager = await UserProfileManager.LoadProfilesAsync(Program.RootDirectory);
        
        var table = new Table();
        table.AddColumn(verbose ? "Selected" : "");
        table.AddColumn("User");
        table.AddColumn("UUID");
        if (verbose)
        {
            table.AddColumn("Auth");
            table.AddColumn("Expires");
            table.AddColumn("Demo");
        }
        foreach (var profile in profileManager.Profiles)
        {
            if (verbose)
            {
                table.AddRow(
                    profile.IsSelected ? "Yes" : "No",
                    profile.Username,
                    profile.Uuid,
                    profile.AuthTypeString,
                    profile.ExpiryTime.ToString() ?? "Unknown",
                    profile.IsDemoUser ? "Yes" : "No"
                );
            }
            else
            {
                table.AddRow(
                    profile.IsSelected ? "*" : "",
                    profile.Username,
                    profile.Uuid
                );
            }
        }
        
        AnsiConsole.Write(table);
    }

    public void Register(RootCommand rootCommand)
    {
        var usersCommand = new System.CommandLine.Command("users", "add, delete and select user accounts.");
        var usersAddCommand = new System.CommandLine.Command("add", "Add a new user.");
        usersAddCommand.AddOption(GlobalOptions.NoInputOption);
        usersAddCommand.SetHandler(AddUserAsync, GlobalOptions.NoInputOption);
        var usersDeleteCommand = new System.CommandLine.Command("delete", "Delete the specified user.");
        usersDeleteCommand.AddOption(GlobalOptions.ForceOption);
        usersDeleteCommand.AddArgument(GlobalOptions.UuidArgument);
        usersDeleteCommand.SetHandler(DeleteUserAsync, GlobalOptions.UuidArgument, GlobalOptions.ForceOption);
        var userSelectCommand = new System.CommandLine.Command("select", "Select a new user as default.");
        userSelectCommand.AddArgument(GlobalOptions.UuidArgument);
        userSelectCommand.SetHandler(SetUserSelectedAsync, GlobalOptions.UuidArgument);
        var userListCommand = new System.CommandLine.Command("list", "List all user accounts that are logged in.");
        userListCommand.SetHandler(ListUsersAsync, GlobalOptions.DebugOutputOption);
        rootCommand.AddCommand(usersCommand);
    }
}