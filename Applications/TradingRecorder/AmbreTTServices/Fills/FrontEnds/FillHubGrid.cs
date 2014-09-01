using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;

namespace Ambre.TTServices.Fills.FrontEnds
{
    using SKACERO;

    using Misty.Lib.Application;
    using Misty.Lib.Utilities;
    using Misty.Lib.Hubs;

    using InstrumentName = Misty.Lib.Products.InstrumentName;

    using Ambre.TTServices.Markets;
    using Ambre.TTServices.Fills;
    using Ambre.TTServices.Talker;

    using TradingTechnologies.TTAPI.WinFormsHelpers;

    /// <summary>
    /// Version 16 April 2013:  Production version
    /// Single fillHub.
    /// Generalization of sort and rowNames to allow repeated instruments (in different groups, or hubs).
    /// </summary>
    public partial class FillHubGrid : UserControl
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // External services
        //private AmbreViewer m_ParentForm = null;
        public FillHub m_FillHub = null;
        private MarketTTAPI m_MarketHub = null;
        private LogHub Log = null;

        // My private tables
        //      InstrName (row name) -->  Instrument look-up.        
        //public Dictionary<string, InstrumentName> m_InstrumentNames = new Dictionary<string, InstrumentName>(); // name -> instr mapping       
        // Grouping tables
        //        private string[] GroupList = new string[]{"Exchange","Product"


        //      exch+prod key -->  {InstrName List}
        private Dictionary<string, List<string>> m_ExchangeGroups = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> m_ProductGroups = new Dictionary<string, List<string>>();

        // Controls
        private int m_MinMembersForProductGroupRow = 2;                                     // number of elements needed before we consider a summary row.
        private int m_MinMembersForExchangeGroupRow = 2;
        private string[] CellNameDeliminaterArray = new string[] { CellNameDelimiter };
        private bool IsSuppressRepetativeExchangeProductLabels = true;
        private bool m_AllInstrumentsFoundInMarket = false;
        private bool m_IsShuttingDown = false;

        // Popups
        private FormAddFills m_FormAddFills = null;


        // Constants
        private const string GroupRowNamePrefix = "Summary";
        private const string GroupMasterRow = "Total";
        private const string CellNameDelimiter = "***";                                     // Used to separate Row name and Column Name.
        private const string ColumnName_StartingRealPnL = "StartingRealPnL";
        private const string ColumnName_RealPnL = "RealPnL";                                // Labels for columns
        private const string ColumnName_UnrealPnL = "UnrealPnL";
        private const string ColumnName_TotalPnL = "TotalPnL";
        private const string ColumnName_Alias = "Alias";

