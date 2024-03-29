// <auto-generated />
//
// To parse this JSON data, add NuGet 'System.Text.Json' then do:
//
//    using Furnace.Modrinth.Data.ProjectVersionList;
//
//    var projectVersionList = ProjectVersionList.FromJson(jsonString);
#nullable enable
using Furnace.Lib.Utility;

#pragma warning disable CS8618
#pragma warning disable CS8601
#pragma warning disable CS8603

namespace Furnace.Modrinth.Data.ProjectVersionList
{
    using System;
    using System.Collections.Generic;

    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Globalization;

    public partial class ProjectVersion
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; }

        [JsonPropertyName("author_id")]
        public string AuthorId { get; set; }

        [JsonPropertyName("featured")]
        public bool Featured { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("version_number")]
        public string VersionNumber { get; set; }

        [JsonPropertyName("changelog")]
        public string Changelog { get; set; }

        [JsonPropertyName("changelog_url")]
        public object ChangelogUrl { get; set; }

        [JsonPropertyName("date_published")]
        public DateTimeOffset DatePublished { get; set; }

        [JsonPropertyName("downloads")]
        public long Downloads { get; set; }

        [JsonPropertyName("version_type")]
        public string VersionType { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("requested_status")]
        public object RequestedStatus { get; set; }

        [JsonPropertyName("files")]
        public File[] Files { get; set; }

        [JsonPropertyName("dependencies")]
        public Dependency[] Dependencies { get; set; }

        [JsonPropertyName("game_versions")]
        public string[] GameVersions { get; set; }

        [JsonPropertyName("loaders")]
        public string[] Loaders { get; set; }
    }

    public partial class Dependency
    {
        [JsonPropertyName("version_id")]
        public string VersionId { get; set; }

        [JsonPropertyName("project_id")]
        public object ProjectId { get; set; }

        [JsonPropertyName("file_name")]
        public string FileName { get; set; }

        [JsonPropertyName("dependency_type")]
        public string DependencyType { get; set; }
    }

    public partial class File
    {
        [JsonPropertyName("hashes")]
        public Hashes Hashes { get; set; }

        [JsonPropertyName("url")]
        public Uri Url { get; set; }

        [JsonPropertyName("filename")]
        public string Filename { get; set; }

        [JsonPropertyName("primary")]
        public bool Primary { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("file_type")]
        public object FileType { get; set; }
    }

    public partial class Hashes
    {
        [JsonPropertyName("sha512")]
        public string Sha512 { get; set; }

        [JsonPropertyName("sha1")]
        public string Sha1 { get; set; }
    }

    public partial class ProjectVersion : IJsonConvertable<ProjectVersion[]>
    {
        public static ProjectVersion[] FromJson(string json) => JsonSerializer.Deserialize<ProjectVersion[]>(json, Furnace.Modrinth.Data.ProjectVersionList.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this ProjectVersion[] self) => JsonSerializer.Serialize(self, Furnace.Modrinth.Data.ProjectVersionList.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerOptions Settings = new(JsonSerializerDefaults.General)
        {
            Converters =
            {
                new DateOnlyConverter(),
                new TimeOnlyConverter(),
                IsoDateTimeOffsetConverter.Singleton
            },
        };
    }
    
    public class DateOnlyConverter : JsonConverter<DateOnly>
    {
        private readonly string serializationFormat;
        public DateOnlyConverter() : this(null) { }

        public DateOnlyConverter(string? serializationFormat)
        {
            this.serializationFormat = serializationFormat ?? "yyyy-MM-dd";
        }

        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            return DateOnly.Parse(value!);
        }

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(serializationFormat));
    }

    public class TimeOnlyConverter : JsonConverter<TimeOnly>
    {
        private readonly string serializationFormat;

        public TimeOnlyConverter() : this(null) { }

        public TimeOnlyConverter(string? serializationFormat)
        {
            this.serializationFormat = serializationFormat ?? "HH:mm:ss.fff";
        }

        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            return TimeOnly.Parse(value!);
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(serializationFormat));
    }

    internal class IsoDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override bool CanConvert(Type t) => t == typeof(DateTimeOffset);

        private const string DefaultDateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";

        private DateTimeStyles _dateTimeStyles = DateTimeStyles.RoundtripKind;
        private string? _dateTimeFormat;
        private CultureInfo? _culture;

        public DateTimeStyles DateTimeStyles
        {
            get => _dateTimeStyles;
            set => _dateTimeStyles = value;
        }

        public string? DateTimeFormat
        {
            get => _dateTimeFormat ?? string.Empty;
            set => _dateTimeFormat = (string.IsNullOrEmpty(value)) ? null : value;
        }

        public CultureInfo Culture
        {
            get => _culture ?? CultureInfo.CurrentCulture;
            set => _culture = value;
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            string text;


            if ((_dateTimeStyles & DateTimeStyles.AdjustToUniversal) == DateTimeStyles.AdjustToUniversal
                || (_dateTimeStyles & DateTimeStyles.AssumeUniversal) == DateTimeStyles.AssumeUniversal)
            {
                value = value.ToUniversalTime();
            }

            text = value.ToString(_dateTimeFormat ?? DefaultDateTimeFormat, Culture);

            writer.WriteStringValue(text);
        }

        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? dateText = reader.GetString();

            if (string.IsNullOrEmpty(dateText) == false)
            {
                if (!string.IsNullOrEmpty(_dateTimeFormat))
                {
                    return DateTimeOffset.ParseExact(dateText, _dateTimeFormat, Culture, _dateTimeStyles);
                }
                else
                {
                    return DateTimeOffset.Parse(dateText, Culture, _dateTimeStyles);
                }
            }
            else
            {
                return default(DateTimeOffset);
            }
        }


        public static readonly IsoDateTimeOffsetConverter Singleton = new IsoDateTimeOffsetConverter();
    }
}
#pragma warning restore CS8618
#pragma warning restore CS8601
#pragma warning restore CS8603
