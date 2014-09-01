using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace AmbreMaintenance
{
    using SKACERO;

    using Misty.Lib.Application;
    using Misty.Lib.Utilities;
    using Misty.Lib.Hubs;
    using InstrumentName = Misty.Lib.Products.InstrumentName;

    using Ambre.TTServices.Markets;
    using Ambre.TTServices.Fills;
    using Ambre.TTServices.Talker;
    using Ambre.TTServices.Fills.FrontEnds;

    public partial class FillHubPage : TabPage
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // External services
        private FillHub m_FillHub = null;
        private LogHub Log = null;

        // Exchange and product groups
        private Dictionary<string, List<string>> m_ExchangeGroups = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> m_ProductGroups = new Dictionary<string, List<string>>();

        // Controls
        private int m_MinMembersForProductGroupRow = 2;                                                                 // number of elements needed before we consider a summary row.
        private int m_MinMembersForExchangeGroupRow = 2;
        private string[] CellNameDeliminaterArray = new string[] { CellNameDelimiter };
        private bool IsSuppressRepetativeExchangeProductLabels = true;
        private bool m_IsShuttingDown = false;

        // Constants
        private const string GroupRowNamePrefix = "Summary";
        private const string CellNameDelimiter = "***";                                                                 // Used to separate Row name and Column Name.
        private const string ColumnName_StartingRealPnL = "StartingRealPnL";
        private const string ColumnName_RealPnL = "RealPnL";                                                            // Labels for columns
        private const string ColumnName_UnrealPnL = "UnrealPnL";
        private const string ColumnName_TotalPnL = "TotalPnL";
        private const string ColumnName_Alias = "Alias";

        // Instrument tables.
        public Dictionary<string, InstrumentRowData> m_InstrumentInfos = new Dictionary<string, InstrumentRowData>();   // instName
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public FillHubPage()
        {
            InitializeComponent();
            UpdateActiveGridColumns();                                                                                  // Create ListView columns.
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
            if (Log == null)
                Log = m_FillHub.Log;        // Use first fillHub's log as my own.

            // Listen to its events
            newFillHub.PositionBookCreated += new EventHandler(FillHub_PositionBookCreated);
            newFillHub.PositionBookDeleted += new EventHandler(FillHub_PositionBookDeleted);
            newFillHub.PositionBookChanged += new EventHandler(FillHub_PositionBookChanged);

            // Rename this page after the fill hub it describes.
            if (string.IsNullOrEmpty(newFillHub.Name))
                this.Text = "Total";
            else
                this.Text = newFillHub.Name;
        }//AddHub()
        //
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

                // Add subscriptions for position book events.
                if (m_FillHub != null)
                {
                    m_FillHub.PositionBookCreated -= new EventHandler(FillHub_PositionBookCreated);
                    m_FillHub.PositionBookDeleted -= new EventHandler(FillHub_PositionBookDeleted);
                    m_FillHub.PositionBookChanged -= new EventHandler(FillHub_PositionBookChanged);
                }

            }
        }//Shutdown()
        #endregion//Public Methods


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
            if (!m_InstrumentInfos.ContainsKey(instrumentName.FullName))
                return;

            // Update info about the position book that changed.           
            InstrumentRowData info = m_InstrumentInfos[instrumentName.FullName];
            IFillBook positionBook;
            if (m_FillHub.TryEnterReadBook(instrumentName, out positionBook))
            {
                info.Position = positionBook.NetPosition;
                info.StartingRealPnL = Math.Round(positionBook.RealizedStartingDollarGains, 2);
                info.RealPnL = Math.Round(positionBook.RealizedDollarGains, 2);
                info.UnrealPnL = Math.Round(positionBook.UnrealizedDollarGains(), 2);
                info.AverageCost = Math.Round(positionBook.AveragePrice, info.MarketPriceDecimals);
                m_FillHub.ExitReadBook(instrumentName);
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
        //
        //
        /// <summary>
        /// Update the cells and flash.
        /// </summary>
        /// <param name="rowName"></param>
        /// <param name="columnName"></param>
        /// <param name="newValue1"></param>
        /// <param name="isFlash"></param>
        private void UpdateActiveGridCell(string rowName, string columnName, object newValue1, bool isFlash)
        {
            string keyCell = GetCellKey(rowName, columnName);
            Decimal newValue;
            if (!Decimal.TryParse(newValue1.ToString(), out newValue))
                return;

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
                if (m_FillHub.TryEnterReadBook(instrumentName, out positionBook))
                {
                    info.StartingRealPnL = Math.Round(positionBook.RealizedStartingDollarGains, 2);
                    info.RealPnL = Math.Round(positionBook.RealizedDollarGains, 2);
                    info.UnrealPnL = Math.Round(positionBook.UnrealizedDollarGains(), 2);
                    m_FillHub.ExitReadBook(instrumentName);
                }

                SKACERO.ActiveRow row = activeGrid1.Items[instrumentName.FullName];
                row.SubItems[GetCellKey(instrumentName.FullName, ColumnName_StartingRealPnL)].Text = info.StartingRealPnL.ToString("0.00");
                row.SubItems[GetCellKey(instrumentName.FullName, ColumnName_RealPnL)].Text = info.RealPnL.ToString("0.00");
                row.SubItems[GetCellKey(instrumentName.FullName, ColumnName_UnrealPnL)].Text = info.UnrealPnL.ToString("0.00");
                row.SubItems[GetCellKey(instrumentName.FullName, "Expiry")].Text = m_InstrumentInfos[instrumentName.FullName].ExpirationDate.ToString("yyyy-MM-dd");
                row.SubItems[GetCellKey(instrumentName.FullName, "Currency")].Text = info.CurrencyCode;
                row.SubItems[GetCellKey(instrumentName.FullName, "SortKey")].Text = string.Format("{0}{1}{2}{3}", instrumentName.Product.Exchange, instrumentName.Product.Type.ToString(), instrumentName.Product.ProductName, m_InstrumentInfos[instrumentName.FullName].ExpirationDate.ToString("yyyyMMdd"));
                activeGrid1.Sort();                                                                 // sort again, now we've changed the SortKey.
                return;
            }

            //
            // Create all tables we need for each new instrument.
            //
            m_InstrumentInfos.Add(instrumentName.FullName, new InstrumentRowData(instrumentName));

            string strKey = instrumentName.Product.Exchange;
            if (!m_ExchangeGroups.ContainsKey(strKey))                                              // Exchange group
                m_ExchangeGroups.Add(strKey, new List<string>());                                   // maintain an entry for each ex
            if (!m_ExchangeGroups[strKey].Contains(instrumentName.FullName))
            {
                m_ExchangeGroups[strKey].Add(instrumentName.FullName);
                if (m_ExchangeGroups[strKey].Count >= m_MinMembersForExchangeGroupRow
                   && (!activeGrid1.Items.ContainsKey(string.Format("{0}{1}", GroupRowNamePrefix, strKey))))
                    CreateSummaryRows(strKey, instrumentName.Product.Exchange, string.Empty);
            }
            strKey = GetProductGroupKey(instrumentName);                                            // Product group: construct the exch+prod group name.
            if (!m_ProductGroups.ContainsKey(strKey))                                               // first time this product group showed up!
                m_ProductGroups.Add(strKey, new List<string>());                                    // create a place for this prod group to hold its membership list.
            if (!m_ProductGroups[strKey].Contains(instrumentName.FullName))                         // If this instrument is not yet part of group, add him.
            {
                m_ProductGroups[strKey].Add(instrumentName.FullName);                               // add new member of group             
                if ((m_ProductGroups[strKey].Count >= m_MinMembersForProductGroupRow)
                    && (!activeGrid1.Items.ContainsKey(string.Format("{0}{1}", GroupRowNamePrefix, strKey))))
                    CreateSummaryRows(strKey, string.Empty, instrumentName.Product.ProductName);    // need to add a new summary line 
            }

            //
            // Create the row.
            //
            SKACERO.ActiveRow aRow = new SKACERO.ActiveRow();
            aRow.Name = instrumentName.FullName;
            aRow.Text = instrumentName.FullName;                                                    // this will appear in the zeroth column of the row.
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

            // Set currency column as empty string.
            aRow.SubItems[GetCellKey(instrumentName.FullName, "Currency")].Text = string.Empty;

            // Add row to our collection
            this.activeGrid1.SuspendLayout();
            try
            {
                if (!this.activeGrid1.Items.ContainsKey(aRow.Name))
                    this.activeGrid1.Items.Add(aRow);
            }
            catch (Exception e)
            {   
                if (Log != null)
                    Log.NewEntry(LogLevel.Major, "ActiveGridAddNewRow: Failed to add new row with name {0}. Execption {1}", aRow.Name, e.Message);
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
                row.Name = string.Format("{0}{1}", row.Name, row.GetHashCode());
                this.activeGrid1.Items.Remove(row);
                this.activeGrid1.EndUpdate();
                ActiveGridUpdateGroup(GetProductGroupKey(instrumentName));
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

            // Add a column for Instrument Names
            column = new SKACERO.ActiveColumnHeader();
            column.Name = "Name";
            column.Width = 0;
            column.Text = "instrument";
            column.CellFormat = "";
            column.CellHorizontalAlignment = System.Drawing.StringAlignment.Far;
            column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
            column.SortOrder = SKACERO.SortOrderEnum.Unsorted;
            this.activeGrid1.Columns.Add(column);

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

            // Currency Name
            column = new SKACERO.ActiveColumnHeader();
            column.Name = "Currency";
            column.Width = 80;
            column.Text = "Currency";
            column.TextAlign = HorizontalAlignment.Right;
            column.CellFormat = "";
            column.CellHorizontalAlignment = System.Drawing.StringAlignment.Far;
            column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
            column.SortOrder = SKACERO.SortOrderEnum.Unsorted;
            this.activeGrid1.Columns.Add(column);

            // Alias - the nice version of the name
            column = new SKACERO.ActiveColumnHeader();
            column.Name = ColumnName_Alias;
            column.Width = 80;
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
            column.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
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
            column.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            column.Text = "ave price";
            column.TextAlign = HorizontalAlignment.Right;
            column.CellHorizontalAlignment = System.Drawing.StringAlignment.Far;
            column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
            column.SortOrder = SKACERO.SortOrderEnum.Unsorted;
            column.DisplayZeroValues = false;
            this.activeGrid1.Columns.Add(column);

            // Starting Real Pnl
            column = new SKACERO.ActiveColumnHeader();
            column.Name = ColumnName_StartingRealPnL;
            column.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
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
            SKACERO.ActiveRow aRow = new SKACERO.ActiveRow();
            aRow.UseItemStyleForSubItems = false;
            if (string.IsNullOrEmpty(rowBaseName))
                aRow.Name = GroupRowNamePrefix;
            else
                aRow.Name = string.Format("{0}{1}", GroupRowNamePrefix, rowBaseName);
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
        #endregion//External Service Event Handlers

    }
}
