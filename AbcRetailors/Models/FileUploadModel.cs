using System.ComponentModel.DataAnnotations;

namespace AbcRetailors.Models
{
    public class FileUploadModel
    {

        [Required]
        [Display(Name = "Proof of Payments")]
        public IFormFile? ProofOfPayment { get; set; }

        [Display(Name ="Order ID")]
        public string? OrderId { get; set; }

        [Display(Name = "Customer Name")]
        public string? CustomerName { get; set; }
    }
}
