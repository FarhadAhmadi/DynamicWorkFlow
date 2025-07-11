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
        Logger.Initialize();
        // Fix Persian/UTF-8 character rendering
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        try
        {
            Console.WriteLine("=== Workflow Engine Started ===");

            DatabaseContext dbContext = new();

            SampleDataInsertor sampleDataInsertor = new(dbContext);
            sampleDataInsertor.InsertAsync();


            #region Execute Workflow

            RuleInterpreter ruleInterpreter = new(dbContext);
            string rulePath = "C:\\Users\\Farhad-LapTop\\Desktop\\Micro-service\\WorkFlow\\WorkFlow\\Rules";

            WorkflowEngine engine = new(dbContext, ruleInterpreter, rulePath);
            await engine.RunAsync();

            Console.WriteLine("=== Workflow execution completed ===");

            #endregion
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FATAL] Unexpected error in workflow engine:\n{ex}");
        }
        finally
        {
            string filePath = $"WorkflowLog-{DateTime.Now:yyyyMMdd-HHmmss}.log";
            Logger.SaveLog(filePath);
            Console.WriteLine($"[INFO] Log file saved to: {filePath}");
        }
    }
}
