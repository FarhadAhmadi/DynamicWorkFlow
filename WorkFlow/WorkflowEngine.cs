using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using WorkFlow.Models;
using WorkFlow.Persistence;
using WorkFlow.RuleInterpreter;

public class WorkflowEngine
{
    private readonly DatabaseContext _dbContext;
    private readonly RuleInterpreter _ruleInterpreter;

    // Base directory for rule files - adjust as needed
    private readonly string _rulesBasePath;

    public WorkflowEngine(DatabaseContext dbContext, RuleInterpreter ruleInterpreter, string rulesBasePath)
    {
        _dbContext = dbContext;
        _ruleInterpreter = ruleInterpreter;
        _rulesBasePath = rulesBasePath;
    }

    #region Public API

    public async Task RunAsync()
    {
        var people = await _dbContext.People.ToListAsync();

        foreach (var person in people)
        {
            Logger.Log($"Starting workflow for Person {person.Id} - {person.Name}, initial state: {person.CurrentState}");

            while (true)
            {
                if (person.CurrentState == PersonState.Final)
                {
                    Logger.Log($"Person {person.Id} reached final state. Exiting workflow loop.", LogSource.Engine, LogLevel.Success);
                    break;
                }

                var ruleFilePath = Path.Combine(_rulesBasePath, $"Rule-{person.CurrentState}.json");

                if (!File.Exists(ruleFilePath))
                {
                    Logger.Log($"Rule file not found: {ruleFilePath}. Exiting workflow for person {person.Id}.", LogSource.Engine, LogLevel.Error);
                    break;
                }

                var ruleJson = LoadJsonFile(ruleFilePath);

                _ruleInterpreter.SetVariable("person", person);

                Logger.Log($"Executing rule for Person {person.Id} at state {person.CurrentState} using rule file {ruleFilePath}");

                var result = await _ruleInterpreter.ExecuteAsync(ruleJson);

                Logger.Log($"Rule execution result for Person {person.Id}: Status={result.Status}, Reason='{result.Reason}', CurrentState={person.CurrentState}");

                if (result.Status)
                {
                    var nextState = GetNextState(person.CurrentState);

                    if (nextState != null)
                    {
                        Logger.Log($"Transitioning Person {person.Id} from state {person.CurrentState} to {nextState}",LogSource.Engine,LogLevel.Success);
                        person.CurrentState = nextState.Value;
                    }
                    else
                    {
                        Logger.Log($"No next state found for Person {person.Id}. Final state reached.", LogSource.Engine, LogLevel.Success);
                        break;
                    }
                }
                else
                {
                    Logger.Log($"Rule failed for Person {person.Id} at state {person.CurrentState} with reason: {result.Reason}. Exiting workflow loop.", LogSource.Engine, LogLevel.Error);
                    break;
                }
            }
        }

        await _dbContext.SaveChangesAsync();

        Logger.Log("Workflow engine run completed and changes saved.");
    }

    #endregion

    #region Helpers

    private dynamic LoadJsonFile(string path)
    {
        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<dynamic>(json);
    }

    public static PersonState? GetNextState(PersonState current)
    {
        var states = Enum.GetValues(typeof(PersonState)).Cast<PersonState>().ToList();
        var index = states.IndexOf(current);

        if (index >= 0 && index < states.Count - 1)
            return states[index + 1];

        return null;
    }

    #endregion
}
