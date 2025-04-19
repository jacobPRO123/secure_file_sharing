using System.ComponentModel.DataAnnotations;

namespace SecureFileSharingApp.Models
{
    public class FileUploadRequest
    {
        [Required]
        public IFormFile File { get; set; }
    }
}
