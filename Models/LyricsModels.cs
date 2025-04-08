using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LyricsFinder.Models
{
    public class LyricsResponse
    {
        public string? Lyrics { get; set; }
        public string? Error { get; set; }
    }

    public class SongMatch
    {
        public string? Artist { get; set; }
        public string? Title { get; set; }
        public string? Lyrics { get; set; }
        public string? AlbumCover { get; set; }
        public string? PreviewUrl { get; set; }
        public string? ArtistImage { get; set; }
        public double MatchScore { get; set; }
    }

    public class LyricsSearchViewModel
    {
        public string? InputLyrics { get; set; }
        public List<SongMatch> MatchedSongs { get; set; } = new List<SongMatch>();
    }

    #region Deezer API Models
    public class DeezerSuggestResponse
    {
        [JsonPropertyName("data")]
        public List<DeezerTrack>? Data { get; set; } = new List<DeezerTrack>();

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("next")]
        public string? Next { get; set; }
    }

    public class DeezerTrack
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("link")]
        public string? Link { get; set; }

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("preview")]
        public string? Preview { get; set; }

        [JsonPropertyName("artist")]
        public DeezerArtist? Artist { get; set; }

        [JsonPropertyName("album")]
        public DeezerAlbum? Album { get; set; }
    }

    public class DeezerArtist
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("picture")]
        public string? Picture { get; set; }

        [JsonPropertyName("picture_medium")]
        public string? PictureMedium { get; set; }
    }

    public class DeezerAlbum
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("cover")]
        public string? Cover { get; set; }

        [JsonPropertyName("cover_medium")]
        public string? CoverMedium { get; set; }
    }
    #endregion
} 