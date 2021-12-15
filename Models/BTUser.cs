using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TheBugTracker.Models
{
    public class BTUser : IdentityUser 
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
