using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TheBugTracker.Models.ViewModels
{
    public class DashboardViewModel
    {
        public Company Company { get; set; }
        public List<Project> Projects { get; set; }
        public List<Ticket> Tickets { get; set; }
        public List<BTUser> Members { get; set; }
    }
}
