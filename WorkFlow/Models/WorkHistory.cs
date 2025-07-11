namespace WorkFlow.Models
{
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
}
