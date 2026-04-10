using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EList.Models.Authorization
{
    public class AuthorizationRequest
    {
        public string Login { get; set; }
        public string Password { get; set; }
    }
}
