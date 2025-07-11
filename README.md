Dynamic Workflow Engine & Rule Interpreter
A modular, extensible workflow engine for executing dynamic business rules defined in JSON-like DSL, built on .NET Core with Entity Framework Core.
________________________________________
Overview
This project allows defining workflows and business rules as JSON objects that consist of multiple steps, executed sequentially or conditionally. It dynamically interprets these rules, querying the database, evaluating conditions, assigning variables, looping collections, and calculating date durations — all without changing the code.
________________________________________
Features
•	Dynamic Entity Fetching: Retrieve single or list entities from the database using dynamic filters.
•	Conditional Branching: Supports if-then-else style logic with flexible condition evaluation.
•	Loops: Iterate over collections with foreach steps.
•	Variable Assignment: Assign values or results of queries to runtime variables for use in later steps.
•	Duration Calculation: Calculate date differences in days, months, or years.
•	Stop Workflow: Ability to stop the workflow execution with custom status and reason.
•	Extensible Step Handlers: Modular step handlers that can be extended for custom business actions.
•	Variable Path Resolution: Support nested object property resolution using "dot notation" variables like @person.Name.
•	Exception Handling: Support for break and continue to control loop flows.
________________________________________
Architecture
Core Components
•	RuleInterpreter: Orchestrates rule execution by iterating over steps and delegating them to the executor.
•	RuleStepExecutor: Dispatches each workflow step action to its respective handler.
•	Step Handlers: Individual classes implementing logic for each action type (fetch, fetchList, foreach, if, assign, calculateDuration, stop, etc.).
•	VariableResolver: Helper class to resolve variable values dynamically from the current workflow context.
•	DatabaseContext: EF Core context injected for querying the database dynamically.
•	Custom Exceptions: BreakException, ContinueException to control flow inside loops.
________________________________________
Example Workflow JSON
json
CopyEdit
{
  "steps": [
    {
      "action": "fetch",
      "entity": "Person",
      "filter": { "Id": 123 },
      "storeAs": "person"
    },
    {
      "action": "if",
      "condition": {
        "field": "person.Age",
        "operator": ">=",
        "value": 18
      },
      "then": [
        {
          "action": "assign",
          "variable": "Status",
          "value": true
        }
      ],
      "else": [
        {
          "action": "assign",
          "variable": "Status",
          "value": false
        }
      ]
    }
  ]
}
________________________________________
Getting Started
Requirements
•	.NET Core 6 or later
•	Entity Framework Core (configured with your database)
•	Newtonsoft.Json
Setup
1.	Clone the repo.
2.	Setup your DatabaseContext and ensure your entities are properly mapped.
3.	Inject DatabaseContext into the RuleInterpreter.
4.	Define your workflows as JSON objects matching the step schema.
5.	Call RuleInterpreter.ExecuteAsync(dynamicRule) with your rule.
________________________________________
Code Snippet Example
csharp
CopyEdit
var ruleJson = File.ReadAllText("path/to/rule.json");
dynamic rule = Newtonsoft.Json.JsonConvert.DeserializeObject(ruleJson);

var ruleInterpreter = new RuleInterpreter(dbContext);
RuleExecutionResult result = await ruleInterpreter.ExecuteAsync(rule);

Console.WriteLine($"Status: {result.Status}, Reason: {result.Reason}");
________________________________________
How to Extend
1.	Create a new class in StepHandlers implementing the required action.
2.	Implement ExecuteAsync(dynamic step) method with your custom logic.
3.	Register the new step action in RuleStepExecutor.ExecuteAsync switch case.
________________________________________
Design Considerations
•	Uses Reflection and Expression Trees for dynamic querying with EF Core.
•	Supports complex nested variable access using dot notation.
•	Modular, allowing easy addition of new workflow step types.
•	Async-await for non-blocking database calls.
•	Strong error handling and meaningful exceptions.
________________________________________
Troubleshooting
•	Entity Not Found: Ensure your DbContext has the DbSet for the entity name you reference.
•	Invalid Variable Path: Variable paths must exist in the current _variables dictionary.
•	Type Conversion Issues: Filters automatically convert types but mismatches may throw exceptions.
•	Async Method Returns: Step handlers returning Task must not return values; use return Task.CompletedTask.
________________________________________
Future Enhancements
•	Add more complex operators in conditions (AND, OR, nested conditions).
•	Support parameterized SQL or stored procedures.
•	Add workflow persistence and state management.
•	Implement event-based triggers.
•	Add logging and auditing capabilities.
________________________________________
License
MIT License — free to use and extend.