        // Instrument tables.
        //      InstrumentName.FullName -->  all info needed for rows for this instr [real,unreal,total] PnL, etc
        public Dictionary<string, InstrumentRowData> m_InstrumentInfos = new Dictionary<string, InstrumentRowData>();   // instName
    

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public FillHubGrid()
        {
            InitializeComponent();
        }
        public FillHubGrid(Form parentForm, MarketTTAPI marketHub)
        {
            InitializeComponent();

            // Store pointers to external services.
            //m_ParentForm = parentForm;            
            m_MarketHub = marketHub;

            UpdateActiveGridColumns();                      // Create ListView columns.

            activeGrid1.MouseDown += new MouseEventHandler(activeGrid1_MouseDown);
            activeGrid1.MouseUp += new MouseEventHandler(activeGrid1_MouseUp);

        }
        //
        //
        //       
        #endregion//Constructors


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        // *********************************************
        // ****             AddHub()                ****
        // *********************************************
        /// <summary>
        /// User must add hub to which this page will listen to/and display 
        /// position changes.  
        /// </summary>
        /// <param name="newFillHub"></param>
        public void AddHub(FillHub newFillHub)
        {
            // Add to our fillhubs
            m_FillHub = newFillHub;
            if ( Log == null )
                Log = m_FillHub.Log;        // Use first fillHub's log as my own.

            // Listen to its events
            newFillHub.PositionBookCreated += new EventHandler(FillHub_PositionBookCreated);
            newFillHub.PositionBookDeleted += new EventHandler(FillHub_PositionBookDeleted);
            newFillHub.PositionBookChanged += new EventHandler(FillHub_PositionBookChanged);
            newFillHub.FillRejectionsUdated += new EventHandler(FillHub_FillRejected);

            // Rename this page after the fill hub it describes.
            if (string.IsNullOrEmpty(newFillHub.Name))
                this.Text = "Total";
            else
                this.Text = newFillHub.Name;
        }//AddHub()
        //
        //
        // *********************************************
        // ****             Delete Hub()            ****
        // *********************************************
        /// <summary>
        /// This is dangerous.  It is called when the user wants to remove this tab, 
        /// and ALSO destroy the associated hub.
        /// </summary>
        public void DeleteHub()
        {
            if (m_FillHub == null)
                return;
            m_FillHub.Stopping += FillHub_Stopping;                 // Ask to be told when my fillHub actually is stopping.
            m_FillHub.Request(new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestShutdown));
            // FillHub shutdown may take some time... while it saves positions etc.
            Shutdown();                                             // disconnect from all the events, stop updating.
                                                                    // Continue the shutdown, when stopping event is triggered.
        }// DeleteHub()
        //
        //
        //
        private void FillHub_Stopping(object sender, EventArgs eventArg)
        {
            // Can delete drop files?
            // Delete myself from parent.            

        }
        //
        //
        // *********************************************
        // ****             UpdatePage()            ****
        // *********************************************
        /// <summary>
        /// This is called within the Timer_Tick() method of the owning TabControl (or parent form).
        /// </summary>
        public void UpdatePage()
        {
            if (m_IsShuttingDown)
                return;

            if (!m_AllInstrumentsFoundInMarket)                     // Look for unknown instruments.
                FindInstrumentInfo();

            UpdateInstrumentMarketsAndPnL();

        }// UpdatePage()
        //
        //
        // *************************************************************
        // ****                     Shutdown()                      ****
        // *************************************************************
        /// <summary>
        /// This is called by the parent form when it is shutting down.
        /// </summary>
        public void Shutdown()
        {
            if (!m_IsShuttingDown)
            {
                m_IsShuttingDown = true;
                activeGrid1.Enabled = false;
                activeGrid1.AllowFlashing = false;
                activeGrid1.MouseDown -= new MouseEventHandler(activeGrid1_MouseDown);
                activeGrid1.MouseUp -= new MouseEventHandler(activeGrid1_MouseUp);

                m_FillHub.PositionBookCreated -= new EventHandler(FillHub_PositionBookCreated);
                m_FillHub.PositionBookDeleted -= new EventHandler(FillHub_PositionBookDeleted);
                m_FillHub.PositionBookChanged -= new EventHandler(FillHub_PositionBookChanged);
                m_FillHub.FillRejectionsUdated -= new EventHandler(FillHub_FillRejected);
                
            }
        }//Shutdown()
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
        // ****                 Find Instrument Info()              ****
        /// <summary>
        /// We need to know its expiry dates for instruments and currency rates.
        /// These must be obtained from the MarketHub, so we have to wait for the 
        /// MarketHub to load the instrument details.  This routine repeatedly checks.
        /// </summary>
        private void FindInstrumentInfo()
        {
            bool allFoundNow = true;
            TradingTechnologies.TTAPI.InstrumentDetails details;
            foreach (string name in m_InstrumentInfos.Keys)
            {
                InstrumentRowData info = m_InstrumentInfos[name];
                InstrumentName instrumentName = m_InstrumentInfos[name].InstrumentName;// m_InstrumentNames[name];
                allFoundNow = allFoundNow && info.IsFoundInMarket;
                if (!info.IsFoundInMarket && m_MarketHub.TryLookupInstrumentDetails(instrumentName, out details))
                {   // Found details for this instrument, update things.
                    info.IsFoundInMarket = true;
                    m_MarketHub.RequestInstrumentSubscription(instrumentName);                  // request market subscriptions for this instrument.
                    double x = details.Currency.GetConversionRate(TradingTechnologies.TTAPI.Currency.PrimaryCurrency);
                    if (double.IsNaN(x) || double.IsInfinity(x))
                        m_InstrumentInfos[instrumentName.FullName].CurrencyRate = 1;
                    else
                        m_InstrumentInfos[instrumentName.FullName].CurrencyRate = x;
                    m_InstrumentInfos[instrumentName.FullName].ExpirationDate = details.ExpirationDate.ToDateTime();
                    m_InstrumentInfos[instrumentName.FullName].IsPriceChanged = true;
                    //m_InstrumentInfos[instrumentName.FullName].MarketPriceDecimals = details.MarketPriceDecimals;

                    // Update the grid.
                    //string ss = m_InstrumentInfos[instrumentName.FullName].ExpirationDate.ToString("yyyy-MM-dd");
                    //UpdateActiveGridCell(instrumentName.FullName, "Expiry", ss , false); 
                    //ss = string.Format("{0}{1}{2}{3}", instrumentName.Product.Exchange, instrumentName.Product.Type.ToString(), instrumentName.Product.ProductName, m_InstrumentInfos[instrumentName.FullName].ExpirationDate.ToString("yyyyMMdd"));
                    //UpdateActiveGridCell(instrumentName.FullName, "SortKey", ss , false);    
                    FillHub.PositionBookChangedEventArgs e = new FillHub.PositionBookChangedEventArgs();
                    e.Instrument = instrumentName;
                    FillHub_PositionBookCreated(this, e);                   // Call myself to re-initialize the row for this book.
                }
            }// next instrument
            activeGrid1.Sort();                                             // Sort rows
            m_AllInstrumentsFoundInMarket = allFoundNow;                    // if all instruments have been found, we won't come back here again.
        } // FindInstrumentInfo()
        //
        //
        //
        // ****             Update Instrument Markets And PnL()         ****
        /// <summary>
        /// The method has two stages.  First, the MarketBook is locked and instrument price changes are noted.
        /// Second, for those instruments whose prices have changed, update their grid rows (and summary rows).
        /// </summary>
        private void UpdateInstrumentMarketsAndPnL()
        {
            //
            // Loop thru each instrument, update their info tables.
            //
            Misty.Lib.BookHubs.Book aBook;
            if (m_MarketHub.TryEnterReadBook(out aBook)) // TODO: remove this for race-problem with MarketHub
            {
                foreach (string instrName in m_InstrumentInfos.Keys)
                {
                    Misty.Lib.BookHubs.Market marketInstrument = null;
                    InstrumentRowData info = m_InstrumentInfos[instrName];
                    if (info.MarketID < 0)                                  // confirm that we know the market for this instrument.
                    {
                        int id;
                        if (m_MarketHub.TryLookupInstrumentID(m_InstrumentInfos[instrName].InstrumentName, out id))
                        {
                            try
                            {
                                marketInstrument = aBook.Instruments[id];
                                info.MarketID = id;
                            }
                            catch (Exception)
                            {
                                if ( Log!=null)
                                Log.NewEntry(LogLevel.Major, "Viewer: Book exception! Book has only {1} instruments and no id {0}. ", id, aBook.Instruments.Count);
                                break;
                            }
                        }
                    }
                    else
                        marketInstrument = aBook.Instruments[info.MarketID];
                    if (marketInstrument != null)
                    {
                        double midPrice = (marketInstrument.Price[0][0] + marketInstrument.Price[1][0]) / 2.0;
                        double lastPrice = marketInstrument.LastPrice;
                        double price = lastPrice;
                        if (price == 0.0)
                            price = midPrice;
                        if (info.Price != price)
                        {
                            info.Price = price;                                 // update this instrument's info object.
                            info.IsPriceChanged = true;                         // market this instrument for Updating in grid (below)!
                        }

                    }
                }
                m_MarketHub.ExitReadBook(aBook);
            }
            //
            // Update grid rows
            //
            List<string> prodKeys = new List<string>();
            foreach (string instrName in m_InstrumentInfos.Keys)
            {
                InstrumentRowData info = m_InstrumentInfos[instrName];
                if (info.IsPriceChanged)
                {
                    InstrumentName instrumentName = m_InstrumentInfos[instrName].InstrumentName;
                    IFillBook positionBook;
                    if (m_FillHub.TryEnterReadBook(instrumentName, out positionBook))
                    {
                        info.UnrealPnL = positionBook.UnrealizedDollarGains(info.Price) * info.CurrencyRate;
                        info.RealPnL = positionBook.RealizedDollarGains * info.CurrencyRate;            // overkill, but useful at start up.
                        m_FillHub.ExitReadBook(instrumentName);
                        // Update product group names we need to update.
                        string prodGroupName = GetProductGroupKey(instrumentName);
                        if (!prodKeys.Contains(prodGroupName))
                            prodKeys.Add(prodGroupName);
                        info.IsPriceChanged = false;                    // unset this flag since we have updated this instrument.
                    }

                    ActiveGridUpateInstrumentPnL(instrumentName);
                }
            }//next instrName
            foreach (string prodName in prodKeys)
                ActiveGridUpdateGroup(prodName);                        // update group rows
            ActiveGridUpdateTotals();                                   // update the totals
        }
        //
        //
        //
        #endregion//Private Methods


        #region ActiveGrid Update Methods
        // *****************************************************************
        // ****                  Active Grid Methods                    ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// This method should be called whenever the PositionBookChanges for a specific instrument.
        /// The instrument should have already triggered the PositionBookCreated event, but we double check just in case.
        /// Then, we proceed to acquire the lock for its fill book, extract data from it, and update all the cells.
        /// </summary>
        /// <param name="eventArgs"></param>
        private void ActiveGridUpdatePosition(FillHub.PositionBookChangedEventArgs eventArgs)
        {
            InstrumentName instrumentName = eventArgs.Instrument;
            if (!m_InstrumentInfos.ContainsKey(instrumentName.FullName))                   // make sure we know this instrument - usually we get the new PosBook event first.
                return;

            // Update info about the position book that changed.           
            InstrumentRowData info = m_InstrumentInfos[instrumentName.FullName];
            IFillBook positionBook;
            if (m_FillHub.TryEnterReadBook(instrumentName, out positionBook))
            {
                info.Position = positionBook.NetPosition;
                info.StartingRealPnL = Math.Round(positionBook.RealizedStartingDollarGains * info.CurrencyRate, 2);
                info.RealPnL = Math.Round(positionBook.RealizedDollarGains * info.CurrencyRate, 2);
                info.UnrealPnL = Math.Round(positionBook.UnrealizedDollarGains() * info.CurrencyRate, 2);      // only this can change outside of position changing.
                info.AverageCost = Math.Round(positionBook.AveragePrice, info.MarketPriceDecimals);
                m_FillHub.ExitReadBook(instrumentName);                         // return the book
            }


            // Update the cells.
            UpdateActiveGridCell(instrumentName.FullName, "Position", info.Position, true);
            UpdateActiveGridCell(instrumentName.FullName, ColumnName_StartingRealPnL, info.StartingRealPnL, false);
            UpdateActiveGridCell(instrumentName.FullName, ColumnName_RealPnL, info.RealPnL, false);
            UpdateActiveGridCell(instrumentName.FullName, ColumnName_UnrealPnL, info.UnrealPnL, false);
            UpdateActiveGridCell(instrumentName.FullName, ColumnName_TotalPnL, info.TotalPnL, false);
            UpdateActiveGridCell(instrumentName.FullName, "AvePrice", info.AverageCost, false);

            // Update group pnl
            ActiveGridUpdateGroup(GetProductGroupKey(instrumentName));

        }//ActiveGridUpdatePosition()
        //
        //
        //
        private void ActiveGridUpateInstrumentPnL(InstrumentName instrumentName)
        {
            InstrumentRowData info = m_InstrumentInfos[instrumentName.FullName];
            UpdateActiveGridCell(instrumentName.FullName, ColumnName_RealPnL, info.RealPnL, false);     // this is overkill, but nice to update this periodically.
            UpdateActiveGridCell(instrumentName.FullName, ColumnName_UnrealPnL, info.UnrealPnL, false);
            UpdateActiveGridCell(instrumentName.FullName, ColumnName_TotalPnL, info.TotalPnL, false);
        }
        //
        //
        /// <summary>
        /// Whenever a PnL has to be updateed for an instrument that is part of a group, the group
        /// PnL must also be updated.
        /// </summary>
        /// <param name="productGroupKey">Name associated with group of instruments, obtained from instrument using 
        /// GetProductGroupKey(instrument);</param>
        private void ActiveGridUpdateGroup(string productGroupKey)
        {

            if (m_ProductGroups.ContainsKey(productGroupKey)
                && this.activeGrid1.Items.ContainsKey(string.Format("{0}{1}", GroupRowNamePrefix, productGroupKey)))
            {
                double sumPnL = 0;
                double sumRealPnL = 0;
                double sumUnRealPnL = 0;
                double sumStartRealPnL = 0;
                InstrumentRowData info;
                foreach (string instrName in m_ProductGroups[productGroupKey])
                    if (m_InstrumentInfos.TryGetValue(instrName, out info))
                    {
                        sumStartRealPnL += info.StartingRealPnL;
                        sumUnRealPnL += info.UnrealPnL;
                        sumRealPnL += info.RealPnL;
                        sumPnL += info.TotalPnL;
                    }
                UpdateActiveGridCell(string.Format("{0}{1}", GroupRowNamePrefix, productGroupKey), ColumnName_TotalPnL, sumPnL, false);
                UpdateActiveGridCell(string.Format("{0}{1}", GroupRowNamePrefix, productGroupKey), ColumnName_StartingRealPnL, sumStartRealPnL, false);
                UpdateActiveGridCell(string.Format("{0}{1}", GroupRowNamePrefix, productGroupKey), ColumnName_RealPnL, sumRealPnL, false);
                UpdateActiveGridCell(string.Format("{0}{1}", GroupRowNamePrefix, productGroupKey), ColumnName_UnrealPnL, sumUnRealPnL, false);
            }
        }// ActiveGridUpdateGroup()
        //
        private void ActiveGridUpdateTotals()
        {
            double sumPnL = 0;
            double sumRealPnL = 0;
            double sumUnRealPnL = 0;
            double sumStartRealPnL = 0.0;
            foreach (InstrumentRowData info1 in m_InstrumentInfos.Values)
            {
                sumPnL += info1.TotalPnL;
                sumStartRealPnL += info1.StartingRealPnL;
                sumRealPnL += info1.RealPnL;
                sumUnRealPnL += info1.UnrealPnL;
            }
            UpdateActiveGridCell(string.Format("{0}{1}", GroupRowNamePrefix, GroupMasterRow), ColumnName_TotalPnL, sumPnL, true);
            UpdateActiveGridCell(string.Format("{0}{1}", GroupRowNamePrefix, GroupMasterRow), ColumnName_StartingRealPnL, sumStartRealPnL, true);
            UpdateActiveGridCell(string.Format("{0}{1}", GroupRowNamePrefix, GroupMasterRow), ColumnName_RealPnL, sumRealPnL, true);
            UpdateActiveGridCell(string.Format("{0}{1}", GroupRowNamePrefix, GroupMasterRow), ColumnName_UnrealPnL, sumUnRealPnL, true);
        }// ActiveGridUpdateTotals()
        //
        //
        private void UpdateActiveGridCell(string rowName, string columnName, object newValue1, bool isFlash)
        {
            string keyCell = GetCellKey(rowName, columnName);
            Decimal newValue;
            if (!Decimal.TryParse(newValue1.ToString(), out newValue))
                return;
            //newValue = Convert.ToDecimal(newValue1);
            SKACERO.ActiveRow.ActiveCell aCell = this.activeGrid1.FindCell(keyCell);
            if (aCell != null)
            {
                decimal oldValue = aCell.DecimalValue;
                aCell.DecimalValue = newValue;
                // Set fore colors
                if (newValue >= 0)
                    aCell.ForeColor = Color.Blue;
                else if (newValue < 0)
                    aCell.ForeColor = Color.Red;
                // Flash
                if (isFlash)
                {
                    if (newValue > oldValue)
                    {
                        aCell.FlashBackColor = Color.Blue;
                        //aCell.FlashPreText = "▲";
                        //aCell.FlashPostText = String.Empty;
                    }
                    else if (newValue < oldValue)
                    {
                        aCell.FlashBackColor = Color.Red;
                        //aCell.FlashPreText = "▼";
                        //aCell.FlashPostText = String.Empty;
                    }
                    else
                    {
                        //aCell.FlashBackColor = Color.White;
                        //aCell.FlashPreText = String.Empty;
                        //aCell.FlashPostText = String.Empty;                        
                    }
                }
            }
        }//UpdateActiveGridCell()
        //
        #endregion //ActiveGrid Update Methods


        #region ActiveGrid Creation Methods
        // *****************************************************************
        // ****             Active Grid Creation Methods                ****
        // *****************************************************************
        //
        //
        //
        private void ActiveGridAddNewRow(FillHub.PositionBookChangedEventArgs eventArgs)
        {
            InstrumentName instrumentName = eventArgs.Instrument;
            if (m_InstrumentInfos.ContainsKey(instrumentName.FullName)
                && activeGrid1.RowExists(instrumentName.FullName))
            {   // We already have a row for this instrument!  Update its values.
                // Update things that depend on the the currency rate too... since this may have updated!  [04 Jun 2013]
                InstrumentRowData info = m_InstrumentInfos[instrumentName.FullName];
                IFillBook positionBook;  
                if ( m_FillHub.TryEnterReadBook(instrumentName,out positionBook) )
                {
                    info.StartingRealPnL = Math.Round(positionBook.RealizedStartingDollarGains * info.CurrencyRate, 2);
                    info.RealPnL = Math.Round(positionBook.RealizedDollarGains * info.CurrencyRate, 2);
                    info.UnrealPnL = Math.Round(positionBook.UnrealizedDollarGains() * info.CurrencyRate, 2);
                    m_FillHub.ExitReadBook(instrumentName);
                }
                SKACERO.ActiveRow row = activeGrid1.Items[instrumentName.FullName];
                row.SubItems[GetCellKey(instrumentName.FullName, ColumnName_StartingRealPnL)].Text = info.StartingRealPnL.ToString("0.00");
                row.SubItems[GetCellKey(instrumentName.FullName, ColumnName_RealPnL)].Text = info.RealPnL.ToString("0.00");
                row.SubItems[GetCellKey(instrumentName.FullName, ColumnName_UnrealPnL)].Text = info.UnrealPnL.ToString("0.00");
                row.SubItems[GetCellKey(instrumentName.FullName, "Expiry")].Text = m_InstrumentInfos[instrumentName.FullName].ExpirationDate.ToString("yyyy-MM-dd");
                row.SubItems[GetCellKey(instrumentName.FullName, "SortKey")].Text = string.Format("{0}{1}{2}{3}", instrumentName.Product.Exchange, instrumentName.Product.Type.ToString(), instrumentName.Product.ProductName, m_InstrumentInfos[instrumentName.FullName].ExpirationDate.ToString("yyyyMMdd"));
                activeGrid1.Sort();                                                     // sort again, now we've changed the SortKey.
                return;
            }
            //
            // Create all tables we need for each new instrument.
            //
            //m_InstrumentNames.Add(instrumentName.FullName, instrumentName);             // add this to my list of instruments to watch.
            m_InstrumentInfos.Add(instrumentName.FullName, new InstrumentRowData(instrumentName));
            m_AllInstrumentsFoundInMarket = false;                                      // with a newly added row, set this to false.


            // Add instrument to appropriate Groups.
            //
            // TODO: Add the instrument to the "hub group" using eventArgs.Sender.Name
            //
            string strKey = instrumentName.Product.Exchange;
            if (!m_ExchangeGroups.ContainsKey(strKey))                      // Exchange group
                m_ExchangeGroups.Add(strKey, new List<string>());           // maintain an entry for each ex
            if (!m_ExchangeGroups[strKey].Contains(instrumentName.FullName))
            {
                m_ExchangeGroups[strKey].Add(instrumentName.FullName);
                if (m_ExchangeGroups[strKey].Count >= m_MinMembersForExchangeGroupRow
                   && (!activeGrid1.Items.ContainsKey(string.Format("{0}{1}", GroupRowNamePrefix, strKey))))
                    CreateSummaryRows(strKey, instrumentName.Product.Exchange, string.Empty);
            }
            strKey = GetProductGroupKey(instrumentName);                    // Product group: construct the exch+prod group name.
            if (!m_ProductGroups.ContainsKey(strKey))                       // first time this product group showed up!
                m_ProductGroups.Add(strKey, new List<string>());            // create a place for this prod group to hold its membership list.
            if (!m_ProductGroups[strKey].Contains(instrumentName.FullName)) // If this instrument is not yet part of group, add him.
            {
                m_ProductGroups[strKey].Add(instrumentName.FullName);       // add new member of group             
                if ((m_ProductGroups[strKey].Count >= m_MinMembersForProductGroupRow)
                    && (!activeGrid1.Items.ContainsKey(string.Format("{0}{1}", GroupRowNamePrefix, strKey))))
                    CreateSummaryRows(strKey, string.Empty, instrumentName.Product.ProductName);  // need to add a new summary line 
            }

            //
            // Create the row.
            //
            SKACERO.ActiveRow aRow = new SKACERO.ActiveRow();
            aRow.Name = instrumentName.FullName;
            aRow.Text = instrumentName.FullName;                            // this will appear in the zeroth column of the row.
            for (int i = 1; i < this.activeGrid1.Columns.Count; i++)
            {
                SKACERO.ActiveRow.ActiveCell cell = new SKACERO.ActiveRow.ActiveCell(aRow, String.Empty);
                cell.Name = GetCellKey(instrumentName.FullName, this.activeGrid1.Columns[i].Name);
                cell.DecimalValue = Decimal.Zero;
                cell.PreTextFont = new Font("Arial", cell.Font.Size);
                cell.PostTextFont = new Font("Arial", cell.Font.Size);
                aRow.SubItems.Add(cell);
            }
            // Load constant cells
            //            aRow.SubItems[GetCellKey(instrumentName.FullName, "HubName")].Text = instrumentName.Product.Type.ToString();
            //aRow.SubItems[GetCellKey(instrumentName.FullName, "ProductType")].Text = instrumentName.Product.Type.ToString();
            aRow.SubItems[GetCellKey(instrumentName.FullName, "Expiry")].Text = m_InstrumentInfos[instrumentName.FullName].ExpirationDate.ToString("yyyy-MM-dd");
            aRow.SubItems[GetCellKey(instrumentName.FullName, "SortKey")].Text = string.Format("{0}{1}{2}{3}", instrumentName.Product.Exchange, instrumentName.Product.Type.ToString(), instrumentName.Product.ProductName, m_InstrumentInfos[instrumentName.FullName].ExpirationDate.ToString("yyyyMMdd"));
            aRow.SubItems[GetCellKey(instrumentName.FullName, ColumnName_Alias)].Text = instrumentName.SeriesName;

            // Load Exchange cell optionally
            strKey = instrumentName.Product.Exchange;
            if (IsSuppressRepetativeExchangeProductLabels && activeGrid1.Items.ContainsKey(string.Format("{0}{1}", GroupRowNamePrefix, strKey)))
                aRow.SubItems[GetCellKey(instrumentName.FullName, "Exchange")].Text = string.Empty;
            else
                aRow.SubItems[GetCellKey(instrumentName.FullName, "Exchange")].Text = instrumentName.Product.Exchange;

            // Load Product cell, optionally.
            strKey = GetProductGroupKey(instrumentName);
            if (IsSuppressRepetativeExchangeProductLabels && activeGrid1.Items.ContainsKey(string.Format("{0}{1}", GroupRowNamePrefix, strKey)))
                aRow.SubItems[GetCellKey(instrumentName.FullName, "Product")].Text = string.Empty;
            else
                aRow.SubItems[GetCellKey(instrumentName.FullName, "Product")].Text = instrumentName.Product.ProductName;

            // Add row to our collection           
            this.activeGrid1.SuspendLayout();
            try
            {
                if (!this.activeGrid1.Items.ContainsKey(aRow.Name))
                    this.activeGrid1.Items.Add(aRow);
            }
            catch(Exception e)
            {   // This is an error!
                // TODO: Fix me!
                if (Log != null)
                    Log.NewEntry(LogLevel.Major, "ActiveGridAddNewRow: Failed to add new row with name {0}. Execption {1}", aRow.Name,e.Message);

            }
            activeGrid1.Sort();
            this.activeGrid1.ResumeLayout();

            // Update the row
            ActiveGridUpdatePosition(eventArgs);

        }//ActiveGridAddNewRow()
        //
        //
        //
        //
        //
        private void ActiveGridRemoveRow(FillHub.PositionBookChangedEventArgs eventArgs)
        {
            InstrumentName instrumentName = eventArgs.Instrument;           // instrument we want to remove.
            if (m_InstrumentInfos.ContainsKey(instrumentName.FullName)
                && activeGrid1.RowExists(instrumentName.FullName))
            {
                // Remove it from groups.
                string strKey = instrumentName.Product.Exchange;
                if (m_ExchangeGroups.ContainsKey(strKey) && m_ExchangeGroups[strKey].Contains(instrumentName.FullName))
                    m_ExchangeGroups[strKey].Remove(instrumentName.FullName);
                strKey = GetProductGroupKey(instrumentName);
                if (m_ProductGroups.ContainsKey(strKey) && m_ProductGroups[strKey].Contains(instrumentName.FullName))
                    m_ProductGroups[strKey].Remove(instrumentName.FullName);

                // Remove its info now
                if (m_InstrumentInfos.ContainsKey(instrumentName.FullName))
                    m_InstrumentInfos.Remove(instrumentName.FullName);

                // Remove it from the row now.
                SKACERO.ActiveRow row = activeGrid1.Items[instrumentName.FullName];
                this.activeGrid1.AllowFlashing = false;
                this.activeGrid1.BeginUpdate();
                string s = row.Name;
                row.Name = string.Format("{0}{1}", row.Name,row.GetHashCode()); // Hack: If I remove a instr row, then get a fill in it, we get a dupe key error!?! This avoids.
                //this.activeGrid1.Items.RemoveByKey(row.Name);                   // remove this
                //this.activeGrid1.Items.Remove(row);

                this.activeGrid1.Items.Clear();

                this.activeGrid1.EndUpdate();
                
                //this.activeGrid1.ResumeLayout();
            }
        }// ActiveGridRemoveRow()
        //
        // 
        //
        private string GetProductGroupKey(InstrumentName instrument)
        {
            return string.Format("{0}{1}{2}", instrument.Product.Exchange, instrument.Product.Type.ToString(), instrument.Product.ProductName);
        }
        //
        //
        // ****                 GetCellKey()                ****
        //
        /// <summary>
        /// Utility functiont that creates a format for the name of the cell 
        /// given its row/column names.
        /// </summary>
        private string GetCellKey(string rowName, string columnName)
        {
            return string.Format("{0}{1}{2}", rowName, CellNameDelimiter, columnName);
        }
        //
        // ****             TryGet RowColumn Names()             ****
        //
        /// <summary>
        /// A utility function, that is the inverse of GetCellKey, splits the CellKey 
        /// string into a RowName string and a ColumnName string.
        /// </summary>
        /// <returns>True if successful.</returns>
        private bool TryGetRowColumnNames(string CellKeyString, out string rowName, out string columnName)
        {
            string[] pieces = CellKeyString.Split(CellNameDeliminaterArray, StringSplitOptions.RemoveEmptyEntries);
            if (pieces.Length == 2)
            {
                rowName = pieces[0];
                columnName = pieces[1];
                return true;
            }
            else
            {
                rowName = string.Empty;
                columnName = string.Empty;
                return false;
            }
        }// TryGetRowColumnNames()
        //
        //
        //
        //
        //
        //
        //
        // ****             Update ActiveGrid Columns()                 ****
        /// <summary>
        /// This is the first method for the grid, called from the constructor.
        /// Defines and formats the columns of the table.
        /// </summary>
        private void UpdateActiveGridColumns()
        {
            this.activeGrid1.SuspendLayout();
            SKACERO.ActiveColumnHeader column;

            //
            // Add a column for Instrument Names
            //
            column = new SKACERO.ActiveColumnHeader();
            column.Name = "Name";
            //column.Width = 150;
            column.Width = 0;
            //column.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
            column.Text = "instrument";
            column.CellFormat = "";
            column.CellHorizontalAlignment = System.Drawing.StringAlignment.Far;
            column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
            column.SortOrder = SKACERO.SortOrderEnum.Unsorted;
            this.activeGrid1.Columns.Add(column);

            //
            // Identity
            //
            // Sort Key
            column = new SKACERO.ActiveColumnHeader();
            column.Name = "SortKey";                    // The rows are sorted by the string in this column, not visible to user. 
            column.Width = 0;
            column.Text = "sort key";
            column.CellFormat = "";
            column.CellHorizontalAlignment = System.Drawing.StringAlignment.Center;
            column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
            column.SortOrder = SKACERO.SortOrderEnum.Ascending;
            this.activeGrid1.Columns.Add(column);

            // Exchange
            column = new SKACERO.ActiveColumnHeader();
            column.Name = "Exchange";
            column.Width = 100;
            column.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            column.Text = "exchange";
            column.CellFormat = "";
            column.CellHorizontalAlignment = System.Drawing.StringAlignment.Near;
            column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
            column.SortOrder = SKACERO.SortOrderEnum.Unsorted;
            this.activeGrid1.Columns.Add(column);

            // Product
            column = new SKACERO.ActiveColumnHeader();
            column.Name = "Product";
            column.Width = 100;
            column.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
            column.Text = "product";            
            column.CellFormat = "";
            column.CellHorizontalAlignment = System.Drawing.StringAlignment.Near;
            column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
            column.SortOrder = SKACERO.SortOrderEnum.Unsorted;
            this.activeGrid1.Columns.Add(column);

            // Product Types - future, etc
            /*
            column = new SKACERO.ActiveColumnHeader();
            column.Name = "ProductType";
            column.Width = 0;
            column.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
            column.Text = "prodType";
            column.CellFormat = "";
            column.CellHorizontalAlignment = System.Drawing.StringAlignment.Near;
            column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
            column.SortOrder = SKACERO.SortOrderEnum.Unsorted;
            this.activeGrid1.Columns.Add(column);
            */ 

            // Expiry
            column = new SKACERO.ActiveColumnHeader();
            column.Name = "Expiry";
            column.Width = 0;
            column.Text = "expiry";
            column.CellFormat = "";
            column.CellHorizontalAlignment = System.Drawing.StringAlignment.Center;
            column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
            column.SortOrder = SKACERO.SortOrderEnum.Unsorted;
            this.activeGrid1.Columns.Add(column);


            // Alias - the nice version of the name
            column = new SKACERO.ActiveColumnHeader();
            column.Name = ColumnName_Alias;
            column.Width = 80;
            //column.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
            column.Text = "alias";
            column.TextAlign = HorizontalAlignment.Right;
            column.CellFormat = "";
            column.CellHorizontalAlignment = System.Drawing.StringAlignment.Far;
            column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
            column.SortOrder = SKACERO.SortOrderEnum.Unsorted;
            this.activeGrid1.Columns.Add(column);


            // Position info
            column = new SKACERO.ActiveColumnHeader();
            column.Name = "Position";
            //column.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
            column.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            //column.Width = 80;
            column.Text = "position";
            column.TextAlign = HorizontalAlignment.Right;
            column.CellFormat = "+0;-0; ";
            column.CellHorizontalAlignment = System.Drawing.StringAlignment.Far;
            column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
            column.SortOrder = SKACERO.SortOrderEnum.Unsorted;
            column.DisplayZeroValues = false;
            this.activeGrid1.Columns.Add(column);


            // Average price Pnl
            column = new SKACERO.ActiveColumnHeader();
            column.Name = "AvePrice";
            //column.Width = 80;
            //column.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
            column.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            column.Text = "ave price";
            column.TextAlign = HorizontalAlignment.Right;
            //column.CellFormat = "0";
            column.CellHorizontalAlignment = System.Drawing.StringAlignment.Far;
            column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
            column.SortOrder = SKACERO.SortOrderEnum.Unsorted;
            column.DisplayZeroValues = false;
            this.activeGrid1.Columns.Add(column);

            // Starting Real Pnl
            column = new SKACERO.ActiveColumnHeader();
            column.Name = ColumnName_StartingRealPnL;
            //column.Width = 80;
            column.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
            //column.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            column.Text = "start PnL";
            column.TextAlign = HorizontalAlignment.Right;
            column.CellFormat = "0.00";
            column.CellHorizontalAlignment = System.Drawing.StringAlignment.Far;
            column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
            column.SortOrder = SKACERO.SortOrderEnum.Unsorted;
            column.DisplayZeroValues = false;
            this.activeGrid1.Columns.Add(column);

            // Real Pnl
            column = new SKACERO.ActiveColumnHeader();
            column.Name = ColumnName_RealPnL;
            //column.Width = 80;
            //column.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
            column.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            column.Text = "real PnL";
            column.TextAlign = HorizontalAlignment.Right;
            column.CellFormat = "0.00";
            column.CellHorizontalAlignment = System.Drawing.StringAlignment.Far;
            column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
            column.SortOrder = SKACERO.SortOrderEnum.Unsorted;
            column.DisplayZeroValues = false;
            this.activeGrid1.Columns.Add(column);


            // UnReal Pnl
            column = new SKACERO.ActiveColumnHeader();
            column.Name = ColumnName_UnrealPnL;
            //column.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
            //column.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            column.Width = 80;
            column.Text = "unreal PnL";
            column.TextAlign = HorizontalAlignment.Right;
            column.CellFormat = "0.00";
            column.CellHorizontalAlignment = System.Drawing.StringAlignment.Far;
            column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
            column.SortOrder = SKACERO.SortOrderEnum.Unsorted;
            column.DisplayZeroValues = false;
            this.activeGrid1.Columns.Add(column);

            // Real Pnl
            column = new SKACERO.ActiveColumnHeader();
            column.Name = ColumnName_TotalPnL;
            //column.Width = 80;
            column.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            column.Text = "total PnL";
            column.TextAlign = HorizontalAlignment.Right;
            column.CellFormat = "0.00";
            column.CellHorizontalAlignment = System.Drawing.StringAlignment.Far;
            column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
            column.SortOrder = SKACERO.SortOrderEnum.Unsorted;
            this.activeGrid1.Columns.Add(column);
            this.activeGrid1.ResumeLayout();

            //
            // Other properties
            //
            this.activeGrid1.FlashDuration = 300;       // msecs
            this.activeGrid1.AllowFlashing = true;
            this.activeGrid1.UseFlashFadeOut = false;
            this.activeGrid1.UseAlternateRowColors = true;

            this.activeGrid1.MultiSelect = false;

            CreateSummaryRows(GroupMasterRow, string.Empty, string.Empty, "0");            // create master summary row.

            // Resize width - Estimate the width of the activeGrid and resize the client window.
            int totalWidth = 0;
            foreach (SKACERO.ActiveColumnHeader col in this.activeGrid1.Columns)
                totalWidth += col.Width + 1;
            int height = this.activeGrid1.Height;
            int edgeWidth = this.activeGrid1.Location.X;
            height = this.ClientSize.Height;
            this.SetClientSizeCore(totalWidth + 2 * edgeWidth + 20, height);


            // Force repainting.
            this.activeGrid1.ListViewItemSorter = new ListViewItemComparer(activeGrid1.Columns["SortKey"].Index);
            this.activeGrid1.Invalidate();
        }//UpdateActiveGridColumns()
        //
        //
        //
        //
        //
        // ****             Create Summary Rows()               ****
        //
        /// <summary>
        /// This creates a summary row, with a SortKey to group it just above
        /// those rows it describes.
        /// </summary>
        /// <param name="rowBaseName"></param>
        /// <param name="exchangeName"></param>
        /// <param name="prodName"></param>
        /// <param name="sortString"></param>
        private void CreateSummaryRows(string rowBaseName, string exchangeName, string prodName, string sortString = "")
        {
            //Log.NewEntry(LogLevel.Minor, "Viewer: Adding new Group row {0}.", rowBaseName);
            //SKACERO.ActiveRow aRow = new SKACERO.ActiveRow(new string[0], 0, Color.Black, Color.Blue, new Font("Arial", 9f, FontStyle.Bold));
            SKACERO.ActiveRow aRow = new SKACERO.ActiveRow();
            aRow.UseItemStyleForSubItems = false;
            if (string.IsNullOrEmpty(rowBaseName))
                aRow.Name = GroupRowNamePrefix; // this option doesn't seem to ever be used.
            else
                aRow.Name = string.Format("{0}{1}", GroupRowNamePrefix, rowBaseName);   // This is name of this row.
            aRow.Text = aRow.Name;
            //
            // Load the remaining cells - empty.
            //
            for (int i = 1; i < this.activeGrid1.Columns.Count; i++)
            {
                SKACERO.ActiveRow.ActiveCell cell = new SKACERO.ActiveRow.ActiveCell(aRow, String.Empty);
                cell.Name = GetCellKey(aRow.Name, this.activeGrid1.Columns[i].Name);
                cell.DecimalValue = Decimal.Zero;
                cell.PreTextFont = new Font("Arial", cell.Font.Size, FontStyle.Bold);
                cell.PostTextFont = new Font("Arial", cell.Font.Size, FontStyle.Bold);
                Font font = cell.Font;
                cell.Font = new Font(font.Name, font.Size, FontStyle.Bold);
                //cell.BackColor = Color.Blue;                
                //cell.FlashFont = new Font(cell.Font.FontFamily,cell.Font.Size,FontStyle.Bold);
                cell.Format = "+0;-0; ";
                cell.Text = "";
                aRow.SubItems.Add(cell);
            }
            //
            // SortKey must be constructed so this row appears just above the group it summarizes.
            //
            if (!string.IsNullOrEmpty(sortString))
                aRow.SubItems[GetCellKey(aRow.Name, "SortKey")].Text = string.Format("{0}", sortString);    // user overriding sortString manually.
            else if (string.IsNullOrEmpty(rowBaseName))
                aRow.SubItems[GetCellKey(aRow.Name, "SortKey")].Text = string.Format("0{0}", aRow.Name);
            else
                aRow.SubItems[GetCellKey(aRow.Name, "SortKey")].Text = string.Format("{0}0", rowBaseName);  // usual sorting key.

            //
            // Display content of row now.
            //
            if (!string.IsNullOrEmpty(exchangeName))
                aRow.SubItems[GetCellKey(aRow.Name, "Exchange")].Text = exchangeName;
            if (!string.IsNullOrEmpty(prodName))
                aRow.SubItems[GetCellKey(aRow.Name, "Product")].Text = prodName;

            // Add to our collection
            this.activeGrid1.SuspendLayout();
            this.activeGrid1.Items.Add(aRow);
            activeGrid1.Sort();
            this.activeGrid1.ResumeLayout();

            // Clean up old instrument entries that are part of my subgroup.
            // This optional feature is that instrument rows will not display an
            // exchange nor product label when they have been grouped beneath a summary
            // row with this info.
            if (IsSuppressRepetativeExchangeProductLabels)
            {
                List<string> members;
                if (m_ExchangeGroups.TryGetValue(rowBaseName, out members))
                {
                    foreach (string instrName in members)
                    {
                        string keyCell = GetCellKey(instrName, "Exchange");
                        SKACERO.ActiveRow.ActiveCell aCell = this.activeGrid1.FindCell(keyCell);
                        if (aCell != null)
                        {
                            //Log.NewEntry(LogLevel.Minor, "Viewer: Deleting cell {0} for {1}.", keyCell, instrName);
                            aCell.Text = string.Empty;
                        }
                    }
                }
                if (m_ProductGroups.TryGetValue(rowBaseName, out members))
                {
                    foreach (string instrName in members)
                    {
                        string keyCell = GetCellKey(instrName, "Product");
                        SKACERO.ActiveRow.ActiveCell aCell = this.activeGrid1.FindCell(keyCell);
                        if (aCell != null)
                        {
                            //Log.NewEntry(LogLevel.Minor, "Viewer: Deleting cell {0} for {1}.", keyCell, instrName);
                            aCell.Text = string.Empty;
                        }
                    }
                }
            }


        }//CreateSummaryRows()
        //
        //
        #endregion//active grid methods



        #region Private Methods
        // **************************************************************
        // ****                  Show AddFills()                     ****
        // **************************************************************
        /// <summary>
        /// A request from somewhere to open the "add fills" form.  If one already
        /// exists, we just raise it to the top.
        /// </summary>
        /// <param name="fillHub">fillHub that will receive the new fill</param>
        /// <param name="instrumentName">instrument to be filled</param>
        public void ShowAddFills(FillHub fillHub, InstrumentName instrumentName)
        {
            if (m_FormAddFills == null || m_FormAddFills.IsDisposed)
            {
                m_FormAddFills = new FormAddFills(m_MarketHub, fillHub);
                m_FormAddFills.FormClosing += new FormClosingEventHandler(ChildForm_Closing);
                m_FormAddFills.Show();
            }
            if (m_FormAddFills.WindowState == FormWindowState.Minimized)
                m_FormAddFills.WindowState = FormWindowState.Normal;
            if (!m_FormAddFills.Visible)
                m_FormAddFills.Visible = true;
            m_FormAddFills.Focus();
            m_FormAddFills.SetInstrument(fillHub, instrumentName);
        }//ShowAddFills()
        //
        //
        //
        #endregion // Private Methods


        #region External Service Event Handlers
        // *****************************************************************
        // ****         External Service Event Handlers                 ****
        // *****************************************************************
        //
        //
        // *********************************************************************
        // ****             FillHub Position Book Created                   ****
        // *********************************************************************
        private void FillHub_PositionBookCreated(object sender, EventArgs eventArgs)
        {
            if (m_IsShuttingDown) return;
            if (this.InvokeRequired)
                this.Invoke(new EventHandler(FillHub_PositionBookCreated), new object[] { sender, eventArgs });
            else
                ActiveGridAddNewRow((FillHub.PositionBookChangedEventArgs)eventArgs);                
        }
        // *********************************************************************
        // ****             FillHub Position Book Deleted                   ****
        // *********************************************************************
        private void FillHub_PositionBookDeleted(object sender, EventArgs eventArgs)
        {
            if (m_IsShuttingDown) return;
            if (this.InvokeRequired)
                this.Invoke(new EventHandler(FillHub_PositionBookDeleted), new object[] { sender, eventArgs });
            else
                ActiveGridRemoveRow((FillHub.PositionBookChangedEventArgs)eventArgs);
        }
        // *********************************************************************
        // ****             FillHub Position Book Changed                   ****
        // *********************************************************************
        private void FillHub_PositionBookChanged(object sender, EventArgs eventArgs)
        {
            if (m_IsShuttingDown) return;
            if (this.InvokeRequired)
                this.Invoke(new EventHandler(FillHub_PositionBookChanged), new object[] { sender, eventArgs });
            else
                ActiveGridUpdatePosition((FillHub.PositionBookChangedEventArgs)eventArgs);
        }
        // *********************************************************************
        // ****             FillHub Position Book PnL Changed               ****
        // *********************************************************************
        private void FillHub_PositionBookPnLChanged(object sender, EventArgs eventArgs)
        {
            if (m_IsShuttingDown) return;
            if (this.InvokeRequired)
                this.Invoke(new EventHandler(FillHub_PositionBookPnLChanged), new object[] { sender, eventArgs });
            else
                ActiveGridUpdatePosition((FillHub.PositionBookChangedEventArgs)eventArgs);
        }
        //
        //
        // *********************************************************************
        // ****             FillHub Fill Rejected                           ****
        // *********************************************************************
        private void FillHub_FillRejected(object sender, EventArgs eventArgs)
        {
            if (m_IsShuttingDown) return;
            if (Log!=null)
                Log.NewEntry(LogLevel.Minor, "Viewer: Fill rejected {0}", eventArgs);
            //m_ParentForm.ShowRejectedFills(sender, eventArgs); 
        }
        //
        //
        // *********************************************************************
        // ****                     Child Form Closing                      ****
        // *********************************************************************
        /// <summary>
        /// If the user closes one of the child forms owned by this object, we want to drop its pointer.
        /// All sub forms, opened by this form, need to fire this event when they are closing.  That is, 
        /// this object will subscribe to "Closing" event for every child form it opens.
        /// </summary>
        private void ChildForm_Closing(object sender, FormClosingEventArgs e)
        {
            if (m_IsShuttingDown) return;
            Type formType = sender.GetType();
            Log.NewEntry(LogLevel.Minor, "Viewer: Child form closing {0}", formType);
            if (formType == typeof(FormAddFills))
            {
                m_FormAddFills.FormClosing -= new FormClosingEventHandler(ChildForm_Closing);
                m_FormAddFills = null;                      // disconnect
            }
            //else if (formType == typeof(FormFillBookViewer))
            //{
            //    m_FormFillBook.FormClosing -= new FormClosingEventHandler(ChildForm_Closing);
            //    m_FormFillBook = null;
            //}
            //else if (formType == typeof(Ambre.TTServices.Fills.RejectedFills.FormRejectViewer))
            //{
            //    //m_RejectedFillViewer.FormClosing -= new FormClosingEventHandler(ChildForm_Closing);
            //    //m_RejectedFillViewer = null;
            //}
        }//ExternalForm_Closing()
        //
        #endregion//External Service Event Handlers



        #region Control Event Handlers
        // *****************************************************************
        // ****              Control Event Handlers                     ****
        // *****************************************************************
        //
        //
        // *************************************************************
        // ****             activeGrid1_MouseUp()                   ****
        // *************************************************************
        private void activeGrid1_MouseUp(object sender, MouseEventArgs e)
        {
            string instrNameStr;
            string columnName;
            if (!TryGetRowColumnClicked(e, out instrNameStr, out columnName))
                return;                                                             // failed to find a valid cell that was clicked on.
            if ( Log!=null)
                Log.NewEntry(LogLevel.Minor, "Viewer: MouseUp on activeGrid, instr={0} col={1}.", instrNameStr, columnName);

            // Analyze click
            //InstrumentName instrumentName;
            InstrumentRowData info;
            if (m_InstrumentInfos.TryGetValue(instrNameStr, out info))
            {
                if (columnName.Equals("Position", StringComparison.CurrentCultureIgnoreCase))       // user wants to change position.
                    this.ShowAddFills(m_FillHub, info.InstrumentName);   // Open the form allowing users to add fills by hand.
            }
            else if (Log!=null)
                Log.NewEntry(Misty.Lib.Hubs.LogLevel.Minor, "Clicked row name {0} not associated with instrument.", instrNameStr);
        }// activeGrid1_MouseUp()
        //
        //
        //     
        // *************************************************************
        // ****            TryGetRowColumnClicked()                 ****
        // *************************************************************
        private bool TryGetRowColumnClicked(MouseEventArgs e, out string rowName, out string colName)
        {
            rowName = string.Empty;
            colName = string.Empty;

            ListViewHitTestInfo hitInfo = activeGrid1.HitTest(e.Location.X, e.Location.Y);  // Determine cell user has clicked on. 
            ListViewItem clickedItem = null;                                                // the row that was clicked on.
            ListViewItem.ListViewSubItem clickedSubItem = null;                             // the exact cell clicked on.
            if (hitInfo != null && hitInfo.Item != null)
            {
                clickedItem = hitInfo.Item;
                clickedSubItem = hitInfo.Item.GetSubItemAt(e.Location.X, e.Location.Y);     // get the cell
                if (clickedSubItem != null)
                    return TryGetRowColumnNames(clickedSubItem.Name, out rowName, out colName);
                else
                    return false;
            }
            else
                return false;
        }// TryGetRowColumnClicked()
        //
        //
        //
        // *************************************************************
        // ****            activeGrid1_MouseDown()                  ****
        // *************************************************************
        private void activeGrid1_MouseDown(object sender, MouseEventArgs e)
        {
            string rowName;
            string colName;
            if (TryGetRowColumnClicked(e, out rowName, out colName))
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Left && colName.Equals(ColumnName_Alias))
                {
                    //InstrumentName instrumentName;
                    TradingTechnologies.TTAPI.InstrumentDetails details;
                    InstrumentRowData info;
                    //if (m_InstrumentNames.TryGetValue(rowName, out instrumentName) && m_MarketHub.TryGetInstrumentDetails(instrumentName, out details))
                    if (m_InstrumentInfos.TryGetValue(rowName, out info) && m_MarketHub.TryLookupInstrumentDetails(info.InstrumentName, out details))
                        activeGrid1.DoDragDrop(details.Key.ToDataObject(), DragDropEffects.Copy);
                }
            }
        }// activeGrid1_MouseDown()
        //
        #endregion//Event Handlers

    }
}
