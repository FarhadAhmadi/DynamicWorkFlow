using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkFlow.DTO
{
    public class RuleExecutionResult
    {
        public bool Status { get; set; } // true = passed, false = failed
        public string Reason { get; set; }
    }
}
