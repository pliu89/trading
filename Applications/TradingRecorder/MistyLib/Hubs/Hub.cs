using System;
//using System.Threading;
//using System.Collections.Generic;

namespace Misty.Lib.Hubs
{
    // *********************************************
    // ****             Hub class               ****
    // *********************************************
    /// <summary>
    /// This class should be inherited by all "hubs."  
    /// This class inherits from the HubBase which maintains a private thread for the hub
    /// along with an associated "wait handle" and event queue.  It also provides some other 
    /// support features, such as a log.
    /// Example of usage:
    /// Class inheritance structure:    HubBase -- Hub -- MarketHub -- MarketSimAPI
    /// Usually an API-specific event is triggered somehow (from the exchange server API) at 
    /// the highest level, which is a sub-class of a sub-class (in this example the sub-sub-class 
    /// is the "MarketSimAPI").
    /// The API-specific event is interpreted by MarketSimAPI and the appropriate MarketHub.MarketHubEvent 
    /// is created and loaded into HubBase.HubEventEnQueue( MarketHubEvent ), which wakes up its 
    /// private HubBase thread to process the event.  
    /// The processing is done by calling HubEventHandler() which MUST be overriden by the intermediate
    /// sub-class (called "MarketHub" here).  The internal state of MarketHub is then changed in accordance
    /// with the event and it informs whatever objects have subscribed to its public events, such as 
    /// "PriceChange" or "EchangeStateChange" etc.
    /// Purpose of the Hub and HubBase classes:
    /// The purpose of this round-about way of doing things is few-fold: 
    /// First: 
    /// API-specific information is only located in the sub-sub-class MarketSimAPI, 
    /// while application specific info lives in the subclass MarketHub.  In this way, API-specific
    /// stuff (inside MarketSimAPI) does not appear in the class in change of the local 
    /// application (MarketHub).
    /// Second: 
    /// The Hub and HubBase classes are solely responsible for thread control and management.
    /// Third:
    /// The initial event is passed to MarketSimAPI from an external thread, the event is quickly
    /// pushed onto a queue and later processed by the local HubBase thread.  This frees up the 
    /// original thread to return to the API as soon as possible.
    /// </summary>
    public abstract class Hub : HubBase
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************

        public LogHub Log;                               // multithreaded Log file server.


        #endregion//members


        #region Constructor
        // *****************************************************************
        // ****                 Constructor                             ****
        // *****************************************************************
        public Hub(string hubName, string logDirName, bool isLogViewDesired, LogLevel logLevel)
            : base(hubName)
        {
            Log = new LogHub(hubName, logDirName, isLogViewDesired, logLevel);    // log is self-starting.

        }//end constructor.
        //
        //
        /// <summary>
        /// Simplified hub constructor.  If this form is used, the subclass MUST directly create the LogHub!  
        /// </summary>
        /// <param name="hubName"></param>
        public Hub(string hubName)
            : base(hubName)
        {

        }
        //
        //
        //
        #endregion//constructor


        #region Public Methods
        // *****************************************************************
        // ****                 Public Methods                          ****
        // *****************************************************************
        //
        //
        //
        //
        //
        //
        protected override void Stop()
        {
            Log.RequestStop();
            base.Stop();
        }//end Stop().
        //
        #endregion//public methods


        #region Private Methods
        // *****************************************************************
        // ****                Private Methods                          ****
        // *****************************************************************
        //
        /// <summary>
        /// This overrides the empty HubBase method and logs the important statistics.
        /// </summary>
        protected override void DiagnosticReport()
        {
            Log.NewEntry(LogLevel.Minor
                , "Base Load={0:0.0}%  EventFreq={1:0.0} hz  EventsWaiting={2:0.0}% SleepSkipped={3:0.0}% EventArrivalRate={4:0.0} "
                //, "Base Load={0:##0.0}%  EventFreq={1:##0.0} hz  EventsWaiting={2:##0.0}% SleepSkipped={3:##0.0}% EventArrivalRate={4:####0.0} "
                , HubWorkLoad * 100.0
                , HubEventFrequency
                , HubEventWaitingFraction * 100.0
                , HubSleepSkippedFraction * 100.0
                , HubEventClusterAverageSize);
        }// Diagnotic report		
        //
        #endregion//Private methods


    }//end class.
}//end namespace.
