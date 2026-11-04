namespace InventoryPurchase.Models
{
    public class PurchaseDetail
    {
        public int DetailId { get; set; }
        public int PurchaseId { get; set; }

        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? LotNo { get; set; }
        public DateTime? ExpiryDate { get; set; }

        public decimal Quantity { get; set; }
        public decimal Rate { get; set; }
        public decimal DiscountPercent { get; set; }

        public decimal GrossAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxableAmount { get; set; }

        // copied from Tax master when the purchase is saved
        public int? TaxId { get; set; }
        public string? TaxName { get; set; }
        public string? TaxType { get; set; }
        public decimal TaxRate { get; set; }
        public decimal AdditionalRate { get; set; }

        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }
    }
}
