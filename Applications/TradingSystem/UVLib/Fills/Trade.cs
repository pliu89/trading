using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace UV.Lib.Fills
{
    using UV.Lib.Products;
    using UV.Lib.MarketHubs;
    using UV.Lib.Utilities;
    using UV.Lib.IO.Xml;             // for IStringifiable

    public class Trade : ICloneable, IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        //
        // Members:
        // 
        public int TradeId = 0;
        public Dictionary<InstrumentName, TradePage> Entry = new Dictionary<InstrumentName, TradePage>();
        public Dictionary<InstrumentName, TradePage> Exit = new Dictionary<InstrumentName, TradePage>();

        //
        // Strategy definition
        //
        private List<PriceLeg> m_Legs = new List<PriceLeg>();// PriceLegs contain information we need.
        //private int m_Side = UnknownSide;                   // Side of the entry, buyside or sellside.
        
        //
        // Dynamic values
        // 
        public bool IsHung = false;                         // Fills in either Entry or Exit are not sufficient for hedge.
        public bool IsPartialFilled = false;                // Trade is missing Entry/Exit fills it wants.  But fills are hedged.
        public bool IsComplete = false;                     // Execution is finished with this trade.
        
        //private double m_Strategy = 0;                    // Qty of strategy units.
        

        //
        // Constants
        //
        public const int BuySide = QTMath.BuySide;
        public const int SellSide = QTMath.SellSide;
        public const int UnknownSide = QTMath.UnknownSide;

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public Trade()      //Empty constructor needed for IStringify.
        {            
        }
        //
        /// <summary>
        /// The PricingLegs are cloned here.
        /// </summary>
        /// <param name="legs"></param>
        public Trade(List<PriceLeg> legs)
        {
            // Define our instrument legs.
            foreach (PriceLeg leg in legs)
                m_Legs.Add(leg.Clone());
        }
        // TODO: Create an overloading that takes QuoteLegs,
        // if that is more convenient for execution engines.
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


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        // *********************************************
        // ****             AddFill()               ****
        // *********************************************
        /// <summary>
        /// Add a new fill to this Trade object.
        /// </summary>
        /// <param name="instrumentName"></param>
        /// <param name="type"></param>
        /// <param name="newFill"></param>
        public void AddFill(InstrumentName instrumentName, TradePageType type, Fill newFill)
        {
            // Locate appropriate TradePage, or if need be, create new page.
            Dictionary<InstrumentName, TradePage> tradePageDict = null;
            if (type == TradePageType.Entry)
                tradePageDict = this.Entry;
            else if (type == TradePageType.Exit)
                tradePageDict = this.Exit;
            TradePage page = null;
            if (tradePageDict.TryGetValue(instrumentName, out page) == false)
                page = new TradePage(instrumentName, type);

            // Add fill to page.
            page.Fills.Add(newFill);
        }// AddFill()
        //
        /// <summary>
        /// Shortcut to add fill to entry page.
        /// </summary>
        public void AddEntryFill(InstrumentName instrumentName, Fill newFill)
        {
            AddFill(instrumentName, TradePageType.Entry, newFill);
        }// AddEntry()
        //
        /// <summary>
        /// Shortcut to add fill to exit page.
        /// </summary>
        public void AddExitFill(InstrumentName instrumentName, Fill newFill)
        {
            AddFill(instrumentName, TradePageType.Exit, newFill);
        } // AddExit()
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
        #endregion//Private Methods


        #region IStringifiable implementation
        // *****************************************************************
        // ****                     IStringifiable                      ****
        // *****************************************************************
        string IStringifiable.GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            s.AppendFormat("TradeId={0}", this.TradeId);
            if (this.IsComplete)
                s.AppendFormat(" IsComplete={0}", this.IsComplete);
            if (this.IsPartialFilled)
                s.AppendFormat(" IsPartialFilled={0}", this.IsPartialFilled);
            if (this.IsHung)
                s.AppendFormat(" IsHung={0}", this.IsHung);

            return s.ToString();    

        }
        List<IStringifiable> IStringifiable.GetElements() 
        {
            List<IStringifiable> list = new List<IStringifiable>();
            foreach (PriceLeg leg in m_Legs)
                list.Add(leg);
            foreach (TradePage page in this.Entry.Values)
                list.Add(page);
            foreach (TradePage page in this.Exit.Values)
                list.Add(page);
            return list;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            bool b;
            int n;
            foreach (KeyValuePair<string,string>a in attributes)
            {
                if (a.Key.Equals("IsComplete") && bool.TryParse(a.Value, out b))
                    this.IsComplete = b;
                else if (a.Key.Equals("IsPartialFilled") && bool.TryParse(a.Value, out b))
                    this.IsPartialFilled = b;
                else if (a.Key.Equals("IsHung") && bool.TryParse(a.Value, out b))
                    this.IsHung = b;
                else if (a.Key.Equals("TradeId") && int.TryParse(a.Value, out n))
                    this.TradeId = n;
            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement) 
        { 
            if (subElement is PriceLeg)
                m_Legs.Add((PriceLeg)subElement);
            else if ( subElement is TradePage)
            {
                TradePage page = (TradePage)subElement;
                if (page.PageType == TradePageType.Entry)
                    this.Entry.Add(page.InstrumentName, page);
            }
        }
        //
        //
        #endregion // IStringifiable


        #region ICloneable implementation
        //
        //
        object ICloneable.Clone()
        {
            return this.Clone();
        }
        /// <summary>
        /// </summary>
        /// <returns>Deep clone of Trade</returns>
        public Trade Clone()
        {
            Trade newTrade = (Trade)this.MemberwiseClone();
            foreach (TradePage page in this.Entry.Values)
                newTrade.Entry.Add(page.InstrumentName, page.Clone());
            foreach (TradePage page in this.Exit.Values)
                newTrade.Exit.Add(page.InstrumentName, page.Clone());
            foreach (PriceLeg leg in this.m_Legs)
                newTrade.m_Legs.Add(leg.Clone());
            // update computed values now
            return newTrade;
        }
        //
        #endregion ICloneable implementation


    }
}
