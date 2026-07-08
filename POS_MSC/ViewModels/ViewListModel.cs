using POS_MSC.Models;

namespace POS_MSC.ViewModels
{
    public class ViewListModel
    {
        public List<MSC_RequestModel> Settlements { get; set; } = new();
        public int Count { get; set; }
        public decimal Sum { get; set; }
    }
}
