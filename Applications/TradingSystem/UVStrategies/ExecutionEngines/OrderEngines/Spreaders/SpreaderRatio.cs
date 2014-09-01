using System;
using System.Collections.Generic;
using System.Text;
/*
namespace UV.Execution.Quoters
{
    using UVSyntheticFill = UV.Execution.SpreaderFills.SyntheticFill;
    using UVFill = UV.Lib.OrderBookHubs.Fill;
    using UV.Lib.OrderBookHubs;
    /// <summary>
    /// This derived class is intended to deal specifically with quoting ratio spreads and pricing them correctly.  
    /// They inherently must only be two legs ie spread = LegA / LegB
    /// 
    /// TODO:
    /// 1. Add logging from hub.
    /// 2. allow "stop" or "exit" method when errors"
    /// </summary>
    public class QuoterRatio : Quoter
    {
        #region no Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public QuoterRatio(BrePortfolioExecutor brePortfolioExec)
            : base(brePortfolioExec)
        {
            int nLegs = m_BrePortfolioExecutor.m_UVInstrList.Count;    // number of legs 
            if (nLegs > 2)
            {
                base.m_BrePortfolioExecutor.m_PortfolioExecutor.Log("Ratio Quoter Must Only Be Given 2 Legs! Strategy Will Exit Now");
                //TODO: stop if this happens
                //base.m_BrePortfolioExecutor.OnExit(QuantOffice.Execution.ExitState.AbortedByUser);
            }
            base.m_SpreadFillGenerator = new SpreaderFills.SpreaderRatioFillGenerator(base.m_SyntheticSpreadFillBook.Instrument,              // override our spread fill generator with one that knows how to handle ratios
                                                                      base.m_LegRatios, base.m_LegPriceMultipliers, 
                                                                      base.m_LegFillBooks);
        }//constructor
        //
        //       
        #endregion//Constructors

        #region no Properties
        // *****************************************************************
        // ****                       Properties                        ****
        // *****************************************************************
        //
        //
        #endregion //Properties

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        // *****************************************************************
        // ****                 Add Hedge Fill()                        ****
        // *****************************************************************
        public override void AddHedgeFill(int stratSide, int legId, UVFill fill)
        {
            // Add to Fill and test for trade completion.
            m_LegFillBooks[legId].TryAdd(fill);
            UVSyntheticFill newSyntheticFill;
            if (base.m_SpreadFillGenerator.TryGenerateSyntheticFill(stratSide, out newSyntheticFill))// find out if we now have sufficient qty to create a synthetic
            {
                base.m_SpreaderPos[UV.Lib.Utilities.QTMath.MktSignToMktSide(newSyntheticFill.Qty)] += newSyntheticFill.Qty;
                m_SyntheticSpreadFillBook.TryAdd(newSyntheticFill);         // add fill to list                
                m_BrePortfolioExecutor.m_PortfolioExecutor.Log(string.Format("         {0}", newSyntheticFill));
                base.OnQuoterStateChange(UV.Lib.Utilities.QTMath.MktSignToMktSide(newSyntheticFill.Qty));
            }
        }//AddHedgeFill()
        //
        //
        #endregion//Public Methods

        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        // *****************************************************
        // ****        UpdateQuotingLegPrices()             ****
        // *****************************************************
        //
        /// <summary>
        /// Caller would like to update all the quote leg prices for instruments we are actively quoting.
        /// </summary>
        /// <param name="strategySide"></param>
        /// <param name="changedLeg"></param>
        protected override void UpdateQuotingLegPrices(int strategySide, int changedLeg)
        {
            switch (changedLeg)
            {
                case -1: // update all legs for a side
                    UpdateLegAQuotePrice(strategySide);
                    UpdateLegBQuotePrice(strategySide);
                    break;
                case 0:  // leg A has changed, upate our quotes in leg B
                    UpdateLegBQuotePrice(strategySide);
                    break;
                case 1: // leg B has changed, upate our quotes in leg A
                    UpdateLegAQuotePrice(strategySide); ;
                    break;
                default: // something wierd happened!
                    m_BrePortfolioExecutor.m_PortfolioExecutor.Log("Ratio Quoter Recieved Unknown Request To Update Quoting Leg Prices");
                    break;
            }
        }
        //
        //
        //
        // *****************************************************
        // ****        UpdateLegAQuotePrice()               ****
        // *****************************************************
        // SpreadPrice = (LegA * LegAMult) / (LegB*abs(LegBMult))
        // LegAQuotePrice = (LegB*abs(LegBMult)) * SpreadPrice
        // LegBQuotePrice = ((LegA * LegAMult) / SpreadPrice) / abs(legBMult)
        /// <summary>
        /// Update our quotng price on Leg A of our ratio spread (the numertaor leg)
        /// </summary>
        /// <param name="strategySide"></param>
        private void UpdateLegAQuotePrice(int strategySide)
        {
            int strategySign = UV.Lib.Utilities.QTMath.MktSideToMktSign(strategySide);                                                            // side of the strategy we are looking at
            int legASide = UV.Lib.Utilities.QTMath.MktSignToMktSide(strategySign * Math.Sign(base.m_Instruments[0].m_LegPriceMultiplier));        // what side is the numerator legA on this strat side
            int legBSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(-strategySign * Math.Sign(base.m_Instruments[1].m_LegPriceMultiplier));       // what side is the denominator legB on this strat side
            base.m_QuotingLegPrices[0][legASide] = (m_StrategyWorkingPrice[strategySide] *                                              // LegAQuotePrice = ((LegB*abs(LegBMult)) * SpreadPrice) / LegAMult
                                                   base.m_Instruments[1].m_LeanablePrices[legBSide] *
                                                   Math.Abs(base.m_Instruments[1].m_LegPriceMultiplier)) / 
                                                   (base.m_Instruments[0].m_LegPriceMultiplier);
        }
        //
        //
        // *****************************************************
        // ****        UpdateLegBQuotePrice()               ****
        // *****************************************************
        /// <summary>
        /// Update our quoting price on Leg B of our ratio spread (the denominator leg)
        /// </summary>
        /// <param name="strategySide"></param>
        private void UpdateLegBQuotePrice(int strategySide)
        {
            int strategySign = UV.Lib.Utilities.QTMath.MktSideToMktSign(strategySide);                                                            // side of the strategy we are looking at
            int legASide = UV.Lib.Utilities.QTMath.MktSignToMktSide(-strategySign * Math.Sign(base.m_Instruments[0].m_LegPriceMultiplier));       // what side is the numerator legA on this strat side
            int legBSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(strategySign * Math.Sign(base.m_Instruments[1].m_LegPriceMultiplier));        // what side is the denominator legB on this strat side
            base.m_QuotingLegPrices[1][legBSide] = ((base.m_Instruments[0].m_LegPriceMultiplier *                                       // LegBQuotePrice = ((LegA * LegAMult) / SpreadPrice) / abs(legBMult)
                                                       base.m_Instruments[0].m_LeanablePrices[legASide]) /
                                                       m_StrategyWorkingPrice[strategySide]) /
                                                       Math.Abs(base.m_Instruments[1].m_LegPriceMultiplier);
        }
        //
        //
        //
        // *************************************************************
        // ****              UpdateMarketPrice()                    ****
        // *************************************************************
        /// <summary>
        /// Computes the current Bid/Ask of the strategy.
        /// </summary>
        protected override void UpdateMarketPrice()
        { // Calculate the market for this strategy. SpreadPrice = (LegA * LegAMult) / (LegB*abs(LegBMult))
            double previousBidPrice = m_StrategyMarketPrice[UV.Lib.Utilities.QTMath.BidSide];       // save our previous bid and ask prices 
            double previousAskPrice = m_StrategyMarketPrice[UV.Lib.Utilities.QTMath.AskSide];
            m_StrategyMarketPrice[UV.Lib.Utilities.QTMath.BidSide] = (base.m_Instruments[0].m_Prices[UV.Lib.Utilities.QTMath.BidSide][0] * 
                                                                                  base.m_Instruments[0].m_LegPriceMultiplier) / 
                                                                                 (base.m_Instruments[1].m_Prices[UV.Lib.Utilities.QTMath.AskSide][0] * 
                                                                                  Math.Abs(base.m_Instruments[1].m_LegPriceMultiplier));
            m_StrategyMarketPrice[UV.Lib.Utilities.QTMath.AskSide] = (base.m_Instruments[0].m_Prices[UV.Lib.Utilities.QTMath.AskSide][0] *
                                                                                  base.m_Instruments[0].m_LegPriceMultiplier) /
                                                                                 (base.m_Instruments[1].m_Prices[UV.Lib.Utilities.QTMath.BidSide][0] *
                                                                                  Math.Abs(base.m_Instruments[1].m_LegPriceMultiplier));

            if ((m_StrategyMarketPrice[UV.Lib.Utilities.QTMath.BidSide] != previousBidPrice) ||
                (previousAskPrice != m_StrategyMarketPrice[UV.Lib.Utilities.QTMath.AskSide]))       // if our prices have changed we need to fire an event to our subscribers/
                base.OnSpreadPriceChanged();
        }//UpdateMarketPrice()
        //
        //
        //
        #endregion//Private Methods

        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                      ****
        // *****************************************************************
        //
        //
        // *****************************************************************
        // ****                 OrderBook_OrderFilled()                ****
        // *****************************************************************
        protected override void OrderBook_OrderFilled(object sender, EventArgs eventArgs)  // this needs to be edited to use a new sythetic fill generator class that can compute ratios!
        {
            OrderEventArgs orderEventArgs = (OrderEventArgs)eventArgs;
            // Get information about who was filled.
            int legId;
            if (!m_InstrumentToId.TryGetValue(orderEventArgs.Order.Symbol, out legId))
                return;
            UVFill fill = orderEventArgs.Fill;
            if (fill == null)
                return;
            int legSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(fill.Qty);
            int stratSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(fill.Qty * m_LegRatios[legId]);

            m_IsPartialFilled[stratSide] = orderEventArgs.Order.WorkingQtyPending != 0;     // if this order has remaining working qty we are partially filled on our quote.
            m_LegFillBooks[legId].TryAdd(fill);                                             // Add to Fill Queue and List record
            m_HedgeManager.HedgeFill(legId, fill);                                          // Call Hedger - this will set Hedger.IsHung state.

            UVSyntheticFill newSyntheticFill;                                               // check to see if we can create a compled synthetic fill now.
            if (base.m_SpreadFillGenerator.TryGenerateSyntheticFill(stratSide, out newSyntheticFill))
            {
                m_SpreaderPos[UV.Lib.Utilities.QTMath.MktSignToMktSide(newSyntheticFill.Qty)] += newSyntheticFill.Qty;
                m_SyntheticSpreadFillBook.TryAdd(newSyntheticFill);                         // add fill to list  
                m_BrePortfolioExecutor.m_PortfolioExecutor.Log(string.Format("      {0}", newSyntheticFill));
            }
            if (m_IsPartialFilled[stratSide] && m_PendingQuoteOrders[legId][legSide] != null)
            { // we are partialled and have a pending order that qty now needs to be updated in case we end up submitting it
                m_PendingQuoteOrders[legId][legSide].OriginalQtyPending = orderEventArgs.Order.WorkingQtyPending;
            }
            m_QuoteFillCount[legId][legSide] += fill.Qty;                                   // add fill qty to unhedged quote count
            m_QuotePartialCount[legId][legSide] = (m_QuoteFillCount[legId][legSide] % (int)m_LegRatios[legId]);
            //Math.DivRem(m_QuoteFillCount[legId][legSide], m_LegRatios[legId], out m_QuotePartialCount[legId][legSide]); // update running partial counts
            OnQuoterStateChange(stratSide);
        }//OrderBook_OrderFilled()

        #endregion//Event Handlers

        #region no Events
        //
        //
        //
        #endregion //Events
    }
}
*/
