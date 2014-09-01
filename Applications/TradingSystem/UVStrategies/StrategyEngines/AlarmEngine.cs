using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace UV.Strategies.StrategyEngines
{
    using UV.Lib.IO.Xml;
    using UV.Lib.Products;
    using UV.Lib.Hubs;
    using UV.Lib.Engines;    
    using UV.Lib.Utilities.Alarms;
    using UV.Lib.DatabaseReaderWriters.Queries;

    using UV.Strategies.StrategyHubs;


    //using UV.Lib.BookHubs;
    //using UV.Lib.MarketHubs;


    /// <summary>
    /// This
    /// </summary>
    public class AlarmEngine : Engine, IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Pointers to outside objects
        protected Strategy ParentStrategy = null;        

        // Internal controls
        private double[] m_ExitTimeSeconds = new double[3];             //  NaN means don't use that exit state.
        private double m_StartTimeSeconds = 0;                          //
        private int[] m_RandomSlopPercent = new int[3];                 // 0 means NO randomness

        #endregion// members


        #region Constructors & Setup
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public AlarmEngine() : base()
        {

        }
        //
        // *****************************************
        // ****     Setup Initialize()          ****
        // *****************************************
        protected override void SetupInitialize(IEngineHub myEngineHub, IEngineContainer engineContainer, int engineID, bool setupGui)
        {
            base.SetupInitialize(myEngineHub, engineContainer, engineID, setupGui);

            // Keep pointers to important objects.
            this.ParentStrategy = (Strategy)engineContainer;
            
            
            // TODO: Locate the Alarm service.            
        }
        //
        // *****************************************
        // ****         Setup Begin()           ****
        // *****************************************
        /// <summary>
        /// This is called after all Engines have been created, and added to the master
        /// list inside the Strategy (IEngineContainer).
        /// As a convenience, the PricingEngine class locates useful Engines and keeps
        /// pointers to them.
        /// </summary>
        /// <param name="myEngineHub"></param>
        /// <param name="engineContainer"></param>
        public override void SetupBegin(IEngineHub myEngineHub, IEngineContainer engineContainer)
        {
            base.SetupBegin(myEngineHub, engineContainer);
            
            //
            // Subscribe to economic numbers of legs.
            //
            List<InstrumentName> instruments = new List<InstrumentName>();
            foreach (IEngine iEngine in engineContainer.GetEngines())
            {
                if (iEngine is PricingEngine)
                {
                    PricingEngine pricingEngine = (PricingEngine)iEngine;
                    foreach (Lib.MarketHubs.PriceLeg leg in pricingEngine.m_Legs)
                        if (!instruments.Contains(leg.InstrumentName))
                            instruments.Add(leg.InstrumentName);
                }
            }            
            //QueryBase query = null;
            //this.ParentStrategy.StrategyHub.RequestQuery(query, this.QueryResponseHandler, this.ParentStrategy);

            //
            // Subscribe to start/end trading times
            //
            // If StartTime=EndTime => strategy never shuts off.
            if (ParentStrategy.QueryItem != null && ParentStrategy.StrategyHub.m_Alarm != null 
                && ParentStrategy.QueryItem.StartTime.CompareTo(ParentStrategy.QueryItem.EndTime) != 0 )
            {
                Random random = new Random();
                StrategyQueryItem strategyItem = ParentStrategy.QueryItem;
                Alarm alarm = ParentStrategy.StrategyHub.m_Alarm;

                // Collect today's time/date
                DateTime today = ParentStrategy.StrategyHub.GetLocalTime().Date;       // collect nearest day
                DateTime day = today;
                if (day.CompareTo(strategyItem.StartDate) < 0)
                    day = strategyItem.StartDate;
                AlarmEventArg eventArg;                                         // my callback event arg.
                while (day.CompareTo(strategyItem.EndDate) <= 0)                // Loop thru each day and set start/end alarms.
                {
                    // Create start event.
                    DateTime dt = day.Add(strategyItem.StartTime);
                    eventArg = new AlarmEventArg();
                    eventArg.State = StrategyState.Normal;
                    double secs = m_StartTimeSeconds + m_StartTimeSeconds * (0.01 * random.Next(0 , m_RandomSlopPercent[0]));
                    eventArg.TimeStemp = dt.AddSeconds(secs);                   // start at start time or after.
                    alarm.Set(dt, this.AlarmEventHandler, eventArg);
                    
                    // Create stop event.
                    dt = day.Add(strategyItem.EndTime);                         // exit time (or earlier)
                    double lastSecs = double.MaxValue;
                    for (int i= 0; i<m_ExitTimeSeconds.Length; ++i)
                        if (m_ExitTimeSeconds[i] != double.NaN)
                        {
                            StrategyState state = (StrategyState)(i + (int)StrategyState.ExitOnly);
                            eventArg = new AlarmEventArg();
                            eventArg.State = state;
                            secs = m_ExitTimeSeconds[i] + m_ExitTimeSeconds[i] * (0.01 * random.Next(0, m_RandomSlopPercent[i]));
                            secs = Math.Max(0,Math.Min(secs, lastSecs));        // don't allow this exit level to be earlier than previous one.
                            lastSecs = secs - 1.0;                              // therefore, states padding is strictly decreasing in seconds!
                            DateTime dt2 = dt.AddSeconds(-secs);
                            alarm.Set(dt, this.AlarmEventHandler, eventArg);                        
                        }

                    // Increment day
                    day = day.AddDays(1.0).Date;                                // increment until past endDate or ...
                    if (day.DayOfWeek == DayOfWeek.Saturday && day!=today)    // ... its Saturday (but continue if today is Sat.).
                        break;
                }//wend day                
            }
        }//SetupBegin()
        //
        //
        //       
        #endregion//Constructors


        #region no Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        #endregion//Properties


        #region no Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
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
        //
        /// <summary>
        /// Called by StrategyHub when our queries are ready to be processed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        protected void QueryResponseHandler(object sender, EventArgs eventArgs)
        {

        }
        //
        //
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        protected void AlarmEventHandler(object sender, EventArgs eventArgs)
        {
            AlarmEventArg e = (AlarmEventArg)eventArgs;

        }//AlarmEventHandler()
        //
        //
        #endregion//Private Methods


        #region no Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                      ****
        // *****************************************************************
        //
        //
        #endregion//Event Handlers


        #region IStringifiable
        // *****************************************************************
        // ****                     IStringifiable                      ****
        // *****************************************************************
        string IStringifiable.GetAttributes()
        {
            return string.Empty;   
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            return null;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            double x;
            //int n;
            foreach(KeyValuePair<string,string> a in attributes)
            {
                if (a.Key.Equals("RandomSlopPercent", StringComparison.CurrentCultureIgnoreCase))
                {
                    for (int i = 0; i < m_RandomSlopPercent.Length; i++)
                        m_RandomSlopPercent[i] = 0;                      // means "don't use"
                    string[] s = a.Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    int imax = Math.Min(s.Length, m_RandomSlopPercent.Length);   // right now only three exit times allowed
                    for (int i = 0; i < imax; i++)
                    {   // Least aggressive exits are first
                        if (double.TryParse(s[i], out x))
                            m_RandomSlopPercent[i] = Math.Max(0,(int) Math.Round(x));
                    }
                }
                else if (a.Key.Equals("StartTimePaddings", StringComparison.CurrentCultureIgnoreCase))
                {
                    string[] s = a.Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (s.Length > 0 && double.TryParse(s[0], out x))
                        m_StartTimeSeconds = x;
                }
                else if (a.Key.Equals("StopTimePaddings", StringComparison.CurrentCultureIgnoreCase))
                {
                    for (int i = 0; i < m_ExitTimeSeconds.Length; i++)
                        m_ExitTimeSeconds[i] = double.NaN;                      // means "don't use"
                    string[] s = a.Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    int imax = Math.Min(s.Length, m_ExitTimeSeconds.Length);   // right now only three exit times allowed
                    for (int i = 0; i < imax; i++)
                    {   // Least aggressive exits are first
                        if (double.TryParse(s[i], out x))
                            m_ExitTimeSeconds[i] = x;
                    }
                }
            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
            
        }
        //
        //
        //
        #endregion//Event Handlers


        #region Class
        // *****************************************************************
        // ****                     Class Handlers                      ****
        // *****************************************************************
        //
        private class AlarmEventArg : EventArgs
        {
            public DateTime TimeStemp;
            public StrategyState State;
        }
        //
        //
        /// <summary>
        /// This is temporary.  
        /// In future consider putting strategy states somewhere more central to strategy.
        /// </summary>
        private enum StrategyState
        {
            Normal = 0,

            // Exits
            ExitOnly = 100,                     // 
            ExitAggressive = 101,               // Note:  ExitAggressive = 1 + (int)ExitOnly (so its one level faster).
            ExitNow = 102                       //      Keep this feature so we can increase aggressiveness easily.

        }//StrategyState
        //
        //
        //
        #endregion//Class Handlers

    }//end class
}
