using System.ComponentModel.DataAnnotations;

namespace InventoryPurchase.Models
{
    public class Tax
    {
        public int TaxId { get; set; }

        [Required(ErrorMessage = "Enter tax name")]
        [StringLength(100)]
        [Display(Name = "Tax Name")]
        public string TaxName { get; set; } = "";

        [Required]
        [Display(Name = "Tax Type")]
        public string TaxType { get; set; } = "Single";

        [Range(0, 100, ErrorMessage = "Rate must be between 0 and 100")]
        [Display(Name = "Tax Rate %")]
        public decimal TaxRate { get; set; }

        [Range(0, 100, ErrorMessage = "Rate must be between 0 and 100")]
        [Display(Name = "Additional Rate %")]
        public decimal AdditionalRate { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}
