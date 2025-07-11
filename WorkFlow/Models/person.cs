using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkFlow.Models
{
    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public PersonState CurrentState { get; set; }

        // Navigation collection for related WorkHistories
        public List<WorkHistory> WorkHistories { get; set; } = new List<WorkHistory>();
    }
}
