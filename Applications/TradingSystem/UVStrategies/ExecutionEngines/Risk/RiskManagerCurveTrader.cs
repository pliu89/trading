using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UV.Strategies.ExecutionEngines.Risk
{
    using UV.Strategies.ExecutionEngines.OrderEngines.TermStructures;
    using UV.Strategies.ExecutionEngines.Scratchers;

    using UV.Lib.Hubs;

    /// <summary>
    /// Object to manage the risk of single "curve trader" execution unit.  
    /// Note: each curve trader unit will have its own risk manager in the current design. 
    /// This means we will have to deal with the fact that an open position in one execution unit
    /// may be properly hedged in another and therefore unfortunately mis represented to the
    /// risk maanger.  We can either try and create a thread responsible for risk hub thread responsible
    /// for the risk of the entire thing or figure out messaging between them later.  A Risk Hub sounds
    /// like it may be an optimal solution, but if any risk checks need to happen pre order submission
    /// that wouldn't be the fastest option.  It is possible the risk managers for each unit only do part 
    /// of the risk management and another hub does the bigger pnl and position aggregation functionality.
    /// </summary>
    public class RiskManagerCurveTrader : RiskManager
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //       
        CurveTrader m_CurveTrader;                          // the curve trader I am directly responsible for.
        ScratchManager m_ScratchManager;                    // scratch manager 

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //    
        protected override void SetupInitialize(Lib.Engines.IEngineHub myEngineHub, Lib.Engines.IEngineContainer engineContainer, int engineID, bool setupGui)
        {
            if (typeof(UV.Strategies.ExecutionHubs.ExecutionContainers.MultiThreadContainer).IsAssignableFrom(engineContainer.GetType()))
            {   // this is the "first" set up call from the manager container.
                base.SetupInitialize(myEngineHub, engineContainer, engineID, setupGui);
            }
            else
            {   // this is the second set up call from the correct container, add correct sub engine mappings 
            }
        }

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
        //
        // ***********************************************************
        // ****               IsThresholdsValid                   ****
        // ***********************************************************
        /// <summary>
        /// Caller would like to validate Threshold values for a curve leg along with its 
        /// corresponding scratcher
        /// </summary>
        /// <param name="curveLeg"></param>
        /// <returns></returns>
        public bool IsLegThresholdsValid(CurveLeg curveLeg)
        {
            // TODO: Add other risk checks, does it matter if this is a "quote leg"

            if (curveLeg.JoinThreshold < curveLeg.PullThreshold)
            {   // our join threshold must be greater than equal!
                m_Log.NewEntry(LogLevel.Error, "RiskManager.IsThresholdsValid: JoinThreshold={0} must be greater than or equal to PullThreshold={1}",
                               curveLeg.JoinThreshold, curveLeg.PullThreshold);
                return false;
            }


            if (curveLeg.m_Scratcher.ActiveScratch)
            {  // our scratcher is in active mode
                if (curveLeg.PullThreshold < curveLeg.m_Scratcher.ScratchThreshold)
                {  // we aren't pulling before we are scratching!
                    m_Log.NewEntry(LogLevel.Error, "RiskManager.IsThresholdsValid: PullThreshold={0} must be greater than ScratchThreshold={1}",
                                    curveLeg.PullThreshold, curveLeg.m_Scratcher.ScratchThreshold);
                    return false;
                }
            }
            return true;
        }
        //
        //
        /// <summary>
        /// This overload will check the parameters for all curve legs
        /// </summary>
        /// <returns></returns>
        public bool IsLegThresholdsValid()
        {
            for (int i = 0; i < m_CurveTrader.m_CurveLegs.Count; i++)
            {   // for each leg, check its thresholds
                if (!IsLegThresholdsValid(m_CurveTrader.m_CurveLegs[i]))
                {
                    return false;
                }
            }
            return true;
        }
        //
        //
        //**************************************************************
        // ****           CheckWorkingOrderCounts()                 ****
        //**************************************************************
        //
        //
        //
        protected override bool CheckWorkingOrderCounts()
        {
            // TODO: build this out
            return false;
        }
        //
        //
        /// <summary>
        /// Aggregate total PnL across all legs of the strategy, and ensure we aren't below
        /// our allowed loss 
        /// </summary>
        /// <returns>true if we are below our maximum allowed loss</returns>
        protected override bool PnLCheck()
        {
            // TODO: ?Cheng Implementation
            //if (m_CurveTrader == null)
            //    return true;

            //if (!m_CurveTrader.m_IsLegSetupCompleted)
            //    return false;
            return true;

            //m_PnL = 0; // reset PnL to 0
            //double midPrice = (m_ExecutionContainer.m_Markets[m_CurveTrader.m_InstrumentDetails.InstrumentName].Price[UV.Lib.Utilities.QTMath.BidSide][0] +
            //m_ExecutionContainer.m_Markets[m_CurveTrader.m_InstrumentDetails.InstrumentName].Price[UV.Lib.Utilities.QTMath.AskSide][0]) / 2;
            //m_PnL = m_IOrderEngine.GetFillBook().m_RealizedGain + m_IOrderEngine.GetFillBook().UnrealizedDollarGains(midPrice);
            //if (m_PnL < m_MaxLossPnL)
            //{
            //    m_Log.NewEntry(LogLevel.Error, "Max Loss PnL Has Been Exceeded: Current PnL = {0} MaxLossPnL = {1}", m_PnL, m_MaxLossPnL);
            //    return true;
            //}
            //else
            //    return false;
        }
        /// <summary>
        /// Caller would like to turn off all order routing, and turn the m_IsRiskTriggered to true.  This will allow for the next second for
        /// the strategy to completely stop.  Hopefully this allows for some hedging to occcur prior to the hard stop of all functionality.
        /// </summary>
        protected override void FlagRiskEvents()
        {
            // TODO: ?Cheng Implementation
            //if (m_CurveTrader == null)
            //    return;
            //m_IOrderEngine.IsRiskCheckPassed = false;
            //m_CurveTrader.BroadcastParameter((IEngineHub)m_CurveTrader.m_Hub, m_CurveTrader.m_ExecutionContainer, "IsRiskCheckPassed");
            //m_IOrderEngine.CancelAllOrders();
            //m_IsRiskTriggered = true;
            //UV.Lib.FrontEnds.Utilities.GuiCreator.ShowMessageBox(string.Format("{0} : Risk Event Triggered! Trading is turning off.", m_CurveTrader), "Risk Triggered");
        }
        //
        //
        public override void TimerSubscriberUpdate()
        {
            // TODO: ?Cheng Implementation
            //if (m_CurveTrader == null)
            //    return;

            //if (PnLCheck() | CheckQuoteLimits())    // max loss pnl tripped or max number of quotes per second exceeded
            //    FlagRiskEvents();
            //m_SecondCount++;                            // increment our second counter
            //if (m_IsRiskTriggered)
            //{
            //    m_RiskCount++;
            //    if (m_RiskCount == 2)
            //    {
            //        m_Log.NewEntry(LogLevel.Error, "Risk Event Triggered! Pausing Strategy");
            //    }
            //}
            //m_TotalNumberOfQuotes += m_NumberOfQuotesThisSecond; // Add to total's count.
            //m_NumberOfQuotesThisSecond = 0; // reset count each second.
            //base.BroadcastParameter((IEngineHub)m_CurveTrader.m_Hub, m_CurveTrader.m_ExecutionContainer, 4);
            //base.BroadcastParameter((IEngineHub)m_CurveTrader.m_Hub, m_CurveTrader.m_ExecutionContainer, 5);
        }
        #endregion//Public Methods


        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
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
    }
}
