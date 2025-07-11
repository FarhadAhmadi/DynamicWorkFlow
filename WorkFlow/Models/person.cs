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

    public class WorkHistory
    {
        public int Id { get; set; }
        public string Location { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Add PersonId as foreign key:
        public int PersonId { get; set; }

        // Navigation property to Person:
        public Person Person { get; set; }

        public Employer Employer { get; set; }
        public string WorkType { get; set; }
    }

    public class Employer
    {
        public int Id { get; set; }
        public bool HasConfirmation { get; set; }
        public string Name { get; set; }
    }

    public class Announcement
    {
        public int Id { get; set; }
        public int RequiredAge { get; set; }
        public string WorkType { get; set; }
    }



    public enum PersonState
    {
        A,
        B,
        Final
    }


    public class WorkflowResult
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public string PersonName { get; set; }
        public bool Passed { get; set; }
        public string Reason { get; set; }
    }
}
