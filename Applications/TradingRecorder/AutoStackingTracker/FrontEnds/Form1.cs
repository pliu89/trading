using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
//using System.Data;
//using System.Linq;
using System.Drawing;
using System.Text;
//using System.Threading.Tasks;

using System.Windows.Forms;

namespace Ambre.AutoStackingTracker.FrontEnds
{   
    using Misty.Lib.Application;
    using Misty.Lib.IO.Xml;
    using Misty.Lib.OrderHubs;
    using Misty.Lib.Products;

    using Ambre.TTServices;
    using Ambre.TTServices.Orders;
    using Ambre.TTServices.Markets;


    public partial class Form1 : Form
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // Services
        //private TTApiService m_TTService = null;
        //private OrderHubTT m_OrderHub = null;
        //private MarketTTAPI m_MarketHub = null;
        private Misty.Lib.OrderHubs.FrontEnds.OrderBookViewMini orderBookViewMini = null;
        
        // Internal application variables.
        //private bool m_IsShuttingDown = false;
        //private bool m_ServiceConnectionRequested = false;              // guarentees we start services only once.

        


        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public Form1()
        {
            InitializeComponent();                                          // windows form stuff

            AppServices appServices = AppServices.GetInstance("AutoStackingTracker", true);  // Set application information - do this before hubs are instantiated.
            appServices.Info.RequestShutdownAddHandler(new EventHandler(this.RequestShutdown));            
            appServices.LoadServicesFromFile("AutoStackingConfig.txt");     // Creates all services.
            CreateOrderHubControls();                                       // Create services we will need.                        
            appServices.Start();
            appServices.Connect();


        }//constructor
        //
        //
        //
        // ****                 CreateOrderHubControls                      ****
        //
        private void CreateOrderHubControls()
        {
            //foreach (OrderHub hub in AppServices.GetInstance().ServiceOrders.Values)
            foreach (IService service in AppServices.GetInstance().GetServices())
            {
                if (service is OrderHub)
                {
                    OrderHub hub = (OrderHub)service;
                    // Create mini viewer
                    this.orderBookViewMini = new Misty.Lib.OrderHubs.FrontEnds.OrderBookViewMini();
                    this.SuspendLayout();

                    this.orderBookViewMini.Location = new System.Drawing.Point(0, 0);
                    this.orderBookViewMini.Name = "orderBookViewMini";
                    System.Drawing.Size size = new System.Drawing.Size(this.ClientSize.Width, this.ClientSize.Height);
                    this.orderBookViewMini.Size = size;
                    this.orderBookViewMini.Anchor = (AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top);

                    this.Controls.Add(orderBookViewMini);
                    this.ResumeLayout();
                    orderBookViewMini.AddHub(hub);
                }
            }

        }// CreateOrderHubControls()
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


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        /// <summary>
        /// Must be called by GUI thread.
        /// </summary>
        private void UpdateDataGrid()
        {
        }
        //
        //
        private void UpdateInstrList()
        {
        }//UpdateInstrList()
        //
        //
        //
        //
        //
        #endregion//Private Methods


        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        //
        //
        //
        // ****                    RequestShutdown()                   ****
        //
        private void RequestShutdown(object sender, EventArgs eventArgs)
        {
            if (this.InvokeRequired)
                this.Invoke(new EventHandler(RequestShutdown), new object[] { sender, eventArgs });
            else
                this.Close();
        }// RequestShutdown()
        //
        //
        //
        #endregion//Event Handlers


        #region Form Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            AppServices.GetInstance().Shutdown();
        }
        private void comboBoxInstrNames_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateDataGrid();
        }
        //
        #endregion//Form Event Handlers

    }
}
