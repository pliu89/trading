using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpreadPriceGenerator
{
    using Ambre.TTServices;
    using Ambre.TTServices.Markets;

    using Misty.Lib.Application;
    using Misty.Lib.BookHubs;
    using Misty.Lib.Hubs;
    using Misty.Lib.MarketHubs;
    using Misty.Lib.Products;

    using InstrumentDetails = TradingTechnologies.TTAPI.InstrumentDetails;

    public partial class SpreadViewer : Form
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        //
        // Basic information for market, product and instruments.
        private List<string> m_MarketList = new List<string>();
        private Dictionary<string, Product> m_SpreadProducts = new Dictionary<string, Product>();
        private Dictionary<string, InstrumentName> m_SpreadInstruments = new Dictionary<string, InstrumentName>();
        private Dictionary<string, Product> m_FutureProducts = new Dictionary<string, Product>();
        private Dictionary<string, InstrumentName> m_FutureInstruments = new Dictionary<string, InstrumentName>();
        private List<InstrumentName> m_InstrumentSubscriptions = new List<InstrumentName>();

        // Other necessary members.
        private MarketTTAPI m_MarketTTAPI = null;
        private System.Timers.Timer m_MarketReadTimer = null;
        private SpreadInfoReader m_CSVSpreadInfoReader = null;
        private LogHub Log = null;
        private string m_ConfigFileName = "SpreadProjectConfig.txt";
        private bool m_IsShuttingDown = false;
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //
        public SpreadViewer()
        {
            InitializeComponent();
            this.FormClosing += new FormClosingEventHandler(FormClosedByUser);

            // Create the needed services.
            typeof(MarketTTAPI).ToString();
            AppServices appServices = AppServices.GetInstance("SpreadPriceGenerator");
            appServices.Info.RequestShutdownAddHandler(new EventHandler(this.RequestShutdown));
            appServices.ServiceStopped += new EventHandler(Service_ServiceStopped);

            // Write service names to config gile if it does not exist.
            string configPath = string.Format("{0}{1}", appServices.Info.UserConfigPath, m_ConfigFileName);
            if (!System.IO.File.Exists(configPath))
            {
                DialogResult result = System.Windows.Forms.DialogResult.Abort;
                result = MessageBox.Show(string.Format("Config file does not exist! \r\nFile: {0}\r\nDir: {1}\r\nShould I create an empty one?", m_ConfigFileName, appServices.Info.UserConfigPath), "Config file not found", MessageBoxButtons.YesNo);
                if (result == System.Windows.Forms.DialogResult.Yes)
                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(configPath, false))
                    {
                        writer.WriteLine("<Ambre.TTServices.Markets.MarketTTAPI/>");
                        writer.WriteLine("<Ambre.TTServices.TTApiService FollowXTrader=True/>");
                        writer.Close();
                    }
            }

            // Create logs.
            bool isLogViewerVisible = true;
            Log = new LogHub("SpreadProjectLogs", AppInfo.GetInstance().LogPath, isLogViewerVisible, LogLevel.ShowAllMessages);

            // Load the services from config file and start services.
            appServices.LoadServicesFromFile(m_ConfigFileName);
            foreach (IService service in appServices.GetServices())
                this.AddService(service);
            string filePath = AppServices.GetInstance().Info.UserPath;
            filePath = string.Format("{0}ProductSpreadInfo.csv", filePath);
            SpreadInfoReader.TryReadSpreadInfoTable(filePath, Log, out m_CSVSpreadInfoReader);

            // Get the market tt api service.
            IService iService = null;
            if (!appServices.TryGetService("MarketTTAPI", out iService))
            {
                Log.NewEntry(LogLevel.Warning, "Failed to find the market tt api service.");
                return;
            }
            m_MarketTTAPI = (MarketTTAPI)iService;

            // Set market reading timer.
            m_MarketReadTimer = new System.Timers.Timer();
            m_MarketReadTimer.Interval = 2000;
            m_MarketReadTimer.Elapsed += new System.Timers.ElapsedEventHandler(MarketReadingTimer_Elapsed);
            m_MarketReadTimer.Start();

            // Start and connect all the services.
            appServices.Start();
            appServices.Connect();

        }//SpreadViewer()
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
        //
        /// <summary>
        /// This method may get the spread instruments in a right path.
        /// By an initial path selection, the program will try to find and match the spread path generated and the spread instruments from TT.
        /// The output values contain a list of spread instruments.
        /// </summary>
        /// <param name="initialPath"></param>
        /// <param name="spreadNameGeneratedCombinations"></param>
        /// <param name="spreadInstruments"></param>
        /// <returns></returns>
        private bool TryPullSpreadInstruments(int initialPath, List<List<string>> spreadNameGeneratedCombinations, out List<InstrumentName> spreadInstruments)
        {
            spreadInstruments = null;
            
            // Pull the spread path starting from initial path index.
            if (spreadNameGeneratedCombinations.Count <= initialPath)
            {
                Log.NewEntry(LogLevel.Warning, "The selected path number exceeds the path number of spread generated. The two numbers are {0} and {1}",
                    spreadNameGeneratedCombinations.Count, initialPath);
                return false;
            }

            // Return false if the selected spread path is null or has zero element.
            List<string> spreadNameInEachPath = spreadNameGeneratedCombinations[initialPath];
            if (spreadNameInEachPath == null || spreadNameInEachPath.Count < 1)
                return false;

            bool spreadPathFound = false;
            spreadInstruments = new List<InstrumentName>();
            // Found a valid spread path by looping.
            while (!spreadPathFound && initialPath < spreadNameGeneratedCombinations.Count)
            {
                spreadInstruments.Clear();
                foreach (string spreadFullName in m_SpreadInstruments.Keys)
                {
                    if (spreadNameInEachPath.Contains(spreadFullName))
                        spreadInstruments.Add(m_SpreadInstruments[spreadFullName]);
                }

                if (spreadInstruments.Count == spreadNameInEachPath.Count)
                    spreadPathFound = true;
                else
                {
                    initialPath++;
                    spreadNameInEachPath = spreadNameGeneratedCombinations[initialPath];
                    if (spreadNameInEachPath == null || spreadNameInEachPath.Count < 1)
                        continue;
                }
            }

            return spreadPathFound;
        }//TryPullSpreadInstruments()

        /// <summary>
        /// This function will use the delimiters read from the csv file to generate all possible spread instrument names.
        /// The input value contains the first delimiter, second delimiter and constructed all expiry combinations.
        /// The output value is spread name collections in all the paths.
        /// </summary>
        /// <param name="firstDelimiter"></param>
        /// <param name="secondDelimiter"></param>
        /// <param name="expiryCombinations"></param>
        /// <param name="spreadNameListInAllPaths"></param>
        /// <returns></returns>
        private bool TryGenerateSpreadInstrumentNamesInAllPaths(string firstDelimiter, string secondDelimiter, List<List<ExpiryPoint>> expiryCombinations, out List<List<string>> spreadNameListInAllPaths)
        {
            spreadNameListInAllPaths = null;
            // If the delimiters are null or empty or the expiry combinations are empty, return false.
            if (string.IsNullOrEmpty(firstDelimiter) || string.IsNullOrEmpty(secondDelimiter))
            {
                Log.NewEntry(LogLevel.Warning, "The delimiters become null or empty, and the two delimiters are {0} and {1} respectively", firstDelimiter, secondDelimiter);
                return false;
            }

            if (expiryCombinations == null || expiryCombinations.Count < 1)
            {
                Log.NewEntry(LogLevel.Warning, "The expiry combinations input is invalid");
                return false;
            }

            // Output all the spread names in all the paths.
            spreadNameListInAllPaths = new List<List<string>>();
            foreach (List<ExpiryPoint> expiryList in expiryCombinations)
            {
                List<string> spreadNameEachPath = null;
                if (!TryGenerateSpreadInstrumentNames(firstDelimiter, secondDelimiter, expiryList, out spreadNameEachPath))
                {
                    Log.NewEntry(LogLevel.Warning, "Failed to generate spread name in one path with inputs of {0},{1},{2}", firstDelimiter, secondDelimiter, expiryList);
                    return false;
                }
                else
                    spreadNameListInAllPaths.Add(spreadNameEachPath);
            }
            return true;
        }//TryGenerateSpreadInstrumentNamesInAllPaths()

        /// <summary>
        /// This method will generate the spread name for one path.
        /// The expiry series is all the possible expiries got from the TT future instruments.
        /// </summary>
        /// <param name="firstDelimiter"></param>
        /// <param name="secondDelimiter"></param>
        /// <param name="expirySeries"></param>
        /// <param name="spreadNameGeneratedList"></param>
        /// <returns></returns>
        private bool TryGenerateSpreadInstrumentNames(string firstDelimiter, string secondDelimiter, List<ExpiryPoint> expirySeries, out List<string> spreadNameGeneratedList)
        {
            // If the expiry series is null or empty, return false.
            spreadNameGeneratedList = null;
            if (expirySeries == null || expirySeries.Count < 2)
            {
                Log.NewEntry(LogLevel.Warning, "The expiry series for this instrument contains invalid numbers of expiry points");
                return false;
            }

            // Generate the spread name for the specific path.
            spreadNameGeneratedList = new List<string>();
            for (int pos = 0; pos < expirySeries.Count - 1; pos++)
            {
                string frontDate = expirySeries[pos].ToString();
                string backDate = expirySeries[pos + 1].ToString();
                string spreadNameGenerated = string.Format("{0} {1}{2}{3}", firstDelimiter, frontDate, secondDelimiter, backDate).Trim();
                spreadNameGeneratedList.Add(spreadNameGenerated);
            }
            return true;
        }//TryGenerateSpreadInstrumentNames()

        /// <summary>
        /// This function adds all the necessary service event handlers.
        /// </summary>
        /// <param name="service"></param>
        private void AddService(IService service)
        {
            if (service is TTApiService)
            {
                ((TTApiService)service).ServiceStateChanged += new EventHandler(Service_ServiceStateChanged);
            }
            else if (service is MarketTTAPI)
            {
                ((MarketTTAPI)service).MarketStatusChanged += new EventHandler(MarketHub_MarketStatusChanged);
                ((MarketTTAPI)service).FoundResource += new EventHandler(MarketHub_FoundResource);
            }
        }//AddServiceView()

        /// <summary>
        /// This function is called to stop the program.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void RequestShutdown(object sender, EventArgs eventArgs)
        {
            if (m_IsShuttingDown)
                return;
            if (this.InvokeRequired)
                this.Invoke(new EventHandler(RequestShutdown), new object[] { sender, eventArgs });
            else
            {
                ShutDown();
                this.Close();
            }
        }//RequestShutdown()

        /// <summary>
        /// Shut down function to stop all services.
        /// </summary>
        private void ShutDown()
        {
            if (!m_IsShuttingDown)
            {
                m_IsShuttingDown = true;
                AppServices.GetInstance().Shutdown();
            }
        }//Shutdown()

        /// <summary>
        /// The remaining things needed to be done after service stopped.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void Service_ServiceStopped(object sender, EventArgs eventArgs)
        {
            // Service stopped and nothing is needed to do here.
        }//Service_ServiceStopped()

        /// <summary>
        /// When the TT gets connected, notify the users.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void Service_ServiceStateChanged(object sender, EventArgs eventArgs)
        {
            if (sender is TTApiService)
            {
                TTApiService ttApi = (TTApiService)sender;

                // Notify the user the TT API is connected successfully or not.
                if (ttApi.IsRunning)
                {
                    Log.NewEntry(LogLevel.Major, "TT API connected successfully!");
                }
                else
                {
                    Log.NewEntry(LogLevel.Warning, "TT API connecting failed!");
                }
            }
        }//Service_ServiceStateChanged()

        /// <summary>
        /// When the market tt api is connected, request the exchanges.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void MarketHub_MarketStatusChanged(object sender, EventArgs eventArgs)
        {
            MarketStatusChangedEventArg marketStatusChangedEventArg = (MarketStatusChangedEventArg)eventArgs;
            if (this.InvokeRequired)
            {
                EventHandler<MarketStatusChangedEventArg> marketStatusDelegate = new EventHandler<MarketStatusChangedEventArg>(MarketHub_MarketStatusChanged);
                this.Invoke(marketStatusDelegate, new object[] { sender, marketStatusChangedEventArg });
            }
            else
            {
                // Add the TT market to the list. It returns multiple markets at multiple times.
                List<string> ttMarketNameList = marketStatusChangedEventArg.MarketNameList;
                if (ttMarketNameList != null && ttMarketNameList.Count > 0)
                {
                    foreach (string ttMarketName in ttMarketNameList)
                    {
                        if (!m_MarketList.Contains(ttMarketName))
                            m_MarketList.Add(ttMarketName);
                    }
                }

                // Update the GUI. This is required by invoke function.
                listBoxMarkets.BeginUpdate();
                listBoxMarkets.Items.Clear();
                foreach (string market in m_MarketList)
                    listBoxMarkets.Items.Add(market);
                listBoxMarkets.EndUpdate();
            }
        }//MarketHub_MarketStatusChanged()

        /// <summary>
        /// The found resources contain product found or instrument found event handlers.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void MarketHub_FoundResource(object sender, EventArgs eventArgs)
        {
            if (eventArgs.GetType() != typeof(FoundServiceEventArg))
                return;
            
            FoundServiceEventArg foundServiceEventArg = (FoundServiceEventArg)eventArgs;
            if (this.InvokeRequired)
            {
                EventHandler<FoundServiceEventArg> marketResourceFoundDelegate = new EventHandler<FoundServiceEventArg>(MarketHub_FoundResource);
                this.Invoke(marketResourceFoundDelegate, new object[] { sender, foundServiceEventArg });
            }
            else
            {
                // Found service contains instrument or product.
                if (foundServiceEventArg.FoundProducts != null)
                {
                    string productName;
                    // Add the product to the corresponding future or spread dictionary.
                    foreach (Misty.Lib.Products.Product product in foundServiceEventArg.FoundProducts)
                    {
                        productName = product.ProductName;
                        if (product.Type == ProductTypes.Spread && !m_SpreadProducts.ContainsKey(productName))
                            m_SpreadProducts.Add(productName, product);
                        else if (product.Type == ProductTypes.Future && !m_FutureProducts.ContainsKey(productName))
                            m_FutureProducts.Add(productName, product);
                    }

                    // Update the product list on the GUI.
                    listBoxProducts.BeginUpdate();
                    listBoxProducts.Items.Clear();
                    foreach (string product in m_FutureProducts.Keys)
                        listBoxProducts.Items.Add(product);
                    foreach (string product in m_SpreadProducts.Keys)
                        listBoxProducts.Items.Add(product);
                    listBoxProducts.EndUpdate();
                }
                if (foundServiceEventArg.FoundInstruments != null)
                {
                    string instrumentFullName;
                    // Add the instrument to the corresponding future or spread dictionary.
                    foreach (Misty.Lib.Products.InstrumentName instrumentName in foundServiceEventArg.FoundInstruments)
                    {
                        instrumentFullName = instrumentName.FullName;
                        if (instrumentName.Product.Type == ProductTypes.Spread && !m_SpreadInstruments.ContainsKey(instrumentFullName))
                            m_SpreadInstruments.Add(instrumentFullName, instrumentName);
                        if (instrumentName.Product.Type == ProductTypes.Future && !m_FutureInstruments.ContainsKey(instrumentFullName))
                            m_FutureInstruments.Add(instrumentFullName, instrumentName);
                    }

                    // Update the instrument list on the GUI.
                    listBoxInstruments.BeginUpdate();
                    listBoxInstruments.Items.Clear();
                    foreach (string instrument in m_FutureInstruments.Keys)
                        listBoxInstruments.Items.Add(instrument);
                    foreach (string instrument in m_SpreadInstruments.Keys)
                        listBoxInstruments.Items.Add(instrument);
                    listBoxInstruments.EndUpdate();
                }
            }
        }//MarketHub_FoundResource()

        /// <summary>
        /// Event handler of closing the windows.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormClosedByUser(object sender, FormClosingEventArgs e)
        {
            Log.RequestStop();
            ShutDown();
        }//FormClosedByUser()

        /// <summary>
        /// When the selected index for market changes, it should show the products for that market.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listBoxMarkets_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Get the products for the selected market.
            string selectedMarket = listBoxMarkets.SelectedItem.ToString();
            m_FutureProducts.Clear();
            m_SpreadProducts.Clear();
            m_FutureInstruments.Clear();
            m_SpreadInstruments.Clear();

            // When the selected market changes, it requests products.
            if (string.IsNullOrEmpty(selectedMarket))
            {
                Log.NewEntry(LogLevel.Warning, "The program does not have a selected market.");
                return;
            }
            else
            {
                if (!m_MarketList.Contains(selectedMarket))
                {
                    Log.NewEntry(LogLevel.Warning, "The selected market is not contained in the market list.");
                    return;
                }
                else
                {
                    if (!m_MarketTTAPI.RequestProducts(selectedMarket))
                    {
                        Log.NewEntry(LogLevel.Warning, "Send product searching failed.");
                        return;
                    }
                    else
                        Log.NewEntry(LogLevel.Minor, "Successfully send request for the product search for market {0}.", selectedMarket);
                }
            }
        }//listBoxMarkets_SelectedIndexChanged()

        /// <summary>
        /// When the selected index for the product changes, it should show the instruments for that product.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listBoxProducts_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Wait the response above and subscribe to every spread instrument under that spread product.
            m_FutureInstruments.Clear();
            m_SpreadInstruments.Clear();
            string selectedProduct = listBoxProducts.SelectedItem.ToString();

            // When the selected product changes, it subscribe to the instruments.
            if (string.IsNullOrEmpty(selectedProduct))
            {
                Log.NewEntry(LogLevel.Warning, "The program does not have a selected spread product.");
                return;
            }
            else
            {
                // Subscribe to the future instruments.
                if (!m_FutureProducts.ContainsKey(selectedProduct))
                {
                    Log.NewEntry(LogLevel.Warning, "The selected spread is not contained in the future dictionary.");
                    return;
                }
                else
                {
                    if (!m_MarketTTAPI.RequestInstruments(m_FutureProducts[selectedProduct]))
                    {
                        Log.NewEntry(LogLevel.Warning, "Send instruments request for future term structure failed.");
                        return;
                    }
                }

                // Subscribe to the spread instruments.
                if (!m_SpreadProducts.ContainsKey(selectedProduct))
                {
                    Log.NewEntry(LogLevel.Warning, "The selected spread is not contained in the spread dictionary.");
                    return;
                }
                else
                {
                    if (!m_MarketTTAPI.RequestInstruments(m_SpreadProducts[selectedProduct]))
                    {
                        Log.NewEntry(LogLevel.Warning, "Send instruments request for spread term structure failed.");
                        return;
                    }
                }

                Log.NewEntry(LogLevel.Minor, "Successfully send request for the instruments search for product {0}.", selectedProduct);
            }
        }//listBoxProducts_SelectedIndexChanged()

        /// <summary>
        /// When the selected future instrument changes, it subscribes to the price for that instrument and also its spread components.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listBoxInstruments_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Wait the response from above and check which spread instruments are desired using boot-strapping method.
            string selectedInstrument = listBoxInstruments.SelectedItem.ToString();
            InstrumentName selectedInstrumentName;

            // Found the spread components for the selected future instrument, which is normally in the long term.
            if (!m_FutureInstruments.TryGetValue(selectedInstrument, out selectedInstrumentName))
            {
                Log.NewEntry(LogLevel.Warning, "Can not find selected instrument.");
                return;
            }

            // Firstly, subscribe to the price of that instrument.
            if (m_MarketTTAPI.RequestInstrumentSubscription(selectedInstrumentName))
            {
                m_MarketReadTimer.Stop();
                m_MarketReadTimer.Start();
                Log.NewEntry(LogLevel.Minor, "Successfully send trade price subscription for instrument {0}.", selectedInstrumentName.FullName);
            }
            else
                Log.NewEntry(LogLevel.Warning, "Failed to send trade price subscription for instrument {0}.", selectedInstrumentName.FullName);

            // Then construct the spread in the path and subscribe to the prices for the spread instrument components.
            ExpiryPoint startPoint = new ExpiryPoint(14, 3);
            ExpiryPoint endPoint = null;
            if (!BootStrappingRule.TryExtractExpiryYearMonth(selectedInstrumentName.SeriesName, out endPoint))
            {
                Log.NewEntry(LogLevel.Warning, "Failed to get the expiry point for future instrument {0}.", selectedInstrumentName);
                return;
            }
            else
            {
                Log.NewEntry(LogLevel.Minor, "Start bootstrapping algorithm for instrument {0}.", selectedInstrumentName);
                List<ExpiryPoint> expirySeries = null;
                List<List<ExpiryPoint>> spreadCombinations = null;

                // Expiry series only contains a limited number of expiry points.
                if (BootStrappingRule.GetExpirySeriesForProduct(m_FutureInstruments, out expirySeries))
                {
                    // Output the spread combinations by our algorithm under maximum step.
                    if (BootStrappingRule.TryGetAllPathsByEnumeration(startPoint, endPoint, expirySeries, 5, out spreadCombinations))
                    {
                        Log.BeginEntry(LogLevel.Minor, "Start bootstrapping enumeration:");
                        foreach (List<ExpiryPoint> row in spreadCombinations)
                        {
                            foreach (ExpiryPoint point in row)
                            {
                                Log.AppendEntry(point.ToString());
                                Log.AppendEntry(" ");
                            }
                            Log.AppendEntry("\r\n");
                        }
                        Log.EndEntry();
                        Log.NewEntry(LogLevel.Minor, "Successfully completes enumeration algorithm.");
                    }
                    else
                    {
                        Log.NewEntry(LogLevel.Warning, "There is problem in getting path combinations");
                        return;
                    }
                }
                else
                {
                    Log.NewEntry(LogLevel.Warning, "There is problem in getting expiry series");
                    return;
                }

                // Try to load product spread information table and construct the spread names that we need.
                string productName = selectedInstrumentName.Product.ProductName;
                List<List<string>> spreadNameGeneratedCombinations = null;
                if (m_CSVSpreadInfoReader.TryDetectProductName(productName))
                {
                    string firstDelimiter;
                    string secondDelimiter;
                    m_CSVSpreadInfoReader.TryGetFirstDateDelimiter(productName, out firstDelimiter);
                    m_CSVSpreadInfoReader.TryGetSecondDateDelimiter(productName, out secondDelimiter);

                    // Use the spread info reader to construct the names for the spread instruments in all paths.
                    if (!TryGenerateSpreadInstrumentNamesInAllPaths(firstDelimiter, secondDelimiter, spreadCombinations, out spreadNameGeneratedCombinations))
                    {
                        Log.NewEntry(LogLevel.Warning, "Failed to generate spread names for all paths");
                        return;
                    }
                    else
                    {
                        // Output the spread names for all paths to log viewer.
                        Log.BeginEntry(LogLevel.Minor); 
                        foreach (List<string> spreadNameEachPath in spreadNameGeneratedCombinations)
                        {
                            foreach (string spreadName in spreadNameEachPath)
                            {
                                Log.AppendEntry(spreadName);
                                Log.AppendEntry(" ");
                            }
                            Log.AppendEntry("\r\n");
                        }
                        Log.EndEntry();
                        Log.NewEntry(LogLevel.Minor, "Successfully generate spread names for all paths.");
                    }
                }
                else
                {
                    Log.NewEntry(LogLevel.Warning, "There is no entry for the product of {0} in the csv file", productName);
                    return;
                }

                // Try to match the generated names with what we have downloaded from the TT.
                // Pull a valid path and subscribe to the prices for the spread components in that path.
                int startPathIndex = 0;
                if (!TryPullSpreadInstruments(startPathIndex, spreadNameGeneratedCombinations, out m_InstrumentSubscriptions))
                {
                    Log.NewEntry(LogLevel.Warning, "There is problem in pulling tt spread instruments");
                    return;
                }
                else
                {
                    // This block subscribes to the inside market price for the spread instruments.
                    foreach (InstrumentName instrumentName in m_InstrumentSubscriptions)
                    {
                        if (m_MarketTTAPI.RequestInstrumentSubscription(instrumentName))
                            Log.NewEntry(LogLevel.Minor, "Successfully send trade price subscription for instrument {0}.", instrumentName.FullName);
                        else
                            Log.NewEntry(LogLevel.Warning, "Failed to send trade price subscription for instrument {0}.", instrumentName.FullName);
                    }
                }
            }
        }//listBoxInstruments_SelectedIndexChanged()

        /// <summary>
        /// The market reading timer ticks and read the market books to get prices.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MarketReadingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                System.Timers.ElapsedEventHandler marketReadingDelegate = new System.Timers.ElapsedEventHandler(MarketReadingTimer_Elapsed);
                this.Invoke(marketReadingDelegate, new object[] { sender, e });
            }
            else
            {
                string instrName;
                string bidPrice;
                string bidQty;
                string askPrice;
                string askQty;

                // Find the price for the future instrument that is in the long term.
                Book aBook;
                string selectedInstrument = listBoxInstruments.SelectedItem.ToString();
                InstrumentName selectedInstrumentName;
                if (!m_FutureInstruments.TryGetValue(selectedInstrument, out selectedInstrumentName))
                {
                    Log.NewEntry(LogLevel.Warning, "Can not find selected instrument.");
                    return;
                }
                instrName = selectedInstrumentName.FullName;
                if (m_MarketTTAPI.TryEnterReadBook(out aBook))
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
                        instrName = aBook.Instruments[instrID].Name.FullName;
                        bidPrice = aBook.Instruments[instrID].Price[0][0].ToString();
                        bidQty = aBook.Instruments[instrID].Qty[0][0].ToString();
                        askPrice = aBook.Instruments[instrID].Price[1][0].ToString();
                        askQty = aBook.Instruments[instrID].Qty[1][0].ToString();

                        // Publish the prices to the GUI.
                        textBoxBidPrice.Text = bidPrice;
                        textBoxBidQty.Text = bidQty;
                        textBoxAskPrice.Text = askPrice;
                        textBoxAskQty.Text = askQty;
                        textBoxExpirySeries.Text = selectedInstrumentName.SeriesName;
                    }
                    m_MarketTTAPI.ExitReadBook(aBook);
                }

                // Get the market prices for the spread instrument components.
                // Record the prices into logs and console.
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Clear();
                Log.BeginEntry(LogLevel.Minor);
                foreach (InstrumentName instrumentName in m_InstrumentSubscriptions)
                {
                    instrName = instrumentName.FullName;
                    if (m_MarketTTAPI.TryEnterReadBook(out aBook))
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
                            instrName = aBook.Instruments[instrID].Name.FullName;
                            bidPrice = aBook.Instruments[instrID].Price[0][0].ToString();
                            bidQty = aBook.Instruments[instrID].Qty[0][0].ToString();
                            askPrice = aBook.Instruments[instrID].Price[1][0].ToString();
                            askQty = aBook.Instruments[instrID].Qty[1][0].ToString();

                            // Write the prices to logs and console.
                            stringBuilder.AppendFormat("{5}_{0}:bid->{1}@{2},ask->{3}@{4}\n", instrName, bidPrice, bidQty, askPrice, askQty, DateTime.Now);
                            Log.AppendEntry("{5}_{0}:bid->{1}@{2},ask->{3}@{4}\n", instrName, bidPrice, bidQty, askPrice, askQty, DateTime.Now);
                        }
                        m_MarketTTAPI.ExitReadBook(aBook);
                    }
                }
                Console.Write(stringBuilder.ToString());
                Log.EndEntry();
            }
        }
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
