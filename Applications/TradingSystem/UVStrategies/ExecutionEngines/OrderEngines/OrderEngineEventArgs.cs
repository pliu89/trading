using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UV.Strategies.ExecutionEngines.OrderEngines
{
    /// <summary>
    /// Event args that inherit from this class are meant to be used for messaging between complex execution systems that have multiple threads
    /// utilizing different listeners being managed by a single strategy.  
    /// 
    /// Typically this would be used to communicate between two listeners 
    /// </summary>
    public class OrderEngineEventArgs : EventArgs
    {
    }
}
