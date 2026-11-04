using System.ComponentModel.DataAnnotations;

namespace InventoryPurchase.Models
{
    public class Product
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Enter product code")]
        [StringLength(30)]
        [Display(Name = "Code")]
        public string ProductCode { get; set; } = "";

        [Required(ErrorMessage = "Enter product name")]
        [StringLength(150)]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = "";

        [Required(ErrorMessage = "Select category")]
        [Display(Name = "Category")]
        public int? CategoryId { get; set; }

        [Display(Name = "Tax")]
        public int? TaxId { get; set; }

        [Range(0, 9999999, ErrorMessage = "Invalid cost price")]
        [Display(Name = "Cost Price")]
        public decimal CostPrice { get; set; }

        [Range(0, 9999999, ErrorMessage = "Invalid selling price")]
        [Display(Name = "Selling Price")]
        public decimal SellingPrice { get; set; }

        [Display(Name = "Track Expiry")]
        public bool HasExpiry { get; set; } = true;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // filled from joins, only for display
        public string? CategoryName { get; set; }
        public string? TaxName { get; set; }
        public string? TaxType { get; set; }
        public decimal TaxRate { get; set; }
        public decimal AdditionalRate { get; set; }
    }
}
