using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies.StrategyEngines.QuoteEngines
{
    using System.Drawing;

    using UV.Lib.Application;
    using UV.Lib.IO.Xml;
    using UV.Lib.Hubs;
    using UV.Lib.Engines;
    using UV.Lib.FrontEnds.GuiTemplates;
    using UV.Lib.BookHubs;
    using UV.Lib.OrderBooks;
    using UV.Lib.Fills;
    using UV.Lib.Utilities;
    using UV.Lib.Data;
    using UV.Lib.FrontEnds.Graphs;
    using UV.Lib.DatabaseReaderWriters.Queries;
    using UV.Strategies.StrategyHubs;
    using QTMath = UV.Lib.Utilities.QTMath;

    using Linq = System.Linq;

    //using UV.Lib.Utilities.Alarms;

    /// <summary>
    /// Specialized quoting engine for distributing simulated "faux" fills and real fills.
    /// </summary>
    public class FauxQuote : QuoteEngine
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // Real/faux fill management  
        protected UserInfo m_FauxUser = null;                           // for saving faux fills to database
        private IComparer<Quote> QuoteComparerByEngineId = null;


        // Graphing
        protected bool m_IsGraphEnabled = true;
        protected ZGraphEngine m_GraphEngine = null;
        protected int m_GraphID = -1;
        private int m_TextOffsetTicks = 3;                              // number of ticks to offset strategy name.
        //
        //
        //
        #endregion// members


        #region Constructors & SetUp
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public FauxQuote() : base()
        {
            this.QuoteComparerByEngineId = new CompareQuoteByEngineID();           // used for sorting quotes by EngineId.
        }
        //
        // *****************************************
        // ****     Setup Initialize()          ****
        // *****************************************
        protected override void SetupInitialize(IEngineHub myEngineHub, IEngineContainer engineContainer, int engineID, bool setupGui)
        {
            base.SetupInitialize(myEngineHub, engineContainer, engineID, false);        // call QuoteEngine base class first.
            m_FauxUser = new UserInfo( m_Services.User );
            m_FauxUser.RunType = RunType.Faux;
        }
        //
        //
        public override void SetupBegin(IEngineHub myEngineHub, IEngineContainer engineContainer)
        {
            base.SetupBegin(myEngineHub, engineContainer);  // call QuoteEngine base class
            if (ParentStrategy != null)
            {
                m_GraphEngine = ParentStrategy.m_GraphEngine;
            }

            #region Initialize Graph
            if (m_IsGraphEnabled && m_GraphEngine != null)
            {
                string[] strArray = this.m_EngineName.Split(':');
                string graphName = strArray[strArray.Length - 1];

                int graphID = 0;
                foreach (CurveDefinition c in m_GraphEngine.CurveDefinitions.CurveDefinitions)
                    if (graphID <= c.GraphID) { graphID = c.GraphID + 1; }	// use next available ID#.  
                m_GraphID = graphID;

                CurveDefinition cdef;
                /*
                cdef = new CurveDefinition();
                cdef.GraphName = string.Format("{0}", graphName);
                cdef.CurveName = "Price";
                cdef.GraphID = graphID;
                cdef.CurveColor = Color.Black;
                cdef.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                cdef.Symbol = ZedGraph.SymbolType.None;
                m_GraphEngine.AddDefinition(cdef);
                */
                cdef = new CurveDefinition();
                cdef.GraphName = string.Format("{0}", graphName);
                cdef.CurveName = "Bid";
                cdef.GraphID = graphID;
                cdef.CurveColor = Color.Gray;
                cdef.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                cdef.Symbol = ZedGraph.SymbolType.None;
                m_GraphEngine.AddDefinition(cdef);

                cdef = new CurveDefinition();
                cdef.GraphName = string.Format("{0}", graphName);
                cdef.CurveName = "Ask";
                cdef.GraphID = graphID;
                cdef.CurveColor = Color.Gray;
                cdef.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                cdef.Symbol = ZedGraph.SymbolType.None;
                m_GraphEngine.AddDefinition(cdef);


                cdef = new CurveDefinition();
                cdef.CurveName = UV.Lib.Utilities.QTMath.MktSideToLongString(0);
                cdef.GraphID = graphID;
                cdef.CurveColor = Color.Blue;
                cdef.CurveWidth = 2.0F;
                cdef.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                cdef.Symbol = ZedGraph.SymbolType.None;
                m_GraphEngine.AddDefinition(cdef);

                cdef = new CurveDefinition();
                cdef.CurveName = UV.Lib.Utilities.QTMath.MktSideToLongString(1);
                cdef.GraphID = graphID;
                cdef.CurveColor = Color.Red;
                cdef.CurveWidth = 2.0F;
                cdef.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                cdef.Symbol = ZedGraph.SymbolType.None;
                m_GraphEngine.AddDefinition(cdef);

                //
                // Position indicators
                //
                cdef = new CurveDefinition();
                cdef.CurveName = "Long Entry";
                cdef.GraphID = graphID;
                cdef.CurveColor = Color.Blue;
                cdef.IsLineVisible = false;
                cdef.Symbol = ZedGraph.SymbolType.Triangle;
                cdef.SymbolFillColor = Color.Blue;
                m_GraphEngine.AddDefinition(cdef);

                cdef = new CurveDefinition();
                cdef.CurveName = "Long Exit";
                cdef.GraphID = graphID;
                cdef.CurveColor = Color.Blue;
                cdef.IsLineVisible = false;
                cdef.Symbol = ZedGraph.SymbolType.TriangleDown;
                cdef.SymbolFillColor = Color.White;
                m_GraphEngine.AddDefinition(cdef);

                cdef = new CurveDefinition();
                cdef.CurveName = "Short Entry";
                cdef.GraphID = graphID;
                cdef.CurveColor = Color.Red;
                cdef.IsLineVisible = false;
                cdef.Symbol = ZedGraph.SymbolType.TriangleDown;
                cdef.SymbolFillColor = Color.Red;
                m_GraphEngine.AddDefinition(cdef);

                cdef = new CurveDefinition();
                cdef.CurveName = "Short Exit";
                cdef.GraphID = graphID;
                cdef.CurveColor = Color.Red;
                cdef.IsLineVisible = false;
                cdef.Symbol = ZedGraph.SymbolType.Triangle;
                cdef.SymbolFillColor = Color.White;
                m_GraphEngine.AddDefinition(cdef);

                //
                // Stops
                //
                cdef = new CurveDefinition();
                cdef.CurveName = "Stop Price";
                cdef.GraphID = graphID;
                cdef.CurveColor = Color.Green;
                cdef.IsLineVisible = false;
                cdef.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                cdef.Symbol = ZedGraph.SymbolType.HDash;
                m_GraphEngine.AddDefinition(cdef);
            }
            else
                m_IsGraphEnabled = false;
            #endregion // graph initialization.

        }
        //
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
        //
        //
        //
        #endregion//Properties

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
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
        public override void UpdateQuotes(bool forceUpdate = false)
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
            List<Fill> fauxFills = null;
            List<Quote> fauxQuotesFilled = null;

            // Update graph
            if (m_IsGraphEnabled && m_FirstPriceEngine != null)
            {
                double bid = m_FirstPriceEngine.ImpliedMarket.Price[0][0];
                double ask = m_FirstPriceEngine.ImpliedMarket.Price[1][0];
                m_GraphEngine.AddPoint(m_GraphID, "Bid", bid);
                m_GraphEngine.AddPoint(m_GraphID, "Ask", ask);
            }



            for (int tradeSide = 0; tradeSide < 2; tradeSide++)
            {
                if (m_IsQuoteSideUpdateRequired[tradeSide] == false && forceUpdate == false)
                    continue;
                    
                //
                // Collect most aggressive non-zero quoters
                //
                int tradeSign = QTMath.MktSideToMktSign(tradeSide);
                int exitSide = QTMath.MktSideToOtherSide(tradeSide);
                bool isEntryAllowed = tradeSign * base.m_Position[tradeSide] < m_MaxPosition;     // true if new entries allowed.
                int rawIPrice = 0;
                int realEntryQty = 0;
                int realExitQty = 0;
                List<Quote> simQuotes = m_QuoteListRecycling.Get();
                simQuotes.Clear();
                foreach (KeyValuePair<int, List<Quote>> kv in m_QuotesByPrice[tradeSide])
                {
                    foreach (Quote quote in kv.Value)
                    {
                        int qty;
                        if (isEntryAllowed && quote.Reason == QuoteReason.Entry)
                            realEntryQty += quote.Qty;          // collect real entry quote size
                        else if (base.m_FillQty[exitSide].TryGetValue(quote.PricingEngine, out qty) && qty != 0)
                            realExitQty += quote.Qty;           // collect real exit quantity
                        else if (quote.Qty != 0)
                            simQuotes.Add(quote);               // collect other quotes, which will be simulated filled.
                    }
                    rawIPrice = -tradeSign * kv.Key;            // price at this level
                    if (realEntryQty != 0 || realExitQty != 0)
                        break;                                  // stop, once we have found non-empty quote stop.
                }

                //
                // Send real quotes.
                //
                int tradeId;
                bool isRealQuoteSent = false;
                int realTotalQty = realExitQty + realEntryQty;
                if (realTotalQty != 0)
                {   // There is some non-zero (REAL) quantity to quote.                    
                    // Constrain REAL quotes inside max position allowed: s*qty <= MaxPos - s*Pos 
                    int quoteQtyAllowed = Math.Max(0, m_MaxPosition - tradeSign * base.m_Position[tradeSide]);//always positive
                    realTotalQty = tradeSign * Math.Min(quoteQtyAllowed, Math.Abs(realTotalQty));
                    if (realTotalQty != 0)
                    {   // We want to quote a real qty.
                        int orderQty = realTotalQty + m_BuySellQty[tradeSide];                          
                        tradeId = ParentStrategy.m_OrderEngine.Quote(tradeSide, rawIPrice * m_QuoteTickSize, orderQty, string.Empty);
                        isRealQuoteSent = true;
                        if (m_IsGraphEnabled)
                            m_GraphEngine.AddPoint(m_GraphID, QTMath.MktSideToLongString(tradeSide), rawIPrice * m_QuoteTickSize);
                    }
                }
                if (! isRealQuoteSent)
                {   // If in the above, we have not sent a real order, send a zero quote.
                    int orderQty = m_BuySellQty[tradeSide];
                    tradeId = ParentStrategy.m_OrderEngine.Quote(tradeSide, rawIPrice * m_QuoteTickSize, orderQty, string.Empty);
                    if (m_IsGraphEnabled)
                        m_GraphEngine.AddPoint(m_GraphID, QTMath.MktSideToLongString(tradeSide), double.NaN);
                }
                
                //
                // Simulate quoting.
                //
                if (simQuotes.Count > 0)
                {
                    foreach (Quote quote in simQuotes)
                    {
                        Fill fill;                            
                        if ( quote.Qty != 0 && FillModels.FillModel.TryFill(quote,out fill) )
                        {
                            if (fauxQuotesFilled == null)
                            {   // Only make these tables when we need them!
                                fauxQuotesFilled = m_QuoteListRecycling.Get();
                                fauxFills = m_FillListRecycling.Get();
                            }
                            fill.LocalTime = now;
                            fill.ExchangeTime = now;
                            fauxFills.Add(fill);
                            fauxQuotesFilled.Add(quote);
                        }
                    }                
                }
                simQuotes.Clear();
                m_QuoteListRecycling.Recycle(simQuotes);
                
                m_IsQuoteSideUpdateRequired[tradeSide] = false;
            }//next side


            //
            // Report simulated fills.
            //
            if (fauxFills != null && fauxFills.Count > 0)
            {
                Log.BeginEntry(LogLevel.Minor, "Quote Simulating fills: ");
                for (int i = 0; i < fauxFills.Count; ++i)
                {
                    Fill fill = fauxFills[i];
                    Quote quote = fauxQuotesFilled[i];
                    Log.AppendEntry("[{0} filled {1}] ", quote.PricingEngine.EngineName, fill);
                }
                Log.EndEntry();
                UV.Lib.DatabaseReaderWriters.Queries.FillsQuery query = new Lib.DatabaseReaderWriters.Queries.FillsQuery();
                // Process sim fills.
                for (int i = 0; i < fauxFills.Count; ++i)
                {
                    Fill fill = fauxFills[i];
                    Quote quote = fauxQuotesFilled[i];
                    quote.Qty -= fill.Qty;

                    string msgStr = quote.FillAttribution();
                    query.AddItemToWrite(ParentStrategy.SqlId, -1, now, m_FauxUser, quote.PricingEngine.EngineName, msgStr, fill.Qty, fill.Price);
                    quote.PricingEngine.Filled(fill);
                }                
                if (query != null)
                    ParentStrategy.StrategyHub.RequestDatabaseWrite(query); 
                fauxFills.Clear();
                m_FillListRecycling.Recycle(fauxFills);
                fauxQuotesFilled.Clear();
                m_QuoteListRecycling.Recycle(fauxQuotesFilled);
            }

        }//UpdateQuotes().
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
        public override bool ProcessSyntheticOrder(SyntheticOrder syntheticOrder, List<Fill> newFills)
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
            base.m_Position.CopyTo(position,0);
            Log.AppendEntry(". ");


            // Try to cancel fills with undistributed fills.
            // TODO: Cancel undistributed fills, if any.
            /*
            if ( m_UndistributedFills[0].Count + m_UndistributedFills[1].Count > 0)
            {
                for (int tradeSide = 0; tradeSide < 2; ++tradeSide)
                {
                    int otherSide = QTMath.MktSideToOtherSide(tradeSide);
                    if (w_NewFills[tradeSide].Count > 0 && m_UndistributedFills[otherSide].Count > 0 )
                    {
                        Log.AppendEntry(" Canceling with undistributed fills: Not implemented!");
                    }
                }
            }
            */

            // Prepare entry for database write.
            DateTime localTime = ParentStrategy.StrategyHub.GetLocalTime();
            UV.Lib.DatabaseReaderWriters.Queries.FillsQuery query = new Lib.DatabaseReaderWriters.Queries.FillsQuery();

            // -----------------------------------------------------
            // Pass: distribute fills to stops
            // -----------------------------------------------------
            for (int tradeSide = 0; tradeSide < 2; ++tradeSide)
            {
                int exitingSide = QTMath.MktSideToActiveMktSide(tradeSide);
                if (w_NewFills[tradeSide].Count == 0 || base.m_FillQty[exitingSide].Count == 0)
                    continue;
                List<Quote> exitList = m_QuoteListRecycling.Get();      // get empty list.
                exitList.Clear();
                foreach (KeyValuePair<PricingEngine,int> kv in base.m_FillQty[exitingSide])
                {
                    Quote quote;
                    if (m_Quotes[tradeSide].TryGetValue(kv.Key, out quote) && quote.Reason == QuoteReason.Stop && quote.Qty != 0)
                        exitList.Add(quote);
                }
                if (exitList.Count > 0)
                {
                    Log.AppendEntry(" Distribute to {0} stop quoters:",exitList.Count);
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
                List<Quote> exitList = m_QuoteListRecycling.Get();      // get empty lists for entry quotes.
                List<Quote> entryList = m_QuoteListRecycling.Get();     // and for exit quoters...
                
                Log.AppendEntry(" Distribute to working quoters");
                List<int> iPriceKeys = new List<int>(m_QuotesByPrice[tradeSide].Keys);
                int priceLevel = 0;
                while (w_NewFills[tradeSide].Count > 0 && priceLevel < iPriceKeys.Count)
                {
                    // On each interation, update our "pos" so we know the remaining qty.
                    int allowedEntryQty = tradeSign * Math.Max(0, m_MaxPosition - Math.Abs(position[tradeSide]));                

                    // Load entry/exit quoters for this price level.
                    Log.AppendEntry(" lvl={0}/{1}:", priceLevel, iPriceKeys.Count);
                    entryList.Clear();
                    exitList.Clear();
                    List<Quote> quotes = null;
                    if (m_QuotesByPrice[tradeSide].TryGetValue(iPriceKeys[priceLevel], out quotes))
                    {
                        foreach (Quote quote in quotes)
                        {
                            if (allowedEntryQty != 0 && quote.Reason == QuoteReason.Entry && quote.Qty != 0)
                                entryList.Add(quote);
                            else if (base.m_FillQty[exitingSide].ContainsKey(quote.PricingEngine) && quote.Reason == QuoteReason.Exit && quote.Qty != 0)
                                exitList.Add(quote);
                        }
                    }
                                        
                    if (exitList.Count > 0)
                    {
                        Log.AppendEntry(" Exits ({0}):",exitList.Count);
                        DistributeFillsToQuoters(ref w_NewFills[tradeSide], ref exitList, ref query, ref w_DistributedFills, ref position);
                    }
                    if (entryList.Count > 0)
                    {
                        entryList.Sort(this.QuoteComparerByEngineId);  // To better match our backtest, consider sorting entryList by engine names...
                        Log.AppendEntry(" Entries ({0}):",entryList.Count);
                        DistributeFillsToQuoters(ref w_NewFills[tradeSide], ref entryList, ref query, ref w_DistributedFills, ref position);                        
                    }
                    // 
                    priceLevel++;
                }// next price level
                // Clean up.
                entryList.Clear();
                exitList.Clear();
                m_QuoteListRecycling.Recycle(entryList);
                m_QuoteListRecycling.Recycle(exitList);
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
                if (base.m_FillQty[exitSide].TryGetValue(kv.Key.PricingEngine, out openPos))
                {   // This is an exit (since this PricingEngine has open position on other side of mkt).
                    openPos += fillQty;
                    
                    // update quoter's graph
                    if (m_IsGraphEnabled)
                    {   
                        if (exitSide == 0)
                        {   // exit long position.
                            m_GraphEngine.AddPoint(m_GraphID, "Long Exit", fillPrice);
                            m_GraphEngine.AddText(m_GraphID, string.Format("{0}", kv.Key.PricingEngine.EngineName), fillPrice + m_TextOffsetTicks * m_QuoteTickSize);
                        }
                        else
                        {   // exit short position.
                            m_GraphEngine.AddPoint(m_GraphID, "Short Exit", fillPrice);
                            m_GraphEngine.AddText(m_GraphID, string.Format("{0}", kv.Key.PricingEngine.EngineName), fillPrice - m_TextOffsetTicks * m_QuoteTickSize);
                        }
                    }
                    
                    // Update real position table.
                    if (openPos * fillQty <= 0)
                        base.m_FillQty[exitSide].Remove(kv.Key.PricingEngine);// complete exit, possibly a side flip
                    if (openPos != 0)
                    {   // There is a new position (on other side of mkt).
                        int posSide = QTMath.MktSignToMktSide(openPos);
                        base.m_FillQty[posSide][kv.Key.PricingEngine] = openPos;
                    }                                       
                }
                else
                {   // This is an entry!
                    if (m_IsGraphEnabled)
                        if (tradeSide == 0)
                        {
                            m_GraphEngine.AddPoint(m_GraphID, "Long Entry", fillPrice);
                            m_GraphEngine.AddText(m_GraphID, string.Format("{0}", kv.Key.PricingEngine.EngineName), fillPrice - m_TextOffsetTicks * m_QuoteTickSize);
                        }
                        else
                        {
                            m_GraphEngine.AddPoint(m_GraphID, "Short Entry", fillPrice);
                            m_GraphEngine.AddText(m_GraphID, string.Format("{0}", kv.Key.PricingEngine.EngineName), fillPrice + m_TextOffsetTicks * m_QuoteTickSize);
                        }
                    // Update real position table.
                    if ( base.m_FillQty[tradeSide].ContainsKey(kv.Key.PricingEngine))
                        base.m_FillQty[tradeSide][kv.Key.PricingEngine] += fillQty;  // add to this engines position.
                    else
                        base.m_FillQty[tradeSide].Add(kv.Key.PricingEngine,fillQty); // store this engines position.
                }
                // Trigger the pricing engine filled event!
                foreach (Fill fill in kv.Value)
                    kv.Key.PricingEngine.Filled(fill);
            }// next filled Quote.
            // Update total sum
            Log.BeginEntry(LogLevel.Major, "Quote.ProcessSynthOrder {0} Summary: ",ParentStrategy.Name);
            for (int tradeSide=0; tradeSide < 2; tradeSide++)
            {   
                // Add up the current position.
                int pos = 0;
                foreach (KeyValuePair<PricingEngine, int> kv in base.m_FillQty[tradeSide])
                    pos += kv.Value;
                base.m_Position[tradeSide] = pos;
                
                // Write some logging.
                Log.AppendEntry(" {0}-side:", QTMath.MktSideToLongString(tradeSide));
                Log.AppendEntry(" Pos={0:+0;-0;0}", base.m_Position[tradeSide]);
                foreach (KeyValuePair<PricingEngine, int> kv in base.m_FillQty[tradeSide])
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

        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //
        //
        //
        //
        //
        //
        #endregion//Private Methods

        #region Class CompareQuoteByEngineID
        // *****************************************************************
        // ****                    Helper Classes                     ****
        // *****************************************************************
        public class CompareQuoteByEngineID : IComparer<Quote>
        {
            public int Compare(Quote x, Quote y)
            {
                int comparedValue = x.PricingEngine.EngineID.CompareTo(y.PricingEngine.EngineID);
                // Can handle equality
                //if (compareId == 0)
                //{
                //    return x.OrderID.CompareTo(y.OrderID);
                //}
                return comparedValue;
            }
        }
        #endregion

        #region no IStringifiable
        // *****************************************************************
        // ****                    IStringifiable                       ****
        // *****************************************************************
        //
        //
        //
        #endregion//IStringifiable




    }//end class




}
