using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameStore.Models
{
    [Table("event")]
    public class Event
    {
        [Key]
        [Column("eventid")]
        public int EventId { get; set; }

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

        [Column("banner")]
        public string? Banner { get; set; }

        [Column("relatedgameid")]
        public string? RelatedGameId { get; set; }

        [Required]
        [StringLength(50)]
        [Column("eventtype")]
        public string EventType { get; set; } = "Tournament";

        [Required]
        [StringLength(50)]
        [Column("accesstype")]
        public string AccessType { get; set; } = "Paid";

        [Column("price", TypeName = "decimal(18,2)")]
        public decimal Price { get; set; } = 0;

        [Column("maxparticipants")]
        public int? MaxParticipants { get; set; }

        [Column("currentparticipants")]
        public int CurrentParticipants { get; set; } = 0;

        [StringLength(500)]
        [Column("prizeinfo")]
        public string? PrizeInfo { get; set; }

        [Required]
        [StringLength(50)]
        [Column("status")]
        public string Status { get; set; } = "Upcoming";

        [Column("startat")]
        public DateTime StartAt { get; set; }

        [Column("endat")]
        public DateTime EndAt { get; set; }

        [Required]
        [Column("createdby")]
        public int CreatedBy { get; set; }

        [Column("createdat")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updatedat")]
        public DateTime? UpdatedAt { get; set; }

        [Column("prizetype")]
        [StringLength(20)]
        public string? PrizeType { get; set; }

        [Column("prizevalue")]
        [StringLength(255)]
        public string? PrizeValue { get; set; }

        [Column("prizecondition")]
        [StringLength(20)]
        public string? PrizeCondition { get; set; }

        [ForeignKey(nameof(RelatedGameId))]
        public virtual Game? Game { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public virtual NguoiDung? NguoiDung { get; set; }

        public virtual ICollection<EventParticipant> EventParticipants { get; set; } = new List<EventParticipant>();
        public virtual ICollection<EventMessage> EventMessages { get; set; } = new List<EventMessage>();
        public virtual ICollection<EventAnnouncement> EventAnnouncements { get; set; } = new List<EventAnnouncement>();
        public virtual ICollection<UserIconEffect> UserIconEffects { get; set; } = new List<UserIconEffect>();
    }
}