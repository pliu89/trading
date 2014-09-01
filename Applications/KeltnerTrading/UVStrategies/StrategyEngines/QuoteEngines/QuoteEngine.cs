using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies.StrategyEngines.QuoteEngines
{
    using UV.Lib.Application;
    using UV.Lib.IO.Xml;
    using UV.Lib.Hubs;
    using UV.Lib.Engines;
    using UV.Lib.BookHubs;
    using UV.Lib.OrderBooks;
    using UV.Lib.Fills;
    using UV.Lib.Utilities;
    using UV.Lib.DatabaseReaderWriters.Queries;
    using UV.Strategies.StrategyHubs;
    using QTMath = UV.Lib.Utilities.QTMath;
    using UV.Lib.FrontEnds.GuiTemplates;

    /// <summary>
    /// Basic quoting engine.
    /// When a PricingEngine calls Quote() method, its request to buy/sell is 
    /// stored (sorted by price).  Regularly, the StrategyHub calls UpdateQuotes() 
    /// and its here that these quotes are converted to orders and sent to OrderEngine.
    /// StrategyHub should call UpdateQuotes() after any PricingEngine update, since
    /// it may have changed is quoting desires.
    /// </summary>
    public class QuoteEngine : Engine
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Strategy services
        protected AppServices m_Services = null;
        protected Strategy ParentStrategy = null;
        protected LogHub Log = null;

        // Quoting parameters
        protected double m_QuoteTickSize = 1.0;
        protected int m_MaxPosition = Int32.MaxValue / 2;
        protected bool m_IsQuoteEnabled = true;

        // Base quote management
        protected int[] m_BuySellQty = new int[2];                      // net fills needed for execution sync.
        protected bool[] m_IsQuoteSideUpdateRequired = new bool[] { false, false };
        protected Dictionary<PricingEngine, Quote>[] m_Quotes = new Dictionary<PricingEngine, Quote>[2];
        protected SortedDictionary<int, List<Quote>>[] m_QuotesByPrice = new SortedDictionary<int, List<Quote>>[2];

        // Base Fill Management
        protected Dictionary<PricingEngine, int>[] m_FillQty = new Dictionary<PricingEngine, int>[2];
        protected List<Fill>[] m_UndistributedFills = new List<Fill>[2];// emergency storage of fills
        protected int[] m_Position = new int[2];                         // current long/short positions held by PricingEngines

        // Recycling & workspaces        
        protected List<Fill>[] w_NewFills = new List<Fill>[2];           // workspace for managing new fills for each mkt side.
        protected List<Quote> w_Quotes = new List<Quote>();
        protected Dictionary<Quote, List<Fill>> w_DistributedFills = new Dictionary<Quote, List<Fill>>();
        protected RecycleFactory<List<Fill>> m_FillListRecycling = new RecycleFactory<List<Fill>>();
        protected RecycleFactory<List<Quote>> m_QuoteListRecycling = new RecycleFactory<List<Quote>>();

        // Pricing Engines
        protected PricingEngine m_FirstPriceEngine = null;
        //
        //
        //
        #endregion// members


        #region Constructors & SetUp
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public QuoteEngine()
            : base()
        {
            // Dimension tables
            for (int side = 0; side < m_Quotes.Length; ++side)
            {
                m_Quotes[side] = new Dictionary<PricingEngine, Quote>();
                m_QuotesByPrice[side] = new SortedDictionary<int, List<Quote>>();
                w_NewFills[side] = new List<Fill>();
                m_FillQty[side] = new Dictionary<PricingEngine, int>();
                m_UndistributedFills[side] = new List<Fill>();
            }
        }
        //
        // *****************************************
        // ****     Setup Initialize()          ****
        // *****************************************
        protected override void SetupInitialize(IEngineHub myEngineHub, IEngineContainer engineContainer, int engineID, bool setupGui)
        {
            base.SetupInitialize(myEngineHub, engineContainer, engineID, false);
            EngineGui engineGui = base.SetupGuiTemplates();

            if (engineContainer is Strategy)
            {
                ParentStrategy = (Strategy)engineContainer;
                Log = ParentStrategy.StrategyHub.Log;
            }
            m_Services = AppServices.GetInstance();
        }
        //
        //
        public override void SetupBegin(IEngineHub myEngineHub, IEngineContainer engineContainer)
        {
            if (ParentStrategy != null)
                m_QuoteTickSize = ParentStrategy.m_OrderEngine.QuoteTickSize;
        }
        //
        //
        //
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // ***************************************************************** 
        //
        //
        /// <summary>
        /// Max absolute difference between m_FillQty[0]-m_FillQty[1] allowed.
        /// </summary>
        public int MaxPos
        {
            get { return m_MaxPosition; }
            set
            {
                if (m_MaxPosition != value)
                {
                    m_MaxPosition = Math.Max(0, value);
                    UpdateQuotes(true);
                }
            }
        }
        //
        //
        public bool IsQuoteEnabled
        {
            get { return m_IsQuoteEnabled; }
            set
            {
                m_IsQuoteEnabled = value;
                if (m_IsQuoteEnabled == false)
                {
                    // Stop quoting.
                    for (int tradeSide = 0; tradeSide < 2; ++tradeSide)
                    {
                        int tradeSign = QTMath.MktSideToMktSign(tradeSide);
                        int orderQty = m_BuySellQty[tradeSide];
                        double price = 0.0;
                        if (m_FirstPriceEngine != null)
                            price = m_FirstPriceEngine.ImpliedMarket.Price[tradeSide][0];
                        price -= 2.0 * tradeSign * m_QuoteTickSize;
                        int tradeId = ParentStrategy.m_OrderEngine.Quote(tradeSide, price, orderQty, string.Empty);
                    }
                }
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
        //
        //
        // *****************************************
        // ****             Quote()             ****
        // *****************************************
        /// <summary>
        /// Method called by PricingEngine to request a change in quoting 
        /// for a particular, side, price and qty.
        /// Sending a quote with only tradeSide will remove quote from price list.
        /// 
        /// Generally,s pricingEngines that don't want to quote a qty should still quote their target price.
        /// This allows overfills to be distributed to the pricing engine that can best use it.
        /// </summary>
        /// <param name="pricingEngine"></param>
        /// <param name="tradeSide"></param>
        /// <param name="price"></param>
        /// <param name="qty">signed integer</param>
        /// <param name="quoteReason">user chosen quote reason.</param>
        public virtual void Quote(PricingEngine pricingEngine, int tradeSide, double price = double.NaN, int qty = 0,
            QuoteReason quoteReason = QuoteReason.None)
        {
            // Compute quote variables.
            int tradeSign = UV.Lib.Utilities.QTMath.MktSideToMktSign(tradeSide);
            bool isPriceListed = (double.IsNaN(price) == false && double.IsInfinity(price) == false);
            int iPriceNew = 0;
            if (isPriceListed)
                iPriceNew = tradeSign * (int)System.Math.Floor(tradeSign * price / m_QuoteTickSize);    // integerized price!

            // Get this engines quote object from our master list.
            Quote quote = null;
            if (!m_Quotes[tradeSide].TryGetValue(pricingEngine, out quote))         // we store quotes for each side of mkt.
            {   // Apparently this is the first quote call from this engine.
                quote = new Quote();
                m_Quotes[tradeSide].Add(pricingEngine, quote);
                quote.PricingEngine = pricingEngine;                                // set constant vars.
                quote.Side = tradeSide;
                if (m_FirstPriceEngine == null)
                    m_FirstPriceEngine = pricingEngine;
            }
            bool isPriceListedPrev = quote.IsPriceListed;
            int iPricePrev = quote.IPrice;
            int qtyPrev = quote.Qty;

            // Test validity of qty value
            if (tradeSign * qty < 0)
            {
                qty = 0;
            }

            // Determine if this quote has changed.
            //      isPriceChanged = should contain all reasons the quotes location in book might change.
            bool isPriceChanged = (isPriceListed != isPriceListedPrev) || (iPriceNew != iPricePrev) || (quote.Reason != quoteReason);
            bool doesThisChangeRequireQuoteUpdating = (qty != qtyPrev) || (isPriceChanged);
            m_IsQuoteSideUpdateRequired[tradeSide] = m_IsQuoteSideUpdateRequired[tradeSide] || doesThisChangeRequireQuoteUpdating; // true if ANY PricingEngines quotes have changed since last update.

            // If the price has changed, and it should have been in our price list, 
            // we need to remove it from its old location in price list.
            if (isPriceChanged && isPriceListedPrev)
                RemoveQuoteFromListing(tradeSide, quote);

            // Update the quote with the new price/qty
            quote.IsPriceListed = isPriceListed;
            quote.IPrice = iPriceNew;
            quote.RawPrice = price;
            quote.Qty = qty;
            quote.Reason = quoteReason;
            quote.FillAttributeStr.Clear();
            quote.FillAttributeStr.Append(pricingEngine.GetFillAttributeString());

            // Now add quote to new price location, if necessary.
            if (isPriceChanged && isPriceListed)
            {   // We need to add it since its price has changed, and it price is valid now.
                List<Quote> quoteList = null;
                int priceKey = -tradeSign * iPriceNew;
                if (!m_QuotesByPrice[tradeSide].TryGetValue(priceKey, out quoteList)) // get all quotes at this price
                {   // There are no previous quotes at this price, create new list.
                    quoteList = m_QuoteListRecycling.Get();
                    quoteList.Clear();
                    m_QuotesByPrice[tradeSide].Add(priceKey, quoteList);
                }
                // Add quote to list at this price.
                if (quote.Reason == QuoteReason.Exit || quote.Reason == QuoteReason.Stop)
                    quoteList.Insert(0, quote);                  // put these in the front of the list.
                else
                    quoteList.Add(quote);                       // add these to end of list.
            }

        }//Quote()
        //
        //
        //
        //
        // *****************************************
        // ****         UpdateQuotes            ****
        // *****************************************
        /// <summary>
        /// Called regularly by the StrategyHub after it has allowed PricingEngines to update
        /// and possibly change their quotes.  This method checks for quote changes, and 
        /// sends them to the OrderEngine.
        /// Whenever the QuoteEngine's parameters are changed (and it would affect how we quote)
        /// we should call this method fith forceUpdate=true!
        /// <param name="forceUpdate">set to to force updating each side of quotes, as is done after fills.</param>
        /// </summary>
        public virtual void UpdateQuotes(bool forceUpdate = false)
        {
            if (m_IsQuoteEnabled == false)
            {   // TODO: We could check to exit undistributed fills.
                return;
            }
            ValidateQuoteTables();                              // Check whether QuoteTickSize has changed.

            if (ValidatePosition() == false)
            {
                this.IsQuoteEnabled = false;
                return;
            }

            DateTime now = ParentStrategy.StrategyHub.GetLocalTime();


            for (int tradeSide = 0; tradeSide < 2; tradeSide++)
            {
                if (m_IsQuoteSideUpdateRequired[tradeSide] == false && forceUpdate == false)
                    continue;

                //
                // Collect most aggressive non-zero quoters
                //
                int tradeSign = QTMath.MktSideToMktSign(tradeSide);
                int rawIPrice = 0;
                int totalQty = 0;
                foreach (KeyValuePair<int, List<Quote>> kv in m_QuotesByPrice[tradeSide])
                {
                    foreach (Quote quote in kv.Value)
                    {

                        totalQty = totalQty + quote.Qty;
                    }

                    rawIPrice = -tradeSign * kv.Key;            // price at this level
                    if (totalQty != 0)
                        break;
                }

                //
                // Send quotes.
                //
                int tradeId;
                int quoteQtyAllowed = Math.Max(0, m_MaxPosition - tradeSign * m_Position[tradeSide]);//always positive
                totalQty = tradeSign * Math.Min(quoteQtyAllowed, Math.Abs(totalQty));
                int orderQty = totalQty + m_BuySellQty[tradeSide];
                tradeId = ParentStrategy.m_OrderEngine.Quote(tradeSide, rawIPrice * m_QuoteTickSize, orderQty, string.Empty);
            }
        }//UpdateQuotes().
        //
        //
        // *************************************************
        // ****         MarketInstrumentChanged()       ****
        // *************************************************
        public void MarketInstrumentChanged(Book marketBook, InstrumentChangeArgs eventArgs)
        {
            if (m_IsQuoteSideUpdateRequired[0] || m_IsQuoteSideUpdateRequired[1])
                UpdateQuotes();
        }// MarketInstrumentChanged()
        //
        //
        //
        //
        // *****************************************************
        // ****         Process Synthetic Order()           ****
        // *****************************************************
        /// <summary>
        /// Process fills from Strategy to PricingEngines.
        /// </summary>
        /// <param name="syntheticOrder"></param>
        /// <param name="newFills"></param>
        /// <returns>True if update required</returns>
        public virtual bool ProcessSyntheticOrder(SyntheticOrder syntheticOrder, List<Fill> newFills)
        {
            if (newFills == null || newFills.Count == 0)
                return false;

            // Collect all fills into work spaces.
            Log.BeginEntry(LogLevel.Major, "Quote.ProcessSynthOrder: {0}  Fills=", ParentStrategy.Name);
            w_NewFills[0].Clear();
            w_NewFills[1].Clear();
            foreach (Fill fill in newFills)
            {
                int tradeSide = QTMath.MktSignToMktSide(fill.Qty);
                w_NewFills[tradeSide].Add(fill);
                m_BuySellQty[tradeSide] += fill.Qty;                // this records the raw fills as they come in.
                Log.AppendEntry(" [{0}]", fill);
            }
            int[] position = new int[2];                            // this will be updated during allocation of fills.
            m_Position.CopyTo(position, 0);
            Log.AppendEntry(". ");


            // Prepare entry for database write.
            DateTime localTime = ParentStrategy.StrategyHub.GetLocalTime();
            UV.Lib.DatabaseReaderWriters.Queries.FillsQuery query = new Lib.DatabaseReaderWriters.Queries.FillsQuery();

            // -----------------------------------------------------
            // Pass: distribute fills to stops
            // -----------------------------------------------------
            for (int tradeSide = 0; tradeSide < 2; ++tradeSide)
            {
                int exitingSide = QTMath.MktSideToActiveMktSide(tradeSide);
                if (w_NewFills[tradeSide].Count == 0 || m_FillQty[exitingSide].Count == 0)
                    continue;
                List<Quote> exitList = m_QuoteListRecycling.Get();      // get empty list.
                exitList.Clear();
                foreach (KeyValuePair<PricingEngine, int> kv in m_FillQty[exitingSide])
                {
                    Quote quote;
                    if (m_Quotes[tradeSide].TryGetValue(kv.Key, out quote) && quote.Reason == QuoteReason.Stop && quote.Qty != 0)
                        exitList.Add(quote);
                }
                if (exitList.Count > 0)
                {
                    Log.AppendEntry(" Distribute to {0} stop quoters:", exitList.Count);
                    DistributeFillsToQuoters(ref w_NewFills[tradeSide], ref exitList, ref query, ref w_DistributedFills, ref position);
                    Log.AppendEntry(". ");
                }
                exitList.Clear();
                m_QuoteListRecycling.Recycle(exitList);
            }//next tradeSide

            // -----------------------------------------------------
            // Pass: distribute fills to quoters who want them.
            // -----------------------------------------------------
            for (int tradeSide = 0; tradeSide < 2; ++tradeSide)
            {
                if (w_NewFills[tradeSide].Count == 0)
                    continue;
                int exitingSide = QTMath.MktSideToOtherSide(tradeSide);
                int tradeSign = QTMath.MktSideToMktSign(tradeSide);
                List<Quote> quotesToFill = m_QuoteListRecycling.Get();      // get empty lists for entry quotes.

                Log.AppendEntry(" Distribute to working quoters");
                List<int> iPriceKeys = new List<int>(m_QuotesByPrice[tradeSide].Keys);
                int priceLevel = 0;
                while (w_NewFills[tradeSide].Count > 0 && priceLevel < iPriceKeys.Count)
                {
                    // On each interation, update our "pos" so we know the remaining qty.
                    int allowedEntryQty = tradeSign * Math.Max(0, m_MaxPosition - Math.Abs(position[tradeSide]));

                    // Load entry/exit quoters for this price level.
                    Log.AppendEntry(" lvl={0}/{1}:", priceLevel, iPriceKeys.Count);
                    quotesToFill.Clear();
                    List<Quote> quotes = null;
                    if (m_QuotesByPrice[tradeSide].TryGetValue(iPriceKeys[priceLevel], out quotes))
                    {
                        foreach (Quote quote in quotes)
                        {
                            if (quote.Qty != 0)
                                quotesToFill.Add(quote);
                        }
                    }

                    if (quotesToFill.Count > 0)
                    {
                        Log.AppendEntry(" Filling ({0}):", quotesToFill.Count);
                        DistributeFillsToQuoters(ref w_NewFills[tradeSide], ref quotesToFill, ref query, ref w_DistributedFills, ref position);
                    }

                    // 
                    priceLevel++;
                }// next price level
                // Clean up.
                quotesToFill.Clear();
                m_QuoteListRecycling.Recycle(quotesToFill);
                Log.AppendEntry(" Finished.");
                if (w_NewFills[tradeSide].Count > 0)
                {
                    Log.AppendEntry(" {0} fills remaining.", w_NewFills[tradeSide].Count);
                }
                else
                    Log.AppendEntry(" No fills remain.");
            }//tradeSide

            // -----------------------------------------------------
            // Start emergency processing!
            // -----------------------------------------------------
            if (w_NewFills[0].Count > 0 || w_NewFills[1].Count > 0)
            {
                Log.AppendEntry(" Process unwanted fills!");
                ProcessUnwantedFills(ref w_NewFills, ref w_DistributedFills);
            }

            Log.EndEntry();                                             // end logging for us now, before we call other methods.
            // -----------------------------------------------------
            // Distribute these fills now.
            // -----------------------------------------------------            
            if (query != null && query.Count != 0)
                ParentStrategy.StrategyHub.RequestDatabaseWrite(query); // submit all the queries            
            foreach (KeyValuePair<Quote, List<Fill>> kv in w_DistributedFills)
            {
                int fillQty = 0;
                double fillPrice = 0;
                foreach (Fill fill in kv.Value)
                {
                    fillQty += fill.Qty;
                    fillPrice = fill.Price;                             // TODO: this should be ave fill price
                }
                int tradeSide = QTMath.MktSignToMktSide(fillQty);
                int exitSide = QTMath.MktSideToOtherSide(tradeSide);
                if (fillQty == 0)
                    continue;
                // Update our position counting.
                int openPos = 0;
                if (m_FillQty[exitSide].TryGetValue(kv.Key.PricingEngine, out openPos))
                {   // This is an exit (since this PricingEngine has open position on other side of mkt).
                    openPos += fillQty;


                    // Update real position table.
                    if (openPos * fillQty <= 0)
                        m_FillQty[exitSide].Remove(kv.Key.PricingEngine);// complete exit, possibly a side flip
                    if (openPos != 0)
                    {   // There is a new position (on other side of mkt).
                        int posSide = QTMath.MktSignToMktSide(openPos);
                        m_FillQty[posSide][kv.Key.PricingEngine] = openPos;
                    }
                }
                else
                {   // This is an entry!
                    // Update real position table.
                    if (m_FillQty[tradeSide].ContainsKey(kv.Key.PricingEngine))
                        m_FillQty[tradeSide][kv.Key.PricingEngine] += fillQty;  // add to this engines position.
                    else
                        m_FillQty[tradeSide].Add(kv.Key.PricingEngine, fillQty); // store this engines position.
                }
                // Trigger the pricing engine filled event!
                foreach (Fill fill in kv.Value)
                    kv.Key.PricingEngine.Filled(fill);
            }// next filled Quote.
            // Update total sum
            Log.BeginEntry(LogLevel.Major, "Quote.ProcessSynthOrder {0} Summary: ", ParentStrategy.Name);
            for (int tradeSide = 0; tradeSide < 2; tradeSide++)
            {
                // Add up the current position.
                int pos = 0;
                foreach (KeyValuePair<PricingEngine, int> kv in m_FillQty[tradeSide])
                    pos += kv.Value;
                m_Position[tradeSide] = pos;

                // Write some logging.
                Log.AppendEntry(" {0}-side:", QTMath.MktSideToLongString(tradeSide));
                Log.AppendEntry(" Pos={0:+0;-0;0}", m_Position[tradeSide]);
                foreach (KeyValuePair<PricingEngine, int> kv in m_FillQty[tradeSide])
                    Log.AppendEntry(" [{1:+0;-0;0} {0}]", kv.Key.EngineName, kv.Value);
                Log.AppendEntry(" TotalQty={0}", m_BuySellQty[tradeSide]);
                // Log undistributed fills too.
                if (m_UndistributedFills[tradeSide].Count > 0)
                {
                    Log.AppendEntry(" Undistributed {0}-fills:", QTMath.MktSideToLongString(tradeSide));
                    foreach (Fill fill in m_UndistributedFills[tradeSide])
                        Log.AppendEntry(" {0}", fill);
                }
            }// next tradeSide
            Log.AppendEntry(" |MaxPos|={0}.", m_MaxPosition);
            Log.EndEntry();

            //
            // Clean up work spaces
            //
            foreach (KeyValuePair<Quote, List<Fill>> kv in w_DistributedFills)
            {
                kv.Value.Clear();
                m_FillListRecycling.Recycle(kv.Value);
            }
            w_DistributedFills.Clear();                                 // Quoters and their fills to distribute.

            return true;
        }// ProcessSyntheticOrder()
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
        //
        //
        //
        //
        // *********************************************************************
        // ****                 DistributeFillsToQuoters()                  ****
        // *********************************************************************
        /// <summary>
        /// This distibutes fills in the list to those quotes provided in the list quotes.
        /// In the process of distrubuting, the fills are removed from the first list, and the 
        /// quote quantities are reduced.  PricingEngines that will receive the fills are 
        /// added to the "distributedFills" dictionary.  Also, the net position[] array is updated and 
        /// fill messages are added to the query.
        /// </summary>
        /// <param name="fillsToDistribute">fills that will be distributed</param>
        /// <param name="quotes">quotes that will be distributed to.</param>
        /// <param name="query"></param>
        /// <param name="distributedFills"></param>
        /// <param name="position"></param>
        protected void DistributeFillsToQuoters(ref List<Fill> fillsToDistribute, ref List<Quote> quotes,
            ref FillsQuery query, ref Dictionary<Quote, List<Fill>> distributedFills, ref int[] position)
        {
            DateTime localTime = ParentStrategy.StrategyHub.GetLocalTime();
            int quoter = 0;
            while (quoter < quotes.Count && fillsToDistribute.Count > 0)// loop thru each quoter until fills gone.
            {
                Quote quote = quotes[quoter];
                Log.AppendEntry(" [{0}]", quote);
                Fill fill = fillsToDistribute[0];                       // consider first fill.
                if (fill.Qty == 0)
                {   // this should never happen.
                    Log.AppendEntry(" Removed empty fill {0}.", fill);
                    fillsToDistribute.Remove(fill);
                    continue;
                }
                int tradeSign = Math.Sign(fill.Qty);
                int tradeSide = QTMath.MktSignToMktSide(tradeSign);

                //
                // Determine how much to allocate to this quoter.
                //
                Fill allocatedFill = null;                              // fill to allocate to quoter.
                int remainingQty = fill.Qty;                            // qty to be added back to fills list.                
                if (quote.Side != tradeSide)
                {   // This is an error.  Never allocate a fill to a quote for the other side of mkt!
                    Log.AppendEntry(" wrong-side {0}.", QTMath.MktSideToLongString(quote.Side));
                }
                //else if (forceFillAbsQty != 0)
                //{   // Here, we are forcing fills to be allocated even to quoters who have qty = 0.                    
                //    remainingQty = tradeSign * Math.Max(0, Math.Abs(fill.Qty) - forceFillAbsQty); // force up to full qty.
                //}
                else if (quote.Reason != QuoteReason.Entry && quote.Qty != 0)
                {   // Normal non-entry request. Try to fill this quote as much as possible completely.
                    remainingQty = tradeSign * Math.Max(0, (fill.Qty - quote.Qty) * tradeSign);
                    Log.AppendEntry(" taking {0}, remaining {1},", quote.Qty, remainingQty);
                }
                else if (quote.Reason == QuoteReason.Entry && quote.Qty != 0)
                {   // Normal entry request.  Don't allow entries to violet position limit!
                    int allowedEntryQty = tradeSign * Math.Max(0, m_MaxPosition - Math.Abs(position[tradeSide]));
                    //remainingQty = tradeSign * Math.Max(0, (allowedEntryQty - quote.Qty) * tradeSign);
                    int qtyWeCanTake = Math.Min(Math.Abs(allowedEntryQty), Math.Abs(quote.Qty)) * tradeSign;
                    remainingQty = tradeSign * Math.Max(0, (fill.Qty - qtyWeCanTake) * tradeSign);
                    Log.AppendEntry(" taking {0}, remaining {1},", qtyWeCanTake, remainingQty);
                }

                //
                // Allocate the fill
                // 
                if (remainingQty == 0)
                {   // This fill is completely consumed by the quoter.
                    fillsToDistribute.Remove(fill);                     // remove fill from list.
                    allocatedFill = fill;
                }
                else if (remainingQty == fill.Qty)
                {   // No qty was consumed at all!?!
                    allocatedFill = null;
                }
                else
                {   // This fill is only partially consumed. 
                    fillsToDistribute.Remove(fill);                     // remove original fill from list.
                    Fill remainingFill = Fill.Create(fill);
                    remainingFill.Qty = remainingQty;
                    fillsToDistribute.Insert(0, remainingFill);         // First, replace the unused portion back onto list.

                    allocatedFill = Fill.Create(fill);
                    allocatedFill.Qty = fill.Qty - remainingQty;        // Allocate the consumed portion. 
                }
                if (allocatedFill != null && allocatedFill.Qty != 0)
                {
                    Log.AppendEntry(" filled {1}.", quote.PricingEngine.EngineName, allocatedFill);
                    List<Fill> fills;
                    if (distributedFills.TryGetValue(quote, out fills) == false)
                    {   // No fills had be distributed to this quote previously.
                        fills = m_FillListRecycling.Get();
                        fills.Clear();
                        distributedFills.Add(quote, fills);
                    }
                    fills.Add(allocatedFill);
                    position[quote.Side] += allocatedFill.Qty;
                    quote.Qty = tradeSign * Math.Max(0, tradeSign * (quote.Qty - allocatedFill.Qty));
                    string msgStr = quote.FillAttribution();
                    query.AddItemToWrite(ParentStrategy.SqlId, -1, localTime, m_Services.User, quote.PricingEngine.EngineName, msgStr, allocatedFill.Qty, allocatedFill.Price);
                }
                else
                {
                    Log.AppendEntry(" skipped.");
                }
                quoter++;                  // otherwise move to next quoter
            }// next quoter
        }//DistributeFillsToQuoters()
        //
        //
        // *************************************************************
        // ****             Process Unwanted Fills()                ****
        // *************************************************************
        protected void ProcessUnwantedFills(ref List<Fill>[] newFills, ref Dictionary<Quote, List<Fill>> distributedFills)
        {
            // 
            // Cancel off-setting fills.
            //
            if (newFills[0].Count > 0 && newFills[1].Count > 0)
            {
                Log.AppendEntry(" Remove offsetting fills:");
                while (newFills[0].Count > 0 && newFills[1].Count > 0)
                {
                    Fill fillLong = newFills[0][0];
                    newFills[0].RemoveAt(0);
                    Fill fillShort = newFills[1][0];
                    newFills[1].RemoveAt(0);
                    int netQty = fillLong.Qty + fillShort.Qty;     // cancel the first fill in each list.
                    int cancelledQty = Math.Min(Math.Abs(fillLong.Qty), Math.Abs(fillShort.Qty));
                    Log.AppendEntry(" PnL={0}", cancelledQty * (fillShort.Price - fillLong.Price));
                    if (netQty > 0)
                    {   // Long side will survive somewhat.                        
                        Fill remainder = Fill.Create(fillLong);
                        remainder.Qty = netQty;
                        newFills[0].Insert(0, remainder);
                    }
                    else if (netQty < 0)
                    {
                        Fill remainder = Fill.Create(fillShort);
                        remainder.Qty = netQty;
                        newFills[1].Insert(0, remainder);
                    }
                }
            }

            //
            // Pass: distribute fills to anyone with position: "forced exit"
            //
            if (newFills[0].Count > 0 || newFills[1].Count > 0)
            {
                for (int tradeSide = 0; tradeSide < 2; ++tradeSide)
                {
                    int exitSide = QTMath.MktSideToOtherSide(tradeSide);
                    if (newFills[tradeSide].Count == 0 || m_FillQty[exitSide].Count == 0)
                        continue;
                    int tradeSign = QTMath.MktSideToMktSign(tradeSide);
                    foreach (KeyValuePair<PricingEngine, int> kv in m_FillQty[exitSide])
                    {
                        Quote quote;
                        List<Fill> fills;
                        int qtyToForce = 0;
                        if (m_Quotes[tradeSide].TryGetValue(kv.Key, out quote) && distributedFills.TryGetValue(quote, out fills))
                        {   // This strategy already has some fills.
                            int fillQty = 0;
                            foreach (Fill fill in fills)
                                fillQty += fill.Qty;
                            int finalQty = (kv.Value + fillQty);
                            qtyToForce = Math.Min(0, tradeSign * finalQty); // qty for fill he can still take.
                        }
                        else
                        {
                            qtyToForce = -kv.Value;
                        }
                        if (qtyToForce != 0)
                        {   // Pass to him extra fills                         
                            foreach (Fill fill in newFills[tradeSide])
                            {

                            }
                        }
                    }
                }// next side
            }

            //
            // Pass: distribute fills to anyone with a quote!
            //
            if (newFills[0].Count > 0 || newFills[1].Count > 0)
            {
                for (int tradeSide = 0; tradeSide < 2; ++tradeSide)
                {
                    if (newFills[tradeSide].Count == 0)
                        continue;
                    Log.AppendEntry(" Forcing fills:");
                    int tradeSign = QTMath.MktSideToMktSign(tradeSide);
                    // Collect all the quotes
                    List<Quote> quotes = m_QuoteListRecycling.Get();
                    quotes.Clear();
                    foreach (KeyValuePair<int, List<Quote>> kv in m_QuotesByPrice[tradeSide])
                        quotes.AddRange(kv.Value);
                    int quoteID = 0;
                    int qtyToForce = 1;                             // TODO: This can be dynamic
                    // Force fills now
                    while (newFills[tradeSide].Count > 0 && quotes.Count > 0)
                    {
                        Quote quote = quotes[quoteID];
                        Fill origFill = newFills[tradeSide][0];
                        newFills[tradeSide].RemoveAt(0);

                        Fill fillToDistribute = null;
                        int fillQty = tradeSign * Math.Min(qtyToForce, Math.Abs(origFill.Qty));
                        if ((origFill.Qty - fillQty) == 0)
                        {   // Entire fill is consumed.
                            fillToDistribute = origFill;
                        }
                        else
                        {
                            fillToDistribute = Fill.Create(origFill);
                            fillToDistribute.Qty = fillQty;
                            Fill remainingFill = Fill.Create(origFill);
                            remainingFill.Qty = origFill.Qty - fillQty;
                            newFills[tradeSide].Insert(0, remainingFill);
                        }
                        List<Fill> fills;
                        if (!distributedFills.TryGetValue(quote, out fills))
                        {   // This is this quotes first fill, so create a fill list.
                            fills = new List<Fill>();
                            distributedFills.Add(quote, fills);
                        }
                        fills.Add(fillToDistribute);
                        Log.AppendEntry(" {0} filled {1}.", quote.PricingEngine.EngineName, fillToDistribute);
                        // increment the while loop!
                        quoteID = (quoteID + 1) % quotes.Count;
                    }//wend
                    // Cleanup.
                    quotes.Clear();
                    m_QuoteListRecycling.Recycle(quotes);

                }// next side
            }// if fills to distribute. 


            //
            // Failed to distribute fills.
            //
            if (newFills[0].Count > 0 || newFills[1].Count > 0)
            {
                Log.AppendEntry(" FAILED to distribute fills:");
                for (int tradeSide = 0; tradeSide < 2; ++tradeSide)
                    foreach (Fill fill in newFills[tradeSide])
                    {
                        m_UndistributedFills[tradeSide].Add(fill);
                        Log.AppendEntry(" {0}", fill);
                    }
            }


        }// ProcessUnwantedFills()
        //
        //
        // *************************************************
        // ****         RemoveQuoteFromListing()        ****
        // *************************************************
        protected void RemoveQuoteFromListing(int tradeSide, Quote quoteToRemove)
        {
            int tradeSign = QTMath.MktSideToMktSign(tradeSide);
            int priceKey = -tradeSign * quoteToRemove.IPrice;
            List<Quote> quoteList;
            if (m_QuotesByPrice[tradeSide].TryGetValue(priceKey, out quoteList) && quoteList.Contains(quoteToRemove))
            {
                quoteList.Remove(quoteToRemove);
                quoteToRemove.IsPriceListed = false;
                if (quoteList.Count == 0)
                {   // There are no remaining quotes at this price level, so clear out the list.
                    m_QuotesByPrice[tradeSide].Remove(priceKey);
                    m_QuoteListRecycling.Recycle(quoteList);
                }
            }
            else
            {
                ParentStrategy.StrategyHub.Log.BeginEntry(LogLevel.Major, "Quote: {0} could not find quote {1}.  Searching for it...", ParentStrategy.Name, quoteToRemove);
                bool isFound = false;
                foreach (KeyValuePair<int, List<Quote>> kv in m_QuotesByPrice[tradeSide])
                {
                    if (kv.Value.Contains(quoteToRemove))
                    {
                        isFound = true;
                        Log.AppendEntry(" Found! Removed from list at priceKey={0} not QuotePriceKey={1}.", priceKey, quoteToRemove.IPrice);
                        Log.EndEntry();
                        priceKey = kv.Key;
                        kv.Value.Remove(quoteToRemove);
                        quoteToRemove.IsPriceListed = false;
                        break;
                    }
                }
                if (isFound)
                {
                    if (m_QuotesByPrice[tradeSide].TryGetValue(priceKey, out quoteList) && quoteList.Count == 0)
                    {
                        m_QuotesByPrice[tradeSide].Remove(priceKey);
                        m_QuoteListRecycling.Recycle(quoteList);
                    }
                }
                else
                {
                    Log.AppendEntry(" Not Found! Marking it as such.");
                    Log.EndEntry();
                    quoteToRemove.IsPriceListed = false;
                }
            }
        }//RemoveQuoteFromListing()
        //
        //
        //
        // *************************************************
        // ****         ValidateQuoteTables()            ****
        // *************************************************
        /// <summary>
        /// 
        /// </summary>
        protected void ValidateQuoteTables()
        {
            if (m_QuoteTickSize == ParentStrategy.m_OrderEngine.QuoteTickSize)
                return;
            // Accept new QuoteTickSize 
            Log.NewEntry(LogLevel.Minor, "QuoteTickSize change detected for {0} {1} -> {2}. Rebuilding tables.", ParentStrategy.Name, m_QuoteTickSize, ParentStrategy.m_OrderEngine.QuoteTickSize);
            m_QuoteTickSize = ParentStrategy.m_OrderEngine.QuoteTickSize;

            // Rebuild the pricing Tables.
            for (int tradeSide = 0; tradeSide < 2; tradeSide++)
            {
                List<Quote> masterQuoteList = m_QuoteListRecycling.Get();     // get a list for all quotes!
                masterQuoteList.Clear();
                // Empty all quotes into our masterList.
                List<int> keyList = new List<int>();
                keyList.AddRange(m_QuotesByPrice[tradeSide].Keys);
                foreach (int priceKey in keyList)
                {
                    List<Quote> quoteList;
                    if (m_QuotesByPrice[tradeSide].TryGetValue(priceKey, out quoteList))
                    {
                        foreach (Quote quote in quoteList)
                            if (!masterQuoteList.Contains(quote))
                                masterQuoteList.Add(quote);
                        m_QuotesByPrice[tradeSide].Remove(priceKey);
                        quoteList.Clear();
                        m_QuoteListRecycling.Recycle(quoteList);
                    }
                }// next priceKey

                // Resort all the quotes in the masterList.
                int tradeSign = QTMath.MktSideToMktSign(tradeSide);
                foreach (Quote quote in masterQuoteList)
                {
                    quote.IPrice = tradeSign * (int)System.Math.Floor(tradeSign * quote.RawPrice / m_QuoteTickSize);
                    List<Quote> quoteList;
                    if (!m_QuotesByPrice[tradeSide].TryGetValue(quote.IPrice, out quoteList))
                    {
                        quoteList = m_QuoteListRecycling.Get();
                        quoteList.Clear();
                        m_QuotesByPrice[tradeSide].Add(quote.IPrice, quoteList);
                    }
                    if (quote.Reason == QuoteReason.Stop || quote.Reason == QuoteReason.Exit)
                        quoteList.Insert(0, quote);
                    else
                        quoteList.Add(quote);
                }

            }

        }// ValidateQuoteTables()
        //
        //
        // *************************************************
        // ****         ValidatePosition()              ****
        // *************************************************
        /// <summary>
        /// This method quickly checks that all positions match.
        /// If not, 
        /// </summary>
        /// <returns></returns>
        protected bool ValidatePosition()
        {
            // Check fill count
            if (m_Position[0] + m_Position[1] != m_BuySellQty[0] + m_BuySellQty[1]
                || m_UndistributedFills[0].Count + m_UndistributedFills[1].Count > 0)
            {   // The two different fill counts should always be equal.
                // If this check fails its probably because we have failed to distribute
                // the fills correctly.
                StringBuilder msg = new StringBuilder();
                msg.AppendFormat("Quote detected inconsistent position counter.\r\n{0} stopped quoting.", ParentStrategy.Name);
                msg.AppendFormat("\r\nRealPos={0} / {1}", m_Position[0], m_Position[1]);
                msg.AppendFormat(" BuySellQty={0} / {1}", m_BuySellQty[0], m_BuySellQty[1]);
                msg.AppendFormat(" Undistributed Fills={0} : ", m_UndistributedFills[0].Count + m_UndistributedFills[1].Count);
                foreach (Fill fill in m_UndistributedFills[0])
                    msg.AppendFormat(" {0}", fill);
                foreach (Fill fill in m_UndistributedFills[1])
                    msg.AppendFormat(" {0}", fill);


                UV.Lib.FrontEnds.Utilities.GuiCreator.ShowMessageBox(msg.ToString(), "Quote engine position error");
                Log.BeginEntry(LogLevel.Error, "UpdateQuote {0} position mismatch error. Stopping quotes.", ParentStrategy.Name);
                Log.AppendEntry(" RealPos={0} / {1}", m_Position[0], m_Position[1]);
                Log.AppendEntry(" BuySellQty={0} / {1}", m_BuySellQty[0], m_BuySellQty[1]);
                Log.AppendEntry(" Undistributed Fills={0} : ", m_UndistributedFills[0].Count + m_UndistributedFills[1].Count);
                foreach (Fill fill in m_UndistributedFills[0])
                    Log.AppendEntry(" {0}", fill);
                foreach (Fill fill in m_UndistributedFills[1])
                    Log.AppendEntry(" {0}", fill);
                Log.EndEntry();
                return false;
            }
            return true;
        }// ValidatePosition()
        //
        //
        //
        #endregion//Private Methods


        #region IStringifiable
        // *****************************************************************
        // ****                    IStringifiable                       ****
        // *****************************************************************
        //
        public override void SetAttributes(Dictionary<string, string> attributes)
        {
            int n;
            base.SetAttributes(attributes);
            foreach (KeyValuePair<string, string> kv in attributes)
            {
                if (kv.Key.Equals("MaxPosition") && int.TryParse(kv.Value, out n))
                    m_MaxPosition = n;
            }
        }
        //
        //
        #endregion//IStringifiable




    }//end class




}
