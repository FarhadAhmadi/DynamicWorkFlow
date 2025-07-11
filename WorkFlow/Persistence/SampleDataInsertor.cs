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

            Logger.Log("Seeding sample data...");

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
                },
                new()
                {
                    Name = "سارا محمدی",
                    Age = 32,
                    CurrentState = PersonState.A,
                    WorkHistories = new List<WorkHistory>
                    {
                        new()
                        {
                            Location = "تهران", // ✅ satisfies Rule A
                            StartDate = new DateTime(2016, 1, 1),
                            EndDate = new DateTime(2023, 1, 1), // 7 years
                            Employer = employers.First(e => e.HasConfirmation), // ✅ confirmed employer
                            WorkType = "برنامه‌نویس" // ✅ matches one of the announcements
                        }
                    }
                },
                new()
                {
                    Name = "علی جوادی",
                    Age = 22, // ❌ Announcement requires 25+ or 30+
                    CurrentState = PersonState.A,
                    WorkHistories = new List<WorkHistory>
                    {
                        new()
                        {
                            Location = "تهران", // ✅
                            StartDate = new DateTime(2020, 1, 1),
                            EndDate = new DateTime(2023, 1, 1), // ✅ > 1 year
                            Employer = employers.First(e => e.HasConfirmation),
                            WorkType = "برنامه‌نویس" // ✅ Matches announcement, but too young
                        }
                    }
                },
                new()
                {
                    Name = "مریم نوروزی",
                    Age = 35, // ✅ Sufficient age
                    CurrentState = PersonState.A,
                    WorkHistories = new List<WorkHistory>
                    {
                        new()
                        {
                            Location = "تهران", // ✅
                            StartDate = new DateTime(2010, 1, 1),
                            EndDate = new DateTime(2020, 1, 1), // ✅ 10 years
                            Employer = employers.First(e => e.HasConfirmation),
                            WorkType = "مدیر منابع انسانی" // ❌ No such WorkType in announcements
                        }
                    }
                },
                new()
                {
                    Name = "حسن رضایی",
                    Age = 40, // ✅
                    CurrentState = PersonState.A,
                    WorkHistories = new List<WorkHistory>
                    {
                        new()
                        {
                            Location = "تهران", // ✅
                            StartDate = new DateTime(2022, 1, 1),
                            EndDate = new DateTime(2023, 12, 31), // ❌ only ~2 years
                            Employer = employers.First(e => e.HasConfirmation),
                            WorkType = "مهندس عمران" // ✅ Exists in announcement
                        }
                    }
                },
                new()
                {
                    Name = "نرگس قنبری",
                    Age = 30,
                    CurrentState = PersonState.A,
                    WorkHistories = new List<WorkHistory>
                    {
                        new()
                        {
                            Location = "شیراز", // ❌ Not تهران
                            StartDate = new DateTime(2016, 1, 1),
                            EndDate = new DateTime(2022, 1, 1),
                            Employer = employers.First(e => e.HasConfirmation),
                            WorkType = "تحلیل‌گر مالی" // ❌ Not listed in any announcement
                        }
                    }
                },
                new()
                {
                    Name = "مهدی کرمی",
                    Age = 29,
                    CurrentState = PersonState.A,
                    WorkHistories = new List<WorkHistory>
                    {
                        new()
                        {
                            Location = "تهران", // ✅
                            StartDate = new DateTime(2020, 1, 1),
                            EndDate = new DateTime(2022, 1, 1), // ❌ 2 years only
                            Employer = employers.First(e => !e.HasConfirmation), // ❌ Not confirmed
                            WorkType = "پرستار" // ✅ Exists in announcement
                        }
                    }
                },
                new()
                {
                    Name = "رضا زمانی",
                    Age = 31,
                    CurrentState = PersonState.A,
                    WorkHistories = new List<WorkHistory>
                    {
                        new()
                        {
                            Location = "مشهد", // ❌ Not تهران
                            StartDate = new DateTime(2015, 1, 1),
                            EndDate = new DateTime(2021, 1, 1),
                            Employer = employers.First(e => e.HasConfirmation),
                            WorkType = "تحلیل‌گر داده" // ❌ Not listed in announcements
                        }
                    }
                },
                new()
                {
                    Name = "فاطمه عباسی",
                    Age = 19, // ❌ Too young for any RequiredAge
                    CurrentState = PersonState.A,
                    WorkHistories = new List<WorkHistory>
                    {
                        new()
                        {
                            Location = "تهران", // ✅
                            StartDate = new DateTime(2023, 1, 1),
                            EndDate = new DateTime(2023, 6, 1), // ❌ < 1 year
                            Employer = employers.First(e => e.HasConfirmation),
                            WorkType = "برنامه‌نویس" // ✅ Exists in announcement
                        }
                    }
                }
            };

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

            Logger.Log("Sample Iranian data seeded successfully.");

            #endregion
        }
    }
}
