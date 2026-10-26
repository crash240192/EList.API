namespace EList.Services.Interfaces
{
    /// <summary>
    /// Фискализация 54‑ФЗ (CloudKassir / stub). Не связана со сплитом денег (Deal/A2C).
    /// </summary>
    public interface IFiscalReceiptProvider
    {
        /// <summary>Имя провайдера для логов / журнала.</summary>
        string ProviderName { get; }

        /// <summary>Выключен → вызовы no-op.</summary>
        bool IsEnabled { get; }

        /// <summary>Чек прихода после успешной оплаты заказа.</summary>
        Task<FiscalReceiptResult> IssueIncomeForOrderAsync(FiscalIncomeReceiptRequest request);

        /// <summary>Чек возврата прихода.</summary>
        Task<FiscalReceiptResult> IssueIncomeReturnForRefundAsync(FiscalRefundReceiptRequest request);

        /// <summary>Статус чека в кассе (Queued / Processed / Error / …).</summary>
        Task<FiscalReceiptStatusResult> GetReceiptStatusAsync(string providerReceiptId);

        /// <summary>Проверка HMAC уведомления Receipt (тело POST).</summary>
        bool VerifyWebhookHmac(string rawBody, string? contentHmac, string? xContentHmac);
    }

    public class FiscalIncomeReceiptRequest
    {
        public Guid OrderId { get; set; }
        public Guid BuyerAccountId { get; set; }
        public Guid SellerOrganizationId { get; set; }
        public decimal AmountTotal { get; set; }
        public decimal AmountSeller { get; set; }
        public decimal AmountCommission { get; set; }
        public string Currency { get; set; } = "RUB";
        public string? Description { get; set; }
        public string? BuyerEmail { get; set; }
        public string? BuyerPhone { get; set; }
        public string? SupplierInn { get; set; }
        public string? SupplierName { get; set; }
        public string? SupplierPhone { get; set; }
        /// <summary>Идемпотентный InvoiceId; default order:{guid:N}.</summary>
        public string? InvoiceId { get; set; }
    }

    public class FiscalRefundReceiptRequest
    {
        public Guid OrderId { get; set; }
        public Guid RefundId { get; set; }
        public Guid BuyerAccountId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RUB";
        public string? Description { get; set; }
        public string? BuyerEmail { get; set; }
        public string? BuyerPhone { get; set; }
        public string? SupplierInn { get; set; }
        public string? SupplierName { get; set; }
        public string? SupplierPhone { get; set; }
        public string? InvoiceId { get; set; }
    }

    public class FiscalReceiptResult
    {
        public bool Success { get; set; }
        public bool Skipped { get; set; }
        public string? ProviderReceiptId { get; set; }
        public string? InvoiceId { get; set; }
        public string? ErrorMessage { get; set; }

        public static FiscalReceiptResult Skip(string reason) => new()
        {
            Success = true,
            Skipped = true,
            ErrorMessage = reason
        };

        public static FiscalReceiptResult Ok(string? receiptId, string? invoiceId = null) => new()
        {
            Success = true,
            ProviderReceiptId = receiptId,
            InvoiceId = invoiceId
        };

        public static FiscalReceiptResult Fail(string message) => new()
        {
            Success = false,
            ErrorMessage = message
        };
    }

    public class FiscalReceiptStatusResult
    {
        public bool Success { get; set; }
        public string? Model { get; set; }
        public string? Message { get; set; }

        public static FiscalReceiptStatusResult Ok(string? model) => new()
        {
            Success = true,
            Model = model
        };

        public static FiscalReceiptStatusResult Fail(string message) => new()
        {
            Success = false,
            Message = message
        };
    }
}
