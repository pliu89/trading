using System;
using System.Threading;
using System.Collections.Generic;

using System.Diagnostics;	// for stopwatch

namespace UV.Lib.Hubs
{
    //using UV.Lib.Application;

    // *********************************************
    // ****             HubBase class           ****
    // *********************************************
    /// <summary>
    /// This class should be inherited by all "hubs."  
    /// This hub base maintains a private thread for the hub along with an associated
    /// "wait handle" and event queue.  
    /// 
    /// For a complicated example of usage: See class "Hub.cs"
    /// 
    /// Simple Usage:
    /// Consider an application for logging messages to a file.  Here, one might have simply
    /// two classes
    ///             LogHub >> HubBase
    /// 
    /// Message logging starts when an external thread from the main application (called main thread)
    /// passes a new msg string into a method called, say, LogHub.NewMessage( string ).  
    /// In this method (called by main thread) the string is re-packaged into an EventArg object 
    /// and passed to the HubBase via HubBase.EnQueue( EventArg ).  The main thread is then free
    /// to exit.
    /// Thereafter, the HubBase thread awakens and passes the eventArg back up to 
    /// Logger.HubEventHandler( EventArg ) which unpacks the string message, and processes it.
    /// All of this work is done by the HubBase thread of course.
    /// </summary>
    public abstract class HubBase
    {

        #region HubBase Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        //
        // Basic Hub Members
        protected string m_HubName;                         // name for both this object and its thread.

        // Hub Thread variables
        private volatile WaitListenState m_WaitListenState = WaitListenState.ReadyToStart;
        private object m_HubBaseLock = new object();        // lock for the hub event queue.
        private Thread m_Thread = null;
        protected ThreadPriority m_ThreadPriority = ThreadPriority.Normal;
        private EventWaitHandle m_WaitHandle = null;
        private List<EventArgs> m_HubEventQueue = null;     // queue of hub events.

        // Periodic update controls
        protected int m_WaitListenUpdatePeriod = -1;        // desired miliseconds between periodic updates(); -1 means none.
        protected DateTime m_NextUpdateTime = DateTime.MinValue;

        // Events
        public event EventHandler Starting;                 // HubBase thread events
        public event EventHandler Stopping;



        #endregion//members


        #region Properties
        // *****************************************************************
        // ****                     Properties                        ****
        // *****************************************************************
        //
        public WaitListenState ListenState
        {
            get { return m_WaitListenState; }
        }
        public string HubName
        {
            get { return m_HubName; }
        }
        //
        //
        #endregion//properties.


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hubName">Generic name for hub. Used to name thread, and log files, etc. 
        /// Can be changed later.
        /// </param>
        public HubBase(string hubName)
        {
            m_HubEventQueue = new List<EventArgs>();
            m_HubName = hubName;            
        }//end constructor().
        //
        //
        #endregion//Constructors


