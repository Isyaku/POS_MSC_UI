using CsvHelper.Configuration.Attributes;

namespace POS_MSC.Models
{
    public class MSC_Request_Download_Model
    {
        public string Status { get; set; }
        public string DebitAcct { get; set; }
        public string CreditAcct { get; set; }
        public string CompanyCode { get; set; }
        public string BranchCode { get; set; }
        public string Currency { get; set; }
        public string GLCode { get; set; }
        public string CIFNO { get; set; }
        public string FVAmount { get; set; }
        public string Serial { get; set; }

        [Name("Void")]
        public string Void1 { get; set; }

        [Name("Void")]
        public string Void2 { get; set; }

        [Name("Void")]
        public string Void3 { get; set; }

        public decimal CVAmount { get; set; }
        public string ValueDate { get; set; }

        [Name("Description")]
        public string Description1 { get; set; }

        public string TRXCode { get; set; }
        public string JVType { get; set; }
        public string TrateDate { get; set; }

        [Name("Description")]
        public string Description2 { get; set; }
        public string? TransCategory { get; set; }
        public string? TransactionId { get; set; }
        public string? SettlementDescription { get; set; }
    }
}
