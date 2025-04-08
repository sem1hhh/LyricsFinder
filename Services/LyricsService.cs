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
using System.Text;

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

        // Method to convert Turkish characters to English equivalents
        private string ConvertTurkishToEnglish(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var result = new StringBuilder(input.Length);
            
            foreach (char c in input)
            {
                // Convert Turkish characters to their English equivalents
                switch (c)
                {
                    case 'ç': result.Append('c'); break;
                    case 'Ç': result.Append('C'); break;
                    case 'ğ': result.Append('g'); break;
                    case 'Ğ': result.Append('G'); break;
                    case 'ı': result.Append('i'); break;
                    case 'İ': result.Append('I'); break;
                    case 'ö': result.Append('o'); break;
                    case 'Ö': result.Append('O'); break;
                    case 'ş': result.Append('s'); break;
                    case 'Ş': result.Append('S'); break;
                    case 'ü': result.Append('u'); break;
                    case 'Ü': result.Append('U'); break;
                    default: result.Append(c); break;
                }
            }
            
            return result.ToString();
        }

        public async Task<List<SongMatch>> SearchSongsByLyrics(string searchQuery, int maxResults = 10)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return new List<SongMatch>();
            }

            try
            {
                // Convert Turkish characters to English equivalents for better search results
                var englishQuery = ConvertTurkishToEnglish(searchQuery);
                Console.WriteLine($"Original query: {searchQuery}");
                Console.WriteLine($"Converted query: {englishQuery}");
                
                // Use the Deezer suggest API to search for tracks
                // Convert spaces to + for API compatibility
                var encodedQuery = HttpUtility.UrlEncode(englishQuery).Replace("+", "%20");
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
                            Title = "API Hatası", 
                            Artist = $"Durum: {response.StatusCode}",
                            Lyrics = $"Hata ayıklama bilgisi: URL={apiUrl}\nYanıt={content}" 
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
                            Title = "Sonuç Bulunamadı", 
                            Artist = "Hata Ayıklama Bilgisi",
                            Lyrics = $"Hata ayıklama bilgisi: URL={apiUrl}\nYanıt={content}" 
                        } 
                    };
                }

                var matchedSongs = new List<SongMatch>();

                // For each track in the response, create a SongMatch 
                foreach (var track in suggestResponse.Data.Take(maxResults))
                {
                    var songMatch = new SongMatch
                    {
                        Artist = track.Artist?.Name ?? "Bilinmeyen Sanatçı",
                        Title = track.Title ?? "Bilinmeyen Şarkı",
                        Lyrics = $"API Yanıtı Önizlemesi (ilk 200 karakter):\n{content.Substring(0, Math.Min(content.Length, 200))}...",
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
                            else
                            {
                                songMatch.Lyrics = "Şarkı sözleri mevcut değil";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error getting lyrics: {ex.Message}");
                        songMatch.Lyrics = "Şarkı sözleri alınırken hata oluştu";
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
                        Title = "Hata Oluştu",
                        Artist = "Hata Detayları",
                        Lyrics = $"Hata Türü: {ex.GetType().Name}\nMesaj: {ex.Message}\nYığın İzleme: {ex.StackTrace}"
                    }
                };
            }
        }

        public async Task<string> GetLyrics(string artist, string title)
        {
            if (string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(title))
            {
                return "Şarkı sözleri mevcut değil";
            }
            
            try
            {
                // Also convert artist and title to English for better lyrics search
                var englishArtist = ConvertTurkishToEnglish(artist);
                var englishTitle = ConvertTurkishToEnglish(title);
                
                var response = await _httpClient.GetAsync($"{_lyricsApiBaseUrl}/{Uri.EscapeDataString(englishArtist)}/{Uri.EscapeDataString(englishTitle)}");
                
                if (!response.IsSuccessStatusCode)
                {
                    return "Şarkı sözleri mevcut değil";
                }

                var content = await response.Content.ReadAsStringAsync();
                var lyricsResponse = JsonSerializer.Deserialize<LyricsResponse>(content);
                
                return string.IsNullOrWhiteSpace(lyricsResponse?.Lyrics) 
                    ? "Şarkı sözleri mevcut değil" 
                    : lyricsResponse.Lyrics;
            }
            catch (HttpRequestException)
            {
                return "Şarkı sözleri mevcut değil";
            }
            catch (JsonException)
            {
                return "Şarkı sözleri mevcut değil";
            }
        }
    }
} 