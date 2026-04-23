using EList.Models.Accounts;
using EList.Models.Events;
using EList.Models.Person;

namespace EList.Models.Invitations
{
    /// <summary>
    /// Приглашение на ивент
    /// </summary>
    public class Invitation
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Идентификатор приглашающего аккаунта
        /// </summary>
        public Guid InviterAccountId { get; set; }

        /// <summary>
        /// Идентификатор приглашаемого аккаунта
        /// </summary>
        public Guid InvitedAccountId { get; set; }

        /// <summary>
        /// Идентификатор приглашающей организации
        /// </summary>
        public Guid? InviterOrganizationId { get; set; }

        /// <summary>
        /// Идентификатор мероприятия
        /// </summary>
        public Guid EventId { get; set; }

        /// <summary>
        /// Дата создания записи
        /// </summary>
        public DateTimeOffset CreationDate { get; set; }

        public Inviter Inviter { get; set; }

        public Event Event { get; set; }
    }

    public class Inviter
    {
        public Account Account { get; set; }
        public PersonInfo PersonInfo { get; set; }
    }
}
