using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;


namespace UV.Lib.OrderHubs
{
    using UV.Lib.Products;
    using UV.Lib.Utilities;

    public class OrderBook
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Book identification
        //
        private OrderHub m_ParentHub = null;
        public InstrumentName m_InstrumentName;                                 // unique name for instrument.

        private string m_TagFormat;
        private int m_TagIndex = 0;                                             // Interlocking keeps this thread safe!

        public ReaderWriterLockSlim Lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        //
        // Private book infrastructure
        //
        private SortedDictionary<int, List<string>>[] m_ByPrice = null;               // key is Iprice
        private Dictionary<string,Order> m_ByTag = new Dictionary<string,Order>();   // key is unique tag

        public double MinimumPriceTick = 1.0;                                   // smallest price between two adjacent tick prices for orders.
        


        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public OrderBook(OrderHub parentOrderHub, InstrumentName instrumentName)
        {            
            this.m_InstrumentName = instrumentName;                             // store required vars
            this.m_ParentHub = parentOrderHub;

            // Create Order tag format
            DateTime dt = DateTime.Now;            
            string s1 = string.Format("{0:yy}{1}{2}{3}{4}{5}_", dt, QTMath.GetMonthCode(dt), QTMath.GetBaseSixtyCode(dt.Day),
                QTMath.GetBaseSixtyCode(dt.Hour), QTMath.GetBaseSixtyCode(dt.Minute), QTMath.GetBaseSixtyCode(dt.Second));
            m_TagFormat = s1 + "{0}";                                           // Make certain this is unique.

            // Create books.
            m_ByPrice = new SortedDictionary<int, List<string>>[2];             // TODO: I think, for speed, use List<Order>?
            for (int side = 0; side < m_ByPrice.Length; ++side)
                m_ByPrice[side] = new SortedDictionary<int, List<string>>();

        }//constructor
        //
        //
        // *********************************************************
        // ****                 TryCreate()                     ****
        // *********************************************************
        //
        public static bool TryCreate(OrderBookEventArgs eventArgs, out OrderBook createdBook)
        {
            bool isSuccess = false;
            createdBook = null;
            if (eventArgs.EventType == OrderBookEventArgs.EventTypes.CreatedBook)
            {
                createdBook = new OrderBook(eventArgs.ParentOrderHub, eventArgs.Instrument);
                isSuccess = true;
            }
            // Exit.
            return isSuccess;
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


        #region Public Peeking Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        public void GetAllOrdersSortedByPrice(ref List<Order> orderSortedByPrice)
        {
            for (int side = 0; side < 2; ++side)
            {
                foreach (int iPrice in m_ByPrice[side].Keys)
                {
                    foreach (string tag in m_ByPrice[side][iPrice])
                    {
                        Order order = m_ByTag[tag];
                        orderSortedByPrice.Add(order);
                    }
                }
            }
        }
        //
        //
        //
        //
        #endregion//peeking methods



        #region Public Manipulation Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        //
        // ****                 Try Add New Order()                 ****
        //
        public bool TryAddOrder(Order newOrder)
        {
            if (!m_ByTag.ContainsKey(newOrder.Tag))
            {
                m_ByTag.Add(newOrder.Tag, newOrder);
                if ( ! m_ByPrice[newOrder.Side].ContainsKey(newOrder.IPrice) )
                    m_ByPrice[newOrder.Side].Add(newOrder.IPrice,new List<string>());
                m_ByPrice[newOrder.Side][newOrder.IPrice].Add(newOrder.Tag);
                return true;
            }
            else
                return false;
        }// TryAddNewOrder()
        //
        //
        // ****                 Try Delete Order                    ****
        //
        /// <summary>
        /// TODO: Can the write lock be obtained here?
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="deletedOrder"></param>
        /// <returns></returns>
        public bool TryDeleteOrder(string tag, out Order deletedOrder)
        {
            deletedOrder = null;
            Order order;

            if (m_ByTag.TryGetValue(tag, out order) && m_ByPrice[order.Side].ContainsKey(order.IPrice))
            {
                m_ByPrice[order.Side][order.IPrice].Remove(order.Tag);
                deletedOrder = order;
                return true;
            }
            else
                return false;
        }//TryDeleteOrder()
        //
        //
        //
        // ****                 Create New Order()                  ****
        /// <summary>
        /// Create a new order for this book.
        /// Note on identification: Orders contain name of hub, and InstrumentName. These are
        /// enough to identify this book (their home), and a tag unique to each order in a single book
        /// (NOT unique across all books) allows us to find its location within this book.
        /// </summary>
        /// <returns></returns>
        public Order CreateNewOrder()
        {
            string newTag = string.Format(m_TagFormat, System.Threading.Interlocked.Increment(ref m_TagIndex) );
            return new Order(this.m_ParentHub.HubName, this.m_InstrumentName, newTag);
        }
        //
        //
        //
        //
        // ****                 ToString()              ****
        //
        public override string ToString()
        {
            return string.Format("[{0} {1} orders]", this.m_InstrumentName, m_ByTag.Count);
        }
        //
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
