using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Ambre.PositionViewer
{
    using SKACERO;

    using Misty.Lib.Application;
    using Misty.Lib.Utilities;
    using Misty.Lib.Hubs;
   
    //using InstrumentBase = Misty.Lib.Products.InstrumentBase;          // to distinguish from TT instrument class.
    using InstrumentName = Misty.Lib.Products.InstrumentName;           
    
    using Ambre.TTServices.Markets;
    using Ambre.TTServices.Fills;
    using Ambre.TTServices.Talker;

    using TradingTechnologies.TTAPI.WinFormsHelpers;

    public partial class FormPositionViewer : Form
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // External services
        private TTServices.TTApiService m_TTServices = null;
        private FillHub m_FillHub = null;
        private MarketTTAPI m_MarketHub = null;
        private TalkerHub m_TalkerHub = null;
        private LogHub Log = null;
        private Timer m_Timer = new Timer();
        private bool m_IsShuttingDown = false;

        // My active child forms
        private FormAddFills m_FormAddFills = null;
        private FormFillBookViewer m_FormFillBook = null;
        private Ambre.TTServices.Fills.RejectedFills.FormRejectViewer m_RejectedFillViewer = null;


        // My private tables
        //      InstrName (row name) -->  Instrument look-up.        
        private Dictionary<string, InstrumentName> m_InstrumentNames = new Dictionary<string, InstrumentName>(); // name -> instr mapping       
        //      exch+prod key -->  {InstrName List}
        private Dictionary<string, List<string>> m_ExchangeGroups = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> m_ProductGroups = new Dictionary<string, List<string>>();

        //private ConcurrentBag<string> m_InstrumentsChanged = new ConcurrentBag<string>();

        // Controls
        private int m_MinMembersForProductGroupRow = 2;                                     // number of elements needed before we consider a summary row.
        private int m_MinMembersForExchangeGroupRow = 2;
        private string[] CellNameDeliminaterArray = new string[] { CellNameDelimiter };
        private bool IsSuppressRepetativeExchangeProductLabels = true;
        private int m_PnLUpdateTimeInterval = 2000;                                         // msecs
        

        private DateTime m_FinalizeNextTime;                                                // time we perform "end of session" stuff.
        private TimeSpan m_FinalizeTime = new TimeSpan(16, 15, 0);                          // 4:15 pm?
        private bool m_AllInstrumentsFoundInMarket = false;

        // Constants
        private const string GroupRowNamePrefix = "Summary";
        private const string GroupMasterRow = "Total";           
        private const string CellNameDelimiter = "***";                                     // Used to separate Row name and Column Name.
        private const string ColumnName_RealPnL = "RealPnL";                                // Labels for columns
        private const string ColumnName_UnrealPnL = "UnrealPnL";
        private const string ColumnName_TotalPnL = "TotalPnL";
        private const string ColumnName_Alias = "Alias";

        //      instrName -->  [real,unreal,total] PnL                                      // local data that appears in data row.
        private Dictionary<string, InstrumentRowData> m_InstrumentInfos = new Dictionary<string, InstrumentRowData>();  // instName
    
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public FormPositionViewer()
        {
            InitializeComponent();
            AppInfo info = AppInfo.GetInstance("Ambre", true);            // Set application information - do this before hubs are instantiated.
            info.RequestShutdownAddHandler(new EventHandler(this.RequestShutdown));  // allows users to request an app shutdown

            InitializeServices();                                   // Create services we will need.            

            if (!m_IsShuttingDown)
                UpdateActiveGridColumns();                          // Create ListView columns.
            
        }
        private void InitializeServices()
        {
            bool isLogViewerVisible = false;
            #if (DEBUG)
                isLogViewerVisible = true;
            #endif

            List<Misty.Lib.IO.Xml.Node> nodes = null;
            if (!TryReadConfigFile(out nodes))
            {
                return;
            }

            // Instantiate TT API.
            m_TTServices = TTServices.TTApiService.GetInstance();
            m_TTServices.ServiceStateChanged += new EventHandler(TTServices_ServiceStateChanged);
            m_TTServices.Start(true);

            // Instantiate hubs
            m_MarketHub = new MarketTTAPI();
            m_MarketHub.Log.IsViewActive = isLogViewerVisible;
            //m_MarketHub.InstrumentChanged += new EventHandler(MarketHub_InstrumentChanged);
            m_MarketHub.Start();


            string fillHubName = typeof(FillHub).FullName;
            foreach (Misty.Lib.IO.Xml.Node aNode in nodes)
            {
                if (aNode.Name.Equals(fillHubName))
                {
                    FillHub fillHub = null;
                    string acctName;
                    if (!aNode.Attributes.TryGetValue("UserAccount", out acctName))
                        acctName = string.Empty;
                    fillHub = new FillHub(acctName, true);
                    fillHub.MarketHub = m_MarketHub;
                    fillHub.PositionBookCreated += new EventHandler(FillHub_PositionBookCreated);
                    fillHub.PositionBookChanged += new EventHandler(FillHub_PositionBookChanged);
                    fillHub.FillRejectionsUdated += new EventHandler(FillHub_FillRejected);
                    if (Log == null)
                        Log = fillHub.Log;                          // use the first log
                    if (m_FillHub == null)
                        m_FillHub = fillHub;
                }
            }
            

            m_TalkerHub = new TalkerHub(false);
            m_TalkerHub.RequestAddHub(m_FillHub);
            m_TalkerHub.ServiceStateChanged += new EventHandler(TalkerHub_ServiceStateChanged);
            m_TalkerHub.Start();

            m_Timer.Tick += new EventHandler(Timer_Tick);
            m_Timer.Interval = m_PnLUpdateTimeInterval;
            //m_Timer.Enabled = true;
            //m_Timer.Start();


            m_FinalizeNextTime = DateTime.Now.Subtract(DateTime.Now.TimeOfDay).Add(m_FinalizeTime); // today at ~4 pm
            if (m_FinalizeNextTime.CompareTo(DateTime.Now) <= 0)
                m_FinalizeNextTime = m_FinalizeNextTime.AddDays(1.0);               // if we already passed ~4pm, we mean tomorrow.

            m_FillHub.Start();

        }//Initialize()
        //
        //
        /// <summary>
        /// Reads config file in Config directory, if exists and returns nodes.
        /// </summary>
        private  bool TryReadConfigFile(out List<Misty.Lib.IO.Xml.Node>nodesLoaded)
        {
            nodesLoaded = null;
            string filePath = string.Format("{0}AmbreConfig.txt",AppInfo.GetInstance().UserConfigPath);
            try
            {
                using (Misty.Lib.IO.Xml.StringifiableReader reader = new Misty.Lib.IO.Xml.StringifiableReader(filePath))
                {
                    nodesLoaded = reader.ReadNodesToEnd();
                    reader.Close();
                    reader.Dispose();
                }
            }
            catch (Exception )
            {
                string caption = "Config file not found";
                string text = string.Format("Ambre failed to find a config file {0}\nIf you want to run without one, choose OK. Otherwise choose Cancel to quit.",filePath);
                DialogResult result = MessageBox.Show(text, caption, MessageBoxButtons.OKCancel);
                if (result == System.Windows.Forms.DialogResult.Cancel)
                {                    
                    return false;
                }

            }
            return true;
        }//ReadConfigFile()
        //
        //
        //       
        #endregion//Constructors


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
            if (!m_InstrumentNames.ContainsValue(instrumentName))                   // make sure we know this instrument - usually we get the new PosBook event first.
                return;

            // Update info about the position book that changed.           
            InstrumentRowData info = m_InstrumentInfos[instrumentName.FullName];
            IFillBook positionBook;
            if (m_FillHub.TryEnterReadBook(instrumentName, out positionBook))
            {
                info.Position = positionBook.NetPosition;
                info.RealPnL = Math.Round(positionBook.RealizedDollarGains * info.CurrencyRate,2);
                info.UnrealPnL = Math.Round(positionBook.UnrealizedDollarGains() * info.CurrencyRate,2);      // only this can change outside of position changing.
                info.AverageCost = Math.Round(positionBook.AveragePrice,info.MarketPriceDecimals);
                m_FillHub.ExitReadBook(instrumentName);                         // return the book
            }

            // Update the cells.
            UpdateActiveGridCell(instrumentName.FullName, "Position", info.Position, true);
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
                InstrumentRowData info;
                foreach (string instrName in m_ProductGroups[productGroupKey])
                    if (m_InstrumentInfos.TryGetValue(instrName, out info))
                    {
                        sumUnRealPnL += info.UnrealPnL;
                        sumRealPnL += info.RealPnL;
                        sumPnL += info.TotalPnL;
                    }
                UpdateActiveGridCell(string.Format("{0}{1}", GroupRowNamePrefix, productGroupKey), ColumnName_TotalPnL, sumPnL, false);
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
            foreach (InstrumentRowData info1 in m_InstrumentInfos.Values)
            {
                sumPnL += info1.TotalPnL;
                sumRealPnL += info1.RealPnL;
                sumUnRealPnL += info1.UnrealPnL;
            }
            UpdateActiveGridCell(string.Format("{0}{1}", GroupRowNamePrefix, GroupMasterRow), ColumnName_TotalPnL, sumPnL, true);
            UpdateActiveGridCell(string.Format("{0}{1}", GroupRowNamePrefix, GroupMasterRow), ColumnName_RealPnL, sumRealPnL, true);
            UpdateActiveGridCell(string.Format("{0}{1}", GroupRowNamePrefix, GroupMasterRow), ColumnName_UnrealPnL, sumUnRealPnL, true);
        }// ActiveGridUpdateTotals()
        //
        //
        private void UpdateActiveGridCell(string rowName, string columnName, object newValue1, bool isFlash)
        {
            string keyCell = GetCellKey(rowName, columnName);
            Decimal newValue;
            if (!Decimal.TryParse(newValue1.ToString(),out newValue))
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
        private void ActiveGridAddNewRow(FillHub.PositionBookChangedEventArgs eventArgs)
        {
            InstrumentName instrumentName = eventArgs.Instrument;
            if (m_InstrumentNames.ContainsValue(instrumentName) && m_InstrumentInfos.ContainsKey(instrumentName.FullName) 
                && activeGrid1.RowExists(instrumentName.FullName) )
            {   // We already have a row for this instrument!
                // Update its details.
                SKACERO.ActiveRow row = activeGrid1.Items[instrumentName.FullName];
                row.SubItems[GetCellKey(instrumentName.FullName, "Expiry")].Text = m_InstrumentInfos[instrumentName.FullName].ExpirationDate.ToString("yyyy-MM-dd");
                row.SubItems[GetCellKey(instrumentName.FullName, "SortKey")].Text = string.Format("{0}{1}{2}{3}", instrumentName.Product.Exchange, instrumentName.Product.Type.ToString(), instrumentName.Product.ProductName, m_InstrumentInfos[instrumentName.FullName].ExpirationDate.ToString("yyyyMMdd"));
                activeGrid1.Sort();
                return;
            }
            //
            // Create all tables we need for each new instrument.
            //
            //Log.NewEntry(LogLevel.Minor, "Viewer: Adding new instrument row for {0}.", instrumentName.FullName);
            m_InstrumentNames.Add(instrumentName.FullName, instrumentName);              // add this to my list of instruments to watch.
            m_InstrumentInfos.Add(instrumentName.FullName, new InstrumentRowData(instrumentName));
            m_AllInstrumentsFoundInMarket = false;                                          // with a newly added row, set this to false.

            



            
            
            // Add instrument to appropriate Groups.
            string strKey = instrumentName.Product.Exchange;
            if (!m_ExchangeGroups.ContainsKey(strKey))        // Exchange group
            {
                m_ExchangeGroups.Add(strKey, new List<string>());
            }
            if (!m_ExchangeGroups[strKey].Contains(instrumentName.FullName))
            {
                m_ExchangeGroups[strKey].Add(instrumentName.FullName);
                if (m_ExchangeGroups[strKey].Count >= m_MinMembersForExchangeGroupRow
                   && (!activeGrid1.Items.ContainsKey(string.Format("{0}{1}",GroupRowNamePrefix,strKey))))
                    CreateSummaryRows(strKey, instrumentName.Product.Exchange, string.Empty);                    
            }
            strKey = GetProductGroupKey(instrumentName);                  // Product group: construct the exch+prod group name.
            if (! m_ProductGroups.ContainsKey(strKey))                       // first time this product group showed up!
                m_ProductGroups.Add(strKey, new List<string>());             // create a place for this prod group to hold its membership list.
            if (!m_ProductGroups[strKey].Contains(instrumentName.FullName))          // If this instrument is not yet part of group, add him.
            {
                m_ProductGroups[strKey].Add(instrumentName.FullName);                // add new member of group             
                if ( (m_ProductGroups[strKey].Count>=m_MinMembersForProductGroupRow) 
                    && (! activeGrid1.Items.ContainsKey(string.Format("{0}{1}",GroupRowNamePrefix,strKey))))
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
                cell.Name = GetCellKey(instrumentName.FullName,this.activeGrid1.Columns[i].Name);
                cell.DecimalValue = Decimal.Zero;
                cell.PreTextFont = new Font("Arial", cell.Font.Size);
                cell.PostTextFont = new Font("Arial", cell.Font.Size);
                aRow.SubItems.Add(cell);
            }
            // Load constant cells
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
            this.activeGrid1.Items.Add(aRow);
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
        private string GetProductGroupKey(InstrumentName instrument)
        {
            return string.Format("{0}{1}{2}", instrument.Product.Exchange, instrument.Product.Type.ToString(), instrument.Product.ProductName);
        }
        //
        //
        // ****                 GetCellKey()                ****
        //
        /// <summary>
        /// Creates a format for the name of the cell given its row/column names.
        /// </summary>
        private string GetCellKey(string rowName, string columnName)
        {
            return string.Format("{0}{1}{2}", rowName, CellNameDelimiter, columnName);
        }
        //
        //
        private bool TryGetRowColumnNames(string CellKeyString, out string rowName, out string columnName)
        {
            string[] pieces = CellKeyString.Split(CellNameDeliminaterArray,StringSplitOptions.RemoveEmptyEntries);
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
        //
        private void UpdateActiveGridColumns()
        {
            this.activeGrid1.SuspendLayout();
            SKACERO.ActiveColumnHeader column;

            //
            // Add a column for Instrument Names
            //
            column = new SKACERO.ActiveColumnHeader();
            //column.Width = 150;
            column.Width = 0;
            //column.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
            column.Text = "instrument";
            column.Name = "Name";
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
            column.Width = 0;
            column.Text = "sort key";
            column.Name = "SortKey";
            column.CellFormat = "";
            column.CellHorizontalAlignment = System.Drawing.StringAlignment.Center;
            column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
            column.SortOrder = SKACERO.SortOrderEnum.Ascending;
            this.activeGrid1.Columns.Add(column);            

            // Exchange
            column = new SKACERO.ActiveColumnHeader();
            column.Width = 100;
            column.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            column.Text = "exchange";
            column.Name = "Exchange";
            column.CellFormat = "";
            column.CellHorizontalAlignment = System.Drawing.StringAlignment.Near;
            column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
            column.SortOrder = SKACERO.SortOrderEnum.Unsorted;
            this.activeGrid1.Columns.Add(column);

            // Product
            column = new SKACERO.ActiveColumnHeader();
            column.Width = 100;
            column.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
            column.Text = "product";
            column.Name = "Product";
            column.CellFormat = "";
            column.CellHorizontalAlignment = System.Drawing.StringAlignment.Near;
            column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
            column.SortOrder = SKACERO.SortOrderEnum.Unsorted;
            this.activeGrid1.Columns.Add(column);

            // Expiry
            column = new SKACERO.ActiveColumnHeader();
            column.Width = 0;
            column.Text = "expiry";
            column.Name = "Expiry";
            column.CellFormat = "";
            column.CellHorizontalAlignment = System.Drawing.StringAlignment.Center;
            column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
            column.SortOrder = SKACERO.SortOrderEnum.Unsorted;
            this.activeGrid1.Columns.Add(column);


            // Alias - the nice version of the name
            column = new SKACERO.ActiveColumnHeader();
            column.Width = 80;
            //column.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
            column.Text = "alias";
            column.Name = ColumnName_Alias;
            column.TextAlign = HorizontalAlignment.Right;
            column.CellFormat = "";
            column.CellHorizontalAlignment = System.Drawing.StringAlignment.Far;
            column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
            column.SortOrder = SKACERO.SortOrderEnum.Unsorted;
            this.activeGrid1.Columns.Add(column);


            // Position info
            column = new SKACERO.ActiveColumnHeader();
            //column.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
            column.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            //column.Width = 80;
            column.Text = "position";
            column.Name = "Position";
            column.TextAlign = HorizontalAlignment.Right;
            column.CellFormat = "+0;-0; ";
            column.CellHorizontalAlignment = System.Drawing.StringAlignment.Far;
            column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
            column.SortOrder = SKACERO.SortOrderEnum.Unsorted;            
            column.DisplayZeroValues = false;
            this.activeGrid1.Columns.Add(column);


            // Real Pnl
            column = new SKACERO.ActiveColumnHeader();
            //column.Width = 80;
            //column.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
            column.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            column.Text = "ave price";
            column.Name = "AvePrice";
            column.TextAlign = HorizontalAlignment.Right;
            //column.CellFormat = "0";
            column.CellHorizontalAlignment = System.Drawing.StringAlignment.Far;
            column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
            column.SortOrder = SKACERO.SortOrderEnum.Unsorted;
            column.DisplayZeroValues = false;
            this.activeGrid1.Columns.Add(column);            

            // Real Pnl
            column = new SKACERO.ActiveColumnHeader();
            //column.Width = 80;
            //column.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
            column.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            column.Text = "real PnL";
            column.Name = ColumnName_RealPnL;
            column.TextAlign = HorizontalAlignment.Right;
            column.CellFormat = "0.00";
            column.CellHorizontalAlignment = System.Drawing.StringAlignment.Far;
            column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
            column.SortOrder = SKACERO.SortOrderEnum.Unsorted;
            column.DisplayZeroValues = false;
            this.activeGrid1.Columns.Add(column);
            

            // UnReal Pnl
            column = new SKACERO.ActiveColumnHeader();
            //column.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
            //column.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            column.Width = 80;
            column.Text = "unreal PnL";
            column.Name = ColumnName_UnrealPnL;
            column.TextAlign = HorizontalAlignment.Right;
            column.CellFormat = "0.00";
            column.CellHorizontalAlignment = System.Drawing.StringAlignment.Far;
            column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
            column.SortOrder = SKACERO.SortOrderEnum.Unsorted;
            column.DisplayZeroValues = false;
            this.activeGrid1.Columns.Add(column);            

            // Real Pnl
            column = new SKACERO.ActiveColumnHeader();
            //column.Width = 80;
            column.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            column.Text = "total PnL";
            column.Name = ColumnName_TotalPnL;
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

            // Resize width
            int totalWidth = 0;
            foreach (SKACERO.ActiveColumnHeader col in this.activeGrid1.Columns)
            {                
                totalWidth += col.Width;
            }
            int height = this.activeGrid1.Height;
            int edgeWidth = this.activeGrid1.Location.X;
            height = this.ClientSize.Height;
            this.SetClientSizeCore(totalWidth + 2 * edgeWidth + 20, height);


            // Force repainting.
            this.activeGrid1.ListViewItemSorter = new ListViewItemComparer( activeGrid1.Columns["SortKey"].Index );            
            this.activeGrid1.Invalidate();
        }//UpdateActiveGridColumns()
        //
        //
        //
        class ListViewItemComparer : System.Collections.IComparer
        {
            private int col;
            public ListViewItemComparer()
            {
                col = 0;
            }
            public ListViewItemComparer(int column)
            {
                col = column;
            }
            public int Compare(object x, object y)
            {
                int returnVal = -1;
                returnVal = String.Compare(((ListViewItem)x).SubItems[col].Text,
                ((ListViewItem)y).SubItems[col].Text);
                return returnVal;
            }
        }
        //
        //
        private void CreateSummaryRows(string rowBaseName,string exchangeName, string prodName, string sortString = "")
        {
            //Log.NewEntry(LogLevel.Minor, "Viewer: Adding new Group row {0}.", rowBaseName);
            //SKACERO.ActiveRow aRow = new SKACERO.ActiveRow(new string[0], 0, Color.Black, Color.Blue, new Font("Arial", 9f, FontStyle.Bold));
            SKACERO.ActiveRow aRow = new SKACERO.ActiveRow();
            aRow.UseItemStyleForSubItems = false;
            if (string.IsNullOrEmpty(rowBaseName))
                aRow.Name = GroupRowNamePrefix;
            else
                aRow.Name = string.Format("{0}{1}",GroupRowNamePrefix,rowBaseName);
            aRow.Text = aRow.Name;
            for (int i = 1; i < this.activeGrid1.Columns.Count; i++)
            {
                SKACERO.ActiveRow.ActiveCell cell = new SKACERO.ActiveRow.ActiveCell(aRow, String.Empty);
                cell.Name = GetCellKey(aRow.Name,this.activeGrid1.Columns[i].Name);
                cell.DecimalValue = Decimal.Zero;
                cell.PreTextFont = new Font("Arial", cell.Font.Size,FontStyle.Bold);
                cell.PostTextFont = new Font("Arial", cell.Font.Size, FontStyle.Bold);
                Font font = cell.Font;
                cell.Font = new Font(font.Name, font.Size, FontStyle.Bold);
                //cell.BackColor = Color.Blue;                
                //cell.FlashFont = new Font(cell.Font.FontFamily,cell.Font.Size,FontStyle.Bold);
                //cell.Fla
                cell.Format = "+0;-0; ";
                cell.Text = "";
                aRow.SubItems.Add(cell);
            }
            if (! string.IsNullOrEmpty(sortString) )
                aRow.SubItems[GetCellKey(aRow.Name, "SortKey")].Text = string.Format("{0}",sortString);
            else if (string.IsNullOrEmpty(rowBaseName))
                aRow.SubItems[GetCellKey(aRow.Name,"SortKey")].Text = string.Format("0{0}", aRow.Name);
            else
                aRow.SubItems[GetCellKey(aRow.Name, "SortKey")].Text = string.Format("{0}0",rowBaseName);

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
            if (IsSuppressRepetativeExchangeProductLabels)
            {
                List<string> members;
                if (m_ExchangeGroups.TryGetValue(rowBaseName,out members) )
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
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //
        //
        // ****                   ShutDown()                       ****
        //
        /// <summary>
        /// Called when the form is about to close to release resources nicely.
        /// </summary>
        private void ShutDown()
        {
            if (!m_IsShuttingDown)
            {
                m_IsShuttingDown = true;
                if (m_TTServices != null)
                {
                    m_TTServices.ServiceStateChanged -= new EventHandler(TTServices_ServiceStateChanged);
                }
                if (m_FillHub != null)
                {
                    m_FillHub.PositionBookCreated -= new EventHandler(FillHub_PositionBookCreated);
                    m_FillHub.PositionBookChanged -= new EventHandler(FillHub_PositionBookChanged);
                    //m_FillHub.PositionBookPnLChanged -= new EventHandler(FillHub_PositionBookPnLChanged);
                }
                // Try to disable activeGrid
                if (activeGrid1 != null && activeGrid1.IsDisposed == false)
                {
                    activeGrid1.AllowFlashing = false;
                    activeGrid1.SuspendLayout();
                }

                if (m_TalkerHub != null)
                {
                    m_TalkerHub.ServiceStateChanged -= new EventHandler(TalkerHub_ServiceStateChanged);
                    m_TalkerHub.Request(TalkerHubRequest.StopService);                 
                }

                this.Log = null;
                if (m_FillHub != null)
                {
                    m_FillHub.RequestStop();
                    m_FillHub = null;
                }
                if (m_MarketHub != null)
                {
                    m_MarketHub.RequestStop();
                    m_MarketHub = null;
                }
                if (m_TTServices != null)
                {
                    m_TTServices.Dispose();
                    m_TTServices = null;
                }

            }
            //System.Threading.Thread.Sleep(10);
        }//Shutdown().
        //
        //
        // *********************************************************
        // ****                 Timer_Tick()                    **** 
        // *********************************************************
        private void Timer_Tick(object sender, EventArgs eventArgs)
        {
            if (m_IsShuttingDown)
            {
                m_Timer.Stop();
                return;
            }
            if (!m_AllInstrumentsFoundInMarket)                     // Look for unknown instruments.
                FindInstrumentInfo();

            UpdateInstrumentMarketsAndPnL();

            if (DateTime.Now.CompareTo(m_FinalizeNextTime) > 0)
            {
                FinalizeSession();
                m_FinalizeNextTime = m_FinalizeNextTime.AddDays(1.0);
            }

        }// Timer_Tick()
        //
        //
        //
        private void FinalizeSession()
        {
            m_FillHub.Request(new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestRealPnLReset));
            m_FillHub.Request(new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestDropCopyArchive));
        }
        //
        //
        private void FindInstrumentInfo()
        {
            bool allFoundNow = true;
            TradingTechnologies.TTAPI.InstrumentDetails details;
            foreach (string name in m_InstrumentInfos.Keys)
            {
                InstrumentRowData info = m_InstrumentInfos[name];
                InstrumentName instrumentName = m_InstrumentNames[name];
                allFoundNow = allFoundNow && info.IsFoundInMarket;
                if (!info.IsFoundInMarket && m_MarketHub.TryLookupInstrumentDetails(instrumentName, out details))
                {   // Found details for this instrument, update things.
                    info.IsFoundInMarket = true;
                    m_MarketHub.RequestInstrumentSubscription(instrumentName);                  // request market subscriptions for this instrument.
                    double x = details.Currency.GetConversionRate(TradingTechnologies.TTAPI.Currency.PrimaryCurrency);
                    if ( double.IsNaN(x) || double.IsInfinity(x) )
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
            m_AllInstrumentsFoundInMarket = allFoundNow;
        }
        //
        //
        //
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
                        if (m_MarketHub.TryLookupInstrumentID(m_InstrumentNames[instrName], out id))
                        {
                            try
                            {
                                marketInstrument = aBook.Instruments[id];
                                info.MarketID = id;
                            }
                            catch (Exception)
                            {
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
                            info.IsPriceChanged = true;
                        }

                    }
                }
                m_MarketHub.ExitReadBook(aBook);
            }
            // Update PnL
            //
            List<string> prodKeys = new List<string>();
            foreach (string instrName in m_InstrumentInfos.Keys)
            {
                InstrumentRowData info = m_InstrumentInfos[instrName];
                if (info.IsPriceChanged)
                {
                    InstrumentName instrumentName = m_InstrumentNames[instrName];
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
                        info.IsPriceChanged = false;                    // unset this flag since we will write
                    }

                    ActiveGridUpateInstrumentPnL(instrumentName);
                }
            }//next instrName
            foreach (string prodName in prodKeys)
                ActiveGridUpdateGroup(prodName);
            ActiveGridUpdateTotals();
        }
        //
        #endregion//Private Methods


        #region External Service Event Handlers
        // *****************************************************************
        // ****                Form Event Handlers                     ****
        // *****************************************************************
        //
        //
        //
        //
        // *************************************************************
        // ****         TTServices_ServiceStateChanged()            ****
        // *************************************************************
        private void TTServices_ServiceStateChanged(object sender, EventArgs eventArgs)
        {
            if (m_IsShuttingDown) return;
            if (eventArgs is TTServices.TTApiService.ServiceStatusChangeEventArgs)
            {
                TTServices.TTApiService.ServiceStatusChangeEventArgs e = (TTServices.TTApiService.ServiceStatusChangeEventArgs)eventArgs;
                if (e.IsConnected)
                {
                    if (!m_ServiceConnectionRequested)
                    {
                        m_ServiceConnectionRequested = true;                // ensure we only try to connect once, even if service state changes multiple times.
                        m_MarketHub.Connect();
                        m_FillHub.Connect();
                    }
                }
                else
                {
                    Log.NewEntry(Misty.Lib.Hubs.LogLevel.Major, "TTServices disconnected.");
                    if (m_TTServices != null)
                    {
                        m_TTServices.Dispose();
                        m_TTServices = null;
                    }
                    
                }
            }
        }// TTServices_ServiceStateChanged()
        //
        private bool m_ServiceConnectionRequested = false;                      // ensures we only start up once.
        //
        //
        // *********************************************************************
        // ****             TalkerHub Server State Changed                  ****
        // *********************************************************************
        private void TalkerHub_ServiceStateChanged(object sender, EventArgs eventArgs)
        {
            if (m_IsShuttingDown) return;
            Log.NewEntry(LogLevel.Minor, "Viewer: Talker hub state change {0}", eventArgs);
            if (m_TalkerHub.IsConnectedToClient)
                SetCheck(toolStripMenuItemConnectToExcel, true);
            else 
                SetCheck(toolStripMenuItemConnectToExcel, false);
        }
        //
        private void SetCheck(ToolStripMenuItem menuItem, bool isCheck)
        {
            if (m_IsShuttingDown) return;
            if (this.InvokeRequired)
                this.Invoke(new Action<ToolStripMenuItem>((c) => c.Checked = isCheck), menuItem);
            else
                menuItem.Checked = isCheck;
        }
        private void SetText(Control control, string text)
        {
            if (m_IsShuttingDown) return;
            if (control.InvokeRequired)
                this.Invoke(new Action<Control>((c) => c.Text = text), control);
            else
                control.Text = text;
        }//SetText()
        //
        bool isTimerStarted = false;
        // *********************************************************************
        // ****             FillHub Position Book Created                   ****
        // *********************************************************************
        private void FillHub_PositionBookCreated(object sender,EventArgs eventArgs)
        {
            if (m_IsShuttingDown) return;           
            if (this.InvokeRequired)
                this.Invoke(new EventHandler(FillHub_PositionBookCreated), new object[] { sender, eventArgs });
            else
            {   // This is the GUI thread.
                //Log.NewEntry(LogLevel.Minor, "Viewer: Position book created {0}", eventArgs);
                ActiveGridAddNewRow((FillHub.PositionBookChangedEventArgs)eventArgs);
                if (!isTimerStarted)
                {
                    m_Timer.Start();
                    isTimerStarted = true;
                }
            }
        }
        // *********************************************************************
        // ****             FillHub Position Book Changed                   ****
        // *********************************************************************
        private void FillHub_PositionBookChanged(object sender,EventArgs eventArgs)
        {
            if (m_IsShuttingDown) return;
            if (this.InvokeRequired)
                this.Invoke(new EventHandler(FillHub_PositionBookChanged), new object[] { sender, eventArgs });
            else
            {   // This is the GUI thread.                
                ActiveGridUpdatePosition((FillHub.PositionBookChangedEventArgs)eventArgs);

            }
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
            {   // This is the GUI thread.                
                ActiveGridUpdatePosition((FillHub.PositionBookChangedEventArgs)eventArgs);
            }
        }
        //
        //
        // *********************************************************************
        // ****             FillHub Fill Rejected                           ****
        // *********************************************************************
        private void FillHub_FillRejected(object sender, EventArgs eventArgs)
        {            
            if (m_IsShuttingDown) return;
            Log.NewEntry(LogLevel.Minor, "Viewer: Fill rejected {0}", eventArgs);
            if (this.InvokeRequired)
                this.Invoke(new EventHandler(FillHub_FillRejected), new object[] { sender, eventArgs });
            else
            {
                if (m_RejectedFillViewer == null || m_RejectedFillViewer.IsDisposed)
                {
                    try
                    {
                        m_RejectedFillViewer = new TTServices.Fills.RejectedFills.FormRejectViewer(m_FillHub);                       
                        m_RejectedFillViewer.Show();
                        m_RejectedFillViewer.FormClosing += new FormClosingEventHandler(ChildForm_Closing);
                        m_RejectedFillViewer.UpdateNow(m_FillHub);
                    }
                    catch (Exception)
                    {
                        Log.NewEntry(LogLevel.Major, "Viewer: Failed to open Rejected fill viewer.");
                    }
                }

            }
        }

        //
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
            else if (formType == typeof(FormFillBookViewer))
            {
                m_FormFillBook.FormClosing -= new FormClosingEventHandler(ChildForm_Closing);
                m_FormFillBook = null;
            }
            else if (formType == typeof(Ambre.TTServices.Fills.RejectedFills.FormRejectViewer))
            {
                m_RejectedFillViewer.FormClosing -= new FormClosingEventHandler(ChildForm_Closing);
                m_RejectedFillViewer = null;
            }
        }//ExternalForm_Closing()
        //
        //
        //
        // *********************************************************************
        // ****                     Request Shutdown                        ****
        // *********************************************************************
        /// <summary>
        /// External thread request an application shutdown.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void RequestShutdown(object sender, EventArgs eventArgs)
        {
            if (this.InvokeRequired)
                this.Invoke(new EventHandler(RequestShutdown), new object[] { sender, eventArgs });
            else
            {
                ShutDown();
                this.Close();
            }
        }
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
        //
        // ****             activeGrid1_ColumnClick()               ****
        //
        private void activeGrid1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            
        }
        //
        // ****             activeGrid1_MouseUp()                   ****
        //
        private void activeGrid1_MouseUp(object sender, MouseEventArgs e)
        {
            string instrNameStr;
            string columnName;
            if (!TryGetRowColumnClicked(e, out instrNameStr, out columnName))
                return;                                                             // failed to find a valid cell that was clicked on.
            Log.NewEntry(LogLevel.Minor, "Viewer: MouseUp on activeGrid, instr={0} col={1}.", instrNameStr,columnName);
            //
            // Analyze click
            //
            //if (m_Instruments.ContainsKey(instrumentName))
            InstrumentName instrumentName;
            if (m_InstrumentNames.TryGetValue(instrNameStr, out instrumentName) )
            {
                if (columnName.Equals("Position", StringComparison.CurrentCultureIgnoreCase))       // user wants to change position.
                {   // Open the form allowing users to add fills by hand.
                    // Create and show the FormAddFills window
                    if (m_FormAddFills == null || m_FormAddFills.IsDisposed)
                    {
                        m_FormAddFills = new FormAddFills(m_MarketHub, m_FillHub);
                        m_FormAddFills.FormClosing += new FormClosingEventHandler(ChildForm_Closing);
                        m_FormAddFills.Show();
                    }
                    if (m_FormAddFills.WindowState == FormWindowState.Minimized)
                        m_FormAddFills.WindowState = FormWindowState.Normal;
                    if (!m_FormAddFills.Visible)
                        m_FormAddFills.Visible = true;
                    m_FormAddFills.Focus();
                    //InstrumentBase instrument = null;
                    //if (m_MarketHub.TryGetInstrument(instrumentName, out instrument))
                    //    m_FormAddFills.SetInstrument(m_FillHub, instrument);
                    m_FormAddFills.SetInstrument(m_FillHub, instrumentName);
                }
            }
            else
            {
                Log.NewEntry(Misty.Lib.Hubs.LogLevel.Minor, "Clicked row name {0} not associated with instrument.", instrNameStr);                
            }
        }
        //
        //
        //     
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
        private void activeGrid1_MouseDown(object sender, MouseEventArgs e)
        {
            string rowName;
            string colName;
            if ( TryGetRowColumnClicked(e,out rowName,out colName) )
            {   
                if (e.Button == System.Windows.Forms.MouseButtons.Left && colName.Equals(ColumnName_Alias) )
                {
                    InstrumentName instrumentName;
                    TradingTechnologies.TTAPI.InstrumentDetails details;
                    if (m_InstrumentNames.TryGetValue(rowName, out instrumentName) && m_MarketHub.TryLookupInstrumentDetails(instrumentName,out details) )
                        activeGrid1.DoDragDrop(details.Key.ToDataObject(), DragDropEffects.Copy);
                }
            }
        }
        //
        //
        //
        //
        //
        // ****                 Form1_DragDrop()                ****
        /// <summary>
        /// Allows user to drag a instrument from any TT window and drop it onto this form.
        /// We then submit a zero-qty fill into the FillHub, which in turn will collect all 
        /// the necessary information about the instrument, and ultimately inform this form 
        /// to add the contract.
        /// </summary>
        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.HasInstrumentKeys())
            {
                foreach (TradingTechnologies.TTAPI.InstrumentKey key in e.Data.GetInstrumentKeys())                       // Loop thru each instr dropped.
                {
                    Log.NewEntry(Misty.Lib.Hubs.LogLevel.Major, "Viewer: Detected dropped instrument {0}.", key.ToString());
                    // Determine if this is a new (unknown) instrument. It is unknown if:
                    // 1) MarketHub doesn't recognize the key.
                    // 2) MarketHub knows the key, but we have no position book yet for this key.
                    InstrumentName instrumentName;
                    if (! m_MarketHub.TryLookupInstrument(key,out instrumentName) || ! m_InstrumentNames.ContainsValue(instrumentName) )
                    {   // To create a fill book for this instrument, pretend we were filled with a zero qty.
                        Misty.Lib.OrderHubs.Fill aFill = Misty.Lib.OrderHubs.Fill.Create();
                        aFill.Price = 0.0;
                        aFill.Qty = 0;
                        aFill.LocalTime = Log.GetTime();
                        FillEventArgs fillEventArgs = new FillEventArgs(key,FillType.UserAdjustment,aFill);
                        m_FillHub.HubEventEnqueue(fillEventArgs);                        
                    }
                }//next instrumentKey
            }
        }
        //
        // ****                 Form1_DragOver()                ****
        /// <summary>
        /// Show dragover effects to let user know we will respond to his drop.
        /// </summary>
        private void Form1_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.HasInstrumentKeys())
                e.Effect = DragDropEffects.Copy;
        }
        //
        //
        //
        // ****                 Menu Click()                    ****
        //
        private void Menu_Click(object sender, EventArgs eventArgs)
        {
            if (sender is ToolStripItem)
            {
                ToolStripItem tool = (ToolStripItem) sender;
                Log.NewEntry(LogLevel.Minor, "Viewer: Menu Click {0}",tool.Name);
                if (tool == menuResetPnL)
                    m_FillHub.Request(new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestRealPnLReset));
                else if (tool == menuDropFile)
                    m_FillHub.Request(new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestDropCopyArchive));
                else if (tool == menuFinalizeSession)
                    this.FinalizeSession();
                else if (tool == exitToolStripMenuItem)
                {
                    ShutDown();
                    this.Close();
                }
                else if (tool == toolStripMenuItemConnectToExcel)
                {
                    if (!m_TalkerHub.IsConnectedToClient)
                        m_TalkerHub.Request(TalkerHubRequest.AmberXLConnect);
                    else
                        m_TalkerHub.Request(TalkerHubRequest.AmbreXLDisconnect);
                }
                else if (tool == fillCatalogToolStripMenuItem)
                {
                    if (m_FormFillBook == null || m_FormFillBook.IsDisposed)
                    {
                        m_FormFillBook = new FormFillBookViewer(m_MarketHub, m_FillHub);
                        m_FormFillBook.Show();
                        m_FormFillBook.FormClosing += new FormClosingEventHandler(ChildForm_Closing);
                    }
                }
                else if (tool == showMarketLogToolStripMenuItem)
                    m_MarketHub.Log.IsViewActive = true;
                else if (tool == menuItemWindowsShowLog)
                    m_FillHub.Log.IsViewActive = true;
                else if (tool == menuRefresh)
                {
                    foreach (InstrumentRowData info in m_InstrumentInfos.Values)
                    {
                        info.IsPriceChanged = true;
                    }
                }
                else if (tool == menuFillsRejected)
                {
                    if (m_RejectedFillViewer == null)
                    {
                        m_RejectedFillViewer = new TTServices.Fills.RejectedFills.FormRejectViewer(m_FillHub);
                        m_RejectedFillViewer.Show();
                        m_RejectedFillViewer.FormClosing += new FormClosingEventHandler(ChildForm_Closing);
                        m_RejectedFillViewer.UpdateNow(m_FillHub);
                    }
                    m_RejectedFillViewer.Focus();
                }
                else
                    Log.NewEntry(LogLevel.Warning, "Viewer: Unknown menu clicked {0}.", tool.Name);
            }// if ToolStripItem
        }
        private void Form_Click(object sender, EventArgs e)
        {
        }
        /*
        // Capture window events
        private const long wm_NclButtonDown = 0x00A1;
        private const long wm_NclButtonUp = 0x00A0;
        private const long wm_NclButtonDblClk = 0x00A3;
        private const long wm_Close = 0x0010;
        protected override void DefWndProc(ref Message m)
        {
            if (m.Msg == wm_NclButtonDblClk)
            {
                Size aSize = GetFullSize();
                this.ClientSize = aSize;
                
            }
            else            
                base.DefWndProc(ref m);
        }
        public Size GetFullSize()
        {
            int width = 20;
            int height = 20;
            foreach (ActiveColumnHeader column in activeGrid1.Columns)
                width += column.Width;
            //foreach (ActiveRow row in activeGrid1.Items)
            //    height += row.Bounds.Height;
            height += (activeGrid1.Items[0].Bounds.Height * activeGrid1.Items.Count + 5);
            return new Size(width, height);
        }
        */
        //
        //
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!m_IsShuttingDown)
            {
                if ( Log!=null) 
                    Log.NewEntry(LogLevel.Minor, "Viewer: Form closing.");
                ShutDown();
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
        }
        //
        //
        #endregion//Event Handlers


    }
}
