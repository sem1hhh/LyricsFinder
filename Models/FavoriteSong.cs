using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LyricsFinder.Models
{
    public class FavoriteSong
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Sanatçı")]
        public string Artist { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Şarkı Adı")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Albüm Kapağı")]
        public string? AlbumCover { get; set; }

        [Display(Name = "Önizleme URL")]
        public string? PreviewUrl { get; set; }

        [Display(Name = "Şarkı Sözleri")]
        public string? Lyrics { get; set; }

        [Display(Name = "Eklenme Tarihi")]
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Notlar")]
        public string? Notes { get; set; }

        [Display(Name = "Kullanıcı")]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;
    }
} 