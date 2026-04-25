using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameStore.Models
{
    [Table("iconeffect")]
    public class IconEffect
    {
        [Key]
        [Column("effectid")]
        public int EffectId { get; set; }

        [Required]
        [StringLength(100)]
        [Column("effectname")]
        public string EffectName { get; set; } = null!;

        [Required]
        [StringLength(100)]
        [Column("effectcode")]
        public string EffectCode { get; set; } = null!;

        [Required]
        [StringLength(50)]
        [Column("effecttype")]
        public string EffectType { get; set; } = "Frame";

        [Required]
        [StringLength(100)]
        [Column("cssclass")]
        public string CssClass { get; set; } = null!;

        [Required]
        [StringLength(30)]
        [Column("rarity")]
        public string Rarity { get; set; } = "Common";

        [Column("isactive")]
        public bool IsActive { get; set; } = true;

        [Column("createdat")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<UserIconEffect> UserIconEffects { get; set; } = new List<UserIconEffect>();
    }
}