using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameStore.Models
{
    [Table("eventparticipant")]
    public class EventParticipant
    {
        [Key]
        [Column("participantid")]
        public int ParticipantId { get; set; }

        [Required]
        [Column("eventid")]
        public int EventId { get; set; }

        [Required]
        [Column("userid")]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        [Column("joinstatus")]
        public string JoinStatus { get; set; } = "Joined";

        [Column("paidamount", TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; } = 0;

        [Column("joinedat")]
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        [Column("ischeckedin")]
        public bool IsCheckedIn { get; set; } = false;

        [Column("checkedinat")]
        public DateTime? CheckedInAt { get; set; }

        [ForeignKey(nameof(EventId))]
        public virtual Event? Event { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual NguoiDung? NguoiDung { get; set; }
    }
}