using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies.Engines
{
    using UV.Lib.IO.Xml;
    using UV.Lib.Products;
    using UV.Lib.Hubs;
    using UV.Lib.Engines;
    using UV.Lib.BookHubs;
    using UV.Lib.DatabaseReaderWriters.Queries;     // Historic data handling

    using UV.Lib.MarketHubs;
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

        //
        // Pricing Engine definition
        //
        public List<PriceLeg> m_Legs = new List<PriceLeg>();    // Convenient place to store legs with markets of interest
        public ImplMarket ImpliedMarket = null;                 // my impl market instr

        
        //
        // Constants
        public const int BidSide = UV.Lib.Utilities.QTMath.BidSide;
        public const int AskSide = UV.Lib.Utilities.QTMath.AskSide;
        #endregion// members


        #region Constructors & Setup
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public PricingEngine() : base()
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
            this.Log = ((Hub)myEngineHub).Log;                              // Pricing engines tend to log a lot, so keep a pointer.            
            
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
            if ( ! TryLocatePricingLegs(marketBook))
            {
                throw new Exception(string.Format("Strategy {0} failed to locate PricingLegs in market.", this.ToString() ));
            }            
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
            s.AppendFormat("#{0} PricingEngine",this.EngineID);
            foreach (PriceLeg leg in this.m_Legs)
                s.AppendFormat(" {0}",leg.InstrumentName);
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
            return (unknownLegs == 0 );
        }//end InitalizeLegs()
        //
        //
        //
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
        /// This is a utility call
        /// </summary>
        /// <param name="startLocal">starting time for data in local time zone</param>
        /// <param name="endLocal">ending time for data range</param>
        protected void RequestHistoricData(DateTime startLocal, DateTime endLocal)
        {
            DateTime startUTC = startLocal.ToUniversalTime();
            DateTime endUTC = endLocal.ToUniversalTime();
            const double round = 10.0;
            double rangeHours = Math.Round(round * (endUTC.Subtract(startUTC)).TotalHours) / round;
            Log.BeginEntry(LogLevel.Minor, "PricingEngine.RequestHistoricData: {0} requesting {1} hours for", this.ParentStrategy.Name, rangeHours);
            foreach (PriceLeg pricingLeg in m_Legs)
            {
                Log.AppendEntry(" {0}", pricingLeg.InstrumentName);
                ParentStrategy.StrategyHub.RequestHistoricData(pricingLeg.InstrumentName, startUTC, endUTC, this.AcceptHistoricData, this.ParentStrategy);
            }
            Log.EndEntry();
        }//RequestHistoricData()
        //
        //
        // *************************************************
        // ****         TryGetTimeSeries()              ****
        // *************************************************
        /// <summary>
        /// During the MarketInitialized() call back, we should call this method to 
        /// collect our waiting data.
        /// </summary>
        /// <param name="historicateTime"></param>
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
                            isFinished = true;
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
                Log.NewEntry(LogLevel.Minor, "{0} AcceptHistoricData: Still waiting for additional historic data.", this.ParentStrategy.Name);
        }//AcceptHistoricData()
        //
        //
        //
        #endregion//Historic data utilities


        #region Engine Events
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
        //
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
        /// <summary>
        /// Called after the order hub has updated itself and return a fill
        /// that needs to be processed by the pricing engine.
        /// </summary>
        /// <param name="fill"></param>
        public virtual void Filled(Lib.Fills.Fill fill)
        {
            
        }
        #endregion // Ipricing enging

    }//end class
}