        #region Public Methods
        // *********************************************************************
        // ****                     Public & Protected Methods              ****
        // *********************************************************************
        //
        //
        // ****             Start()                 ****
        //
        /// <summary>
        /// Called by main thread, it is here that we might finalize any form/gui
        /// owned by this hub.  Also creates the hub's thread by invoking WaitListenStart().
        /// </summary>
        public virtual void Start()
        {
            // These events are triggered by MAIN GUI thread so that forms etc, 
            // can be finalized by subscribing to the "Starting" event.  This is by convention.
            // TODO: Generalize this using Invokes for GUIs if needed. Test such approach.
            if (this.Starting != null) { Starting(this, EventArgs.Empty); } // call subscribers who want to know about starting event.

            //
            // Create and launch Listening thread & wait handle.
            //
            if (m_WaitHandle == null)
            {
                m_WaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
            }
            if ((m_Thread == null) || (!m_Thread.IsAlive))
            {
                m_Thread = new Thread(new ThreadStart(WaitListenStart));	// Method thread will first call.
                m_Thread.Name = m_HubName;									// Name thread after hub itself.
                m_Thread.Priority = m_ThreadPriority;
                //m_Thread.IsBackground = m_IsBackgroundThread;
                m_Thread.Start();
            }
        }//end Start()
        //
        //
        //
        //
        // ****             Stop()                  ****
        //
        /// <summary>
        /// Called by any external thread, this attempts a friendly shut-down of the hub listener.
        /// When this is overloaded by another class, it should call this base method also!!
        /// </summary>
        protected virtual void Stop()
        {
            if (this.Stopping != null) { Stopping(this, EventArgs.Empty); }            // Call any objects requesting Stop call.

            if (m_DiagnoticStopWatch != null && m_DiagnoticStopWatch.IsRunning) m_DiagnoticStopWatch.Stop();
            m_WaitListenState = WaitListenState.Stopping;       // this enum is volatile.
            if (m_WaitHandle != null) { m_WaitHandle.Set(); }   // trigger a wake-up signal for hub thread.
            // TODO: Push stop request event onto queue.
        }//end Stop().
        //
        //
        //
        public abstract void RequestStop();
        //
        //
        //
        //
        // ****             HubEventEnqueue             ****
        //
        /// <summary>
        /// Called an external thread, this method is usually called by the 
        /// api-specific sub-sub-class to load new events and then wake the Hub thread to 
        /// process them.
        /// This is where the shared HubEventQueue are accessed by the external threads.
        /// </summary>
        /// <param name="e"></param>
        /// <returns>Returns true if event is successfully triggered.</returns>
        public virtual bool HubEventEnqueue(EventArgs e)
        {
            lock (m_HubBaseLock)
            {
                m_HubEventQueue.Add(e);            // Enqueue the events. 
            }
            return TriggerEvent();
        }//end HubEventEnqueue
        /// <summary>
        /// Overloading that allows for multiple events to be send simultaneously.
        /// </summary>
        /// <param name="e"></param>
        /// <returns>Returns true if event is successfully triggered.</returns>
        public virtual bool HubEventEnqueue(EventArgs[] e)
        {
            lock (m_HubBaseLock)
            {
                foreach (EventArgs eventArg in e) 
                {
                    m_HubEventQueue.Add(eventArg); 
                }            // Enqueue the events.
            }
            return TriggerEvent();
        }//end HubEventEnqueue
        //
        public virtual bool HubEventEnqueue(List<EventArgs> e)
        {
            lock (m_HubBaseLock)
            {
                foreach (EventArgs eventArg in e) 
                {
                    m_HubEventQueue.Add(eventArg); 
                }// Enqueue the events.
            }
            return TriggerEvent();
        }//end HubEventEnqueue
        /// <summary>
        /// This overloading is of the general EventHandler form.
        /// </summary>
        public virtual void HubEventEnqueue(object sender, EventArgs e)
        {
            lock (m_HubBaseLock)
            {
                m_HubEventQueue.Add(e);            // Enqueue the events. 
            }
            TriggerEvent();
        }//end HubEventEnqueue
        //
        //
        //
        // ****                 Trigger Event()                 ****
        //
        /// <summary>
        /// This is called by an external (or internal) thread.  Its calls Set() for the
        /// internal (or local) HubBase thread that is sleeping.
        /// </summary>
        /// <returns></returns>
        private bool TriggerEvent()
        {
            bool isSuccess = true;
            if ((m_WaitListenState == WaitListenState.Waiting)
                || (m_WaitListenState == WaitListenState.Working))
            {   // We are allowed to add to the event queue when 
                // 1. state is waiting or working, only.
                /*
                System.Text.StringBuilder msg = new System.Text.StringBuilder();
                msg.AppendFormat("Thread {0} buzzes {1}.", Thread.CurrentThread.Name, m_Thread.Name);
                Log.SubmitLogEntry(msg.ToString(), EngineLog.Verbose);
                */
                isSuccess = m_WaitHandle.Set();
            }
            else
            {
                isSuccess = false;
            }
            return isSuccess;
        }//end TriggerEvent().
        //
        //
        //
        #endregion//public methods


