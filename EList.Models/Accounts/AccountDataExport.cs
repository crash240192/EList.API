using EList.Models.ContactData;
using EList.Models.Events;
using EList.Models.Person;
using EList.Models.Subscriptions;

namespace EList.Models.Accounts
{
    /// <summary>
    /// Machine-readable dump данных аккаунта (GDPR Art. 20 / 152-ФЗ).
    /// </summary>
    public class AccountDataExport
    {
        public DateTimeOffset ExportedAtUtc { get; set; } = DateTimeOffset.UtcNow;
        public Account? Account { get; set; }
        public PersonInfo? Person { get; set; }
        public List<ContactDataItem> Contacts { get; set; } = new();
        public List<EventShort> OrganizedEvents { get; set; } = new();
        public List<EventShort> ParticipatingEvents { get; set; } = new();
        public List<Subscription> Subscriptions { get; set; } = new();
        public List<string> AcceptedAgreements { get; set; } = new();
    }
}
