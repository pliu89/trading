using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
//using System.Timers;
//using System.Threading;

namespace UV.Lib.Utilities
{
    using UV.Lib.Hubs;

    public class EventWaitQueue : IDisposable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        //
        public event EventHandler ResubmissionReady;                        // event is ready for a retry.

        // Control variables
        private int m_FirstAttemptCountMax = 5;                             // times to retry before denoting eventArg as chronic failure.
        private double m_FirstAttempTimeDelaySecs = 10.0;                   // wait time between first attemps        
        private double m_ChronicTimeDelaySecs = 60.0;
        private double m_ChronicTimeDelaySecsMax = 5 * 60.0;                // max time between chronic resubmissions

        // Services
        private System.Threading.Timer m_Timer = null;
        private LogHub Log = null;                                          // optional log.

        // Queues
        private ConcurrentQueue<EventArgs> m_InQueue = new ConcurrentQueue<EventArgs>();    // external thread will push onto this.

        // private queues
        private SortedList<DateTime, EventArgs> m_Queue = new SortedList<DateTime, EventArgs>();    // our time-ordered queue.
        private Dictionary<EventArgs, int> m_QueueCount = new Dictionary<EventArgs, int>();                     // number of times an event has been in our queue.
        private int m_QueueLength = 0;

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public EventWaitQueue(LogHub aLog)
        {
            this.Log = aLog;

            int timerPeriod = (int) Math.Round(1000 * m_FirstAttempTimeDelaySecs);
            m_Timer = new System.Threading.Timer(new System.Threading.TimerCallback(Timer_CallBack),
                       null, System.Threading.Timeout.Infinite, timerPeriod);
            int timerDelayStart = timerPeriod;
            m_Timer.Change(timerDelayStart, timerPeriod);               // starting the timer.

        }
        //
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        public int PausedCount
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
        /// <summary>
        /// Any thread can push onto this queue.
        /// </summary>
        /// <param name="e"></param>
        public void Add(EventArgs e)
        {
            if (ResubmissionReady == null)
            {
                Exception ex = new Exception("Cannot add to queue with no subscribers to ResubmissionReady event.");
                throw ex;
            }
            if (e == null)
            {
                Exception ex = new Exception("Cannot add a null EventArg to queue.");
                throw ex;            
            }
            m_InQueue.Enqueue(e);
            if (Log != null)
                Log.NewEntry(LogLevel.Minor, "EventWaitQueue: Adding {0} to queue.", e);
    
        } // Add()
        //
        //
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
        //
        //
        //
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        /// <summary>
        /// When new fills are found in the InQueue, they are processed and pushed onto
        /// the working Queue here.
        /// </summary>
        private void ProcessNewEvents()
        {     
            EventArgs e;
            while ( m_InQueue.TryDequeue(out e) )                                                       // remove from public queue
            {
                int count = 0;
                DateTime nextTry;
                if (m_QueueCount.TryGetValue(e, out count))
                {   // Yes.  This is an event we have seen before.
                    count++;                                                                            // increment his counter
                    m_QueueCount[e] = count;                                                            // update his counter                    
                    double seconds;
                    if (count <= m_FirstAttemptCountMax)                                                 // Is the failure chronic yet?
                        seconds = m_FirstAttempTimeDelaySecs;
                    else
                    {
                        seconds = Math.Min(m_ChronicTimeDelaySecs * (count - m_FirstAttemptCountMax), m_ChronicTimeDelaySecsMax);  // yes - chronic
                        if ( Log != null )
                            Log.NewEntry(LogLevel.Minor,"EventWaitQueue: Chronic event {0}.  Wait {1} minutes.",e,(seconds/60.0).ToString("0.0"));
                    }
                    nextTry = DateTime.Now.AddSeconds(seconds);
                }
                else
                {
                    m_QueueCount.Add(e,1);
                    nextTry = DateTime.Now.AddSeconds(m_FirstAttempTimeDelaySecs);
                }
                
                // Add event to queue.  We allow identical events to appear here more than once. But their counter will advance super fast, when that happens..
                while (m_Queue.ContainsKey(nextTry))
                    nextTry = DateTime.Now.AddTicks(1L);            // smallest increment
                m_Queue.Add(nextTry, e);                
            }
            m_QueueLength = m_InQueue.Count;
        } // ProcessNewEvents()
        //
        //
        //
        //
        private void ProcessWaitingEvents()
        {
            DateTime now = DateTime.Now;
            while (m_Queue.Count > 0 && now.CompareTo(m_Queue.Keys[0]) > 0)         // check time of first event, perhaps resubmit it.
            {
                EventArgs e;
                if (m_Queue.TryGetValue(m_Queue.Keys[0], out e))
                {
                    m_Queue.RemoveAt(0);
                    if (ResubmissionReady != null)
                    {
                        if (Log!=null)
                            Log.NewEntry(LogLevel.Minor, "EventWaitQueue: Resubmitting {0}. {1} events still waiting.", e,this.m_Queue.Count);
                        ResubmissionReady(this, e);                                 // resubmit this event.
                    }
                }
            }//wend
            m_QueueLength = m_InQueue.Count;
        } // ProcessWaitingEvents()
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
        /// <summary>
        /// The thread that call this method is an arbitrary pool thread.
        /// </summary>
        /// <param name="stateInfo"></param>
        private void Timer_CallBack(object stateInfo)
        {
            if (m_Timer == null)
                return;
            if (m_InQueue.Count > 0 || m_Queue.Count > 0)
            {
                m_Timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);       // pause timer
                if (m_InQueue.Count > 0)                                            // are there new events in the InQueue?
                    ProcessNewEvents();
                if (m_Queue.Count > 0)
                    ProcessWaitingEvents();
                int timerPeriod = (int)Math.Round(1000 * m_FirstAttempTimeDelaySecs);                       // Restart the timer.
                m_Timer.Change(timerPeriod, timerPeriod);
            }
        }// Timer_Callback()
        //
        //
        #endregion//Event Handlers

    }
}
