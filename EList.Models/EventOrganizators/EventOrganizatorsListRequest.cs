using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EList.Models.EventOrganizators
{
    public class EventOrganizatorsListRequest
    {
        public List<Guid> AccountIds { get; set; }
        public List<Guid> OrganizationIds { get; set; }
        public Guid EventId { get; set; }
    }
}
