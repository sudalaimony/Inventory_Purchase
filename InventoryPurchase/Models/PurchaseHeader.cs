using System.ComponentModel.DataAnnotations;

namespace InventoryPurchase.Models
{
    public class PurchaseHeader
    {
        public int PurchaseId { get; set; }

        [Required(ErrorMessage = "Select supplier")]
        [Display(Name = "Supplier")]
        public int? SupplierId { get; set; }

        [Required(ErrorMessage = "Enter invoice number")]
        [StringLength(50)]
        [Display(Name = "Invoice No")]
        public string InvoiceNo { get; set; } = "";

        [Required(ErrorMessage = "Enter invoice date")]
        [DataType(DataType.Date)]
        [Display(Name = "Invoice Date")]
        public DateTime? InvoiceDate { get; set; }

        public decimal GrossAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }

        public string Status { get; set; } = "Draft";
        public DateTime CreatedOn { get; set; }
        public DateTime? PostedOn { get; set; }

        public string? SupplierName { get; set; }

        public List<PurchaseDetail> Details { get; set; } = new List<PurchaseDetail>();

        public bool IsPosted
        {
            get { return Status == "Posted"; }
        }
    }
}
