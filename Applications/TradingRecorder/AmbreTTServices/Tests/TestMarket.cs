using System;
using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
using System.Drawing;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Windows.Forms;

using Misty.Lib.Utilities;
using Misty.Lib.Application;
using Misty.Lib.Products;
using Misty.Lib.MarketHubs;
using Misty.Lib.BookHubs;


using Ambre.TTServices;


namespace Ambre.TTServices.Tests
{
    /// <summary>
    /// Request instrument look up.  
    /// Request subscription to instruments found.
    /// </summary>
    public partial class TestMarket : Form
    {



        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        private Markets.MarketTTAPI m_Market = null;
        //private TTApiService m_TTServices = null;

        private Dictionary<string, Product> m_Products = new Dictionary<string, Product>();
        private Dictionary<string, InstrumentName> m_Instruments = new Dictionary<string, InstrumentName>();

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        public TestMarket()
        {
            InitializeComponent();

            // Set application information - do this before hubs are instantiated.
            string basePath = FilesIO.GetPathToDirName(System.IO.Directory.GetCurrentDirectory(), "Ambre", true);
            AppInfo info = AppInfo.GetInstance();
            info.BasePath = basePath;
            info.LogDirectory = string.Format("{0}{1}", info.LogDirectory, Misty.Lib.Utilities.FilesIO.GetTodaysLogDirAndClean(info.LogPath));


            // Instantiate the API.
            //m_TTServices = TTApiService.GetInstance();
            //m_TTServices.ServiceStateChanged += new EventHandler(TTServices_ServiceStateChanged);
            

            // Instantiate hubs
            //  but do not Connect until the user name/pw has been authenticated.
            m_Market = new Markets.MarketTTAPI();
            m_Market.Start();
            //m_Market.MarketStatusChanged += new EventHandler<MistyLib.MarketHubs.MarketStatusChangedEventArg>(Market_MarketStatusChanged);
            //m_Market.FoundServiceResource += new EventHandler<MarketFoundServiceResource>(Market_MarketFoundServiceResource);
            m_Market.MarketStatusChanged += new EventHandler(Market_MarketStatusChanged);
            m_Market.FoundResource += new EventHandler(Market_MarketFoundServiceResource);

            timer1.Tick += new EventHandler(timer1_Tick);
            timer1.Interval = 1000;
        }
        //
        //
        //       
        #endregion//Constructors



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
        //
        //
        // ****             Shutdown()              ****
        //
        /// <summary>
        /// Complete shutdown of all hubs and their threads.
        /// </summary>
        private void Shutdown()
        {
            if ( m_Market != null )
                m_Market.RequestStop();
            TTApiService ttService = TTApiService.GetInstance();
            if (ttService != null)
                ttService.Dispose();
            if (timer1 != null)
            {
                timer1.Stop();
                timer1.Enabled = false;
                timer1.Dispose();
            }
        }// Shutdown()
        //
        //
        private bool m_IsMarketUpdating = false;
        private void ToggleUpdateState()
        {
            // Validate
            if (listBoxInstruments.SelectedIndex >= 0 && listBoxInstruments.SelectedIndex <= listBoxInstruments.Items.Count)
            {

                if (m_IsMarketUpdating)
                {   // Stop it
                    timer1.Enabled = false;
                    timer1.Stop();
                    buttonUpdate.BackColor = Color.Gray;
                    m_IsMarketUpdating = ! m_IsMarketUpdating;
                }
                else
                {   // Start it
                    timer1.Start();
                    timer1.Enabled = true;
                    buttonUpdate.BackColor = Color.DarkGreen;
                    m_IsMarketUpdating = ! m_IsMarketUpdating;

                }
            }

        }

        //
        //
        /// <summary>
        /// Must be called by Gui thread.
        /// </summary>
        private void UpdateMarket()
        {
            int selectedIndex = listBoxInstruments.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < listBoxInstruments.Items.Count)
            {
                string instrName = (string)listBoxInstruments.SelectedItem;
                Book aBook;
                if (m_Market.TryEnterReadBook(out aBook))
                {
                    int instrID = -1;
                    foreach (Misty.Lib.BookHubs.Market mkt in aBook.Instruments.Values)
                    {
                        if (instrName.Equals(mkt.Name.FullName))
                        {
                            instrID = mkt.ID;
                            break;
                        }
                    }
                    if (instrID >= 0)
                    {
                        txtInstrumentName.Text = aBook.Instruments[instrID].Name.FullName;
                        txtBidSide.Text = aBook.Instruments[instrID].Price[0][0].ToString();
                        txtAskSide.Text = aBook.Instruments[instrID].Price[1][0].ToString();
                        txtBidQty.Text = aBook.Instruments[instrID].Qty[0][0].ToString();
                        txtAskQty.Text = aBook.Instruments[instrID].Qty[1][0].ToString();
                    }
                    m_Market.ExitReadBook(aBook);
                }
            }
        }
        //
        //
        //
        #endregion//Private Methods


