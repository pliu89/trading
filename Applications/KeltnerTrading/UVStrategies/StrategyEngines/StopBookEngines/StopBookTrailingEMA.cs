using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UV.Strategies.StrategyEngines.StopBookEngines
{
    using UV.Lib.IO.Xml;
    using UV.Lib.Utilities;
    using UV.Lib.Hubs;

    /// <summary>
    /// This is a specialized stop book with a trailing stop that 
    /// utilizes an EMA filter to move the stop closer to the current price.
    /// It doesn't "trail" the way a typically rachet style trailing stop works.
    /// </summary>
    public class StopBookTrailingEMA : StopBookEngine, IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        private double[] m_TrailingStopAlpha = new double[2];           // trailing stop alpha, one for each side of market.
        private double[] m_TrailingStopTheta = new double[2];           // trailing stop theta, one for each side of market.

        private ZGraphEngine m_GraphEngine;
        private int m_GraphId;
        private string m_StopCurvveName;

        private int m_PlotUpdateFrequency = 10;                         // seconds between replotting the "stop" line
        private int m_PlotUpdateCount = 0;                              // simple counter.
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        public StopBookTrailingEMA()
            : base()
        {
            base.IsTrailing = true;     // this stop has to be set as trailing.
            base.IsBlockingResetOnCrossing = false; // this has to be false also for the functionality here to work
        }
        //       
        #endregion//Constructors


        #region no Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// This is the update method for this specialized stop book.
        /// The currently volatility must be passed in each time it is called.
        /// This is used for calculating the initial stop price from which we evolve towards
        /// the current price.
        /// </summary>
        /// <param name="currentVolatility"></param>
        public override void Update(double currentVolatility)
        { // do not call base class - this handles all needed functionality.
            base.StopBase = currentVolatility;
            if (m_StopSign != 0)
            {	// We are stopped.				
                //
                // Check blocking timeout.
                //
                if (m_IsBlockingTimeOut)
                {
                    m_BlockingTimeOutRemaining -= 1;		// decrease countdown to timeout.
                    if (m_BlockingTimeOutRemaining <= 0)
                    {
                        if (IsLogWrite) Log.NewEntry(LogLevel.Major, "StopRule: Blocking timed out. StopLoss for {1} returning to normal trading on {0} side."
                                                , QTMath.MktSignToString(m_StopSign), m_Name);
                        m_StopSign = 0;    // return to normal trading.	
                        this.IsUpdateRequired = true;
                        base.BroadcastParameters();
                    }
                }
            }
        }
        //
        //
        /// <summary>
        /// Called by a model to add a graphing engine if we would like our stop prices to be 
        /// added to the graph.
        /// </summary>
        /// <param name="zGraphEngine"></param>
        /// <param name="graphID"></param>
        /// <param name="curveName"></param>
        public void AddGraphEngine(ZGraphEngine zGraphEngine, int graphID, string curveName)
        {
            m_GraphEngine = zGraphEngine;
            m_GraphId = graphID;
            m_StopCurvveName = curveName;
        }
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
        /// Override to allow us to set our "Theta" or multiplier of the volatility for our
        /// initial stop
        /// </summary>
        /// <param name="signedQty"></param>
        /// <param name="fillPrice"></param>
        /// <param name="preFillPosition"></param>
        protected override void UpdateNewFill(int signedQty, double fillPrice, int preFillPosition)
        {
            //
            // Initial stop price:
            // StopPrice[0] = FillPrice - StopSign*Theta*Vol[0]
            //
            base.StopBaseMultiplier = m_TrailingStopTheta[QTMath.MktSignToMktSide(signedQty)];  // set our multiplier for this side to theta for this fill!
            base.UpdateNewFill(signedQty, fillPrice, preFillPosition);
            if (Position != 0 && m_GraphEngine != null)
            {
                m_GraphEngine.AddPoint(m_GraphId, m_StopCurvveName, NearestStopPrice);
                m_PlotUpdateCount = 0;
            }
        }
        //
        /// <summary>
        /// This override provides the implementation for the specialized EMA trailing
        /// stops.
        /// </summary>
        /// <param name="mktPrice"></param>
        protected override void UpdateTrailingStops(double mktPrice)
        {
            int posSign = Math.Sign(base.Position);
            int posSide = QTMath.MktSignToMktSide(posSign);
            m_PriceToDelete.Clear();									// place to store old stop price.
            m_PriceToAdd.Clear();										// place to store new stop price.
            // Check state of each stop price.
            for (int i = 0; i < m_StopPriceList.Count; ++i)				// loop thru each stop-price in list.
            {
                //  Compute the new stop price for this level.
                //  Stop price evolution:
                //  StopPrice[Time1] = Alpha*StopPrice[Time0] + (1-Alpha)*MidPrice[Time1]
                //
                double alpha = m_TrailingStopAlpha[posSide];
                double newStopPrice = m_StopPriceList[i] * alpha + (1 - alpha) * mktPrice;

                m_PriceToDelete.Add(m_StopPriceList[i]);			
                m_PriceToAdd.Add(newStopPrice);
            }//next stop level i
            // Update our stop lists.
            for (int i = 0; i < m_PriceToAdd.Count; ++i)
            {
                double newStopPrice = m_PriceToAdd[i];
                if (!m_StopPriceList.Contains(newStopPrice))
                {
                    m_StopPriceList.Add(newStopPrice);
                    m_StopPriceQty.Add(newStopPrice, 0);			// put a zero qty space holder.
                }
                double oldStopPrice = m_PriceToDelete[i];			// original price to remove now.
                m_StopPriceList.Remove(oldStopPrice);
                m_StopPriceQty[newStopPrice] += m_StopPriceQty[oldStopPrice];
                m_StopPriceQty.Remove(oldStopPrice);
            }

            m_PlotUpdateCount++;
            if (Position != 0 && m_GraphEngine != null && m_PlotUpdateCount % m_PlotUpdateFrequency == 0)
                m_GraphEngine.AddPoint(m_GraphId, m_StopCurvveName, NearestStopPrice);
            
        }
        //
        //

        #endregion//Private Methods


        #region no Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        #endregion//Event Handlers


        #region Istringifiable implementation
        // *****************************************************************
        // ****                     Istringifiable                      ****
        // *****************************************************************
        //
        //
        string IStringifiable.GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            s.Append(base.GetAttributes());
            s.AppendFormat(" StopManager1_Stop0.TrailingStop.T={0}", UV.Lib.Utilities.QTMath.AlphaToPeriod(m_TrailingStopAlpha[0]));
            s.AppendFormat(" StopManager1_Stop1.TrailingStop.T={0}", UV.Lib.Utilities.QTMath.AlphaToPeriod(m_TrailingStopAlpha[1]));
            s.AppendFormat(" StopManager1_Stop0.TrailingStop.BlockingPeriodSecs={0}", base.BlockingTimeOut);
            s.AppendFormat(" StopManager1_Stop0.TrailingStop.Theta={0}",m_TrailingStopTheta[0]);
            s.AppendFormat(" StopManager1_Stop1.TrailingStop.Theta={0}",m_TrailingStopTheta[1]);            
            return s.ToString();
        }
        //
        List<IStringifiable> IStringifiable.GetElements()
        {
            return base.GetElements();
        }

        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            base.SetAttributes(attributes);
            double x;
            foreach (KeyValuePair<string, string> attr in attributes)
            {
                if (attr.Key.Equals("StopManager1_Stop0.TrailingStop.T") && double.TryParse(attr.Value, out x))
                    m_TrailingStopAlpha[0] = UV.Lib.Utilities.QTMath.PeriodToAlpha((int)x, 1);
                else if (attr.Key.Equals("StopManager1_Stop1.TrailingStop.T") && double.TryParse(attr.Value, out x))
                    m_TrailingStopAlpha[1] = UV.Lib.Utilities.QTMath.PeriodToAlpha((int)x, 1);
                else if ((attr.Key.Equals("StopManager1_Stop0.TrailingStop.BlockingPeriodSecs") || attr.Key.Equals("StopManager1_Stop1.TrailingStop.BlockingPeriodSecs")) && double.TryParse(attr.Value, out x))
                    base.BlockingTimeOut = (int)x;
                else if (attr.Key.Equals("StopManager1_Stop0.TrailingStop.Theta") && double.TryParse(attr.Value, out x))
                    m_TrailingStopTheta[0] = x;
                else if (attr.Key.Equals("StopManager1_Stop1.TrailingStop.Theta") && double.TryParse(attr.Value, out x))
                    m_TrailingStopTheta[1] = x;
            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
            base.AddSubElement(subElement);
        }
        #endregion // Istring
    }
}
