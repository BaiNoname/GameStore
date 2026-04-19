using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameStore.Models
{
    [Table("eventmessage")]
    public class EventMessage
    {
        [Key]
        [Column("messageid")]
        public int MessageId { get; set; }

        [Required]
        [Column("eventid")]
        public int EventId { get; set; }

        [Required]
        [Column("userid")]
        public int UserId { get; set; }

        [Required]
        [StringLength(1000)]
        [Column("content")]
        public string Content { get; set; } = null!;

        [Column("createdat")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("isdeleted")]
        public bool IsDeleted { get; set; } = false;

        [ForeignKey(nameof(EventId))]
        public virtual Event? Event { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual NguoiDung? NguoiDung { get; set; }
    }
}