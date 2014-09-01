using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
//using System.Data;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Windows.Forms;

namespace Misty.Lib.OrderHubs.FrontEnds
{
    public partial class OrderBookViewMini : UserControl
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //       
        // Order books
        //
        private OrderHub m_OrderHub = null;
        private OrderBookCollection m_OrderBooks = new OrderBookCollection();


        //private Misty.Lib.FrontEnds.GuiCreator m_GuiCreator = new Lib.FrontEnds.GuiCreator();

        //
        // List variables.
        //
        private BindingList<string> m_OrderList = null;
        private Dictionary<Order, string> m_OrderDetails = new Dictionary<Order, string>();

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public OrderBookViewMini()
        {
            InitializeComponent();
            

            InitializeList();
        }
        //
        //
        //
        //
        // ****             Initialize List()               ****
        //
        public void InitializeList()
        {
            m_OrderList = new BindingList<string>();
            m_OrderList.AllowRemove = true;
            m_OrderList.AllowNew = true;
            m_OrderList.AllowEdit = true;
            m_OrderList.RaiseListChangedEvents = true;

            this.listBox.DataSource = m_OrderList;
            //this.listBox1.DisplayMember 
            //m_OrderList.AddingNew += orderList_AddingNew;
            //orderList.ListChanged += orderList_ListChanged;
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


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        //
        // *********************************************
        // ****             AddHub()                ****
        // *********************************************
        public void AddHub(OrderHub parentOrderHub)
        {
            if (m_OrderHub!=null)
            {   // Remove the old order hub.
                
            }
            m_OrderHub = parentOrderHub;
            m_OrderHub.BookChanged += OrderHub_BookChanged;
            m_OrderHub.BookCreated += OrderHub_BookCreated;
            m_OrderHub.BookDeleted += OrderHub_BookDeleted;
        }// AddHub()
        //
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
        //
        // ****             UpdateView()                ****
        //
        private void UpdateView(OrderBookEventArgs eventArg)
        {
            string s;
            switch (eventArg.EventType)
            {
                case OrderBookEventArgs.EventTypes.NewOrder:
                    s = eventArg.Order.ToString();
                    m_OrderDetails.Add(eventArg.Order, s);
                    m_OrderList.Add(s);                    
                    break;
                case OrderBookEventArgs.EventTypes.DeletedOrder:                    
                    if (m_OrderDetails.TryGetValue(eventArg.Order,out s) && m_OrderList.Contains(s) )
                    {
                        m_OrderList.Remove(s);
                    }
                    break;
            }
        }//UpdateView()
        //
        //
        //
        //
        //
        #endregion//Private Methods



        #region External Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        //
        // ****         OrderHub_BookChanged()              ****
        //
        void OrderHub_BookChanged(object sender, EventArgs eventArgs)
        {
            if (this.InvokeRequired)
                this.Invoke(new EventHandler(OrderHub_BookChanged), new object[] { sender, eventArgs });
            else
            {
                OrderBookEventArgs eventArg = (OrderBookEventArgs)eventArgs;
                if (m_OrderBooks.TryUpdate(eventArg))
                    UpdateView(eventArg);
            }
        }
        //
        // ****         OrderHub_BookCreated()              ****
        //
        void OrderHub_BookCreated(object sender, EventArgs eventArgs)
        {
            //m_GuiCreated.InvokeCreate();
            if (this.InvokeRequired)
                this.Invoke(new EventHandler(OrderHub_BookCreated), new object[] { sender, eventArgs });
            else
            {
                OrderBookEventArgs eventArg = (OrderBookEventArgs)eventArgs;
                if (m_OrderBooks.TryUpdate(eventArg))
                    UpdateView(eventArg);
            }

        }
        //
        // ****         OrderHub_BookDeleted()              ****
        //
        void OrderHub_BookDeleted(object sender, EventArgs eventArgs)
        {
            if (this.InvokeRequired)
                this.Invoke(new EventHandler(OrderHub_BookCreated), new object[] { sender, eventArgs });
            else
            {
                OrderBookEventArgs eventArg = (OrderBookEventArgs)eventArgs;


            }
        }
        //
        #endregion//External Event Handlers


        #region Form Event Handlers
        // *****************************************************************
        // ****                 Form Event Handlers                     ****
        // *****************************************************************
        //
        //
        #endregion//Form Event Handlers



    }
}
