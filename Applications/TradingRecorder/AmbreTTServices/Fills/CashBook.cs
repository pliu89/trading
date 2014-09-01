using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ambre.TTServices.Fills
{
    using Misty.Lib.Products;
    using Misty.Lib.IO.Xml;                         // for IStringifiable
    using Misty.Lib.OrderHubs;
    using System.Threading;

    public class CashBook : IFillBook, IStringifiable
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // Control parameters
        private InstrumentName m_Name;
        private double m_DollarPerPoint = 1.0;
        private double m_SmallestFillPriceIncr = 0.01;
        private string m_CurrencyName;
        private double m_CurrencyRate = 1.0;
        private ReaderWriterLockSlim m_Lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        //
        // Fill book PnL variables.
        //
        public DateTime RealizedGainStartTime = DateTime.Now;           // Time from which PnL will be accumulated.
        private double m_RealizedGain = 0.0;                            // measured in points.  
        private double m_RealizedStartingGain = 0.0;                    // Real gain at the last time we reset RealGain; CummulativeRealGain = RealGain + RealGainAtStart
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public CashBook()
        {

        }
        public CashBook(double smallestFillPriceIncrement, double dollarAmtOfSmallestPriceIncrement, InstrumentName name)
        {
            this.m_Name = name;
            m_DollarPerPoint = 1;
            m_SmallestFillPriceIncr = 1;
        }
        //
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        public InstrumentName Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }
        public double DollarPerPoint
        {
            get { return m_DollarPerPoint; }
            set { m_DollarPerPoint = value; }
        }
        public double SmallestFillPriceIncr
        {
            get { return m_SmallestFillPriceIncr; }
            set { m_SmallestFillPriceIncr = value; }
        }
        public ReaderWriterLockSlim Lock
        {
            get { return m_Lock; }
        }
        public double RealizedDollarGains
        {
            get { return m_RealizedGain; }
            set { m_RealizedGain = value; }
        }
        public double RealizedStartingDollarGains
        {
            get { return m_RealizedStartingGain; }
            set { m_RealizedStartingGain = value; }
        }
        public int NetPosition
        {
            get { return 0; }
        }
        public double AveragePrice
        {
            get { return 0; }
        }
        public List<Fill> Fills
        {
            get { return null; }
        }
        public int Volume
        {
            get { return 0; }
        }
        public int StartingVolume
        {
            get { return 0; }
        }
        public string CurrencyName
        {
            get { return m_CurrencyName; }
            set { m_CurrencyName = value; }
        }
        public double CurrencyRate
        {
            get { return m_CurrencyRate; }
            set { m_CurrencyRate = value; }
        }
        //
        #endregion//Properties


        #region Public Methods
        public virtual void Add(Fill aFill)
        {
            return;
        }
        public void ResetRealizedDollarGains()
        {
            m_RealizedStartingGain += m_RealizedGain;               // Roll the gain into the cummulative gain.
            m_RealizedGain = 0;
        }
        public void ResetRealizedDollarGains(double todaysRealPnL, double openingRealPnL)
        {
            if (!double.IsNaN(todaysRealPnL))
                m_RealizedGain = todaysRealPnL;
            if (!double.IsNaN(openingRealPnL))
                m_RealizedStartingGain = openingRealPnL;
        }
        public void RecalculateAll()
        {
            return;
        }
        public bool TryAdd(FillEventArgs eventArg, out RejectedFills.RejectedFillEventArgs rejection)
        {
            rejection = null;
            return true;
        }
        public bool IsFillNew(FillEventArgs fillEventArgs, out RejectedFills.RejectedFillEventArgs rejectedEventArgs)
        {
            rejectedEventArgs = null;
            return true;
        }
        public void GetRejectedFills(ref List<RejectedFills.RejectedFillEventArgs> rejectedFills)
        {
            return;
        }
        public double UnrealizedDollarGains()
        {
            return 0;
        }
        public double UnrealizedDollarGains(double midPrices)
        {
            return 0;
        }
        //
        #endregion


        #region IStringifiable Implementation
        // *****************************************************************
        // ****                     IStringifiable                      ****
        // *****************************************************************
        public string GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            s.AppendFormat("InstrumentName={0}", InstrumentName.Serialize(m_Name));
            s.AppendFormat(" CurrencyName={0}", CurrencyName);
            s.AppendFormat(" RealGain={0} RealStartingGain={1}", m_RealizedGain, m_RealizedStartingGain);
            s.AppendFormat(" CurrencyRate={0}", CurrencyRate);
            return s.ToString();
        }
        public List<IStringifiable> GetElements()
        {
            return null;
        }
        public void SetAttributes(Dictionary<string, string> attributes)
        {
            double x;
            InstrumentName name;
            foreach (string key in attributes.Keys)
                if (key.Equals("InstrumentName") && InstrumentName.TryDeserialize(attributes[key], out name))
                    this.m_Name = name;
                else if (key.Equals("CurrencyName") && !string.IsNullOrEmpty(attributes[key]))
                    this.CurrencyName = attributes[key];
                else if (key.Equals("RealGain") && Double.TryParse(attributes[key], out x))
                    this.m_RealizedGain = x;
                else if (key.Equals("RealStartingGain") && Double.TryParse(attributes[key], out x))
                    this.m_RealizedStartingGain = x;
                else if (key.Equals("CurrencyRate") && Double.TryParse(attributes[key], out x))
                    this.CurrencyRate = x;
        }
        public void AddSubElement(IStringifiable subElement)
        {

        }
        //
        //
        #endregion // IStringifiable
    }
}
