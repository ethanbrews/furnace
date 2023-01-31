using Furnace.Auth.Microsoft;
using Furnace.Log;

namespace Furnace.Auth;

public class UserProfileManager
{
    private static readonly Logger _logger;
    public List<UserProfile> Profiles { get; }

    public UserProfile? SelectedProfile => Profiles.FirstOrDefault(x => x.IsSelected);

    private static UserProfileManager? _instance;

    private UserProfileManager(IEnumerable<UserProfile> profiles)
    {
        Profiles = new List<UserProfile>(profiles);
        _instance = this;
    }
    
    static UserProfileManager() { _logger = LogManager.GetLogger(); }

    public static async Task<UserProfileManager> LoadProfilesAsync(DirectoryInfo rootDirectory)
    {
        if (_instance != null)
            return _instance;
        
        var profiles = new List<UserProfile>();
        var userDir = rootDirectory.CreateSubdirectory("Users");
        foreach (var file in userDir.EnumerateFiles())
        {
            try
            {
                await using var readStream = file.OpenRead();
                using var reader = new StreamReader(readStream);
                profiles.Add(UserProfile.FromJson(await reader.ReadToEndAsync()));
            }
            catch (Exception ex)
            {
                _logger.W($"Unable to read file: {file.Name}");
                _logger.D(ex.StackTrace ?? "No stack trace");
            }
        }
        
        _instance = new UserProfileManager(profiles);
        return _instance;
    }

    public async Task<UserProfile> SignInWithMicrosoftAsync()
    {
        var profile = await new MicrosoftAuth().AuthenticateAsync();
        profile.IsSelected = true;
        Profiles.ForEach(x => { x.IsSelected = false; });
        Profiles.Add(profile);
        return profile;
    }
}