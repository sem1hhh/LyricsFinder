using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace LyricsFinder.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Display(Name = "Ad")]
        public string? FirstName { get; set; }

        [Display(Name = "Soyad")]
        public string? LastName { get; set; }

        [Display(Name = "Doğum Tarihi")]
        public DateTime? BirthDate { get; set; }

        [Display(Name = "Profil Resmi")]
        public string? ProfilePictureUrl { get; set; }

        [Display(Name = "Kayıt Tarihi")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Son Giriş")]
        public DateTime? LastLoginAt { get; set; }

        // Navigation Properties
        public virtual ICollection<SearchHistory> SearchHistories { get; set; } = new List<SearchHistory>();
        public virtual ICollection<FavoriteSong> FavoriteSongs { get; set; } = new List<FavoriteSong>();
    }
} 