using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using LyricsFinder.Models;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace LyricsFinder.Services
{
    public class LyricsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _lyricsApiBaseUrl = "https://api.lyrics.ovh/v1";
        private readonly string _deezerSuggestApiUrl = "https://api.lyrics.ovh/suggest/";

        public LyricsService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<SongMatch>> SearchSongsByLyrics(string searchQuery, int maxResults = 10)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return new List<SongMatch>();
            }

            try
            {
                // Use the Deezer suggest API to search for tracks
                // Convert spaces to + for API compatibility
                var encodedQuery = HttpUtility.UrlEncode(searchQuery).Replace("+", "%20");
                var apiUrl = $"{_deezerSuggestApiUrl}{encodedQuery}";
                
                Console.WriteLine($"Searching with URL: {apiUrl}");
                
                var response = await _httpClient.GetAsync(apiUrl);
                
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Response: {content}");
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"API Error: {response.StatusCode}");
                    return new List<SongMatch> 
                    { 
                        new SongMatch 
                        { 
                            Title = "API Error", 
                            Artist = $"Status: {response.StatusCode}",
                            Lyrics = $"Debug info: URL={apiUrl}\nResponse={content}" 
                        } 
                    };
                }

                var suggestResponse = JsonSerializer.Deserialize<DeezerSuggestResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (suggestResponse == null || suggestResponse.Data == null || !suggestResponse.Data.Any())
                {
                    Console.WriteLine("No results from API");
                    return new List<SongMatch> 
                    { 
                        new SongMatch 
                        { 
                            Title = "No Results Found", 
                            Artist = "Debug Information",
                            Lyrics = $"Debug info: URL={apiUrl}\nResponse={content}" 
                        } 
                    };
                }

                var matchedSongs = new List<SongMatch>();

                // For each track in the response, create a SongMatch 
                foreach (var track in suggestResponse.Data.Take(maxResults))
                {
                    var songMatch = new SongMatch
                    {
                        Artist = track.Artist?.Name ?? "Unknown Artist",
                        Title = track.Title ?? "Unknown Title",
                        Lyrics = $"API Response Preview (first 200 chars):\n{content.Substring(0, Math.Min(content.Length, 200))}...",
                        AlbumCover = track.Album?.CoverMedium,
                        PreviewUrl = track.Preview,
                        ArtistImage = track.Artist?.PictureMedium,
                        MatchScore = 100 // Direct search
                    };
                    
                    // Try to get lyrics if possible
                    try
                    {
                        if (track.Artist?.Name != null && track.Title != null)
                        {
                            var lyrics = await GetLyrics(track.Artist.Name, track.Title);
                            if (!string.IsNullOrWhiteSpace(lyrics) && lyrics != "Lyrics not available")
                            {
                                songMatch.Lyrics = lyrics;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error getting lyrics: {ex.Message}");
                    }
                    
                    matchedSongs.Add(songMatch);
                }

                return matchedSongs;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching songs: {ex.Message}");
                return new List<SongMatch>
                {
                    new SongMatch
                    {
                        Title = "Error Occurred",
                        Artist = "Exception Details",
                        Lyrics = $"Error Type: {ex.GetType().Name}\nMessage: {ex.Message}\nStack Trace: {ex.StackTrace}"
                    }
                };
            }
        }

        public async Task<string> GetLyrics(string artist, string title)
        {
            if (string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(title))
            {
                return "Lyrics not available";
            }
            
            try
            {
                var response = await _httpClient.GetAsync($"{_lyricsApiBaseUrl}/{Uri.EscapeDataString(artist)}/{Uri.EscapeDataString(title)}");
                
                if (!response.IsSuccessStatusCode)
                {
                    return "Lyrics not available";
                }

                var content = await response.Content.ReadAsStringAsync();
                var lyricsResponse = JsonSerializer.Deserialize<LyricsResponse>(content);
                
                return string.IsNullOrWhiteSpace(lyricsResponse?.Lyrics) 
                    ? "Lyrics not available" 
                    : lyricsResponse.Lyrics;
            }
            catch (HttpRequestException)
            {
                return "Lyrics not available";
            }
            catch (JsonException)
            {
                return "Lyrics not available";
            }
        }
    }
} 