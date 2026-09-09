namespace EList.Models.Orders
{
    /// <summary>
    /// Запрос на возврат билетов заказа. Без ticketIds — все ещё issued билеты заказа.
    /// </summary>
    public class CreateRefundRequest
    {
        public Guid OrderId { get; set; }

        /// <summary>Конкретные билеты (должны быть issued и принадлежать заказу).</summary>
        public List<Guid>? TicketIds { get; set; }

        public string? Reason { get; set; }
    }
}
