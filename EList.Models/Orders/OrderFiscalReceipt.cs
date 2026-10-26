using EList.Models.Enums;

namespace EList.Models.Orders
{
    public class OrderFiscalReceipt
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid? RefundId { get; set; }
        public FiscalReceiptKind Kind { get; set; }
        public string Provider { get; set; } = "cloudKassir";
        public string InvoiceId { get; set; } = string.Empty;
        public string? ProviderReceiptId { get; set; }
        public FiscalReceiptStatus Status { get; set; }
        public string? ReceiptUrl { get; set; }
        public string? QrCodeUrl { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RUB";
        public string? ErrorMessage { get; set; }
        public DateTimeOffset CreateDate { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
    }
}
