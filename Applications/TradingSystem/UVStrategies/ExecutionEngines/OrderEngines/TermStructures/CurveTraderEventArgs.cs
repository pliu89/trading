using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UV.Strategies.ExecutionEngines.OrderEngines.TermStructures
{
    public class CurveTraderEventArgs : OrderEngineEventArgs
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        //
        public EventType MsgType = EventType.None;
        public EventStatus Status = EventStatus.None;

        public int EngineId = -1;
        #endregion



        #region Enums
        // *****************************************************************
        // ****                     Enums                               ****
        // *****************************************************************
        //
        //
        //
        public enum EventType
        {
            Launched,
            Stopping,
            Stopped,
            None
        }
        //
        //
        public enum EventStatus
        {
            Confirm,
            Request,
            None
        }
        #endregion
    }
}
