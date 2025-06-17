using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LyricsFinder.Models
{
    public class SearchHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Arama Terimi")]
        public string SearchTerm { get; set; } = string.Empty;

        [Display(Name = "Arama Tarihi")]
        public DateTime SearchedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Sonuç Sayısı")]
        public int ResultCount { get; set; }

        [Display(Name = "Arama Süresi (ms)")]
        public long SearchDurationMs { get; set; }

        [Display(Name = "Kullanıcı")]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;
    }
} 