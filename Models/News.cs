using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameStore.Models
{
    [Table("news")]
    public class News
    {
        [Key]
        [Column("newsid")]
        public int NewsId { get; set; }

        [Required]
        [StringLength(255)]
        [Column("title")]
        public string Title { get; set; } = null!;

        [Required]
        [StringLength(255)]
        [Column("slug")]
        public string Slug { get; set; } = null!;

        [StringLength(500)]
        [Column("summary")]
        public string? Summary { get; set; }

        [Required]
        [Column("content")]
        public string Content { get; set; } = null!;

        [Column("thumbnail")]
        public string? Thumbnail { get; set; }

        [Required]
        [Column("authoruserid")]
        public int AuthorUserId { get; set; }

        [Column("relatedgameid")]
        public string? RelatedGameId { get; set; }

        [Required]
        [StringLength(50)]
        [Column("newstype")]
        public string NewsType { get; set; } = "General";

        [Required]
        [StringLength(50)]
        [Column("status")]
        public string Status { get; set; } = "Published";

        [Column("isfeatured")]
        public bool IsFeatured { get; set; } = false;

        [Column("viewcount")]
        public int ViewCount { get; set; } = 0;

        [Column("publishedat")]
        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

        [Column("expiredat")]
        public DateTime? ExpiredAt { get; set; }

        [Column("createdat")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updatedat")]
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(AuthorUserId))]
        public virtual NguoiDung? NguoiDung { get; set; }

        [ForeignKey(nameof(RelatedGameId))]
        public virtual Game? Game { get; set; }
    }
}