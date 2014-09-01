using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Misty.Lib.TaskHubs
{
    public enum TaskStatus
    {
        New,
        WaitAndTryAgain,
        Failed,
        Success


    }
}
