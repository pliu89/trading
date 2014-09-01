using System;
using System.Collections.Generic;
using System.Timers;

namespace UV.Lib.Utilities.Alarms
{
    using UV.Lib.Utilities;

    /// <summary>
    /// Basic alarm clock that allows users to set time that they 
    /// want a call back.  They provide the time, delegate to call and
    /// the EventArg to be passed to them.
    /// This is thread safe.
    /// </summary>
    public class Alarm : IDisposable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        //
        private Timer m_Timer = null;
        private double m_SlopSecs = 5.0;                               // trigger events within five seconds

        //
        // Event processing
        //
        private object m_EventListLock = new object();
        private SortedList<DateTime, AlarmEventArgs> m_EventList = null;
        private RecycleFactory<AlarmEventArgs> m_AlarmFactory = null;

        // 
        public delegate DateTime GetCurrentTime();
        private GetCurrentTime m_GetCurrentTime = null;             // The function to call to get the time.
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public Alarm() 
        {
            m_Timer = new Timer();
            m_Timer.AutoReset = false;
            m_Timer.Elapsed += Timer_Elapsed;

            m_EventList = new SortedList<DateTime, AlarmEventArgs>();
            m_AlarmFactory = new RecycleFactory<AlarmEventArgs>();

            this.SetTimeDelegate( GetDefaultTime );
        }
        //
        //
        //
        // *************************************
        // ****         Dispose()           ****
        // *************************************
        //
        private bool m_IsDisposed = false;
        //
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        //
        protected virtual void Dispose(bool isDisposing)
        {
            if (!m_IsDisposed)
            {
                if (isDisposing)
                {
                    if (m_Timer != null)
                    {
                        m_Timer.Dispose();
                        m_Timer = null;
                    }
                    lock(m_EventListLock)
                    {
                        foreach (AlarmEventArgs e in m_EventList.Values)
                            e.Clear();
                    }
                    if (m_AlarmFactory != null)
                    {   // Do we want to implement disposible for alarm factory?
                        //m_AlarmFactory.Di
                        m_AlarmFactory = null;
                    }
                }
                m_IsDisposed = true;
            }
        }
        //
        //       
        #endregion//Constructorsr


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        public double SlopSeconds
        {
            get { return m_SlopSecs; }
            set
            {
                if (value > 0)
                    m_SlopSecs = value;
            }
        }
        //
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        // *********************************
        // ****         Set()           ****
        // *********************************
        public void Set(DateTime requestedAlarmTime, EventHandler delegateToCall, EventArgs eventArgToReturn)
        {
            // Validate time
            DateTime now = m_GetCurrentTime();             //DateTime.Now;
            if (requestedAlarmTime.CompareTo(now) <= 0)
                return;                                 // alarm time must be in future!

            // Create
            AlarmEventArgs e = m_AlarmFactory.Get();
            e.Time = requestedAlarmTime;
            e.Delegate = delegateToCall;
            e.Event = eventArgToReturn;

            lock(m_EventListLock)
            {   
                // Create a unique EventList key.
                DateTime eventTime = e.Time;
                while (m_EventList.ContainsKey(eventTime))
                    eventTime = eventTime.AddTicks(1L);
                // Add entry with unique key.
                m_EventList.Add(eventTime, e);
                if (m_EventList.IndexOfValue(e) == 0)
                {   // This event is in front of list. 
                    // This event will be the nearest to trigger.
                    m_Timer.Interval = Math.Round(eventTime.Subtract(now).TotalMilliseconds);
                    m_Timer.Enabled = true;
                }
            }//unlock()

        }//Set()
        //
        //
        public void SetTimeDelegate(GetCurrentTime d)
        {
            m_GetCurrentTime = d;
        }
        //
        // *********************************************
        // ****         GetDefaultTime()            ****
        // *********************************************
        /// <summary>
        /// Get the DateTime.
        /// </summary>
        /// <returns></returns>
        private DateTime GetDefaultTime()
        {
            return DateTime.Now;
        }//GetDefaultTime()
        //
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
        //
        //
        // *****************************************
        // ****         Timer_Elapsed()         ****
        // *****************************************
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            m_Timer.Enabled = false;                                        // disable the timer immediately!
            DateTime now = m_GetCurrentTime();
            // Collect all alarm events within now and the slop time.
            Queue<AlarmEventArgs> alarmList = new Queue<AlarmEventArgs>();    // local var allows each worker thread its own list!
            lock (m_EventListLock)
            {
                DateTime slopDateTime = now.AddSeconds(m_SlopSecs);           // we will trigger all events within next few seconds...

                while (m_EventList.Count > 0 && m_EventList.Keys[0].CompareTo(slopDateTime) <= 0)
                {
                    AlarmEventArgs alarmEventArg = m_EventList.Values[0];   // take the next event off now.
                    alarmList.Enqueue(alarmEventArg);                           // hold onto it.
                    m_EventList.RemoveAt(0);                                // remove from list
                }
            }

            // Trigger the events.
            while (alarmList.Count > 0)
            {
                AlarmEventArgs alarmEventArg = alarmList.Dequeue();
                if (alarmEventArg.Delegate != null)
                    alarmEventArg.Delegate(this,alarmEventArg.Event);
                alarmEventArg.Clear();
                m_AlarmFactory.Recycle(alarmEventArg);
            }            

            // Determine next alarm to set.
            bool isEnableTimer = false;
            bool isRefireNow = false;
            lock (m_EventListLock)
            {
                if (m_EventList.Count > 0)
                {
                    isEnableTimer = true;
                    DateTime nextTime = m_EventList.Keys[0];
                    TimeSpan ts = nextTime.Subtract(now);
                    if (ts.TotalSeconds >= m_SlopSecs)
                        m_Timer.Interval = Math.Round(ts.TotalMilliseconds);
                    else
                        isRefireNow = true;
                }
            }
            m_Timer.Enabled = isEnableTimer;
            if (isRefireNow)
                Timer_Elapsed(sender, e);
        }//Timer_Elapsed()
        //
        //
        #endregion//Private Methods


        #region Private Class AlarmEventArgs
        // *****************************************************************
        // ****             Class Alarm Event Args                      ****
        // *****************************************************************
        //
        private class AlarmEventArgs : EventArgs
        {
            //
            // Members
            //
            public DateTime Time;
            public EventHandler Delegate = null;
            public EventArgs Event = EventArgs.Empty;
            //
            // Constructor
            //
            public AlarmEventArgs()
            {
            }
            //
            // Methods
            //
            public void Clear()
            {
                this.Delegate = null;
                this.Event = EventArgs.Empty;
            }
        }// AlarmEventArgs class
        //
        //
        //
        //
        //
        #endregion//Public Classes



    }//end class
}
