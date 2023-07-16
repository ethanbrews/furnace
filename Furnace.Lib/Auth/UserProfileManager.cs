using Furnace.Lib.Auth.Microsoft;
using Furnace.Lib.Logging;
using Furnace.Lib.Utility.Extension;

namespace Furnace.Lib.Auth;

public class UserProfileManager
{
    private static readonly Logger Logger;
    private readonly DirectoryInfo _rootDir;
    public List<UserProfile> Profiles { get; }

    public UserProfile? SelectedProfile => Profiles.FirstOrDefault(x => x.IsSelected);

    private static UserProfileManager? _instance;

    public bool DeleteProfile(UserProfile profile)
    {
        var userDir = _rootDir.CreateSubdirectory("users");
        var file = userDir.GetFileInfo($"{profile.Uuid}.json");
        file.Delete();
        return Profiles.Remove(profile);
    }

    private UserProfileManager(IEnumerable<UserProfile> profiles, DirectoryInfo rootDirectory)
    {
        Profiles = new List<UserProfile>(profiles);
        _instance = this;
        _rootDir = rootDirectory;
    }

    static UserProfileManager()
    { Logger = Logger.GetLogger(); }

    public static async Task<UserProfileManager> LoadProfilesAsync(DirectoryInfo rootDirectory)
    {
        if (_instance != null)
            return _instance;
        
        var profiles = new List<UserProfile>();
        var userDir = rootDirectory.CreateSubdirectory("users");
        foreach (var file in userDir.EnumerateFiles().Where(x => x.Extension == ".json"))
        {
            try
            {
                await using var readStream = file.OpenRead();
                using var reader = new StreamReader(readStream);
                profiles.Add(UserProfile.FromJson(await reader.ReadToEndAsync()));
            }
            catch (Exception ex)
            {
                Logger.W($"Unable to read file: {file.Name}");
                Logger.D(ex.StackTrace ?? "No stack trace");
            }
        }
        
        _instance = new UserProfileManager(profiles, rootDirectory);
        return _instance;
    }

    public void ChangeSelectedProfile(UserProfile newSelectedProfile)
    {
        Profiles.ForEach(x => { x.IsSelected = false; });
        newSelectedProfile.IsSelected = true;
    }

    public async Task WriteProfilesAsync()
    {
        var userDir = _rootDir.CreateSubdirectory("users");
        foreach (var profile in Profiles)
        {
            var file = userDir.GetFileInfo($"{profile.Uuid}.json");
            file.Delete();
            await using var fs = file.OpenWrite();
            await using var writer = new StreamWriter(fs);
            await writer.WriteAsync(profile.ToJson());
        }
    }

    public async Task<UserProfile> SignInWithMicrosoftAsync(bool setAsDefault = false)
    {
        var profile = await new MicrosoftAuth().AuthenticateAsync();
        if (setAsDefault)
            ChangeSelectedProfile(profile);
        Profiles.Add(profile);
        return profile;
    }
}