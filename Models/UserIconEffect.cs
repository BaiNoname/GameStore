using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameStore.Models
{
    [Table("usericoneffect")]
    public class UserIconEffect
    {
        [Key]
        [Column("usericoneffectid")]
        public int UserIconEffectId { get; set; }

        [Required]
        [Column("manguoidung")]
        public int MaNguoiDung { get; set; }

        [Required]
        [Column("effectid")]
        public int EffectId { get; set; }

        [Column("eventid")]
        public int? EventId { get; set; }

        [Column("isequipped")]
        public bool IsEquipped { get; set; } = false;

        [Column("grantedat")]
        public DateTime GrantedAt { get; set; } = DateTime.Now;

        [Column("expiredat")]
        public DateTime? ExpiredAt { get; set; }

        [ForeignKey(nameof(MaNguoiDung))]
        public virtual NguoiDung? NguoiDung { get; set; }

        [ForeignKey(nameof(EffectId))]
        public virtual IconEffect? IconEffect { get; set; }

        [ForeignKey(nameof(EventId))]
        public virtual Event? Event { get; set; }
    }
}