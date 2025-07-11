using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkFlow.Exceptions
{
    public class BreakException : Exception { }
    public class ContinueException : Exception { }
    public class StopWorkflowException : Exception
    {
        public string Reason { get; }
        public StopWorkflowException(string reason) => Reason = reason;
    }
}
