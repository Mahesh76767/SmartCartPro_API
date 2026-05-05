namespace SmartCartPro.Models.DTOs.AI
{
    public class InvoiceExtractDto
    {
        public string? VendorName { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? InvoiceDate { get; set; }
        public string? DueDate { get; set; }
        public decimal? SubTotal { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? TotalAmount { get; set; }
        public List<InvoiceLineItemDto> LineItems { get; set; } = new();
    }
    public class InvoiceLineItemDto
    {
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }
    }
}