        #region Private & Protected Methods
        // *************************************************************
        // ****                     Private Methods                 ****
        // *************************************************************
        //
        // ****         WaitListStart()         ****
        /// <summary>
        /// Any actions that are to be performed by the hub thread immediatedly
        /// before it first enters the WaitListen() method can be implememented here
        /// by overriding this method. Overloading method must finally call this method at end.
        /// </summary>
        protected virtual void WaitListenStart()
        {
            m_WaitListenState = WaitListenState.Working;

            m_DiagnoticStopWatch = new Stopwatch();
            m_DiagnoticStopWatch.Start();
            StopwatchTickFreq = Stopwatch.Frequency;

            // TODO: Shouldn't the Starting event be triggered here?  (If gui invokes work nicely?)
            WaitListen();
        }//end WaitListenStart().
        //
        //
        //
        // ****             WaitListen()            ****
        //
        /// <summary>
        /// This method is owned by the hub thread only.
        /// </summary>
        private void WaitListen()
        {
            while (m_WaitListenState != WaitListenState.Stopping)
            {
                //
                // First check my event queue.
                //
                m_WaitListenLoopCount++;							// increment loop counter.
                m_WaitListenState = WaitListenState.Working;
                EventArgs[] eventArgs = null;
                lock (m_HubBaseLock)
                {
                    int nEventsInQueue = m_HubEventQueue.Count;
                    if (nEventsInQueue > 0)
                    {   // Found events in my queue.						
                        // Copy them into array, call event handler.
                        eventArgs = new EventArgs[nEventsInQueue];
                        m_HubEventQueue.CopyTo(eventArgs);
                        m_HubEventQueue.Clear();
                        m_EventCount += nEventsInQueue;				// increment our diagnostic event counter.
                    }
                }

                //
                // Send my events out.
                //
                if (eventArgs != null)
                {   // There were events in the HubQueue.   Process them.
                    // Log.SubmitLogEntry(String.Format("WaitListen(): Handling {0} events in HubQueue.", eventArgs.Length.ToString()), EngineLog.Verbose);
                    HubEventHandler(eventArgs);     // Overriden by sub-class, here is where work gets done.
                    if (m_WaitListenUpdatePeriod > 0) { UpdatePeriodicCheck(); }
                }
                else
                {   // I awoke, but with an empty queue.
                    if (m_WaitListenUpdatePeriod > 0) { UpdatePeriodicCheck(); }
                }

                //
                // Prepare to Sleep!
                //
                int nMsgsInQueue = 0;   // First check for any new messages.
                lock (m_HubBaseLock) { nMsgsInQueue = m_HubEventQueue.Count; }
                if ((nMsgsInQueue == 0))
                {   // No messages waiting for us in the queue, continue preparing to sleep.
                    if (m_EventCount > m_EventCountMax) UpdateDiagnostics();	// before sleeping update periodics.
                    if (m_WaitListenState != WaitListenState.Stopping)
                    {   // I am not supposed to stop running, prepare to sleep.
                        //
                        // Go to sleep now!
                        //
                        m_WaitListenState = WaitListenState.Waiting;				// set my flag to waiting.
                        long ticksAtSleep = m_DiagnoticStopWatch.ElapsedTicks;		// take note of the time.
                        if (m_WaitListenUpdatePeriod > 0)
                        {   // User has set a update period.  Go to sleep, after setting the alarm clock.                            
                            m_WaitHandle.WaitOne(m_WaitListenUpdatePeriod, false);
                        }
                        else
                        {   // User has no periodic update set. Go peacefully to sleep - without setting alarm clock!                            
                            m_WaitHandle.WaitOne();
                        }
                        m_TicksWhileASleep += (m_DiagnoticStopWatch.ElapsedTicks - ticksAtSleep);	// note the time elapsed since we went to sleep.
                    }//if not stopping.
                }
                else
                {   // New messages have appeared while I was away.
                    m_SleepSkippedCounter++;
                    m_EventsFoundWaitingCounter += nMsgsInQueue;
                    m_WaitHandle.Reset();       // reset wait handle since I will now handle these messages now.
                }
            }//wend not stopping - repeat this main wait loop forever.

            //
            // Exit.
            //
            Finalizing();
            // Stop wait/listen cycle by falling out the bottom.
        }//end WaitListen().
        //
        //
        //
        // ****                 UpdatePeriodicCheck()               ****
        // 
        /// <summary>
        /// Checks whether enough time has passed to require a call to PeriodicUpdate().
        /// </summary>
        private void UpdatePeriodicCheck()
        {
            if (DateTime.Now.CompareTo(m_NextUpdateTime) > 0)
            {   // Now is after the time we are suppose to do an update.                 
                UpdatePeriodic();       // call the update routine.
                m_EventCount++;			// periodic update should count as an event!
                m_NextUpdateTime = DateTime.Now.AddMilliseconds(m_WaitListenUpdatePeriod);
            }
        }//end UpdatePeriodicCheck().
        //
        //
        //
        //
        //
        #endregion//private methods


