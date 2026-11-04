using System.ComponentModel.DataAnnotations;

namespace InventoryPurchase.Models
{
    public class Category
    {
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Enter category name")]
        [StringLength(100)]
        [Display(Name = "Category")]
        public string CategoryName { get; set; } = "";
    }
}
