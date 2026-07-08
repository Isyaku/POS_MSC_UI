using POS_MSC.Models;
using System.ComponentModel.DataAnnotations;

namespace POS_MSC.ViewModels
{
    public class UploadViewModel
    {
        [Required(ErrorMessage = "Please select a file to upload.")]
        public IFormFile ExcelFile { get; set; }      
        public List<MSC_RequestModel> FilteredRecords { get; set; } = new();        
        public List<UploadModel> UploadModel { get; set; } = new(); 
    }

};
