using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace UnitTests.Json
{
    public class MovieInfo
    {
        [JsonPropertyName("bluray"), JsonInclude]
        public BlurayInfo? Bluray { get; set; }

        [JsonPropertyName("xml"), JsonInclude]
        public XmlInfo? xml { get; set; }

        [JsonPropertyName("titles"), JsonInclude]
        public List<TitleInfo>? Titles = new();

        public static MovieInfo? Load(string path)
        {
            string jsonString = File.ReadAllText(path);
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.AllowTrailingCommas = true;
            return JsonSerializer.Deserialize<MovieInfo>(jsonString, options)!;
        }
    }

    public class BlurayInfo
    {
        [JsonPropertyName("disc name"), JsonInclude]
        public string? DiscName { get; set; }

        [JsonPropertyName("udf title"), JsonInclude]
        public string? UdfTitle { get; set; }

        [JsonPropertyName("disc id"), JsonInclude]
        public string? DiscID { get; set; }

        [JsonPropertyName("main playlist"), JsonInclude]
        public int? MainPlaylist { get; set; }

        [JsonPropertyName("longest playlist"), JsonInclude]
        public int? LongestPlaylist { get; set; }

        [JsonPropertyName("first play supported"), JsonInclude]
        public bool? FirstPlaySupported { get; set; }

        [JsonPropertyName("top menu supported"), JsonInclude]
        public bool? TopMenuSupported { get; set; }

        [JsonPropertyName("provider data"), JsonInclude]
        public string? ProviderData { get; set; }

        [JsonPropertyName("3D content"), JsonInclude]
        public bool? Content3D { get; set; }

        [JsonPropertyName("initial mode"), JsonInclude]
        public string? InitialMode { get; set; }

        [JsonPropertyName("titles"), JsonInclude]
        public int? Titles { get; set; }

        [JsonPropertyName("bdinfo titles"), JsonInclude]
        public int? BdInfoTitles { get; set; }

        [JsonPropertyName("hdmv titles"), JsonInclude]
        public int? HdmvTitles { get; set; }

        [JsonPropertyName("bd-j titles"), JsonInclude]
        public int? BdjTitles { get; set; }

        [JsonPropertyName("unsupported titles"), JsonInclude]
        public int? UnsupportedTitles { get; set; }

        [JsonPropertyName("aacs"), JsonInclude]
        public bool? AACS { get; set; }

        [JsonPropertyName("bdplus"), JsonInclude]
        public bool? BdPlus { get; set; }

        [JsonPropertyName("bd-j"), JsonInclude]
        public bool? BDJ { get; set; }
    }

    public class XmlInfo
    {
        [JsonPropertyName("filename"), JsonInclude]
        public string? Filename { get; set; }

        [JsonPropertyName("language"), JsonInclude]
        public string? Language { get; set; }

        [JsonPropertyName("num sets"), JsonInclude]
        public int? NumSets { get; set; }

        [JsonPropertyName("set number"), JsonInclude]
        public int? SetNumber { get; set; }
    }

    public class TitleInfo
    {
        [JsonPropertyName("title"), JsonInclude]
        public int? Title { get; set; }

        [JsonPropertyName("playlist"), JsonInclude]
        public int? Playlist { get; set; }

        [JsonPropertyName("length"), JsonInclude]
        public string? Length { get; set; }

        [JsonPropertyName("msecs"), JsonInclude]
        public ulong? Msecs { get; set; }

        [JsonPropertyName("angles"), JsonInclude]
        public int? Angles { get; set; }

        [JsonPropertyName("blocks"), JsonInclude]
        public ulong? Blocks { get; set; }

        [JsonPropertyName("filesize"), JsonInclude]
        public ulong? FileSize { get; set; }

        [JsonPropertyName("video"), JsonInclude]
        public List<VideoInfo>? Videos = new();

        [JsonPropertyName("audio"), JsonInclude]
        public List<AudioInfo>? Audios = new();

        [JsonPropertyName("subtitles"), JsonInclude]
        public List<SubtitleInfo>? Subtitles = new();

        [JsonPropertyName("chapters"), JsonInclude]
        public List<ChapterInfo>? Chapters = new();
    }

    public class VideoInfo
    {
        [JsonPropertyName("track"), JsonInclude]
        public int? Track { get; set; }

        [JsonPropertyName("stream"), JsonInclude]
        public string? Stream { get; set; }

        [JsonPropertyName("format"), JsonInclude]
        public string? Format { get; set; }

        [JsonPropertyName("aspect ratio"), JsonInclude]
        public string? AspectRatio { get; set; }

        [JsonPropertyName("framerate"), JsonInclude]
        public double? FrameRate { get; set; }

        [JsonPropertyName("codec"), JsonInclude]
        public string? Codec { get; set; }

        [JsonPropertyName("codec name"), JsonInclude]
        public string? CodecName { get; set; }
    }

    public class AudioInfo
    {
        [JsonPropertyName("track"), JsonInclude]
        public int? Track { get; set; }

        [JsonPropertyName("stream"), JsonInclude]
        public string? Stream { get; set; }

        [JsonPropertyName("language"), JsonInclude]
        public string? Language { get; set; }

        [JsonPropertyName("codec"), JsonInclude]
        public string? Codec { get; set; }

        [JsonPropertyName("codec name"), JsonInclude]
        public string? CodecName { get; set; }

        [JsonPropertyName("format"), JsonInclude]
        public string? Format { get; set; }

        [JsonPropertyName("rate"), JsonInclude]
        public string? Rate { get; set; }
    }

    public class SubtitleInfo
    {
        [JsonPropertyName("track"), JsonInclude]
        public int? Track { get; set; }

        [JsonPropertyName("stream"), JsonInclude]
        public string? Stream { get; set; }

        [JsonPropertyName("language"), JsonInclude]
        public string? Language { get; set; }
    }

    public class ChapterInfo
    {
        [JsonPropertyName("chapter"), JsonInclude]
        public int? Chapter { get; set; }

        [JsonPropertyName("start time"), JsonInclude]
        public string? StartTime { get; set; }

        [JsonPropertyName("length"), JsonInclude]
        public string? Length { get; set; }

        [JsonPropertyName("start"), JsonInclude]
        public ulong? Start { get; set; }

        [JsonPropertyName("duration"), JsonInclude]
        public ulong? Duration { get; set; }

        [JsonPropertyName("blocks"), JsonInclude]
        public ulong? Blocks { get; set; }

        [JsonPropertyName("filesize"), JsonInclude]
        public ulong? FileSize { get; set; }
    }
}
