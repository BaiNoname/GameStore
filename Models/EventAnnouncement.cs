using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameStore.Models
{
    // Lớp đại diện cho thông báo sự kiện trong hệ thống
    [Table("eventannouncement")]
    public class EventAnnouncement
    {
        [Key]
        [Column("announcementid")]
        public int AnnouncementId { get; set; }

        [Required]
        [Column("eventid")]
        public int EventId { get; set; }

        [Required]
        [StringLength(255)]
        [Column("title")]
        public string Title { get; set; } = null!;

        [Required]
        [Column("content")]
        public string Content { get; set; } = null!;

        [Column("createdat")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("createdby")]
        public int CreatedBy { get; set; }

        [ForeignKey(nameof(EventId))]
        public virtual Event? Event { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public virtual NguoiDung? NguoiDung { get; set; }
    }
}