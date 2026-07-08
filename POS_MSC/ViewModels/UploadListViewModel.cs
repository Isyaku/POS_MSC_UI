using POS_MSC.Models;

namespace POS_MSC.ViewModels
{
    public class UploadListViewModel
    {
        public int ID { get; set; }
        public string UploadDate { get; set; }
        public double Amount { get; set; }
        public List<MSC_RequestModel> Records { get; set; } = new();
    }
}
