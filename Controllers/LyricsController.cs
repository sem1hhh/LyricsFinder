using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using LyricsFinder.Models;
using LyricsFinder.Services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace LyricsFinder.Controllers
{
    public class LyricsController : Controller
    {
        private readonly LyricsService _lyricsService;
        private readonly ILogger<LyricsController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public LyricsController(
            LyricsService lyricsService, 
            ILogger<LyricsController> logger,
            UserManager<ApplicationUser> userManager)
        {
            _lyricsService = lyricsService;
            _logger = logger;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            // Show the initial search form
            return View(new LyricsSearchViewModel());
        }

        [HttpPost]
        [EnableRateLimiting("fixed")] // API rate limiting uygula
        public async Task<IActionResult> Search(LyricsSearchViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.InputLyrics))
            {
                ModelState.AddModelError("InputLyrics", "Lütfen bir arama terimi girin");
                return View("Index", model);
            }

            try
            {
                _logger.LogInformation($"Search request received for: {model.InputLyrics}");
                
                // Get current user ID if logged in
                string? userId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    var user = await _userManager.GetUserAsync(User);
                    userId = user?.Id;
                }
                
                // Call the service to search for songs using the Deezer API
                var matchedSongs = await _lyricsService.SearchSongsByLyrics(model.InputLyrics, 10, userId);
                model.MatchedSongs = matchedSongs;

                _logger.LogInformation($"Search completed for '{model.InputLyrics}'. Found {matchedSongs.Count} results.");
                
                return View("Results", model);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception in Search action for '{model.InputLyrics}': {ex.Message}");
                ModelState.AddModelError("", $"Bir hata oluştu: {ex.Message}");
                return View("Index", model);
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddToFavorites(string artist, string title, string? albumCover, string? previewUrl, string? lyrics)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Kullanıcı bulunamadı" });
                }

                var result = await _lyricsService.AddToFavorites(artist, title, albumCover, previewUrl, lyrics, user.Id);
                
                if (result)
                {
                    return Json(new { success = true, message = "Şarkı favorilere eklendi" });
                }
                else
                {
                    return Json(new { success = false, message = "Bu şarkı zaten favorilerinizde" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding to favorites: {ex.Message}");
                return Json(new { success = false, message = "Bir hata oluştu" });
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Favorites()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var favorites = await _lyricsService.GetUserFavorites(user.Id);
                return View(favorites);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting favorites: {ex.Message}");
                return View(new List<FavoriteSong>());
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> SearchHistory()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var history = await _lyricsService.GetUserSearchHistory(user.Id);
                return View(history);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting search history: {ex.Message}");
                return View(new List<SearchHistory>());
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
} 