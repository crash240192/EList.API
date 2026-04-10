using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EList.Models.EventOrganizators
{
    public class EventOrganizatorRequest
    {
        public Guid EventId { get; set; }
        public Guid? AccountId { get; set; }
        public Guid? OrganizationId { get; set; }
    }
}
