using EList.Models.Enums;

namespace EList.Models.Orders
{
    public class OrderPayout
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public OrderPayoutKind Kind { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RUB";
        public PaymentProvider Provider { get; set; }
        public string? ProviderDealId { get; set; }
        public string? PartnerId { get; set; }
        public string? ProviderPaymentId { get; set; }
        public string? ProviderOrderId { get; set; }
        public bool FinalPayout { get; set; }
        public OrderPayoutStatus Status { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTimeOffset CreateDate { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
    }
}
