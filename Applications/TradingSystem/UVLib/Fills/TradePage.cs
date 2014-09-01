using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.Fills
{
    using UV.Lib.Products;
    using UV.Lib.IO.Xml;

    /// <summary>
    /// Container for fills, either to Enter or Exit a strategy.
    /// This class exists solely to stringify Fill lists inside a Trade object.
    /// </summary>
    public class TradePage : IStringifiable, ICloneable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************        
        public InstrumentName InstrumentName;
        public TradePageType PageType = TradePageType.Unknown;
        public List<Fill> Fills = null;

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public TradePage()                              // IString constructor
        {
            Fills = new List<Fill>();
        }
        public TradePage(InstrumentName instrumentName, TradePageType type)
        {
            this.InstrumentName = instrumentName;
            this.PageType = type;
            Fills = new List<Fill>();
        }
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


        #region IStringifiable
        // *****************************************************************
        // ****                     IStringifiable                      ****
        // *****************************************************************        
        string IStringifiable.GetAttributes()
        {
            return string.Format("PageType={0}", this.PageType);
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            List<IStringifiable> list = new List<IStringifiable>();
            foreach (Fill fill in this.Fills)
                list.Add(fill);
            return list;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            TradePageType pageType;
            foreach (KeyValuePair<string, string> a in attributes)
            {
                if (a.Key == "PageType" && Enum.TryParse<TradePageType>(a.Value, out pageType))
                    this.PageType = pageType;
            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
            if (subElement is Fill)
                this.Fills.Add((Fill)subElement);
        }
        #endregion//Private Methods

        #region ICloneable 
        //
        //
        object ICloneable.Clone()
        {
            return this.Clone();
        }
        /// <summary>
        /// </summary>
        /// <returns>Deep clone of Trade</returns>
        public TradePage Clone()
        {
            TradePage newPage = (TradePage)this.MemberwiseClone();
            foreach (Fill fill in this.Fills)
                newPage.Fills.Add(fill.Clone());
            return newPage;
        }
        //
        #endregion ICloneable implementation


    }//end class



}
