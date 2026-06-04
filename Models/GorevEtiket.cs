using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GorevTakipSistemi.Models
{
    public class GorevEtiket
    {
        [Key]
        public int Id { get; set; }

        public int GorevId { get; set; }
        [ForeignKey("GorevId")]
        public virtual Gorev? Gorev { get; set; }

        public int EtiketId { get; set; }
        [ForeignKey("EtiketId")]
        public virtual Etiket? Etiket { get; set; }
    }
}
