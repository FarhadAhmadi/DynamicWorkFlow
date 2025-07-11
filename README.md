# 🚀 Dynamic Workflow Engine & Rule Interpreter

A modular, extensible workflow engine for executing dynamic business rules defined in a JSON-like DSL, built on .NET Core with Entity Framework Core.

---

## 📖 Overview

This project enables defining workflows and business rules as JSON objects consisting of multiple steps executed sequentially or conditionally. It dynamically interprets these rules by:

- 🔍 **Querying the database**
- ⚖️ **Evaluating conditions**
- 📝 **Assigning variables**
- 🔄 **Looping over collections**
- 📅 **Calculating date durations**

All without requiring code changes.

---

## ✨ Features

- 🔎 **Dynamic Entity Fetching:** Retrieve single or list entities from the database with dynamic filters.
- 🤔 **Conditional Branching:** Supports flexible if-then-else logic.
- 🔄 **Loops:** Iterate over collections with foreach steps.
- 🗂 **Variable Assignment:** Assign query results or values to runtime variables for reuse.
- ⏳ **Duration Calculation:** Calculate date differences in days, months, or years.
- 🛑 **Stop Workflow:** Stop execution with custom status and reason.
- 🛠 **Extensible Step Handlers:** Easily add new business logic by implementing custom step handlers.
- 📌 **Variable Path Resolution:** Supports nested object property access using "dot notation" (e.g. `@person.Name`).
- 🚦 **Exception Handling:** Control loop flow with support for break and continue.

---

## 🏗 Architecture

### Core Components

- ⚙️ **RuleInterpreter:** Orchestrates rule execution, iterating over steps.
- 🚦 **RuleStepExecutor:** Dispatches each step to the correct handler.
- 🧩 **Step Handlers:** Classes implementing actions (`fetch`, `fetchList`, `foreach`, `if`, `assign`, `calculateDuration`, `stop`,`log`, etc.).
- 🔍 **VariableResolver:** Resolves variable values dynamically within the workflow context.
- 🗄 **DatabaseContext:** EF Core context for querying data dynamically.
- 🚨 **Custom Exceptions:** `BreakException`, `ContinueException` to manage loops.

---

## 📝 Example Workflow JSON

```json
{
    "name": "Rule A - Validate 1 year تهران experience with confirmed employer",
    "steps": [
        {
            "action": "log",
            "message": "Rule A started for person: @person.Id - @person.Name"
        },
        {
            "action": "fetchList",
            "entity": "WorkHistories",
            "filter": { "PersonId": "@person.Id" },
            "storeAs": "histories"
        },
        {
            "action": "log",
            "message": "Fetched @histories.Count work history records."
        },
        {
            "action": "assign",
            "variable": "foundValidHistory",
            "value": false
        },
        {
            "action": "foreach",
            "source": "histories",
            "var": "history",
            "body": [
                {
                    "action": "log",
                    "message": "Checking history record: @history.Id at location @history.Location"
                },
                {
                    "action": "if",
                    "condition": {
                        "field": "history.Location",
                        "operator": "==",
                        "value": "تهران"
                    },
                    "then": [
                        {
                            "action": "log",
                            "message": "History is located in تهران, calculating duration..."
                        },
                        {
                            "action": "calculateDuration",
                            "startDate": "history.StartDate",
                            "endDate": "history.EndDate",
                            "unit": "days",
                            "storeAs": "DurationInDays"
                        },
                        {
                            "action": "log",
                            "message": "Calculated duration in days: @DurationInDays"
                        },
                        {
                            "action": "if",
                            "condition": {
                                "field": "DurationInDays",
                                "operator": ">=",
                                "value": 365
                            },
                            "then": [
                                {
                                    "action": "log",
                                    "message": "Duration is >= 365 days, checking employer confirmation..."
                                },
                                {
                                    "action": "fetch",
                                    "entity": "Employers",
                                    "filter": { "Id": "@history.Employer.Id" },
                                    "storeAs": "employer"
                                },
                                {
                                    "action": "log",
                                    "message": "Fetched employer @employer.Name - HasConfirmation: @employer.HasConfirmation"
                                },
                                {
                                    "action": "if",
                                    "condition": {
                                        "field": "employer.HasConfirmation",
                                        "operator": "==",
                                        "value": true
                                    },
                                    "then": [
                                        {
                                            "action": "log",
                                            "message": "Employer is confirmed. Valid history found."
                                        },
                                        {
                                            "action": "assign",
                                            "variable": "foundValidHistory",
                                            "value": true
                                        },
                                        { "action": "break" }
                                    ],
                                    "else": [
                                        {
                                            "action": "log",
                                            "message": "Employer is not confirmed. Stopping rule."
                                        },
                                        {
                                            "action": "stop",
                                            "reason": "Rule A failed: Employer not confirmed.",
                                            "status": false
                                        }
                                    ]
                                }
                            ],
                            "else": [
                                {
                                    "action": "log",
                                    "message": "Duration is less than 365 days. Continuing loop."
                                },
                                { "action": "continue" }
                            ]
                        }
                    ],
                    "else": [
                        {
                            "action": "log",
                            "message": "Location is not تهران. Skipping this history."
                        },
                        { "action": "continue" }
                    ]
                }
            ]
        },
        {
            "action": "if",
            "condition": {
                "field": "foundValidHistory",
                "operator": "==",
                "value": true
            },
            "then": [
                {
                    "action": "log",
                    "message": "Rule A passed: Valid تهران history confirmed."
                },
                {
                    "action": "stop",
                    "reason": "Rule A passed: 1+ year confirmed work in تهران found.",
                    "status": true
                }
            ],
            "else": [
                {
                    "action": "log",
                    "message": "Rule A failed: No valid تهران history found."
                },
                {
                    "action": "stop",
                    "reason": "Rule A failed: No valid تهران work history found.",
                    "status": false
                }
            ]
        }
    ]
}



## 🚀 Getting Started

### Requirements

- 🖥 .NET 6 or later
- 📦 Entity Framework Core (configured with your database)
- 📚 Newtonsoft.Json

### Setup

- 🔄 Clone the repository.
- 🛠 Configure your DatabaseContext and ensure your entities are mapped.
- 🔌 Inject DatabaseContext into RuleInterpreter.
- ✍️ Define your workflows as JSON matching the step schema.
- ▶️ Call `RuleInterpreter.ExecuteAsync(dynamicRule)` to execute your workflow.

---

## 💻 Code Snippet Example

```csharp
var ruleJson = File.ReadAllText("path/to/rule.json");
dynamic rule = Newtonsoft.Json.JsonConvert.DeserializeObject(ruleJson);

var ruleInterpreter = new RuleInterpreter(dbContext);
RuleExecutionResult result = await ruleInterpreter.ExecuteAsync(rule);

Console.WriteLine($"Status: {result.Status}, Reason: {result.Reason}");


