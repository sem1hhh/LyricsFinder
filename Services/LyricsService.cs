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
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Threading;
using Microsoft.AspNetCore.Identity;
using LyricsFinder.Data;
using Microsoft.EntityFrameworkCore;

namespace LyricsFinder.Services
{
    public class LyricsService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<LyricsService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly string _lyricsApiBaseUrl = "https://api.lyrics.ovh/v1";
        private readonly string _deezerSuggestApiUrl = "https://api.deezer.com/search?q=";
        
        // Cache ayarları
        private readonly TimeSpan _searchCacheDuration = TimeSpan.FromMinutes(30);
        private readonly TimeSpan _lyricsCacheDuration = TimeSpan.FromHours(24);
        private readonly SemaphoreSlim _lyricsSemaphore = new SemaphoreSlim(5, 5); // Aynı anda maksimum 5 lyrics isteği

        public LyricsService(
            HttpClient httpClient, 
            IMemoryCache cache, 
            ILogger<LyricsService> logger,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
            _context = context;
            _userManager = userManager;
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

        public async Task<List<SongMatch>> SearchSongsByLyrics(string searchQuery, int maxResults = 10, string? userId = null)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return new List<SongMatch>();
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Cache key oluştur
                var cacheKey = $"search_{searchQuery.ToLower().Trim()}_{maxResults}";
                
                // Cache'den kontrol et
                if (_cache.TryGetValue(cacheKey, out List<SongMatch> cachedResults))
                {
                    _logger.LogInformation($"Cache hit for search: {searchQuery}");
                    
                    // Arama geçmişini kaydet
                    if (!string.IsNullOrEmpty(userId))
                    {
                        await SaveSearchHistory(searchQuery, cachedResults.Count, stopwatch.ElapsedMilliseconds, userId);
                    }
                    
                    return cachedResults;
                }

                // Convert Turkish characters to English equivalents for better search results
                var englishQuery = ConvertTurkishToEnglish(searchQuery);
                _logger.LogInformation($"Searching for: {searchQuery} (converted: {englishQuery})");
                
                // Use the Deezer suggest API to search for tracks
                var encodedQuery = HttpUtility.UrlEncode(englishQuery).Replace("+", "%20");
                var apiUrl = $"{_deezerSuggestApiUrl}{encodedQuery}";
                
                _logger.LogInformation($"API URL: {apiUrl}");
                
                var response = await _httpClient.GetAsync(apiUrl);
                
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"API Response Status: {response.StatusCode}");
                _logger.LogInformation($"API Response Content Length: {content.Length}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"API Error: {response.StatusCode} for query: {searchQuery}");
                    _logger.LogWarning($"API Error Content: {content}");
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

                // Log first 500 characters of response for debugging
                _logger.LogInformation($"API Response Preview: {content.Substring(0, Math.Min(500, content.Length))}");

                var suggestResponse = JsonSerializer.Deserialize<DeezerSuggestResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (suggestResponse?.Data == null || !suggestResponse.Data.Any())
                {
                    _logger.LogInformation($"No results found for: {searchQuery}");
                    _logger.LogInformation($"SuggestResponse: {JsonSerializer.Serialize(suggestResponse)}");
                    return new List<SongMatch> 
                    { 
                        new SongMatch 
                        { 
                            Title = "Sonuç Bulunamadı", 
                            Artist = "Arama Sonucu",
                            Lyrics = $"'{searchQuery}' için sonuç bulunamadı.\nAPI URL: {apiUrl}\nResponse: {content.Substring(0, Math.Min(200, content.Length))}" 
                        } 
                    };
                }

                _logger.LogInformation($"Found {suggestResponse.Data.Count} tracks from API");

                var tracks = suggestResponse.Data.Take(maxResults).ToList();
                var matchedSongs = new List<SongMatch>();

                for (int i = 0; i < tracks.Count; i++)
                {
                    var track = tracks[i];
                    var songMatch = new SongMatch
                    {
                        Artist = track.Artist?.Name ?? "Bilinmeyen Sanatçı",
                        Title = track.Title ?? "Bilinmeyen Şarkı",
                        AlbumCover = track.Album?.CoverMedium,
                        PreviewUrl = track.Preview,
                        ArtistImage = track.Artist?.PictureMedium,
                        MatchScore = 100
                    };

                    if (i == 0 && track.Artist?.Name != null && track.Title != null)
                    {
                        // Sadece ilk şarkı için lyrics çek
                        try
                        {
                            await _lyricsSemaphore.WaitAsync();
                            try
                            {
                                var lyrics = await GetLyrics(track.Artist.Name, track.Title);
                                songMatch.Lyrics = string.IsNullOrWhiteSpace(lyrics) || lyrics == "Lyrics not available"
                                    ? "Şarkı sözleri mevcut değil"
                                    : lyrics;
                            }
                            finally
                            {
                                _lyricsSemaphore.Release();
                            }
                        }
                        catch
                        {
                            songMatch.Lyrics = "Şarkı sözleri alınırken hata oluştu";
                        }
                    }
                    else
                    {
                        songMatch.Lyrics = "Şarkı sözleri için tıklayın";
                    }

                    matchedSongs.Add(songMatch);
                }

                // Sonuçları cache'e kaydet
                _cache.Set(cacheKey, matchedSongs, _searchCacheDuration);
                
                // Arama geçmişini kaydet
                if (!string.IsNullOrEmpty(userId))
                {
                    await SaveSearchHistory(searchQuery, matchedSongs.Count, stopwatch.ElapsedMilliseconds, userId);
                }
                
                _logger.LogInformation($"Found {matchedSongs.Count} results for: {searchQuery}");
                return matchedSongs;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching songs for '{searchQuery}': {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                return new List<SongMatch>
                {
                    new SongMatch
                    {
                        Title = "Hata Oluştu",
                        Artist = "Sistem Hatası",
                        Lyrics = $"Arama sırasında bir hata oluştu. Lütfen tekrar deneyin.\nHata: {ex.Message}"
                    }
                };
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        private async Task SaveSearchHistory(string searchTerm, int resultCount, long durationMs, string userId)
        {
            try
            {
                var searchHistory = new SearchHistory
                {
                    SearchTerm = searchTerm,
                    ResultCount = resultCount,
                    SearchDurationMs = durationMs,
                    UserId = userId,
                    SearchedAt = DateTime.UtcNow
                };

                _context.SearchHistories.Add(searchHistory);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Search history saved for user {userId}: {searchTerm}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to save search history: {ex.Message}");
            }
        }

        public async Task<bool> AddToFavorites(string artist, string title, string? albumCover, string? previewUrl, string? lyrics, string userId)
        {
            try
            {
                // Check if already exists
                var existing = await _context.FavoriteSongs
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.Artist == artist && f.Title == title);

                if (existing != null)
                {
                    return false; // Already exists
                }

                var favoriteSong = new FavoriteSong
                {
                    Artist = artist,
                    Title = title,
                    AlbumCover = albumCover,
                    PreviewUrl = previewUrl,
                    Lyrics = lyrics,
                    UserId = userId,
                    AddedAt = DateTime.UtcNow
                };

                _context.FavoriteSongs.Add(favoriteSong);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Song added to favorites: {artist} - {title} for user {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding to favorites: {ex.Message}");
                return false;
            }
        }

        public async Task<List<FavoriteSong>> GetUserFavorites(string userId)
        {
            try
            {
                return await _context.FavoriteSongs
                    .Where(f => f.UserId == userId)
                    .OrderByDescending(f => f.AddedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user favorites: {ex.Message}");
                return new List<FavoriteSong>();
            }
        }

        public async Task<List<SearchHistory>> GetUserSearchHistory(string userId, int limit = 20)
        {
            try
            {
                return await _context.SearchHistories
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.SearchedAt)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user search history: {ex.Message}");
                return new List<SearchHistory>();
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
                // Cache key oluştur
                var cacheKey = $"lyrics_{artist.ToLower().Trim()}_{title.ToLower().Trim()}";
                
                // Cache'den kontrol et
                if (_cache.TryGetValue(cacheKey, out string cachedLyrics))
                {
                    _logger.LogInformation($"Cache hit for lyrics: {artist} - {title}");
                    return cachedLyrics;
                }

                // Also convert artist and title to English for better lyrics search
                var englishArtist = ConvertTurkishToEnglish(artist);
                var englishTitle = ConvertTurkishToEnglish(title);
                
                var response = await _httpClient.GetAsync($"{_lyricsApiBaseUrl}/{Uri.EscapeDataString(englishArtist)}/{Uri.EscapeDataString(englishTitle)}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Lyrics API error for {artist} - {title}: {response.StatusCode}");
                    return "Şarkı sözleri mevcut değil";
                }

                var content = await response.Content.ReadAsStringAsync();
                var lyricsResponse = JsonSerializer.Deserialize<LyricsResponse>(content);
                
                var lyrics = string.IsNullOrWhiteSpace(lyricsResponse?.Lyrics) 
                    ? "Şarkı sözleri mevcut değil" 
                    : lyricsResponse.Lyrics ?? "Şarkı sözleri mevcut değil";

                // Sonucu cache'e kaydet
                _cache.Set(cacheKey, lyrics, _lyricsCacheDuration);
                
                return lyrics;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"HTTP request error for lyrics {artist} - {title}: {ex.Message}");
                return "Şarkı sözleri mevcut değil";
            }
            catch (JsonException ex)
            {
                _logger.LogWarning($"JSON parsing error for lyrics {artist} - {title}: {ex.Message}");
                return "Şarkı sözleri mevcut değil";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error getting lyrics for {artist} - {title}: {ex.Message}");
                return "Şarkı sözleri mevcut değil";
            }
        }
    }
} 