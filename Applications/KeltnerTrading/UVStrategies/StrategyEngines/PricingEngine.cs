using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies.StrategyEngines
{
    using UV.Lib.IO.Xml;
    using UV.Lib.Products;
    using UV.Lib.Hubs;
    using UV.Lib.Engines;
    using UV.Lib.Fills;
    using UV.Lib.OrderBooks;
    using UV.Lib.BookHubs;
    using UV.Lib.DatabaseReaderWriters.Queries;     // Historic data handling
    using UV.Lib.MarketHubs;
    using UV.Lib.Utilities.Alarms;

    using UV.Strategies.StrategyHubs;


    /// <summary>
    /// </summary>
    public class PricingEngine : Engine, IStringifiable, IPricingEngine
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // My strategy and other services.
        //
        public Strategy ParentStrategy = null;
        protected LogHub Log = null;
        protected ZGraphEngine m_GraphEngine = null;
        protected List<IMarketSubscriber> m_MarketSubscribers = null;
        protected ExecutionRemote m_IOrderEngineRemote;
        //
        // Pricing Engine definition
        //
        public List<PriceLeg> m_Legs = new List<PriceLeg>();    // Convenient place to store legs with markets of interest
        public ImplMarket ImpliedMarket = null;                 // my impl market instr


        //
        // Constants
        public const int BidSide = UV.Lib.Utilities.QTMath.BidSide;
        public const int AskSide = UV.Lib.Utilities.QTMath.AskSide;

        //
        // Alarm clock and time variables
        //
        protected Alarm Alarm = new Lib.Utilities.Alarms.Alarm();    // protected so model can use same alarm.
        protected int EventPaddingSeconds = -1;                      // -1 means we don't care about events
        protected TimeSpan TradingStartTime;
        protected TimeSpan TradingEndTime;
        private List<TimeSpan> CustomEvents = new List<TimeSpan>();  // times to avoid trading during, uses same blocking time as event

        //
        // TradingStateFlags    - eventually this should be turned into an enum
        //
        protected bool IsBlockedForCustomEvent;                     // custom event blocking defined by user
        protected bool IsBlockedForEconomicEvent;                   // are we withing the blocking period of time around events for the model
        protected bool IsTradingPeriod;                             // are we within the trading hours of the model

        #endregion// members

        #region Constructors & Setup
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public PricingEngine()
            : base()
        {

        }
        //
        // *****************************************
        // ****     Setup Initialize()          ****
        // *****************************************
        protected override void SetupInitialize(IEngineHub myEngineHub, IEngineContainer engineContainer, int engineID, bool setupGui)
        {
            base.SetupInitialize(myEngineHub, engineContainer, engineID, setupGui);
            //
            // Keep pointers to important objects.
            //
            this.ParentStrategy = (Strategy)engineContainer;
            this.Log = ((Hub)myEngineHub).Log;                                  // Pricing engines tend to log a lot, so keep a pointer.            
            DateTime startTime = ParentStrategy.StrategyHub.GetLocalTime();

            if (!Strategy.TryGetOrderEngineRemote(engineContainer.GetEngines(), out m_IOrderEngineRemote))
                Log.NewEntry(LogLevel.Error, "{0}:{1} was unable to find IOrderEngine: This is a critical error", ParentStrategy.Name, m_EngineName);       //throw exception?

            RequestEconomicData(startTime.AddHours(-1), startTime.AddDays(1));   // get data from 1 hr ago to the following day.
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

            // Locate engines we need.
            m_MarketSubscribers = new List<IMarketSubscriber>();
            foreach (IEngine iEngine in engineContainer.GetEngines())
            {
                if (iEngine is ZGraphEngine)
                    m_GraphEngine = (ZGraphEngine)iEngine;                  // PricingEngines also plot a lot.
                else if (iEngine is IMarketSubscriber)
                    m_MarketSubscribers.Add((IMarketSubscriber)iEngine);
            }

            //
            // Subscribe to Pricing instruments.
            //
            List<InstrumentName> instruments = new List<InstrumentName>();
            foreach (PriceLeg leg in m_Legs)                                // Subscribe to each "PricingLeg" object.
                instruments.Add(leg.InstrumentName);
            this.ParentStrategy.StrategyHub.SubscribeToMarketInstruments(instruments, this.ParentStrategy);

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
        #endregion//Properties

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        //
        //
        // *********************************************
        // ****     GetFillAttributeString()        ****
        // *********************************************
        /// <summary>
        /// Whenever a fill is passed to the PricingEngine, it can override this function
        /// to return a string that will be written the Fills Table in the database.
        /// This is useful for the model to "explain" the meaning of its fill, or its internal state
        /// for later studying.
        /// </summary>
        /// <returns></returns>
        public virtual string GetFillAttributeString()
        {
            return string.Empty;                        // default behavior is to write nothing.
        }
        //
        //
        // *********************************************
        // ****             ToString()              ****
        // *********************************************
        /// <summary>
        /// Provides details of this engine.
        /// </summary>
        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            s.AppendFormat("#{0} PricingEngine", this.EngineID);
            foreach (PriceLeg leg in this.m_Legs)
                s.AppendFormat(" {0}", leg.InstrumentName);
            return s.ToString();
        }
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
        // ****            TryLocatePricingLegs             ****
        // *****************************************************
        /// <summary>
        /// The LegID's inside are set to -1 at construction time to denote that we have not 
        /// received events yet for those legs.
        /// </summary>
        /// <param name="marketBook">Most recent marketbook.</param>
        /// <returns>true if all legs have been found in mkt book.</returns>
        protected bool TryLocatePricingLegs(Book marketBook)
        {
            int unknownLegs = 0;
            for (int leg = 0; leg < m_Legs.Count; ++leg)                    // This is the old way of doing things,                 
            {                                                               // where we actually searched for instrtument.
                if (m_Legs[leg].MarketID == -1)
                {
                    unknownLegs += 1;                                       // now, assume we won't find this instrument.
                    foreach (Market mkt in marketBook.Instruments.Values)
                        if (mkt.Name == m_Legs[leg].InstrumentName && mkt.DeepestLevelKnown > -1)
                        {                                                   // Found the instrument we are looking for.
                            m_Legs[leg].MarketID = mkt.ID;
                            unknownLegs -= 1;                               // remove from counter since we found the instrument.
                            break;
                        }
                }
            }
            if (unknownLegs == 0)
            {   // Now that all legs are known, lets perform the first update
                // for the implied market.
                this.ImpliedMarket = ImplMarket.Create(ParentStrategy.Name);
                this.ImpliedMarket.SetMarket(marketBook, m_Legs);
            }
            // Exit
            return (unknownLegs == 0);
        }//end InitalizeLegs()
        //
        //
        //
        // *****************************************
        // ****         Setup Alarms()          ****
        // *****************************************
        /// <summary>
        /// Called during during the first MarektIntialized call to set up timers for economic events,
        /// and start and stop trading times.  The call backs are sent to the strategy hub to allow 
        /// threadsafety!
        /// </summary>
        public void SetUpAlarms()
        {
            EngineEventArgs eventArgToCopy = new EngineEventArgs();                 // create event arg we can copy that is already set up
            eventArgToCopy.EngineID = m_EngineID;
            eventArgToCopy.EngineContainerID = ParentStrategy.EngineContainerID;
            eventArgToCopy.DataObjectList = new List<object>();
            eventArgToCopy.MsgType = EngineEventArgs.EventType.AlarmTriggered;

            //
            // Alarms for Events.
            //
            if (m_EconomicDataDateTime != null && m_EconomicDataUnique != null && EventPaddingSeconds > -1)
            {
                for (int i = 0; i < m_EconomicDataDateTime.Count; i++)
                { //for each unique time 
                    DateTime dt = m_EconomicDataDateTime[i];
                    //
                    // Create stop trading alert
                    //
                    DateTime stopAlertTime = dt.AddSeconds(-EventPaddingSeconds);
                    Log.NewEntry(LogLevel.Major, "SetUpTimers: {0}:{1} Setting up Economic Event Blocking @ {2} For The Following Events:", ParentStrategy.Name, m_EngineName, stopAlertTime);
                    EngineEventArgs stopEventArgs = eventArgToCopy.Copy();
                    stopEventArgs.Status = EngineEventArgs.EventStatus.EconomicEventStart;
                    foreach (EconomicDataItem economicData in m_EconomicDataUnique)
                    { // find all events at that time and package them into one event arg to be handed back.
                        if (dt.Equals(economicData.TimeStamp))
                        {
                            stopEventArgs.DataObjectList.Add(economicData);
                            Log.NewEntry(LogLevel.Major, "          {0} @ {1}", economicData.EventName, economicData.TimeStamp);
                        }
                    }
                    Alarm.Set(stopAlertTime, ParentStrategy.StrategyHub.HubEventEnqueue, stopEventArgs);

                    //
                    // Create start trading alert - we have to be careful to see that we don't have overlapping blocking periods, and therefore can't send a start trading alarm until later
                    //
                    EngineEventArgs startEventArgs = stopEventArgs.Copy();
                    startEventArgs.Status = EngineEventArgs.EventStatus.EconomicEventEnd;
                    DateTime startAlertTime = dt.AddSeconds(EventPaddingSeconds);                       // usual time when we would stop blocking.
                    int nextEventPtr = i + 1;                                                           // point to next event after this one.
                    while (m_EconomicDataDateTime.Count > nextEventPtr)                                 // loop thru following events, to check for overlapping blocking periods.
                    {
                        DateTime nextEventTime = m_EconomicDataDateTime[nextEventPtr];                  // find the next event time
                        DateTime nextEventBlockingTime = nextEventTime.AddSeconds(-EventPaddingSeconds);// and its start-blocking time,
                        TimeSpan timeDiff = startAlertTime.Subtract(nextEventBlockingTime);
                        if (timeDiff.TotalSeconds > -5)
                        {   // if the next alarm (to stop trading) is within 5 seconds after current start-trading time.
                            startAlertTime = nextEventTime.AddSeconds(EventPaddingSeconds);             // set the start trading time to the next one, and recheck using same process
                            Log.NewEntry(LogLevel.Major, "SetUpTimers: Overlapping blocking periods found. Combining!", ParentStrategy.Name, m_EngineName);
                        }
                        else
                        { // there isn't overlap, we can proceed, no need to check the next time
                            break;                                                                      
                        }
                        nextEventPtr++;
                    }
                    
                    Alarm.Set(startAlertTime, ParentStrategy.StrategyHub.HubEventEnqueue, startEventArgs);

                    if (stopAlertTime.CompareTo(DateTime.Now) <= 0)
                    { // the actual event already happened
                        if (startAlertTime.CompareTo(DateTime.Now) == 1)
                        { // the blocking hasn't passed yet however, so put us in a blocking state!
                            IsBlockedForEconomicEvent = true;
                            Log.NewEntry(LogLevel.Major, "SetUpTimers: {0}:{1} Starting Strategy in Blocking State!", ParentStrategy.Name, m_EngineName);
                        }
                    }
                }

                //////temp debug feature to test call back
                //DateTime alertTime1 = DateTime.Now.AddSeconds(30); // call us back in 30 seconds
                //EngineEventArgs engineEventArgs1 = eventArgToCopy.Copy();
                //engineEventArgs1.Status = EngineEventArgs.EventStatus.EconomicEventStart;
                //engineEventArgs1.DataObjectList.Add(m_EconomicDataUnique[0]);
                //Alarm.Set(alertTime1, ParentStrategy.StrategyHub.HubEventEnqueue, engineEventArgs1);

                Log.NewEntry(LogLevel.Major, "SetUpTimers: {0}:{1} Created timer for {2} Economic Events", ParentStrategy.Name, m_EngineName, m_EconomicDataUnique.Count);
            }
            else
                Log.NewEntry(LogLevel.Major, "SetUpTimers: {0}:{1} Failed To Set Up Any Economic Timer Events", ParentStrategy.Name, m_EngineName);

            //
            // Alarms for strategy Start and End - this may be stupid, why not just pad going far forward? 
            //

            if (TradingStartTime != null)
            { // the model has specified a start time for itself

                DateTime startTradingDateTime = DateTime.Today.Add(TradingStartTime);
                if (TradingStartTime > DateTime.Now.TimeOfDay)
                { // We have a trading start time and it is later than right now.
                    EngineEventArgs startEventArgs = eventArgToCopy.Copy();
                    startEventArgs.Status = EngineEventArgs.EventStatus.TradingStart;
                    Alarm.Set(startTradingDateTime, ParentStrategy.StrategyHub.HubEventEnqueue, startEventArgs);
                    Log.NewEntry(LogLevel.Major, "SetUpTimers: {0}:{1} Created TradingStartTimer for {2}", ParentStrategy.Name, m_EngineName, startTradingDateTime);
                }
                else
                { // trading start time has already passed for today, lets just set a time in case we want to start trading at this time tomorrow
                    EngineEventArgs startEventArgs = eventArgToCopy.Copy();
                    startEventArgs.Status = EngineEventArgs.EventStatus.TradingStart;
                    startTradingDateTime = startTradingDateTime.AddDays(1);                         // adding a day
                    Alarm.Set(startTradingDateTime, ParentStrategy.StrategyHub.HubEventEnqueue, startEventArgs);
                    Log.NewEntry(LogLevel.Major, "SetUpTimers: {0}:{1} Created TradingStartTimer for {2}", ParentStrategy.Name, m_EngineName, startTradingDateTime);
                }
            }

            if (TradingEndTime != null)
            {
                DateTime stopTradingDateTime = DateTime.Today.Add(TradingEndTime);
                if (TradingEndTime > DateTime.Now.TimeOfDay)
                { // we have and end time that is in the future.
                    EngineEventArgs stopEventArgs = eventArgToCopy.Copy();
                    stopEventArgs.Status = EngineEventArgs.EventStatus.TradingEnd;
                    Alarm.Set(stopTradingDateTime, ParentStrategy.StrategyHub.HubEventEnqueue, stopEventArgs);
                    Log.NewEntry(LogLevel.Major, "SetUpTimers: {0}:{1} Created TradingEndTimer for {2}", ParentStrategy.Name, m_EngineName, stopTradingDateTime);
                }
                else
                { // we have an end time that has already passed, lets add a day to it and create one for the following day
                    stopTradingDateTime = stopTradingDateTime.AddDays(1);
                    EngineEventArgs stopEventArgs = eventArgToCopy.Copy();
                    stopEventArgs.Status = EngineEventArgs.EventStatus.TradingEnd;
                    Alarm.Set(stopTradingDateTime, ParentStrategy.StrategyHub.HubEventEnqueue, stopEventArgs);
                    Log.NewEntry(LogLevel.Major, "SetUpTimers: {0}:{1} Created TradingEndTimer for {2}", ParentStrategy.Name, m_EngineName, stopTradingDateTime);
                    IsTradingPeriod = false;
                }
            }


            if (DateTime.Now.TimeOfDay < TradingEndTime && DateTime.Now.TimeOfDay > TradingStartTime)
            { // our start time has already passed, and we are not passed our end time, trade away!
                IsTradingPeriod = true;
            }

            //
            // Model defined Times To Avoid
            // 
            if (CustomEvents.Count != 0)
            { // we have times to avoid
                Log.NewEntry(LogLevel.Major, "SetUpTimers: {0}:{1} Found {2} custom event times to avoid, setting up now", ParentStrategy.Name, m_EngineName, CustomEvents.Count);
                foreach (TimeSpan time in CustomEvents)
                {
                    DateTime eventTime = DateTime.Today.Add(time);

                    //
                    // Set Up Start of Custom Events - Padding by an extra day should always work for our current needs.
                    //
                    DateTime eventBlockStart = eventTime.AddSeconds(-EventPaddingSeconds);
                    EngineEventArgs startEventArgs = eventArgToCopy.Copy();
                    startEventArgs.Status = EngineEventArgs.EventStatus.CustomEventStart;
                    Alarm.Set(eventBlockStart, ParentStrategy.StrategyHub.HubEventEnqueue, startEventArgs);
                    Alarm.Set(eventBlockStart.AddDays(1), ParentStrategy.StrategyHub.HubEventEnqueue, startEventArgs); // create another event the following day just in case we have already past event or are running longer


                    //
                    // Set Up stop of Custom Events 
                    //
                    DateTime eventBlockingEnd = eventTime.AddSeconds(EventPaddingSeconds);
                    EngineEventArgs stopEventArgs = eventArgToCopy.Copy();
                    stopEventArgs.Status = EngineEventArgs.EventStatus.CustomEventEnd;
                    Alarm.Set(eventBlockingEnd, ParentStrategy.StrategyHub.HubEventEnqueue, stopEventArgs);
                    Alarm.Set(eventBlockingEnd.AddDays(1), ParentStrategy.StrategyHub.HubEventEnqueue, stopEventArgs); // create another event the following day just in case we have already past event or are running longer

                }
            }
        }
        //
        //
        //
        #endregion//Private Methods

        #region Historic data utilities
        // *****************************************************
        // ****             Historic Data                   ****
        // *****************************************************
        //
        //
        // Repository for the synchronized data.
        //
        private List<MarketDataQuery> m_HistoricRaw = null;                 // List of all queries returned from db.
        private List<DateTime> m_HistoricDateTime = null;                   // Synchronized time stamps.
        private List<double> m_HistoricStrategyMid = null;                  // mid price of strategy, computed from pricelegs, one-to-one with HistoricDateTime
        private List<MarketDataItem[]> m_HistoricLegMarkets = null;         // markets for each leg, one-to-one with HistoricDateTime
        //
        // *****************************************************
        // ****             Request Historic Data()         ****
        // *****************************************************
        /// <summary>
        /// Called by a model who would like to try and get historical data for is pricing legs during the start up routine.
        /// The query will be then handled and data should be available when the model recieves the MarketInstrumentInitialized
        /// call. Currently only data during trading sessions is returned. It can be accessed by calling TryGetTimeSeries
        /// </summary>
        /// <param name="startLocal">starting time for data in local time zone</param>
        /// <param name="endLocal">ending time for data range</param>
        protected void RequestHistoricData(DateTime startLocal, DateTime endLocal)
        {
            DateTime startUTC = startLocal.ToUniversalTime();
            DateTime endUTC = endLocal.ToUniversalTime();
            const double round = 10.0;
            double rangeHours = Math.Round(round * (endUTC.Subtract(startUTC)).TotalHours) / round;
            Log.BeginEntry(LogLevel.Minor, "PricingEngine.RequestHistoricData: {0}:{1} requesting {2} hours for", ParentStrategy.Name, m_EngineName, rangeHours);
            foreach (PriceLeg pricingLeg in m_Legs)
            {
                Log.AppendEntry(" {0}", pricingLeg.InstrumentName);
                ParentStrategy.StrategyHub.RequestHistoricData(pricingLeg.InstrumentName, startUTC, endUTC, this.AcceptHistoricData, this.ParentStrategy);
            }
            Log.EndEntry();
        }//RequestHistoricData()
        //
        //
        /// <summary>
        /// This overload allows for the user to simply request a number of seconds back.  They need not know anything about sessions, as long
        /// as there is data in the database they will get back the correct number of seconds of data during trading sessions.
        /// </summary>
        /// <param name="endLocal"></param>
        /// <param name="nSeconds"></param>
        protected void RequestHistoricData(DateTime endLocal, int nSeconds)
        {
            DateTime endUTC = endLocal.ToUniversalTime();
            Log.BeginEntry(LogLevel.Minor, "PricingEngine.RequestHistoricData: {0}:{1} requesting {2} minutes for", ParentStrategy.Name, m_EngineName, (int)(nSeconds / 60));
            foreach (PriceLeg pricingLeg in m_Legs)
            {
                Log.AppendEntry(" {0}", pricingLeg.InstrumentName);
                ParentStrategy.StrategyHub.RequestHistoricData(pricingLeg.InstrumentName, endUTC, nSeconds, this.AcceptHistoricData, this.ParentStrategy);
            }
            Log.EndEntry();
        }
        //
        //
        // *************************************************
        // ****         TryGetTimeSeries()              ****
        // *************************************************
        /// <summary>
        /// Typically called during the MarketInitialized() call back.  This method allows for a model to 
        /// collect waiting historic data.
        /// </summary>
        /// <param name="historicDateTime"></param>
        /// <param name="historicStrategy"></param>
        /// <returns></returns>
        protected bool TryGetTimeSeries(out List<DateTime> historicDateTime, out List<double> historicStrategy)
        {
            historicDateTime = null;
            historicStrategy = null;
            //
            // Validate: check there is good data.
            //
            if (m_HistoricStrategyMid == null || m_HistoricDateTime == null)
                return false;
            if (m_HistoricStrategyMid.Count != m_HistoricDateTime.Count)
                return false;
            if (m_HistoricStrategyMid.Count == 0)
                return false;
            //
            // Load a copy of the data into out-going variables.
            //
            //dateTime = new List<DateTime>();
            //timeSeries = new List<double>();
            //dateTime.AddRange(m_HistoricDateTime);
            //timeSeries.AddRange(m_HistoricStrategyMid);
            // Space-saving version:  Give caller the original copy of data.
            historicDateTime = m_HistoricDateTime;
            historicStrategy = m_HistoricStrategyMid;


            return true;
        }// TryGetTimeSeries
        //
        protected bool TryGetTimeSeries(out List<DateTime> historicDateTime, out List<double> historicStrategy, out List<MarketDataItem[]> historicLegMarkets)
        {
            historicDateTime = null;
            historicStrategy = null;
            historicLegMarkets = null;
            //
            // Validate: check there is good data.
            //
            if (m_HistoricStrategyMid == null || m_HistoricDateTime == null)
                return false;
            if (m_HistoricStrategyMid.Count != m_HistoricDateTime.Count)
                return false;
            if (m_HistoricStrategyMid.Count == 0)
                return false;
            //
            // Load a copy of the data into out-going variables.
            //
            //dateTime = new List<DateTime>();
            //timeSeries = new List<double>();
            //dateTime.AddRange(m_HistoricDateTime);
            //timeSeries.AddRange(m_HistoricStrategyMid);
            // Space-saving version:  Give caller the original copy of data.
            historicDateTime = m_HistoricDateTime;
            historicStrategy = m_HistoricStrategyMid;
            historicLegMarkets = m_HistoricLegMarkets;


            return true;
        }// TryGetTimeSeries
        //
        //
        // *****************************************************
        // ****         Accept Historic Data()              ****
        // *****************************************************
        /// <summary>
        /// This is where the data is handed to the pricing engine.
        /// Notes:
        ///     1) HistoricRaw and HistoricLegMarkets contain pointers to the SAME MarketDataItems! 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void AcceptHistoricData(object sender, EventArgs eventArgs)
        {
            if (m_HistoricRaw == null)
                m_HistoricRaw = new List<MarketDataQuery>();           // create a place for the raw data.            
            m_HistoricRaw.Add((MarketDataQuery)eventArgs);             // Store each marketdata as they arrive.

            //
            // Analyze historic data, once all legs are known.
            //
            if (m_HistoricRaw.Count == m_Legs.Count)
            {   // Data for all instruments have been obtained.
                // Create data tables for syncrhonized data.
                m_HistoricDateTime = new List<DateTime>();    // Synchronized time stamps here.
                m_HistoricStrategyMid = new List<double>();           // store "mid" price for strategy (most strategies want this).
                m_HistoricLegMarkets = new List<MarketDataItem[]>();    // store "mid" price for strategy (most strategies want this).

                // Perform data synchronization.
                int[] ptr = new int[m_Legs.Count];                      // Pointers for current time in each time series.
                uint nextTime = 0;                                      // starting time.
                bool isFinished = false;
                while (!isFinished)
                {
                    for (int leg = 0; leg < ptr.Length; ++leg)          // Find the next largest time stamp.
                    {
                        if (m_HistoricRaw[leg].Result == null || m_HistoricRaw[leg].Result.Count == 0)
                            isFinished = true;                          // Done if there is no data.
                        else
                            nextTime = System.Math.Max(nextTime, m_HistoricRaw[leg].Result[ptr[leg]].UnixTime);
                    }
                    if (isFinished)
                        break;                                          // exit early if at least one leg has NO data.

                    // Increment ptrs - until all match
                    bool isAllMatch = true;
                    for (int leg = 0; leg < ptr.Length; ++leg)
                    {
                        while (ptr[leg] < m_HistoricRaw[leg].Result.Count && m_HistoricRaw[leg].Result[ptr[leg]].UnixTime < nextTime)
                            ptr[leg]++;
                        if (ptr[leg] >= m_HistoricRaw[leg].Result.Count)
                        {
                            isFinished = true;                          // one leg has reached the end of its data.  We must stop.
                            isAllMatch = false;                         // therefore, this set of legs does not match.
                        }
                        else
                            isAllMatch = (m_HistoricRaw[leg].Result[ptr[leg]].UnixTime == nextTime) && isAllMatch;
                    }
                    // Add the new synchronized point to our time series collections.
                    if (isAllMatch)
                    {
                        double mid = 0;
                        MarketDataItem[] legsBar = new MarketDataItem[m_Legs.Count];
                        for (int leg = 0; leg < m_Legs.Count; ++leg)
                        {
                            legsBar[leg] = m_HistoricRaw[leg].Result[ptr[leg]]; // store bar for this leg. Note this is the same MarketDataItem (not a copy).

                            // Compute useful strategy markets
                            double bid = legsBar[leg].Price[BidSide];
                            double ask = legsBar[leg].Price[AskSide];
                            double w = m_Legs[leg].PriceMultiplier;
                            mid += w * (bid + ask) * 0.5;
                        }
                        m_HistoricLegMarkets.Add(legsBar);              // save legs
                        m_HistoricStrategyMid.Add(mid);                 // save strategy market
                        DateTime dt = UV.Lib.Utilities.QTMath.EpochToDateTime(nextTime).ToLocalTime();
                        m_HistoricDateTime.Add(dt);                     // save local time stamp.
                    }
                    // Advance our time series.
                    ptr[0]++;                                           // Arbitrarily increment any of the ptrs now, above we will advance all others.
                    if (ptr[0] >= m_HistoricRaw[0].Result.Count)       // Check end of data condition
                        isFinished = true;
                }//wend isFinished
            }
            else
                Log.NewEntry(LogLevel.Minor, "{0}:{1} AcceptHistoricData: Still waiting for additional historic data.", ParentStrategy.Name, m_EngineName);
        }//AcceptHistoricData()
        //
        //
        //
        #endregion//Historic data utilities

        #region Economic Numbers Utilities
        // *****************************************************
        // ****             Economic Numbers                ****
        // *****************************************************
        //
        private List<EconomicDataQuery> m_EconomicDataRaw = null;           // List of all queries returned from db.
        private List<EconomicDataItem> m_EconomicDataUnique = null;         // list of unqiue economic data events.
        private List<DateTime> m_EconomicDataDateTime = null;               // unique datetimes of economic data
        //
        //
        // *****************************************************
        // ****             Request Economic Data()         ****
        // *****************************************************
        /// <summary>
        /// Called by a model who would like to try and get upcoming economic data for is pricing legs during the start up routine.
        /// The query will be then handled and data should be available when the model recieves the MarketInstrumentInitialized
        /// call.  It can then be accessed by calling TryGetEconomicData
        /// </summary>
        /// <param name="startLocal">starting time for data in local time zone</param>
        /// <param name="endLocal">ending time for data range</param>
        protected void RequestEconomicData(DateTime startLocal, DateTime endLocal)
        {
            DateTime startUTC = startLocal.ToUniversalTime();
            DateTime endUTC = endLocal.ToUniversalTime();
            const double round = 10.0;
            double rangeHours = Math.Round(round * (endUTC.Subtract(startUTC)).TotalHours) / round;
            Log.BeginEntry(LogLevel.Minor, "PricingEngine.RequestEconomicData: {0}:{1} requesting upcoming economic data.", ParentStrategy.Name, m_EngineName);
            foreach (PriceLeg pricingLeg in m_Legs)
            {
                Log.AppendEntry(" {0}", pricingLeg.InstrumentName);
                ParentStrategy.StrategyHub.RequestEconomicData(pricingLeg.InstrumentName, startUTC, endUTC, this.AcceptEconomicData, this.ParentStrategy);
            }
            Log.EndEntry();
        }//RequestHistoricData()
        //
        //
        // *****************************************************
        // ****             Accept  Economic Data()         ****
        // *****************************************************
        private void AcceptEconomicData(object sender, EventArgs eventArgs)
        {
            if (m_EconomicDataRaw == null)
                m_EconomicDataRaw = new List<EconomicDataQuery>();          // create a place for the raw data.            
            m_EconomicDataRaw.Add((EconomicDataQuery)eventArgs);                  // Store each marketdata as they arrive.

            //
            // Analyze historic data, once all legs are known.
            //
            if (m_EconomicDataRaw.Count == m_Legs.Count)
            {   // Data for all instruments have been obtained.
                m_EconomicDataDateTime = new List<DateTime>();
                //
                // Create list of all unique economic data releases
                //
                m_EconomicDataUnique = m_EconomicDataRaw[0].Result;          // unqiue data items go here, we will start with the full list from the first leg.
                for (int i = 1; i < m_EconomicDataRaw.Count; i++)
                { // loop through remaining raw data queries to find any unique economic data
                    foreach (EconomicDataItem dataItem in m_EconomicDataRaw[i].Result)
                    { // if we don't have the data in our list add it now.
                        if (!m_EconomicDataUnique.Contains(dataItem))
                            m_EconomicDataUnique.Add(dataItem);
                    }
                }

                //
                // Create list of all unique times with economic data releases (often many release occur simultaneously.
                //
                foreach (EconomicDataItem dataItem in m_EconomicDataUnique)
                {
                    DateTime dateTime = dataItem.TimeStamp.ToLocalTime();  // find all uniquie date times
                    if (!m_EconomicDataDateTime.Contains(dateTime))
                        m_EconomicDataDateTime.Add(dateTime);
                }
            }
            else
                Log.NewEntry(LogLevel.Minor, "{0}:{1} AcceptEconomicData: Still waiting for additional Economic data.", ParentStrategy.Name, m_EngineName);
        }
        //
        //
        // *************************************************
        // ****         TryGetEconomicData()            ****
        // *************************************************
        /// <summary>
        /// During the MarketInitialized() call back, we should call this method to 
        /// collect our waiting economic event data.
        /// </summary>
        /// <param name="economicDateTimes">Unique list of date times for upcoming events</param>
        /// <param name="economicEventdata"></param>
        /// <returns></returns>
        protected bool TryGetEconomicData(out List<DateTime> economicDateTimes, out List<EconomicDataItem> economicEventdata)
        {

            economicDateTimes = null;
            economicEventdata = null;
            //
            // Validate: check there is good data.
            //
            if (m_EconomicDataUnique == null || m_EconomicDataDateTime == null)
                return false;
            if (m_EconomicDataUnique.Count == 0 || m_EconomicDataDateTime.Count == 0)
                return false;
            // Space-saving version:  Give caller the original copy of data.
            economicDateTimes = m_EconomicDataDateTime;
            economicEventdata = m_EconomicDataUnique;
            return true;
        }
        #endregion // Economic Numbers

        #region Events
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        public override void AddSpontaneousEngineEvents(List<EngineEventArgs> eventList)
        {
            base.AddSpontaneousEngineEvents(eventList);                     // add any previously queued events to list.

            //
            // Cluster Market update events.
            //
            // As a convenience, we will send a ClusterUpdate event to change
            // the implied market depth in the GUI display.
            if (this.ImpliedMarket != null)
            {
                EngineEventArgs eArgs = new EngineEventArgs();
                eArgs.MsgType = EngineEventArgs.EventType.ClusterUpdate;
                eArgs.Status = EngineEventArgs.EventStatus.Confirm;
                eArgs.EngineHubName = ParentStrategy.StrategyHub.ServiceName;   // my hub's name.
                eArgs.EngineContainerID = ParentStrategy.EngineContainerID;     // this is my Container ID.  
                eArgs.EngineID = this.EngineID;                                 // this is my ID.

                // Load market depth          
                int n = this.ImpliedMarket.Price[BidSide].Length;
                eArgs.DataA = new double[n];
                eArgs.DataB = new double[n];
                eArgs.DataIntA = new int[n];
                eArgs.DataIntB = new int[n];
                this.ImpliedMarket.Price[BidSide].CopyTo(eArgs.DataB, 0);
                this.ImpliedMarket.Price[AskSide].CopyTo(eArgs.DataA, 0);
                this.ImpliedMarket.Qty[BidSide].CopyTo(eArgs.DataIntB, 0);
                this.ImpliedMarket.Qty[AskSide].CopyTo(eArgs.DataIntA, 0);

                // load hi-light data
                //eArgs.DataC = new double[2];                            // buy/sell target prices.            
                //eArgs.DataC[0] = m_Target[0];                           // If the Buy/Sell targets are present, update them.
                //eArgs.DataC[1] = m_Target[1];

                // Exit
                eventList.Add(eArgs);
            }
        }//end AddSpontaneousEvents()
        //
        //**************************************************
        //*****              Alarm Triggered            ****
        //**************************************************
        /// <summary>
        /// Called whenever our alarm is triggered on a specific time.  This is called only by the 
        /// strategy hub thread so should be completely threadsafe!
        /// 
        /// Models can completely override this method if desired, or call this base class as well 
        /// as implement more complex structure on top.
        /// 
        /// This can be called for such events as custom events, economic events, and custom trading times
        /// </summary>
        /// <param name="engineEventArgs"></param>
        public virtual void AlarmTriggered(EngineEventArgs engineEventArgs)
        {
            if (engineEventArgs.MsgType == EngineEventArgs.EventType.AlarmTriggered)
            {
                //
                // Trading Period Events
                //

                if (engineEventArgs.Status == EngineEventArgs.EventStatus.TradingStart)
                {
                    IsTradingPeriod = true;
                    Log.NewEntry(LogLevel.Major, "AlarmTriggered: {0}:{1} TradingStart alarm message received", ParentStrategy.Name, m_EngineName);
                }
                else if (engineEventArgs.Status == EngineEventArgs.EventStatus.TradingEnd)
                {
                    IsTradingPeriod = false;
                    Log.NewEntry(LogLevel.Major, "AlarmTriggered: {0}:{1} TradingEnd alarm message received", ParentStrategy.Name, m_EngineName);
                }

                //
                // Economic Events
                //

                else if (engineEventArgs.Status == EngineEventArgs.EventStatus.EconomicEventEnd)
                { // We are clear to trade 
                    Log.NewEntry(LogLevel.Major, "AlarmTriggered: {0}:{1} Economic event alarm message received to resume trading", ParentStrategy.Name, m_EngineName);
                    IsBlockedForEconomicEvent = false;
                }
                else if (engineEventArgs.Status == EngineEventArgs.EventStatus.EconomicEventStart)
                { // We should stop trading for the number
                    IsBlockedForEconomicEvent = true;
                    Log.NewEntry(LogLevel.Major, "AlarmTriggered: {0}:{1} Economic event alarm message received to stop trading", ParentStrategy.Name, m_EngineName);
                    foreach (object o in engineEventArgs.DataObjectList)
                    {
                        if (o.GetType() == typeof(EconomicDataItem))
                        {
                            EconomicDataItem economicData = (EconomicDataItem)o;
                            Log.NewEntry(LogLevel.Major, "EconomicEventName: {0} @ {1}", economicData.EventName, economicData.TimeStamp);
                        }
                    }
                }
                else if (engineEventArgs.Status == EngineEventArgs.EventStatus.CustomEventStart)
                {
                    Log.NewEntry(LogLevel.Major, "AlarmTriggered: {0}:{1} Custom Event Starting", ParentStrategy.Name, m_EngineName);
                    IsBlockedForCustomEvent = true;
                }
                else if (engineEventArgs.Status == EngineEventArgs.EventStatus.CustomEventEnd)
                {
                    Log.NewEntry(LogLevel.Major, "AlarmTriggered: {0}:{1} Custom Event Ending", ParentStrategy.Name, m_EngineName);
                    IsBlockedForCustomEvent = false;
                }
                else
                {
                    Log.NewEntry(LogLevel.Major, "AlarmTriggered: {0}:{1} Uknown Event Economic Event Type Received.", ParentStrategy.Name, m_EngineName);
                }
            }
        }
        #endregion//Event Handlers

        #region IStringifiable
        // *************************************************
        // ****             IStringifiable              ****
        // *************************************************
        public override string GetAttributes()
        {
            StringBuilder s = new StringBuilder(base.GetAttributes());
            return s.ToString();
        }
        public override List<IStringifiable> GetElements()
        {
            List<IStringifiable> elements = base.GetElements();
            if (elements == null)                                   // check this in case base class has elements in future.
                elements = new List<IStringifiable>();
            foreach (PriceLeg priceLeg in this.m_Legs)
                elements.Add(priceLeg);
            // Exit
            return elements;
        }
        public override void SetAttributes(Dictionary<string, string> attributes)
        {
            base.SetAttributes(attributes);
            double x;
            foreach (KeyValuePair<string, string> att in attributes)
            {
                if (att.Key == "EventPaddingSeconds" && double.TryParse(att.Value, out x))
                {
                    EventPaddingSeconds = (int)x;
                }
                else if (att.Key.Equals("TimesToAvoid") || att.Key.Equals("CustomEventTimes"))
                {
                    string[] stringArray = att.Value.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    TimeSpan timeSpan;
                    foreach (string s in stringArray)
                        if (TimeSpan.TryParse(s, out timeSpan))
                            this.CustomEvents.Add(timeSpan);
                        else
                            throw new Exception("Failed to convert Value to TimeSpan.");
                }
            }
        }
        public override void AddSubElement(IStringifiable subElement)
        {
            base.AddSubElement(subElement);
            if (subElement is PriceLeg)
                m_Legs.Add((PriceLeg)subElement);
        }
        #endregion// IStringifiable

        #region IPricingEngine
        // *****************************************************************
        // ****                     IPricingEngine                      ****
        // *****************************************************************
        // *************************************************
        // ****     Market Instrument Initialized()     ****
        // *************************************************
        /// <summary>
        /// Called by the StrategyHub when our market instruments first become
        /// available.  
        /// Notes:
        ///     1) Ensure that classes that extend PricingEngine method, 
        ///     call this base version first!
        /// </summary>
        /// <param name="marketBook"></param>
        /// <returns></returns>
        public override void MarketInstrumentInitialized(Book marketBook)
        {
            // Find my PricingLegs in the market book.
            if (!TryLocatePricingLegs(marketBook))
            {
                throw new Exception(string.Format("Strategy {0}:{1} failed to locate PricingLegs in market.", ParentStrategy.Name, m_EngineName));
            }
            SetUpAlarms();
        }// MarketInstrumentInitialized()
        //
        //
        //
        // ****************************************************
        // ****         Market Instrument Changed()        ****
        // ****************************************************
        /// <summary>
        /// Called whenever the market changes.
        /// Notes:
        ///     1) If you extend the PricingEngine class, and override this
        ///         method, call this base version first!
        /// </summary>
        /// <param name="marketBook"></param>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        public virtual bool MarketInstrumentChanged(Book marketBook, InstrumentChangeArgs eventArgs)
        {
            this.m_IsUpdateRequired = false;                    // reset my flag.
            ImpliedMarket.SetMarket(marketBook, m_Legs);        // update my market depth

            // Inform subscribers of market change.
            bool isMarketSubscriberChanged = false;
            foreach (IMarketSubscriber iMarketSubscriber in m_MarketSubscribers)
                isMarketSubscriberChanged = iMarketSubscriber.MarketInstrumentChanged(marketBook, eventArgs) || isMarketSubscriberChanged;

            return isMarketSubscriberChanged;
        }//end MarketChange().
        //
        //
        //
        // *************************************************
        // ****                 Filled                  ****
        // *************************************************
        /// <summary>
        /// Called after the TradeEngine has been shown the trade event arg, 
        /// and processed it.  
        /// If the TradeEngine considered the TradeEventArg it received to be 
        /// equivalent to a synthetic fill for the Strategy, then it creates a
        /// synthetic fill, and this is passed to the PricingEngine here.
        /// Typically, a pricing model will overload only one of the Filled methods
        /// depending on the type of information it wishes to receive.
        /// </summary>
        /// <param name="syntheticFill"></param>
        public virtual void Filled(Fill syntheticFill)
        {

        }
        /// <summary>
        /// Called after the TradeEngine has been shown the trade event arg, 
        /// and processed it.   This is the raw message that the TradeEngine saw.
        /// Typically, a pricing model will overload only one of the Filled methods
        /// depending on the type of information it wishes to receive.
        /// </summary>
        /// <param name="syntheticOrder"></param>
        public virtual void Filled(SyntheticOrder syntheticOrder)
        {

        }
        #endregion // Ipricing enging


    }//end class
}
