using System;
using System.Collections.Generic;
//using System.ComponentModel;
//using System.Drawing;
//using System.Data;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
using System.Windows.Forms;

namespace UV.Lib.OrderHubs.FrontEnds
{
    using GlacialComponents.Controls;
    using UV.Lib.OrderHubs;
    using UV.Lib.Products;
    using UV.Lib.Utilities;

    public partial class OrderBookDepthListView : UserControl
    {
        #region no Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        //
        //
        //
        // Internal components
        //
        private GlacialList[] m_GList = new GlacialList[2];         // convenient storage for buyside/sellside
        private OrderBook m_OrderBook;


        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public OrderBookDepthListView()
        {
            InitializeComponent();
            InitializeGlacialList();
            
        }
        //
        private void InitializeGlacialList()
        {
            // Store in our convenient list.
            m_GList[QTMath.BuySide] = glacialListBuy;
            m_GList[QTMath.SellSide] = glacialListSell;

            ImageList list = new ImageList();
            list.Images.Add(System.Drawing.Image.FromFile( string.Format("{0}BalloonYellow.bmp",Application.AppInfo.GetInstance().UserConfigPath) ));
            list.Images.Add(System.Drawing.Image.FromFile(string.Format("{0}BalloonRed.bmp", Application.AppInfo.GetInstance().UserConfigPath)));
            list.Images.Add(System.Drawing.Image.FromFile(string.Format("{0}BalloonGreen.bmp", Application.AppInfo.GetInstance().UserConfigPath)));


            // Create columns for views.
            GLColumn column;
            for (int side = 0; side < 2; side++)
            {
                m_GList[side].Selectable = false;
                //m_GList[side].ActivatedEmbeddedControl.Ite
                m_GList[side].AllowColumnResize = false;
                m_GList[side].ControlStyle = GLControlStyles.Normal;
                //m_GList[side].GridLineStyle = GLGridLineStyles.gridNone;
                //m_GList[side].GridLines = GLGridLines.
                //m_GList[side].Click += new EventHandler(ListView_Click);
                //m_GList[side].ItemChangedEvent += new ChangedEventHandler(ListView_ItemChangedEvent);
                m_GList[side].MouseClick += new MouseEventHandler(ListView_MouseClick);

                
                // Load image table
                m_GList[side].ImageList = new ImageList();                
                for (int i=0; i<list.Images.Count; ++i)
                    m_GList[side].ImageList.Images.Add(list.Images[i], System.Drawing.Color.White);
                int n = m_GList[side].ItemHeight;
                m_GList[side].ItemHeight = 14;

                if (side == 0)
                {
                    column = new GLColumn("");
                    column.Width = 80;
                    column.NumericSort = false;
                    column.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
                    column.ActivatedEmbeddedType = GLActivatedEmbeddedTypes.ComboBox;
                    ComboBox cb = (ComboBox)column.ActivatedEmbeddedControlTemplate;                    // Create the prototypical combo box.
                    cb.Items.Clear();
                    cb.Items.Add("Default");
                    cb.Items.Add("Model 1");
                    cb.Items.Add("Model 2");
                    cb.Items.Add("Model 3");
                    m_GList[side].Columns.Add(column);

                    column = new GLColumn("");
                    column.Width = 20;
                    column.NumericSort = false;
                    m_GList[side].Columns.Add(column);

                    column = new GLColumn("Qty");
                    column.TextAlignment = System.Drawing.ContentAlignment.MiddleRight;
                    column.Width = 30;
                    column.NumericSort = false;
                    m_GList[side].Columns.Add(column);

                    column = new GLColumn("Price");
                    column.TextAlignment = System.Drawing.ContentAlignment.MiddleRight;
                    column.Width = 40;
                    column.NumericSort = false;
                    m_GList[side].Columns.Add(column);

                }
                if (side == 1)
                {
                    // Load columns
                    column = new GLColumn("Price");
                    column.TextAlignment = System.Drawing.ContentAlignment.MiddleRight;
                    column.Width = 40;
                    column.NumericSort = false;
                    m_GList[side].Columns.Add(column);

                    column = new GLColumn("Qty");
                    column.TextAlignment = System.Drawing.ContentAlignment.MiddleRight;
                    column.Width = 30;
                    column.NumericSort = false;
                    m_GList[side].Columns.Add(column);

                    column = new GLColumn("");
                    column.Width = 20;
                    column.NumericSort = false;
                    m_GList[side].Columns.Add(column);

                    column = new GLColumn("");
                    column.Width = 80;
                    column.NumericSort = false;
                    column.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
                    column.ActivatedEmbeddedType = GLActivatedEmbeddedTypes.ComboBox;
                    // Create the prototypical combo box.
                    ComboBox cb = (ComboBox)column.ActivatedEmbeddedControlTemplate;
                    cb.Items.Clear();
                    cb.Items.Add("Default");
                    cb.Items.Add("Model 1");
                    cb.Items.Add("Model 2");
                    cb.Items.Add("Model 3");
                    m_GList[side].Columns.Add(column);
                }
                

            }


        }// InitializeListView()
        //       
        //
        //
        // ****             TryCreate()             ****
        //
        /// <summary>
        /// This must be called by the UI thread.
        /// </summary>
        public static bool TryCreate(OrderBookEventArgs eventArgs, out OrderBookDepthListView newView)
        {
            // Validate
            newView = null;
            if (eventArgs.EventType != OrderBookEventArgs.EventTypes.CreatedBook)
                return false;
            // Create object
            OrderBook newBook;
            bool isSucessful = OrderBook.TryCreate(eventArgs, out newBook);
            if (isSucessful)
            {
                newView = new OrderBookDepthListView();
                newView.m_OrderBook = newBook;
            }

            return isSucessful;
        }//TryCreate()
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


        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        
        //
        //
        //
        
