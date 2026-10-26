using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("order_fiscal_receipts")]
    public class OrderFiscalReceiptDto
    {
        [Column("id"), PrimaryKey]
        public Guid Id { get; set; }

        [Column("order_id")]
        public Guid OrderId { get; set; }

        [Column("refund_id")]
        public Guid? RefundId { get; set; }

        [Column("kind", DataType = DataType.Enum)]
        public FiscalReceiptKind Kind { get; set; }

        [Column("provider")]
        public string Provider { get; set; } = "cloudKassir";

        [Column("invoice_id")]
        public string InvoiceId { get; set; } = string.Empty;

        [Column("provider_receipt_id")]
        public string? ProviderReceiptId { get; set; }

        [Column("status", DataType = DataType.Enum)]
        public FiscalReceiptStatus Status { get; set; }

        [Column("receipt_url")]
        public string? ReceiptUrl { get; set; }

        [Column("qr_code_url")]
        public string? QrCodeUrl { get; set; }

        [Column("amount")]
        public decimal Amount { get; set; }

        [Column("currency")]
        public string Currency { get; set; } = "RUB";

        [Column("error_message")]
        public string? ErrorMessage { get; set; }

        [Column("create_date")]
        public DateTimeOffset CreateDate { get; set; }

        [Column("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }

        [Column("completed_at")]
        public DateTimeOffset? CompletedAt { get; set; }
    }
}
