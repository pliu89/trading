using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.OrderBookHubs
{
    using UV.Lib.Products;
    using UV.Lib.IO.Xml;                         // for IStringifiable

    /// <summary>
    /// A time ordered net-fill book.
    /// Its a "net" book in that it matches new fills with old fills, cancelling them
    /// if they are of opposite sides.  Therefore, the fills in this book are on one side
    /// of the market or the other; never both.
    /// </summary>
    public class FillBookLifo : IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // Control parameters
        public InstrumentName Name;
        public double m_DollarPerPoint = 1.0;
        public double m_SmallestFillPriceIncr = 0.01;

        // Internally maintained active fills.
        private List<Fill> m_FillsOpen = new List<Fill>(16);            // store for open fills that comprise our CURRENT position.
        private List<Fill> m_FillTrail = new List<Fill>(128);           // all fills received this session.
        
        public DateTime ExchangeTimeLast = DateTime.MinValue;
        public DateTime LocalTimeLast = DateTime.MinValue;

        private int m_NetPosition = 0;                                  // Short cut to remember current net position.

        //
        // Fill book PnL variables.
        //
        public DateTime RealizedGainStartTime = DateTime.Now;           // Time from which PnL will be accumulated.
        private double m_RealizedGain = 0.0;                            // measured in points.  
        private double m_RealizedStartingGain = 0.0;                    // Real gain at the last time we reset RealGain; CummulativeRealGain = RealGain + RealGainAtStart

        private int m_Volume = 0;                                       // Sum of total absolute quantity fill.
        private int m_StartingVolume = 0;


        private double m_AveCost = 0.0;                                 // Maintain the average fill price here.            
        private double m_LastMidPrice = 0;                              // Most recent price used for evaluating value of open position.

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public FillBookLifo()
        {

        }
        public FillBookLifo(double smallestFillPriceIncrement, double dollarAmtOfSmallestPriceIncrement, InstrumentName name)
        {
            this.Name = name;
            m_DollarPerPoint = dollarAmtOfSmallestPriceIncrement / smallestFillPriceIncrement;
            m_SmallestFillPriceIncr = smallestFillPriceIncrement;
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
        public double RealizedDollarGains
        {
            get { return m_RealizedGain * m_DollarPerPoint; }
        }
        public double RealizedStartingDollarGains
        {
            get { return m_RealizedStartingGain * m_DollarPerPoint; }
        }
        public int NetPosition
        {
            get { return m_NetPosition; }
        }
        public double AveragePrice
        {
            get { return m_AveCost; }
        }
        public List<Fill> Fills
        {
            get { return m_FillsOpen; }
        }
        public int Volume
        {
            get { return m_Volume; }
        }
        public int StartingVolume
        {
            get { return m_StartingVolume; }
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
        // ****                     Add()                       ****
        //
        /// <summary>
        /// Method:
        ///     if (qty*NetPosition) >= 0 we are simply added to position.
        ///     else
        ///         while (qty!=0 and Fills.Count > 0)
        ///             if (LastFill.Qty+qty)*qty >= 0 we cancel this entire fill, remove it,
        ///                 and have left over qty += LastFill.Qty (qty may be exactly zero).
        ///             else LastFill.Qty += qty, and LastFill.Qty is not zero, qty=0.
        ///         if (qty!=0) we have completely flipped our position to other side.  Add new fill.
        /// </summary>
        /// <param name="aFill">A ref to the Fill is kept here.</param>
        public virtual void Add(Fill aFill)
        {

            int remainingQty = aFill.Qty;
            if (remainingQty == 0)
                return;     // do nothing in this case.

            // Store the fill.
            if (aFill.LocalTime.CompareTo(LocalTimeLast) > 0)
                LocalTimeLast = aFill.LocalTime;
            if (aFill.ExchangeTime.CompareTo(ExchangeTimeLast) > 0)
                ExchangeTimeLast = aFill.ExchangeTime;
            m_FillTrail.Add(aFill);

            // Update total volume
            m_Volume += Math.Abs(remainingQty);

            //
            // Allocate fill against our open posiion.
            //           
            if ( m_FillsOpen.Count==0 || remainingQty * m_FillsOpen[0].Qty >= 0)
            {   // adding to our position, or new position.
                Fill newFill = Fill.Create(aFill);
                m_FillsOpen.Add(newFill);                
            }
            else
            {   // cancelling positions.
                double realPnL = 0.0;
                while (remainingQty!=0 && m_FillsOpen.Count > 0)
                {
                    Fill oldFill = m_FillsOpen[m_FillsOpen.Count - 1];
                    if ((oldFill.Qty + remainingQty) * remainingQty >= 0)
                    {   // Incoming qty kills old fill qty "Q0" entirely; so the transacting amount is Q0
                        realPnL += -oldFill.Qty * oldFill.Price + oldFill.Qty * aFill.Price;// -Q0*P0 - (-Q0)*P
                        remainingQty += m_FillsOpen[m_FillsOpen.Count - 1].Qty;                     // Q = Q + Q0
                        m_FillsOpen.RemoveAt(m_FillsOpen.Count - 1);                                // remove entire old fill [P0,Q0]
                    }
                    else
                    {   // Old fill completely absorbs new fill qty "Q"; transacting amount is Q
                        realPnL += remainingQty * oldFill.Price - remainingQty * aFill.Price;// -pnl = -(-Q)*P1 - Q*P
                        oldFill.Qty += remainingQty;                                        // reduce oldQty by amount transacted.
                        remainingQty = 0;                                                   // there is no quantity left.
                    }
                }
                if (remainingQty != 0)
                {   // After cancelling out all levels of old fills, we still have some left!
                    Fill newFill = Fill.Create(aFill);
                    newFill.Qty = remainingQty;                                              // overwrite the quantity with remaining qty.
                    m_FillsOpen.Add(newFill);
                }
                m_RealizedGain += realPnL;
            }
            UpdatePositionInfo();
        }//Add()
        //
        //
        // ****                     Delete All Fills                        ****
        //
        public void DeleteAllFills()
        {
            m_FillsOpen.Clear();
            UpdatePositionInfo();
        }//DeleteAllFills()
        //
        //
        //
        // *********************************************************************
        // ****                         PnL Methods                         ****
        // *********************************************************************
        /// <summary>
        /// Allows user to reset the PnL.  Useful at the end of a trading session, 
        /// if we don't want to roll our PnL forward into the next session.
        /// </summary>
        public void ResetRealizedDollarGains()
        {
            m_RealizedStartingGain += m_RealizedGain;               // Roll the gain into the cummulative gain.
            m_RealizedGain = 0;
            m_StartingVolume += m_Volume;                           // Roll volume into the cummulative volume.
            m_Volume = 0;
        }
        /// <summary>
        /// Allows the user to set the current gains for this book.  If only one of them needs to be set, 
        /// then the user can pass a NaN for the other (which will be skipped).
        /// </summary>
        /// <param name="todaysRealPnL">PnL (in book's currency) realized since last reset time.</param>
        /// <param name="openingRealPnL">PnL (in book's currency) realized over lifetime.</param>
        public void ResetRealizedDollarGains(double todaysRealPnL, double openingRealPnL)
        {
            if ( ! double.IsNaN(todaysRealPnL) )
                m_RealizedGain = todaysRealPnL / m_DollarPerPoint;
            if ( ! double.IsNaN(openingRealPnL) )
                m_RealizedStartingGain = openingRealPnL / m_DollarPerPoint;
        }
        //
        public double UnrealizedDollarGains()
        {
            return UnrealizedDollarGains(m_LastMidPrice);
        }
        public double UnrealizedDollarGains(double midPrices)
        {
            m_LastMidPrice = midPrices;
            double gain = 0.0;
            foreach (Fill aFill in m_FillsOpen)
            {
                gain -= aFill.Qty*(aFill.Price - m_LastMidPrice);
            }
            return (gain * m_DollarPerPoint);
        }
        /// <summary>
        /// The user can demand that all internal values are recaulated.  Useful upon initialization.
        /// </summary>
        public void RecalculateAll()
        {
            UpdatePositionInfo();
            UnrealizedDollarGains(this.m_AveCost);
        }
        //
        //
        public override string ToString()
        {
            StringBuilder msg = new StringBuilder();
            msg.AppendFormat("{0} ", this.Name);
            msg.AppendFormat("Net={0} ", this.NetPosition);
            foreach (Fill fill in m_FillsOpen)
                msg.AppendFormat("[{0}]", fill.ToString());
            return msg.ToString();
        }//ToString()
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
        // ****                 Update Position Info()                  *****
        //
        private void UpdatePositionInfo()
        {
            if (m_FillsOpen.Count == 0)
            {
                m_NetPosition = 0;
                m_AveCost = 0.0;
                return;
            }
            int net = 0;
            double aveCost = 0.0;
            foreach (Fill aFill in m_FillsOpen)
            {
                net += aFill.Qty;
                aveCost += aFill.Qty * aFill.Price;
            }
            m_NetPosition = net;
            m_AveCost = aveCost / m_NetPosition;
        }// UpdatePositionInfo().
        //
        //
        //
        //
        #endregion//Private Methods




        #region IStringifiable Implementation
        // *****************************************************************
        // ****                     IStringifiable                      ****
        // *****************************************************************
        public string GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            s.AppendFormat("InstrumentName={0}",InstrumentName.Serialize(this.Name));
            s.AppendFormat(" DollarPerPoint={0} SmallestFillPriceIncr={1}", m_DollarPerPoint,m_SmallestFillPriceIncr);
            s.AppendFormat(" LocalTimeLast={0} ExchangeTimeLast={1}", LocalTimeLast.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"),ExchangeTimeLast.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
            s.AppendFormat(" RealGain={0} RealStartingGain={1}",m_RealizedGain,m_RealizedStartingGain);
            s.AppendFormat(" Volume={0} StartingVolume={1}", m_Volume, m_StartingVolume);
            return s.ToString();
            //return string.Format("InstrumentName={4} DollarPerPoint={0} SmallestFillPriceIncr={1} LocalTimeLast={2} ExchangeTimeLast={3} RealGain={5} RealStartingGain={6}"
            //    , m_DollarPerPoint, m_SmallestFillPriceIncr, LocalTimeLast.ToString("yyyy-MM-dd HH:mm:ss.fff zzz")
            //    , ExchangeTimeLast.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"),InstrumentName.Serialize(this.Name) , m_RealizedGain, m_RealizedStartingGain);
        }
        public List<IStringifiable> GetElements()
        {
            List<IStringifiable> elements = new List<IStringifiable>();
            foreach (Fill fill in m_FillsOpen)
                elements.Add(fill);
            return elements;
        }
        public void SetAttributes(Dictionary<string, string> attributes)
        {
            double x;
            int n;
            DateTime dt;
            InstrumentName name;
            foreach (string key in attributes.Keys)
                if (key.Equals("DollarPerPoint") && Double.TryParse(attributes[key], out x))
                    this.m_DollarPerPoint = x;
                else if (key.Equals("SmallestFillPriceIncr") && Double.TryParse(attributes[key], out x))
                    this.m_SmallestFillPriceIncr = x;
                else if (key.Equals("LocalTimeLast") && DateTime.TryParse(attributes[key], out dt))
                    this.LocalTimeLast = dt;
                else if (key.Equals("ExchangeTimeLast") && DateTime.TryParse(attributes[key], out dt))
                    this.ExchangeTimeLast = dt;
                else if (key.Equals("InstrumentName") && InstrumentName.TryDeserialize(attributes[key], out name))
                    this.Name = name;
                else if (key.Equals("RealGain") && Double.TryParse(attributes[key], out x))
                    this.m_RealizedGain = x;
                else if (key.Equals("RealStartingGain") && Double.TryParse(attributes[key], out x))
                    this.m_RealizedStartingGain = x;
                else if (key.Equals("Volume") && int.TryParse(attributes[key], out n))
                    this.m_Volume = n;
                else if (key.Equals("StartingVolume") && int.TryParse(attributes[key], out n))
                    this.m_StartingVolume = n;
        }
        public void AddSubElement(IStringifiable subElement)
        {  
            if (subElement is Fill)
                m_FillsOpen.Add((Fill)subElement);
        }
        //
        //
        #endregion // IStringifiable


    }
}
