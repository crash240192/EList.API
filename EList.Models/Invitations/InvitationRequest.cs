using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EList.Models.Invitations
{
    /// <summary>
    /// Приглашение на ивент
    /// </summary>
    public class InvitationRequest
    {
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
    }
}
