using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace AbcRetailors.Models
{
    public class Product: ITableEntity
    {
        
    
        public string PartitionKey { get; set; } = "Product";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [Display(Name = "Product ID")]
        public string ProductId => RowKey;

        [Required]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage ="Price is required")]
        [Display(Name = "Price")]
        public string PriceString { get; set; } = string.Empty;

        
        [Display(Name = "Price")]
        public double Price
        {
            get => double.TryParse(PriceString, out var price) ? price : 0;
            set => PriceString = value.ToString("F2");
        }

        [Required]
        [Display(Name = "Stock Avaliable")]
        public int StockAvaliable { get; set; }

        [Display(Name = "Image Url")]
        public string ImageUrl { get; set; } = string.Empty;
    }
}
