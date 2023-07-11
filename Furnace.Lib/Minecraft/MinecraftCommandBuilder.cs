using System.Text;
using Furnace.Lib.Auth;
using Furnace.Lib.Utility.Extension;
using Furnace.Minecraft.Data.GameManifest;

namespace Furnace.Lib.Minecraft;

public class MinecraftCommandBuilder
{
    private readonly GameManifest _manifest;
    private readonly UserProfile _userProfile;
    private static bool IsDemoUser => false;
    private static bool IsCustomResolution => false;

    public IList<FileInfo> ClassPathList { get; }
    public string MainClass { get; set; }
    public required DirectoryInfo RootDirectory { get; init; }
    public required DirectoryInfo NativesDirectory { get; init; }
    public required DirectoryInfo GameDirectory { get; init; }

    private string? GetAssemblyFileVersion()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
        return fvi.FileVersion;
    }

    public MinecraftCommandBuilder(GameManifest manifest, UserProfile profile)
    {
        _manifest = manifest;
        ClassPathList = new List<FileInfo>();
        MainClass = _manifest.MainClass;
        _userProfile = profile;
    }

    private IEnumerable<string> GetGameArgs()
    {
        return _manifest.Arguments.Game.SelectMany(arg =>
        {
            if (arg.String != null) return new[] { arg.String! };
            if (arg.GameClass == null)
                throw new ArgumentNullException(nameof(arg.GameClass), "Should never be null in game manifest");

            if (arg.GameClass.Rules?.All(rule =>
                {
                    var meetsFeature = ((rule.Features?.IsDemoUser ?? false) && IsDemoUser) ||
                                       ((rule.Features?.HasCustomResolution ?? false) && IsCustomResolution);
                    return meetsFeature && rule.Action == Furnace.Minecraft.Data.GameManifest.Action.Allow;
                }) ?? true)
                return arg.GameClass.Value.String != null ? new[] { arg.GameClass.Value.String } : arg.GameClass.Value.StringArray!;

            return Array.Empty<string>();
        }).ToList();
    }

    private IEnumerable<string> GetJvmArgs()
    {
        return _manifest.Arguments.Jvm.SelectMany(arg =>
        {
            if (arg.String != null) return new[] { arg.String! };
            if (arg.JvmClass == null)
                throw new ArgumentNullException(nameof(arg.JvmClass), "Should never be null in game manifest");

            if (arg.JvmClass.Rules?.All(rule => rule.IsTrueForThisSystem()) ?? true)
            {
                return arg.JvmClass.Value.StringArray ?? new[] { arg.JvmClass.Value.String! };
            }

            return Array.Empty<string>();
        }).ToList();
    }

    private static string Join(IEnumerable<string> list, string separator = " ") =>
        string.Join(separator, list.Select(s => s.Any(char.IsWhiteSpace) ? $"\"{s}\"" : s));

    private StringBuilder BuildStringTemplate()
    {
        var s = new StringBuilder();
        s.Append("java ");
        s.Append(Join(GetJvmArgs()));
        s.Append(" ${main_class} ");
        s.Append(Join(GetGameArgs()));
        return s;
    }

    public string Build()
    {
        return BuildStringTemplate()
            .Replace("${natives_directory}", NativesDirectory!.FullName)
            .Replace("${launcher_name}", "Furnace")
            .Replace("${launcher_version}", GetAssemblyFileVersion()!)
            .Replace("${classpath}", Join(ClassPathList.Select(x => x.PathRelativeTo(RootDirectory!)), ";"))
            .Replace("${main_class}", MainClass)
            .Replace("${version_name}", _manifest.Id)
            .Replace("${game_directory}", GameDirectory!.PathRelativeTo(RootDirectory!))
            .Replace("${assets_root}", "minecraft/assets")
            .Replace("${assets_index_name}", _manifest.AssetIndex.Id)
            .Replace("${version_type}", _manifest.Type)
            .Replace("${auth_uuid}", _userProfile.Uuid)
            .Replace("${auth_access_token}", _userProfile.AccessToken)
            .Replace("${clientid}", _userProfile.ClientId ?? "UNSET")
            .Replace("${auth_xuid}", _userProfile.XboxUserId ?? "UNSET")
            .Replace("${auth_player_name}", _userProfile.Username)
            .Replace("${user_type}", _userProfile.AuthTypeString)
            .ToString();
    }
}