using System;
using System.Collections.Generic;
using System.Text;
using UV.Lib.Utilities;

namespace UV.Lib.OrderBookHubs
{
    using UV.Lib.Products;

    public class Order : ICloneable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Order identification 
        //
        public int Id = -1;                             // unique global id number.
        private static int m_NextId = 0;                // next id
        public InstrumentName Instrument;               // name of associated instrument.        
        public int ReplacingOrderId = -1;               // the id of the order I am going to replace.

        //
        // Order variables
        //
        public int Side = UnknownSide;      
        public int IPricePending;                       // Integerized price desired
        public int IPriceConfirmed;                     // Integerized price confirmed in market
        public double TickSize = 1;                     // Is this the correct value to use?

        //public double Price;
        private int m_OriginalQtyPending;               // SIGNED original order quantity pending 
        private int m_OriginalQtyConfirmed;             // SIGNED original order quantity confirmed in market
        private int m_ExececutedQty = 0;                // SIGNED order quantity already filled 
        
        
        //
        // States of Order
        //
        public OrderType OrderType = OrderType.Unknown;
        public OrderTIF OrderTIF = OrderTIF.GTD;
        public OrderState OrderStatePending = OrderState.Unsubmitted;
        public OrderState OrderStateConfirmed = OrderState.Unsubmitted;

        //
        // User-defined variables
        //
        public int UserDefinedTag;              // this field can be used for any Int the user would like to pass around with orders.

        
        //
        // Static constants
        //
        public const int BuySide = UV.Lib.Utilities.QTMath.BuySide;
        public const int SellSide = UV.Lib.Utilities.QTMath.SellSide;
        public const int UnknownSide = UV.Lib.Utilities.QTMath.UnknownSide;
        //
        #endregion// members

        #region Constructors 
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public Order()
        {
        }
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
        // ****         Price Properties            ****
        //
        /// <summary>
        /// The elemental value is the integer IPrice. 
        /// This provides a double-valued price. This is the pending
        /// value, not yet confirmed by the market.
        /// </summary>
        public double PricePending
        {
            get { return IPricePending * TickSize; }
        }
        //
        /// <summary>
        /// Confirmed price of order.
        /// </summary>
        public double PriceConfirmed
        {
            get { return IPriceConfirmed * TickSize; }
        }
        // ****         Quantity Properties         ****
        /// <summary>
        /// The qty requested for the order
        /// </summary>
        public int OriginalQtyPending
        {
            get { return m_OriginalQtyPending; }
            set
            {
                m_OriginalQtyPending = value;
                if (this.Side == UnknownSide)   // The first time thru, set Side.
                    this.Side = QTMath.MktSignToMktSide(m_OriginalQtyPending);
            }
        }
        //
        //
        /// <summary>
        /// This is the current confirmed qty of the order
        /// WARNING : THIS SHOULD ONLY BE SET BY THE ORDER LISTENER!
        /// </summary>
        public int OriginalQtyConfirmed
        {
            get { return m_OriginalQtyConfirmed; }
            set { m_OriginalQtyConfirmed = value; }
        }
        //
        /// <summary>
        /// signed current working qty of order confirmed working.
        /// </summary>
        public int WorkingQtyConfirmed
        {
            get { return m_OriginalQtyConfirmed - m_ExececutedQty; }
            
        }
        //
        /// <summary>
        /// signed current working qty of order pending confirmation
        /// </summary>
        public int WorkingQtyPending
        {
            get { return m_OriginalQtyPending - m_ExececutedQty; }

        }
        /// <summary>
        /// signed qty this order OR the parent order (if a cancel/replace occured) was filled on.
        /// /// WARNING : THIS SHOULD ONLY BE SET BY THE ORDER LISTENER!
        /// </summary>
        public int ExecutedQty
        {
            get { return m_ExececutedQty; }
            set { m_ExececutedQty = value;}
        }
        #endregion//Properties

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        // ***************************
        // ****     GetNextId     ****
        // ***************************
        /// <summary>
        /// Static function that returns next unused Id
        /// number for an order.  Each session, the order id
        /// numbers restart at zero.
        /// </summary>
        /// <returns></returns>
        public static int GetNextId()
        {
            return System.Threading.Interlocked.Increment(ref Order.m_NextId);
        }
        //        
        //
        //
        // *************************
        // ****     Clone()     ****
        // *************************
        object ICloneable.Clone()
        {
            return this.MemberwiseClone();
        }
        //
        //
        // *************************
        // ****     Clone()     ****
        // *************************
        /// <summary>
        /// Shallow clone method for Order object
        /// </summary>
        /// <returns></returns>
        public Order Clone()
        {
            return (Order)this.MemberwiseClone();
        }
        //
        //
        // *************************
        // ****     ToString()  ****
        // *************************
        public override string ToString()
        {
            return (string.Format("{0} {1}/{2} origQty={3} working={4}@{5} #{6}", 
                this.Instrument,            // 0
                this.OrderStatePending.ToString().Substring(0,3),     // 1
                this.OrderStateConfirmed.ToString().Substring(0, 3),   // 2
                this.OriginalQtyPending,    // 3
                this.WorkingQtyPending,     // 4
                this.PricePending,          // 5
                this.Id));                  // 6
        }
        //
        //
        // *************************
        // ****     Clear()  ****
        // *************************
        /// <summary>
        /// Called to set order attributes back to default values.
        /// </summary>
        public void Clear()
        {
            this.Id = -1;
            this.IPricePending = 0;
            this.IPriceConfirmed = 0;
            this.m_ExececutedQty = 0;
            this.m_OriginalQtyPending = 0;
            this.m_OriginalQtyConfirmed = 0;
            this.OrderStateConfirmed = OrderState.Unsubmitted;
            this.OrderStatePending = OrderState.Unsubmitted;
            this.OrderType = OrderType.Unknown;
            this.ReplacingOrderId = -1;
            this.UserDefinedTag = -1;
            this.TickSize = 1;
            this.Side = UnknownSide;
        }
        // *****************************
        // ****     CopyTo()        ****
        // *****************************
        /// <summary>
        /// Copies the contents of this object into order.
        /// </summary>
        /// <param name="order"></param>
        public void CopyTo(Order order)
        {
            order.Id = this.Id;
            order.Instrument = this.Instrument;
            order.ReplacingOrderId = this.ReplacingOrderId;
            order.Side = this.Side;
            order.IPricePending = this.IPricePending;
            order.IPriceConfirmed = this.IPriceConfirmed;
            order.TickSize = this.TickSize;

            order.m_OriginalQtyPending = this.m_OriginalQtyPending;
            order.m_OriginalQtyConfirmed = this.m_OriginalQtyConfirmed;
            order.m_ExececutedQty = this.m_ExececutedQty;

            order.OrderType = this.OrderType;
            order.OrderTIF = this.OrderTIF;
            order.OrderStatePending = this.OrderStatePending;
            order.OrderStateConfirmed = this.OrderStateConfirmed;

            order.UserDefinedTag = this.UserDefinedTag;
        }// CopyTo()
        //
        //
        #endregion//Public Methods

    }
}
