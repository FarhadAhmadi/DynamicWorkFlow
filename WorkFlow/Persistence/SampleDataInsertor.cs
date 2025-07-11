using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkFlow.Models;

namespace WorkFlow.Persistence
{
    public class SampleDataInsertor
    {
        private readonly DatabaseContext dbContext;
        public SampleDataInsertor(DatabaseContext databaseContext)
        {
            dbContext = databaseContext;
        }

        public async Task InsertAsync()
        {
            #region Seed Sample Data

            Console.WriteLine("Seeding sample data...");

            var employers = new List<Employer>
            {
                new() { Name = "آذرخش", HasConfirmation = true },
                new() { Name = "شرکت فناوران پویا", HasConfirmation = true },
                new() { Name = "درمان‌گستر", HasConfirmation = false },
                new() { Name = "راهکارهای آموزشی پویش", HasConfirmation = true },
                new() { Name = "ساختمون‌یار", HasConfirmation = true },
            };

            await dbContext.Employers.AddRangeAsync(employers);
            await dbContext.SaveChangesAsync();

            var iranianCities = new[] { "تهران", "مشهد", "اصفهان", "تبریز", "شیراز", "اهواز", "رشت", "کرمان", "یزد", "ارومیه" };
            var workTypes = new[] { "برنامه‌نویس", "پرستار", "کارشناس شبکه", "توسعه‌دهنده وب", "پزشک", "مهندس عمران" };
            var random = new Random();

            var people = new List<Person>
            {
                new()
                {
                    Name = "فرهاد احمدی",
                    Age = 28,
                    CurrentState = PersonState.A,
                    WorkHistories = new List<WorkHistory>
                    {
                        new()
                        {
                            Location = "تهران",
                            StartDate = new DateTime(2015, 1, 1),
                            EndDate = new DateTime(2022, 12, 31),
                            Employer = employers.First(),
                            WorkType = "برنامه‌نویس"
                        }
                    }
                }
            };

            string[] names = { "علی رضایی", "مریم موسوی", "حسن کریمی", "نگار اسدی", "رضا قاسمی", "زهرا احمدی", "سینا تقوی", "فاطمه محمدی", "مهدی سلطانی", "لیلا شریفی" };

            for (int i = 0; i < names.Length; i++)
            {
                var person = new Person
                {
                    Name = names[i],
                    Age = 22 + i,
                    CurrentState = PersonState.A,
                    WorkHistories = new List<WorkHistory>
                    {
                        new()
                        {
                            Location = iranianCities[random.Next(iranianCities.Length)],
                            StartDate = DateTime.Now.AddYears(-random.Next(2, 8)),
                            EndDate = DateTime.Now.AddYears(-random.Next(0, 2)),
                            Employer = employers[random.Next(employers.Count)],
                            WorkType = workTypes[random.Next(workTypes.Length)]
                        }
                    }
                };
                people.Add(person);
            }

            await dbContext.People.AddRangeAsync(people);
            await dbContext.SaveChangesAsync();

            var announcements = new List<Announcement>
            {
                new() { RequiredAge = 25, WorkType = "مهندس عمران" },
                new() { RequiredAge = 30, WorkType = "پرستار" },
                new() { RequiredAge = 20, WorkType = "برنامه‌نویس" },
            };

            await dbContext.Announcements.AddRangeAsync(announcements);
            await dbContext.SaveChangesAsync();

            Console.WriteLine("Sample Iranian data seeded successfully.");

            #endregion
        }
    }
}
