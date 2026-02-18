using System.ComponentModel.DataAnnotations;

namespace IPOClient.Models.Entities
{
    /// <summary>
    /// Caches the IPO list scraped from each registrar website.
    /// One row per registrar. Updated on every successful live fetch.
    /// Used as fallback when live scrape fails.
    /// </summary>
    public class IPO_RegistrarCache
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Registrar name: Linkin, KFinTech, BigShare, Purva, SkyLine, Integrated, Maashitla, Cambridge
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string RegistrarName { get; set; } = string.Empty;

        /// <summary>
        /// JSON-serialized List of IPOAllotmentCompany from the last successful fetch
        /// </summary>
        public string? CachedIposJson { get; set; }

        /// <summary>
        /// Number of IPOs in the cached list (for quick status checks without deserializing)
        /// </summary>
        public int CachedIpoCount { get; set; }

        /// <summary>
        /// Timestamp of the last successful live fetch that populated this cache
        /// </summary>
        public DateTime LastFetchedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Error message from the most recent failed fetch attempt
        /// </summary>
        [MaxLength(1000)]
        public string? LastErrorMessage { get; set; }

        /// <summary>
        /// Timestamp of the last failed fetch attempt (null if last attempt succeeded)
        /// </summary>
        public DateTime? LastFailedAt { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }
    }
}
