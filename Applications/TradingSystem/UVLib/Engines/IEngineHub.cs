using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.Engines
{
    /// <summary>
    /// An object that contains a list of IEngineContainers.
    /// </summary>
    public interface IEngineHub
    {
        string ServiceName { get; }


        //
        // I own a list of IEngineContainers  
        //
        List<IEngineContainer> GetEngineContainers();   

        //
        // I allow others to subscribe to my engine change events.
        //
        bool HubEventEnqueue(EventArgs e);
        event EventHandler EngineChanged;

        /// <summary>
        /// Usually, this would be a private function.  However, in the multithreading model 
        /// for individual strategy (as in Execution framework), its convenient for each 
        /// strategy to call this method directly themselves.
        /// </summary>
        /// <param name="engineEventArgs"></param>
        void OnEngineChanged(EventArgs engineEventArgs);
    }
}
