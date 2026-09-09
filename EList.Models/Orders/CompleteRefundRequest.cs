namespace EList.Models.Orders
{
    /// <summary>
    /// Stub/debug: имитация успешного refund webhook.
    /// </summary>
    public class CompleteRefundRequest
    {
        public Guid? RefundId { get; set; }
        public Guid? OrderId { get; set; }
        public string? ProviderRefundId { get; set; }
    }
}
