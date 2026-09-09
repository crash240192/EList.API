namespace EList.Models.Orders
{
    /// <summary>
    /// Передача (подарок) билета другому пользователю.
    /// Покупатель заказа не меняется — меняется только holder билета.
    /// </summary>
    public class TransferTicketRequest
    {
        /// <summary>Id билета (если не указан code).</summary>
        public Guid? TicketId { get; set; }

        /// <summary>Код билета (если не указан ticketId).</summary>
        public string? Code { get; set; }

        /// <summary>Новый владелец билета.</summary>
        public Guid NewHolderAccountId { get; set; }
    }
}
