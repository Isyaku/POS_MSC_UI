using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace POS_MSC.Models
{
    public class MSC_AuditLogModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public string Staff { get; set; }
        public string Action { get; set; }
        public DateTime Time { get; set; }
    }
}
