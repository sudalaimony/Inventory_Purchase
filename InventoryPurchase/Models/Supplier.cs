using System.ComponentModel.DataAnnotations;

namespace InventoryPurchase.Models
{
    public class Supplier
    {
        public int SupplierId { get; set; }

        [Required(ErrorMessage = "Enter supplier name")]
        [StringLength(150)]
        [Display(Name = "Supplier Name")]
        public string SupplierName { get; set; } = "";

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(250)]
        public string? Address { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}