        //
        #endregion//Private Methods

        int[] m_PriceCell = new int[] { 4, 0 };


        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        //
        // ****         OrderHub_BookChanged()          ****
        //
        public void OrderHub_BookChanged(object sender, EventArgs eventArgs)
        {
            if (this.InvokeRequired)
                this.Invoke(new EventHandler(OrderHub_BookChanged), new object[] { sender, eventArgs });
            else
            {
                OrderBookEventArgs eventArg = (OrderBookEventArgs)eventArgs;
                Order order = eventArg.Order;
                if (eventArg.EventType == OrderBookEventArgs.EventTypes.NewOrder)
                {
                    m_OrderBook.TryAddOrder(order);
                    
                    GLItem item = new GLItem();
                    m_GList[order.Side].Items.Add(item);
                    if (order.Side == 1)
                    {
                        item.SubItems[0].Text = order.IPrice.ToString();
                        item.SubItems[1].Text = order.Qty.ToString();
                        item.SubItems[2].ImageIndex = 0;
                        //item.SubItems[2].ImageAlignment = HorizontalAlignment.Center;
                        item.SubItems[3].Text = "Default";
                    }
                    else
                    {
                        item.SubItems[3].Text = order.IPrice.ToString();
                        item.SubItems[2].Text = order.Qty.ToString();
                        item.SubItems[1].ImageIndex = 0;
                        //item.SubItems[2].ImageAlignment = HorizontalAlignment.Center;
                        item.SubItems[0].Text = "Default";
                    }


                }
                else if (eventArg.EventType == OrderBookEventArgs.EventTypes.DeletedOrder)
                {
                    m_OrderBook.TryDeleteOrder(order.Tag, out order);
                    GLItem foundItem = null;
                    foreach (GLItem item in m_GList[order.Side].Items)
                    {
                        if (item.Text == order.Tag)
                        {
                            foundItem = item;
                        }
                    }
                    if (foundItem != null)
                        m_GList[order.Side].Items.Remove(foundItem);
                }
                else if (eventArg.EventType == OrderBookEventArgs.EventTypes.DeletedOrder)
                {
                    //m_OrderBook.TryDeleteOrder(eventArg.Order);
                }
            }
            
        }//OrderHub_BookChanged()
        //
        //
        //
        #endregion//Event Handlers

        #region Form Event Handlers
        // *****************************************************************
        // ****                Form Event Handlers                     ****
        // *****************************************************************
        //
        //
        private void ListView_MouseClick(object source, EventArgs eventArgs)
        {
            if ( !(eventArgs is MouseEventArgs) )
                return;
            MouseEventArgs e = (MouseEventArgs) eventArgs;
            int locX = e.X;
            int locY = e.Y;
            int bookSide;
            if (source == m_GList[0])
                bookSide = 0;
            else if (source == m_GList[1])
                bookSide = 1;
            else
                return;
            
            // Determine where click occured.
            System.Drawing.Point pointLocalMouse = this.PointToClient(Cursor.Position);
            pointLocalMouse = new System.Drawing.Point(e.X, e.Y);
            int nItem = 0, nColumn = 0, nCellX = 0, nCellY = 0;
			ListStates eState;
			GLListRegion listRegion;
			m_GList[bookSide].InterpretCoords( pointLocalMouse.X, pointLocalMouse.Y, out listRegion, out nCellX, out nCellY, out nItem, out nColumn, out eState );

            if (listRegion == GLListRegion.client)
            {
                if (m_GList[bookSide].Items[nItem].SubItems[nColumn].ImageIndex >= 0)
                {
                    int nCount = m_GList[bookSide].ImageList.Images.Count;
                    int n = m_GList[bookSide].Items[nItem].SubItems[nColumn].ImageIndex;
                    n = (n + 1) % nCount;
                    m_GList[bookSide].Items[nItem].SubItems[nColumn].ImageIndex = n;
                    //m_GList[bookSide].Items[nItem].SubItems[nColumn].Parent.Invalidate();
                }
            }


        }
        //
        //
        //
        //
        //
        #endregion//Form Event Handlers



    }
}
