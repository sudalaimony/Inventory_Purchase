namespace InventoryPurchase.Models
{
    public class Stock
    {
        public int StockId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string LotNo { get; set; } = "";
        public DateTime? ExpiryDate { get; set; }
        public decimal Quantity { get; set; }
        public decimal PurchaseRate { get; set; }
        public int PurchaseId { get; set; }
        public DateTime ReceivedOn { get; set; }
        public int FifoOrder { get; set; }
    }
}
