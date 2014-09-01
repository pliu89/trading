using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UV.Lib.Application
{

    /// <summary>
    /// 
    /// </summary>
    public class ServiceStateEventArgs : EventArgs
    {
        public string ServiceName = string.Empty;
        public ServiceStates CurrentState = ServiceStates.Unstarted;
        public ServiceStates PreviousState = ServiceStates.Unstarted;

        //
        // **** Constructor ****
        //
        /// <summary>
        /// Private means this is disabled and users must call other constructor.
        /// </summary>
        private ServiceStateEventArgs()
        {
        }
        /// <summary>
        /// The constructor that ensures the proper creation of the eventArgs.
        /// </summary>
        public ServiceStateEventArgs(IService iService, ServiceStates current, ServiceStates previous)
        {
            this.ServiceName = iService.ServiceName;           
            this.CurrentState = current;
            this.PreviousState = previous;
        }
        //
        //
        public override string ToString()
        {
            return string.Format("ServiceStateChange {0} -> {1}",PreviousState,CurrentState);
        }

    }

}
