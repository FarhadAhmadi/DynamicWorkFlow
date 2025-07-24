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
- 🧩 **Step Handlers:** Classes implementing actions (`fetch`, `fetchList`, `foreach`, `if`, `assign`, `calculateDuration`, `stop`, `log`, etc.).  
- 🔍 **VariableResolver:** Resolves variable values dynamically within the workflow context.  
- 🗄 **DatabaseContext:** EF Core context for querying data dynamically.  
- 🚨 **Custom Exceptions:** `BreakException`, `ContinueException` to manage loops.

---

## 📝 Example Workflow JSON

```json
{
    "name": "Rule A - Validate 1 year تهران experience with confirmed employer",
    "variables": {
        "foundValidHistory": false,
        "ruleName": "Rule A"
    },
    "steps": [
        {
            "id": "start",
            "action": "log",
            "message": "[@ruleName] Starting validation for Person ID: @person.Id, Name: @person.Name",
            "next": "fetchHistories"
        },
        {
            "id": "fetchHistories",
            "action": "fetchList",
            "entity": "WorkHistories",
            "filter": { "PersonId": "@person.Id" },
            "storeAs": "histories",
            "next": "logCount"
        },
        {
            "id": "logCount",
            "action": "log",
            "message": "[@ruleName] Retrieved @histories.Count work history records for Person ID: @person.Id",
            "next": "assignFoundValidFalse"
        },
        {
            "id": "assignFoundValidFalse",
            "action": "assign",
            "variable": "foundValidHistory",
            "value": false,
            "next": "foreachHistories"
        },
        {
            "id": "foreachHistories",
            "action": "foreach",
            "source": "histories",
            "var": "history",
            "body": [
                "logHistory",
                "checkLocation"
            ],
            "next": "checkResult"
        },
        {
            "id": "logHistory",
            "action": "log",
            "message": "[@ruleName] Processing history ID: @history.Id | Location: @history.Location | Start: @history.StartDate | End: @history.EndDate",
            "next": null
        },
        {
            "id": "checkLocation",
            "action": "if",
            "condition": {
                "field": "history.Location",
                "operator": "==",
                "value": "تهران"
            },
            "then": [
                "logTehran",
                "calculateDuration",
                "logDuration",
                "checkDuration"
            ],
            "else": [
                "logSkipTehran",
                "continueStep"
            ],
            "next": null
        },
        {
            "id": "logTehran",
            "action": "log",
            "message": "[@ruleName] Work history ID @history.Id is in location تهران. Proceeding to calculate duration...",
            "next": null
        },
        {
            "id": "calculateDuration",
            "action": "calculateDuration",
            "startDate": "history.StartDate",
            "endDate": "history.EndDate",
            "unit": "days",
            "storeAs": "DurationInDays",
            "next": null
        },
        {
            "id": "logDuration",
            "action": "log",
            "message": "[@ruleName] Duration for history ID @history.Id: @DurationInDays days",
            "next": null
        },
        {
            "id": "checkDuration",
            "action": "if",
            "condition": {
                "field": "DurationInDays",
                "operator": ">=",
                "value": 365
            },
            "then": [
                "logValidDuration",
                "fetchEmployer",
                "logEmployer",
                "checkEmployerConfirmed"
            ],
            "else": [
                "logInvalidDuration",
                "continueStep"
            ],
            "next": null
        },
        {
            "id": "logValidDuration",
            "action": "log",
            "message": "[@ruleName] Duration is >= 365 days. Verifying employer confirmation for Employer ID: @history.Employer.Id",
            "next": null
        },
        {
            "id": "fetchEmployer",
            "action": "fetch",
            "entity": "Employers",
            "filter": { "Id": "@history.Employer.Id" },
            "storeAs": "employer",
            "next": null
        },
        {
            "id": "logEmployer",
            "action": "log",
            "message": "[@ruleName] Employer fetched: @employer.Name (ID: @employer.Id), HasConfirmation: @employer.HasConfirmation",
            "next": null
        },
        {
            "id": "checkEmployerConfirmed",
            "action": "if",
            "condition": {
                "field": "employer.HasConfirmation",
                "operator": "==",
                "value": true
            },
            "then": [
                "logEmployerConfirmed",
                "markSuccess",
                "breakLoop"
            ],
            "else": [
                "logUnconfirmed",
                "stopEarlyFailure"
            ],
            "next": null
        },
        {
            "id": "logEmployerConfirmed",
            "action": "log",
            "message": "[@ruleName] Employer confirmation is true. Valid work history found. Rule A will pass.",
            "next": null
        },
        {
            "id": "markSuccess",
            "action": "assign",
            "variable": "foundValidHistory",
            "value": true,
            "next": null
        },
        {
            "id": "breakLoop",
            "action": "break",
            "next": null
        },
        {
            "id": "logUnconfirmed",
            "action": "log",
            "message": "[@ruleName] Employer confirmation is false. Rule A will fail for Person ID: @person.Id",
            "next": null
        },
        {
            "id": "stopEarlyFailure",
            "action": "stop",
            "reason": "Rule A failed: Employer not confirmed for qualifying history.",
            "status": false
        },
        {
            "id": "logSkipTehran",
            "action": "log",
            "message": "[@ruleName] Location of history ID: @history.Id is not تهران. Skipping.",
            "next": null
        },
        {
            "id": "logInvalidDuration",
            "action": "log",
            "message": "[@ruleName] Duration is less than 365 days for history ID: @history.Id. Skipping.",
            "next": null
        },
        {
            "id": "continueStep",
            "action": "continue",
            "next": null
        },
        {
            "id": "checkResult",
            "action": "if",
            "condition": {
                "field": "foundValidHistory",
                "operator": "==",
                "value": true
            },
            "then": [
                "logFinalPass",
                "stopSuccess"
            ],
            "else": [
                "logFinalFail",
                "stopFailure"
            ],
            "next": null
        },
        {
            "id": "logFinalPass",
            "action": "log",
            "message": "[@ruleName] Rule passed: Found valid work history in تهران with confirmed employer for at least 1 year.",
            "next": null
        },
        {
            "id": "stopSuccess",
            "action": "stop",
            "reason": "Rule A passed: 1+ year confirmed work in تهران found.",
            "status": true
        },
        {
            "id": "logFinalFail",
            "action": "log",
            "message": "[@ruleName] Rule failed: No valid work history in تهران with confirmed employer found.",
            "next": null
        },
        {
            "id": "stopFailure",
            "action": "stop",
            "reason": "Rule A failed: No valid تهران work history found.",
            "status": false
        }
    ]
}
