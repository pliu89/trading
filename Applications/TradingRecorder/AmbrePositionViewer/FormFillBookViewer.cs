using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Ambre.PositionViewer
{
    using Misty.Lib.Hubs;
    using Ambre.TTServices.Markets;
    using Ambre.TTServices.Fills;

    using TradingTechnologies.TTAPI;

    public partial class FormFillBookViewer : Form
    {
       


        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        //
        private MarketTTAPI m_Market = null;
        private FillHub m_FillHub = null;
        private LogHub Log = null;

        // internal variables
        private List<Misty.Lib.OrderHubs.Fill> m_Fills = new List<Misty.Lib.OrderHubs.Fill>();
        private List<string> m_PropertyNames = new List<string>();          // column names
        private List<string> m_PropertyFormat = new List<string>();         // column formats
        //
        //
        //
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public FormFillBookViewer(Ambre.TTServices.Markets.MarketTTAPI priceHub, FillHub aFillHub)
        {
            InitializeComponent();
            Log = aFillHub.Log;
            m_FillHub = aFillHub;
            m_Market = priceHub;

            UpdateNewFills();
           

        }// FormFillBookViewer()
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
        public void UpdateNewFills()
        {

            //bool needUpdateColumns = false;
            /*   // TODO: USe new FillBooks to show fills!
            if (!m_FillHub.Fills.IsEmpty)
            {
                int nPrevColumns = m_PropertyNames.Count;
                m_Fills.Clear();
                m_Fills.AddRange(m_FillHub.Fills.ToArray());
                for (int i=0; i<m_Fills.Count; ++i)
                    CreatePropertyInventory(m_Fills[i]);
                if (nPrevColumns != m_PropertyNames.Count)
                    needUpdateColumns = true;
            }
            if ( needUpdateColumns )
                UpdateColumns();
            AddRows();
            */ 
        }
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
        private void CreatePropertyInventory(Misty.Lib.OrderHubs.Fill fill)
        {
            FindNewProperties(fill);             // Automatically collect all Property names.  
            //if (fill.FillDetails != null)
            //    FindNewProperties(fill.FillDetails);
        }// CreatePropertyInventory()
        //
        private void FindNewProperties(object o)
        {
            Type type = o.GetType();
            PropertyInfo[] propertyInfo = type.GetProperties();
            Log.BeginEntry(LogLevel.Minor, "FillBookView: Found columns : ");
            foreach (PropertyInfo info in propertyInfo)
            {
                bool isGood = true;
                isGood = isGood && info.CanRead;
                Type propType = info.PropertyType;
                isGood = isGood && (! propType.IsClass );

                if (isGood && ! m_PropertyNames.Contains(info.Name))
                {
                    m_PropertyNames.Add(info.Name);
                    if (propType == typeof(DateTime))
                        m_PropertyFormat.Add("{0:dd-mm-yyyy hh:MM:ss}");
                    else
                        m_PropertyFormat.Add("{0}");
                    Log.AppendEntry("{0}({1}) ", info.Name,propType.ToString());
                }
            }
            Log.EndEntry();
        } //FindNewProperties()       
        //
        //
        //
        //
        private void AddRows()
        {
            // Assume that we add all Fills one for each row, in order of their listing.
            int rowID = activeGrid1.Items.Count;
            this.activeGrid1.SuspendLayout();
            while (rowID < m_Fills.Count)
            {
                // Create a new row                
                SKACERO.ActiveRow aRow = new SKACERO.ActiveRow();
                aRow.Name = rowID.ToString();
                aRow.Text = rowID.ToString();
                for (int i = 1; i < this.activeGrid1.Columns.Count; i++)
                {
                    SKACERO.ActiveRow.ActiveCell cell = new SKACERO.ActiveRow.ActiveCell(aRow, String.Empty);
                    cell.Name = String.Format("{0}_{1}", rowID, this.activeGrid1.Columns[i].Name);
                    cell.DecimalValue = Decimal.Zero;
                    cell.PreTextFont = new Font("Arial", cell.Font.Size);
                    cell.PostTextFont = new Font("Arial", cell.Font.Size);
                    aRow.SubItems.Add(cell);
                }
                //
                // Update values
                //
                for (int i=0; i<m_PropertyNames.Count;++i)
                {
                    string propertyName = m_PropertyNames[i];                                        
                    object o = m_Fills[rowID];
                    PropertyInfo propertyInfo = m_Fills[rowID].GetType().GetProperty(propertyName);
                    //if (propertyInfo == null && m_Fills[rowID].FillDetails!=null)
                    //{
                    //    o = m_Fills[rowID].FillDetails;
                    //    propertyInfo = o.GetType().GetProperty(propertyName);           // Property may belong to FillDetails.
                    //}
                    if (o!=null && propertyInfo != null)
                    {
                        object value = propertyInfo.GetValue(o);
                        string cellName = String.Format("{0}_{1}", rowID,propertyName);
                        SKACERO.ActiveRow.ActiveCell cell = aRow.SubItems[cellName];
                        cell.Text = string.Format(m_PropertyFormat[i], value);
                    }
                }

                //
                this.activeGrid1.Items.Add(aRow);
                rowID++;
            }                        
            this.activeGrid1.ResumeLayout();
        }//
        //
        //
        private void UpdateColumns()
        {
            this.activeGrid1.SuspendLayout();
            SKACERO.ActiveColumnHeader column;
            string columnName;

            if (!activeGrid1.Items.ContainsKey("Row"))
            {
                columnName = "Row";
                column = new SKACERO.ActiveColumnHeader();
                column.Width = 60;
                column.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
                column.Text = columnName;
                column.Name = columnName;
                column.CellHorizontalAlignment = System.Drawing.StringAlignment.Far;
                column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
                column.SortOrder = SKACERO.SortOrderEnum.Unsorted;
                this.activeGrid1.Columns.Add(column);
            }
            for (int i=0; i<m_PropertyNames.Count; ++i)
            {
                columnName = m_PropertyNames[i];               
                if (!activeGrid1.Items.ContainsKey(columnName))
                {
                    column = new SKACERO.ActiveColumnHeader();
                    column.Width = 40;
                    if (m_PropertyFormat[i].Contains("MM"))
                        column.Width = 120;
                    column.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
                    column.Text = columnName;
                    column.Name = columnName;
                    column.DisplayZeroValues = false;
                    //column.CellFormat = m_PropertyFormat[i];
                    column.CellHorizontalAlignment = System.Drawing.StringAlignment.Far;
                    column.CellVerticalAlignment = System.Drawing.StringAlignment.Center;
                    column.SortOrder = SKACERO.SortOrderEnum.Unsorted;
                    this.activeGrid1.Columns.Add(column);
                }
            }

            //
            // Other properties
            //
            this.activeGrid1.FlashDuration = 100;       // msecs
            this.activeGrid1.AllowFlashing = false;
            this.activeGrid1.UseFlashFadeOut = false;
            this.activeGrid1.UseAlternateRowColors = true;

            // Force repainting.
            //this.activeGrid1.ListViewItemSorter = new ListViewItemComparer(activeGrid1.Columns["SortKey"].Index);

            this.activeGrid1.ResumeLayout();
            this.activeGrid1.Invalidate();            
        }//UpdateActiveGridColumns
        //
        //
        //
        #endregion//Private Methods


        #region Form Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        private void button1_Click(object sender, EventArgs e)
        {
            UpdateNewFills();
        }
        //
        #endregion//Event Handlers

    }
}
