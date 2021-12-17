using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace TheBugTracker.Models
{
    public class ProjectPriority
    {
        public int Id { get; set; }

        [DisplayName("Priority Name")]
        public string Name { get; set; }
    }
}