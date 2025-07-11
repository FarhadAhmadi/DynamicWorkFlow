using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkFlow.Models;
using WorkFlow.Persistence;
using WorkFlow.RuleInterpreter;

class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Logger.Initialize();
        try
        {
            Logger.Log("=== Workflow Engine Started ===");

            DatabaseContext dbContext = new();

            SampleDataInsertor sampleDataInsertor = new(dbContext);
            sampleDataInsertor.InsertAsync();


            #region Execute Workflow

            RuleInterpreter ruleInterpreter = new(dbContext);
            string rulePath = "C:\\Users\\Farhad-LapTop\\Desktop\\Micro-service\\WorkFlow\\WorkFlow\\Rules";

            WorkflowEngine engine = new(dbContext, ruleInterpreter, rulePath);
            await engine.RunAsync();

            Logger.Log("=== Workflow execution completed ===");

            #endregion

            var groupedPeopleByState = dbContext.People
                .GroupBy(p => p.CurrentState)
                .Select(g => new
                {
                    State = g.Key,
                    Count = g.Count(),
                    People = g.Select(p => new { p.Id, p.Name }).ToList()
                })
                .ToList();

            foreach (var group in groupedPeopleByState)
            {
                // Change color for State header
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"State: {group.State} - People count: {group.Count}");
                Console.ResetColor();

                for (int i = 0; i < group.People.Count; i++)
                {
                    var person = group.People[i];
                    string prefix = (i == group.People.Count - 1) ? "└─" : "├─";

                    // Color person info differently
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("  " + prefix + " ");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"Id: {person.Id}");
                    Console.ResetColor();

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(", Name: ");
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine(person.Name);
                    Console.ResetColor();
                }
            }




        }
        catch (Exception ex)
        {
            Logger.Log($"Unexpected error in workflow engine:\n{ex}", LogSource.Engine, LogLevel.Error);
        }
        finally
        {
            string filePath = $"WorkflowLog-{DateTime.Now:yyyyMMdd-HHmmss}.log";
            Logger.SaveLog(filePath);
            Logger.Log($"Log file saved to: {filePath}");
        }
    }
}
