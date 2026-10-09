namespace EList.Models.Events
{
    /// <summary>
    /// Элемент списка staff при PUT ticket-staff
    /// </summary>
    public class EventTicketStaffItemRequest
    {
        public Guid AccountId { get; set; }
        public bool CanCheckIn { get; set; } = true;
        public bool CanViewStats { get; set; } = true;
    }
}
