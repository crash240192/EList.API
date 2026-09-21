namespace EList.Models.Enums
{
    /// <summary>
    /// Статус билета
    /// </summary>
    public enum TicketStatus
    {
        Issued = 0,
        Used = 1,
        Refunded = 2,
        Void = 3,
        /// <summary>Создана заявка на возврат; ждём подтверждения провайдера.</summary>
        RefundPending = 4
    }
}
