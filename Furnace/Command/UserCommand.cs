using Furnace.Auth;
using Sharprompt;

namespace Furnace.Command;

public static class UserCommand
{
    public static async Task AddUserAsync(bool setAsDefault)
    {
        Console.WriteLine("Please sign in using the browser.");
        try
        {
            var profileManager = await UserProfileManager.LoadProfilesAsync(Program.RootDirectory);
            var profile = await profileManager.SignInWithMicrosoftAsync(setAsDefault);
            await profileManager.WriteProfilesAsync();
            if (profile.IsDemoUser)
            {
                Console.WriteLine("This profile is in Demo mode with limited playtime and other restrictions. Purchase the full game on minecraft.net!");
            }
            Console.WriteLine($"Signed in as {profile.Username}!");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: Something went wrong! Please check your Internet connection, or register your account on Xbox Live first.\nDetails: {e.Message}");
        }

    }

    public static async Task SetUserSelectedAsync(string? uuid)
    {
        var profileManager = await UserProfileManager.LoadProfilesAsync(Program.RootDirectory);
        if (uuid == null)
        {
            if (profileManager.Profiles.Count == 0)
                throw new ArgumentOutOfRangeException(nameof(profileManager), "No users have been added yet!");
            var selectedUserName = Prompt.Select("Select the user", profileManager.Profiles.Select(x => x.Username));
            profileManager.ChangeSelectedProfile(profileManager.Profiles.First(x => x.Username == selectedUserName));
        }
        else
        {
            var profile = profileManager.Profiles.First(x => x.Uuid == uuid);
            profileManager.ChangeSelectedProfile(profile);
        }
        await profileManager.WriteProfilesAsync();
        Console.WriteLine($"Selected {profileManager.SelectedProfile?.Username} as the default profile!");
    }

    public static async Task DeleteUserAsync(string? uuid, bool force)
    {
        var profileManager = await UserProfileManager.LoadProfilesAsync(Program.RootDirectory);
        UserProfile profile;
        if (uuid == null)
        {
            if (profileManager.Profiles.Count == 0)
                throw new ArgumentOutOfRangeException(nameof(profileManager), "No users have been added yet!");
            var selectedUserName = Prompt.Select("Select the user", profileManager.Profiles.Select(x => x.Username));
            profile = profileManager.Profiles.First(x => x.Username == selectedUserName);
        }
        else
        {
            profile = profileManager.Profiles.First(x => x.Uuid == uuid);
        }

        if (!force)
        {
            var answer = Prompt.Confirm($"Delete user: {profile.Username}? ({profile.Uuid})");
            if (!answer)
            {
                Console.WriteLine("Cancelling operation.");
                return;
            }
        }

        Console.WriteLine(profileManager.DeleteProfile(profile)
            ? $"Deleted {profile.Username} from saved user accounts."
            : $"An error occured removing {profile.Username} from saved user accounts.");

        await profileManager.WriteProfilesAsync();
    }

    public static async Task ListUsersAsync(bool verbose)
    {
        var profileManager = await UserProfileManager.LoadProfilesAsync(Program.RootDirectory);
        Console.WriteLine("Currently signed in user accounts:");
        foreach (var profile in profileManager.Profiles)
        {
            if (verbose)
            {
                Console.WriteLine($"{profile.Username} {{" +
                                  $"\n\tUUID = {profile.Uuid}," +
                                  $"\n\tAuthenticationType = {profile.AuthTypeString}," +
                                  $"\n\tExpires = {profile.ExpiryTime}," +
                                  $"\n\tSelected = {profile.IsSelected}" +
                                  $"\n\tIsDemoAccount = {profile.IsDemoUser}" +
                                  $"\n}}");
            }
            else
            {
                Console.WriteLine("   " + (profile.IsSelected ? "*" : " ") + profile.Username);
            }
        }
    }
}