using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS_MSC.Models
{
    public class UploadModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public string? StaffID { get; set; }
        public string? UploadDate { get; set; }
        public string? Status { get; set; }
        public string? Comment { get; set; }
        public string? TransientAccount { get; set; }
        public string? CreditAccount { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? Supervisor { get; set; }
        public string? TransactionId { get; set; }
        public int? ProgressPercentage { get; set; }
    }
}
