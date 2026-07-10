namespace EList.Models.Media
{
    public class AccountAvatarItem
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public Guid PhotoId { get; set; }
        public DateTimeOffset AssignmentDate { get; set; }
    }
}
