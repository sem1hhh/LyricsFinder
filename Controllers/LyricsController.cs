using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LyricsFinder.Models;
using LyricsFinder.Services;

namespace LyricsFinder.Controllers
{
    public class LyricsController : Controller
    {
        private readonly LyricsService _lyricsService;

        public LyricsController(LyricsService lyricsService)
        {
            _lyricsService = lyricsService;
        }

        public IActionResult Index()
        {
            // Show the initial search form
            return View(new LyricsSearchViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Search(LyricsSearchViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.InputLyrics))
            {
                ModelState.AddModelError("InputLyrics", "Lütfen bir arama terimi girin");
                return View("Index", model);
            }

            try
            {
                // Log search term
                Console.WriteLine($"Searching for: {model.InputLyrics}");
                
                // Call the service to search for songs using the Deezer API
                var matchedSongs = await _lyricsService.SearchSongsByLyrics(model.InputLyrics);
                model.MatchedSongs = matchedSongs;

                Console.WriteLine($"Found {matchedSongs.Count} results");
                
                return View("Results", model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in Search action: {ex.Message}");
                ModelState.AddModelError("", $"Bir hata oluştu: {ex.Message}");
                return View("Index", model);
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
} 