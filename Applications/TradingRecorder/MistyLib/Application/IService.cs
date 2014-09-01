using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Misty.Lib.Application
{
    /// <summary>
    /// 
    /// </summary>
    public interface IService 
    {
        //
        // Properties
        //
        string ServiceName
        {
            get;
        }
        //
        // Methods
        //
        void Start();
        void Connect();
        void RequestStop();

        //
        // events
        // 
        event EventHandler ServiceStateChanged;
        event EventHandler Stopping;
        // TODO: Need to create enum with various state flags, and EventArg for this.
        // Most important are the states: Connected/Disconnected



    }
}