        #region Diagnostic Members & Methods
        // *************************************************************
        // ****               Diagnostic Methods					****
        // *************************************************************
        //
        //
        // Diagnostic controls	- added April 2012
        //
        private Stopwatch m_DiagnoticStopWatch;				// tracks time between diagnostic updates.
        private double StopwatchTickFreq = 1;				// ticks per second. Update this value at start up. (Hardware dependent.)		
        // internal counters	
        private long m_TicksWhileASleep = 0;				// cummulative ticks elapsed that we were asleep.
        private int m_EventCount = 0;						// total number of events processed since last diagnostic update.
        protected int m_EventCountMax = 500;				// number of events before diagnostic update is called; controls frequency of updates.
        private double m_WaitListenLoopCount = 0;			// total times we entered the WaitListen loop.
        private double m_SleepSkippedCounter = 0;			// times we found events waiting for us, and skipped sleeping.
        private double m_EventsFoundWaitingCounter = 0;		// number of events that were found waiting.

        // Diagnostic Results
        public double HubEventFrequency = 0;			// Number of events processed per second.
        public double HubWorkLoad = 0;					// Fraction of time hub thread is working.
        public double HubEventWaitingFraction = 0;		// Fraction of events that had to wait for thread to return from work to be collected.
        public double HubSleepSkippedFraction = 0;		// Fraction of time we couldn't sleep 
        public double HubEventClusterAverageSize = 0;
        //
        //
        //
        // ****				Update Diagnostics						****
        //
        /// <summary>
        /// Updates average load calculations and other diagnostics.  Resets StopWatch.
        /// </summary>
        private void UpdateDiagnostics()
        {
            // Compute observables
            double elapsedTicks = m_DiagnoticStopWatch.ElapsedTicks;	// total time elapsed since last update.
            double elapsedSecs = elapsedTicks / StopwatchTickFreq;		// total time elapsed (in seconds).
            HubWorkLoad = 1.0 - (m_TicksWhileASleep / elapsedTicks);	// (total-asleep) / total = ratio spent working.
            HubEventFrequency = m_EventCount / elapsedSecs;				// event frequency in Hz.
            HubEventWaitingFraction = m_EventsFoundWaitingCounter / m_EventCount;	// fraction of events that had to wait.
            HubSleepSkippedFraction = m_SleepSkippedCounter / m_WaitListenLoopCount;
            HubEventClusterAverageSize = m_EventCount / m_WaitListenLoopCount;		// events per loop

            // Reset counters and exit.
            DiagnosticReport();											// Call overridable reporting method.
            m_DiagnoticStopWatch.Reset();								// stop and reset to zero.
            m_EventCount = 0;
            m_WaitListenLoopCount = 0;
            m_SleepSkippedCounter = 0;
            m_EventsFoundWaitingCounter = 0;
            m_TicksWhileASleep = 0;
            m_WaitListenLoopCount = 0;
            m_SleepSkippedCounter = 0;
            m_DiagnoticStopWatch.Start();
        }//UpdateDiagnostics()
        //
        //
        /// <summary>
        /// Inheriting classes should override this and read the above diagnostic variables.
        /// 
        /// </summary>
        protected virtual void DiagnosticReport()
        {

        }//DiagnosticReport().
        //
        #endregion// diagnostics methods



        #region Methods to override
        // *********************************************************************
        // ****                     Methods to Override                     ****
        // *********************************************************************
        //
        //
        //
        //
        //
        // ****         HubEventHandler()               ***
        //
        /// <summary>
        /// This method should be overrided by the sub-class otherwise, events that 
        /// are received by this HubBase will be sent into this method and into oblivion.
        /// </summary>
        /// <param name="e"></param>
        protected abstract void HubEventHandler(EventArgs[] e);
        //
        //
        //
        // ****             UpdatePeriodic()            ****
        //
        /// <summary>
        /// This method may be implemented by a sub-class if there are periodic tasks
        /// to be performed in that class.  The period may set.
        /// The implementation of this is optional.
        /// </summary>
        protected virtual void UpdatePeriodic() { }
        //
        //
        //
        //
        // ****                 Finalizing()                    ****
        //
        /// <summary>
        /// This is called by the internal hub thread, just after it falls out the bottom
        /// of the WaitListen() loop, immediately prior to dying forever.
        /// </summary>
        protected virtual void Finalizing()
        {
            // Log.SubmitLogEntry("Stopping.", EngineLog.Highest);
            m_WaitHandle.Close();
            m_WaitHandle = null;
            m_WaitListenState = WaitListenState.ReadyToStart;
            m_Thread = null;

            // TODO: Signal to subscribers my state change?
            //HubEventHandler(null);         // signal my state change. 
        }//end Finalizing().
        //
        //
        //
        #endregion//method to override! methods



    }//end of HubBase class
}//namespace

