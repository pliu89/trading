using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace UV.Lib.Utilities
{
    using UV.Lib.Hubs;
    /// <summary>
    /// This class allows hubs to push events onto a queue that will be 
    /// resubmitted automatically at a specified later time.
    /// This is a simpler version of the EventWaitQueue. 
    /// (EventWaitQueue also tracks multiple failures automatically.)
    /// Usage:
    ///     1) Create an instance of this object.  Setting parameters like
    ///     the min seconds between resubmissions, if desired.
    ///     2) Subscribe to the ResubmissionReady event, for a hub simply set
    ///         eventWaitQueueList.ResubmissionReady += new EventHandler( HubEventEnqueue );
    ///     3) Then, push eventArgs onto the queue using 
    ///         eventWaitQueueList.AddPending( eventArg, 2 );
    ///        where 2 indicates the eventArg will be resubmitted in two seconds.
    /// TODO: 
    ///     1) Can we set the timer to turn on/off depending on status of queue?
    ///     2) Can we keep track of failures?
    /// Created: 19 November 2013
    /// </summary>
    public class EventWaitQueueLite : IDisposable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // Public variables
        public event EventHandler ResubmissionReady;                                                    // event is ready for a retry.

        // Services
        private System.Threading.Timer m_Timer = null;
        private LogHub Log = null;                                                                      // optional log.

        // Queues
        private ConcurrentQueue<IncomingEventArgs> m_InQueue = new ConcurrentQueue<IncomingEventArgs>();// external thread will push onto this.
        private RecycleFactory<IncomingEventArgs> m_IncomingFactory = new RecycleFactory<IncomingEventArgs>();

        // private controls and queues
        private SortedList<DateTime, EventArgs> m_Queue = new SortedList<DateTime, EventArgs>();    // our time-ordered queue.

        private int m_QueueLength = 0;
        private double m_MinSecondsBetweenUpdates = 1;

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="aLog">Optional logging allowed.</param>
        public EventWaitQueueLite(LogHub aLog = null)
        {
            this.Log = aLog;

            int timerPeriod = (int)Math.Round(1000 * m_MinSecondsBetweenUpdates);
            m_Timer = new System.Threading.Timer(new System.Threading.TimerCallback(Timer_CallBack),
                       null, System.Threading.Timeout.Infinite, timerPeriod);
            int timerDelayStart = timerPeriod;
            m_Timer.Change(timerDelayStart, timerPeriod);       // this starts the timer.

        }//constructor
        //
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        // *************************************
        // ****         Pending Count       ****
        // *************************************
        /// <summary>
        /// Number of events in the pending queue.
        /// </summary>
        public int PendingCount
        {
            get { return (m_InQueue.Count + m_QueueLength); }
        }
        //
        //
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        // *****************************************
        // ***          AddPending()            ****
        // *****************************************
        /// <summary>
        /// Any thread can push onto this queue as an event to be resubmitted at later time.
        /// </summary>
        /// <param name="eventArg"></param>
        /// <param name="secsToWait"></param>
        public void AddPending(EventArgs eventArg, int secsToWait)
        {
            if (ResubmissionReady == null)
            {
                Exception ex = new Exception("EventWaitQueue: Cannot add to queue with no subscribers to ResubmissionReady event.");
                throw ex;
            }
            if (eventArg == null)
            {
                Exception ex = new Exception("EventWaitQueue: Cannot add a null EventArg to queue.");
                throw ex;
            }
            // Add to input queue
            IncomingEventArgs e = m_IncomingFactory.Get();
            e.EventArg = eventArg;
            e.DelayInSeconds = secsToWait;
            m_InQueue.Enqueue(e);
            if (Log != null)
                Log.NewEntry(LogLevel.Minor, "EventWaitQueue: Adding {0} to queue.", e);
        } // Add()
        //
        //
        //
        // *********************************************
        // ****             Dispose()               ****
        // *********************************************
        public void Dispose()
        {
            if (m_Timer != null)
            {
                m_Timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                m_Timer.Dispose();
                m_Timer = null;
            }
        }// Dispose()
        //
        //
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //
        // *****************************************************
        // ****             ProcessNewPending()             ****
        // *****************************************************
        /// <summary>
        /// When new EventArgs are added to the InQueue, they are processed here
        /// and pushed onto internal pending Queue.
        /// </summary>
        private void ProcessNewPending()
        {
            IncomingEventArgs eventArgs;
            while (m_InQueue.TryDequeue(out eventArgs))                                     // remove from public queue
            {
                IncomingEventArgs e = (IncomingEventArgs)eventArgs;
                DateTime nextTry = DateTime.Now.AddSeconds(e.DelayInSeconds);
                // Elaborate counting used by EventWaitQueue.
                // TODO: Lets implement something simpler, like a counter for failed attempts inside event itself?
                // Add event to queue.  
                while (m_Queue.ContainsKey(nextTry))
                    nextTry = DateTime.Now.AddTicks(1L);            // smallest increment of counter.
                m_Queue.Add(nextTry, e.EventArg);                   // pass in the original event.
                m_IncomingFactory.Recycle(e);                        // return to recycle bin.
            }
            m_QueueLength = m_InQueue.Count;                        // this is threadsafe I believe.
        } // ProcessNewPending()
        //
        //
        // *********************************************
        // ****         Process Pending()           ****
        // *********************************************
        private void ProcessPending()
        {
            DateTime now = DateTime.Now;
            while (m_Queue.Count > 0 && now.CompareTo(m_Queue.Keys[0]) > 0) // check time of first event, perhaps resubmit it.
            {
                EventArgs e;
                if (m_Queue.TryGetValue(m_Queue.Keys[0], out e))
                {
                    m_Queue.RemoveAt(0);
                    if (ResubmissionReady != null)
                    {
                        if (Log != null)
                            Log.NewEntry(LogLevel.Minor, "EventWaitQueue: Resubmitting {0}. {1} events still waiting.", e, this.m_Queue.Count);
                        ResubmissionReady(this, e);                                 // resubmit this event.
                    }
                }
            }//wend
            m_QueueLength = m_InQueue.Count;
        } // ProcessPending()
        //
        //
        //
        #endregion//Private Methods


        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        // *********************************************
        // ****         Timer_CallBack()            ****
        // *********************************************
        /// <summary>
        /// The thread that call this method is an arbitrary pool thread.
        /// </summary>
        /// <param name="stateInfo"></param>
        private void Timer_CallBack(object stateInfo)
        {
            if (m_Timer == null)
                return;
            m_Timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);       // pause timer
            lock (m_Queue)                                              // This lock allows only one timer thread to enter.
            {   
                if (m_InQueue.Count > 0 || m_Queue.Count > 0)
                {
                    if (m_InQueue.Count > 0)                            // are there new events in the InQueue?
                        ProcessNewPending();
                    if (m_Queue.Count > 0)
                        ProcessPending();
                }
            }
            int timerPeriod = (int)Math.Round(1000 * m_MinSecondsBetweenUpdates);
            m_Timer.Change(timerPeriod, timerPeriod);
        }// Timer_Callback()
        //
        //
        #endregion//Event Handlers


        #region Pending EventArgs class
        //
        //
        private class IncomingEventArgs : EventArgs
        {
            //
            // Members
            //
            public EventArgs EventArg = null;
            public int DelayInSeconds;

            //
            // Public methods
            //
            public override string ToString()
            {
                if (this.EventArg == null)
                    return "empty";
                else
                    return this.EventArg.ToString();
            }//ToString()
        }
        //
        #endregion//Private Methods


    }//end class
}
