using System.ComponentModel.DataAnnotations;

namespace POS_MSC.Models
{
    public class UserSessionModel
    {
        [Key]
        public string UserID { get; set; }
        public string SessionToken { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}