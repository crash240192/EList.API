namespace EList.Models.Orders
{
    /// <summary>
    /// Проверка / check-in билета организатором.
    /// </summary>
    public class TicketCheckInRequest
    {
        /// <summary>Мероприятие, на вход которого сканируют билет.</summary>
        public Guid EventId { get; set; }

        /// <summary>Код / QR билета.</summary>
        public string Code { get; set; }
    }
}
