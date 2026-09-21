using EList.Models.Enums;

namespace EList.Models.Orders
{
    /// <summary>
    /// Отмена заявки на возврат (пока Refund.Pending и билеты RefundPending).
    /// </summary>
    public class CancelRefundRequest
    {
        /// <summary>Идентификатор возврата</summary>
        public Guid RefundId { get; set; }
    }
}