        #region MarketHub Event Handlers
        // *****************************************************************
        // ****                Private Event Handlers                   ****
        // *****************************************************************
        //
        /// <summary>
        /// Call back from TTServices after Login is authenticated.
        /// </summary>
        private void TTServices_ServiceStateChanged(object sender, EventArgs eventArg)
        {
            if (eventArg is TTApiService.ServiceStatusChangeEventArgs)
            {
                TTApiService.ServiceStatusChangeEventArgs e = (TTApiService.ServiceStatusChangeEventArgs)eventArg;
                if (e.IsConnected)
                {   // We have a good connection to a user session now.
                    m_Market.Connect();                             // initialize connection to API.
                    m_Market.RequestMarketServers();                // request all market connections.
                }
            } 
        }
        //
        /// <summary>
        /// Call-back from MarketHub every time the status of a market changes.
        /// All Markets will be returned in this call.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArg"></param>
        private void Market_MarketStatusChanged(object sender, EventArgs eArg)
        {
            MarketStatusChangedEventArg eventArg = (MarketStatusChangedEventArg)eArg;
            if (this.InvokeRequired)
            {
                EventHandler<MarketStatusChangedEventArg> d = new EventHandler<MarketStatusChangedEventArg>(Market_MarketStatusChanged);
                this.Invoke(d, new object[] { sender, eventArg });
            }
            else
            {
                if (eventArg.MarketNameList.Count > 0)
                {
                    listBoxMarkets.BeginUpdate();
                    listBoxMarkets.Items.Clear();
                    foreach (string marketName in eventArg.MarketNameList)
                        listBoxMarkets.Items.Add(marketName);
                    listBoxMarkets.EndUpdate();
                }
            }
        }
        //
        //
        private void Market_MarketFoundServiceResource(object sender, EventArgs eventArgBase)
        {
            if (eventArgBase.GetType() != typeof(FoundServiceEventArg))
                return;

            FoundServiceEventArg eventArg = (FoundServiceEventArg)eventArgBase;
            if (this.InvokeRequired)
            {
                EventHandler<FoundServiceEventArg> d = new EventHandler<FoundServiceEventArg>(Market_MarketFoundServiceResource);
                this.Invoke(d, new object[] { sender, eventArg });
            }
            else
            {
                // Products
                if (eventArg.FoundProducts != null)
                {
                    //m_Products.Clear();

                    foreach (Misty.Lib.Products.Product prod in eventArg.FoundProducts)
                    {
                        m_Products.Add(prod.FullName, prod);
                        listBoxProducts.Items.Add(prod.FullName);
                    }
                }
                // Instruments
                if (eventArg.FoundInstruments != null)
                {
                    m_Instruments.Clear();
                    foreach (Misty.Lib.Products.InstrumentName instr in eventArg.FoundInstruments)
                    {
                        m_Instruments.Add(instr.FullName, instr);
                        listBoxInstruments.Items.Add(instr.FullName);
                    }
                }
            }
        }
        //
        //
        #endregion//Private Event Handlers


        #region Form Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        private void button_Click(object sender, EventArgs e)
        {
            // Validate
            if (!(sender is Button))
                return;
            Button button = (Button)sender;
            //
            // Process button click
            //
            if (button == buttonStart)
            {
                TTApiService service = TTApiService.GetInstance();
                service.Start(this.checkBoxUseXTraderFollowLogin.Checked);                
            }
            else if (button == buttonGetProducts)
            {
                int selectedIndex = listBoxMarkets.SelectedIndex;
                if (selectedIndex >= 0 && selectedIndex < listBoxMarkets.Items.Count)
                {
                    listBoxProducts.ClearSelected();                // deselect any selected products.
                    listBoxProducts.Items.Clear();                  // remove products from list.
                    m_Market.RequestProducts((string)listBoxMarkets.SelectedItem);// Request new products.
                }
            }
            else if (button == buttonGetInstruments)
            {
                int selectedIndex = listBoxProducts.SelectedIndex;
                if (selectedIndex >= 0 && selectedIndex < listBoxProducts.Items.Count)
                {
                    listBoxInstruments.ClearSelected();                // deselect any selected products.
                    listBoxInstruments.Items.Clear();                  // remove products from list.
                    string productUniqueName = ((string)listBoxProducts.SelectedItem);// Request new products.
                    Product product;
                    if (m_Products.TryGetValue(productUniqueName, out product))
                        m_Market.RequestInstruments(product);
                }
            }
            else if (button == buttonStop)
            {
            }
            else if (button == buttonExit)
            {
                Shutdown();
                this.Close();
            }
            else if (button == buttonUpdate)
            {
                ToggleUpdateState();
                //UpdateMarket();
            }
            else
            {   // unknown button
                return;
            }
        }
        //
        private void timer1_Tick(object sender, EventArgs e)
        {
            UpdateMarket();
        }
        //
        //
        private void TestMarket_FormClosing(object sender, FormClosingEventArgs e)
        {
            Shutdown();
        }
        /// <summary>
        /// Use selected new instrument to subscribe to.
        /// </summary>
        private void listBoxInstruments_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectName = (string) listBoxInstruments.SelectedItem;
            if (!string.IsNullOrEmpty(selectName))
            {
                InstrumentName instr;
                if (m_Instruments.TryGetValue(selectName, out instr))
                {
                    m_Market.RequestInstrumentSubscription(instr);
                }
            }

        }
        private void ListBox_DoubleClick(object sender, EventArgs e)
        {
            if (sender is ListBox)
            {
                ListBox listBox = (ListBox)sender;
                if (listBox == listBoxMarkets)
                {
                    button_Click(buttonGetProducts, e);
                }
                else if (listBox == listBoxProducts)
                {
                    button_Click(buttonGetInstruments, e);
                }

            }
        }
        //
        //
        //
        #endregion//Event Handlers

    }
}
