using System;
using System.Collections.Generic;
//using System.ComponentModel;
//using System.Drawing;
//using System.Data;
//using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
//using System.Windows.Threading; // WindowsBase assembly!
using System.Windows.Forms;
using Ambre.Breconcile.Reconciler;
using Misty.Lib.Application;

namespace Ambre.Breconcile.BookReaders
{
    using Misty.Lib.OrderHubs;                  // Fills
    using Misty.Lib.Products;                   // InstrumentName
    using Misty.Lib.IO.Xml;                     // Nodes.

    /// <summary>
    /// Gui for displaying a detailed list of an EventSeries.  Allows user to examine trades for a SINGLE account.
    /// These are contained in a serial list of non-overlapping files.
    /// </summary>
    public partial class EventSeriesView : UserControl
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // Data variables
        private readonly string m_BasePath = string.Empty;
        private readonly string m_FileNamePattern = string.Empty;
        private EventPlayer m_EventPlayer = null;
        private AppInfo m_AppInfo = null;
        DateTime m_SettlementDate = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified);
        ProductSettlementTable m_ProductSettlementTable = null;

        //
        //
        //
        //        
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************


        public EventSeriesView()
        { 
            InitializeComponent();       
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="basePath">Directory with date-named subdirectories; @"\\fileserver\Users\dv_bre\Ambre\Drops\".</param>
        public EventSeriesView(string basePath, string userAccountFileNamePattern, DateTime startTime)
        {
            InitializeComponent();
            m_BasePath = basePath;
            m_FileNamePattern = userAccountFileNamePattern;

            /// *********************************************
            /// ***     get settlement time table here    ***
            /// *********************************************
            this.textMessageBox.Text = "";
            textMessageBox.Visible = false;

            m_AppInfo = AppInfo.GetInstance("Breconcile", true);
            string settlementTable = string.Format("{0}ProductSettleTimes.txt", m_AppInfo.UserPath);
            ProductSettlementTable.TryCreate(settlementTable, out m_ProductSettlementTable, m_SettlementDate);
            /// settlement table got, stored at m_ProductSettlementTable

            if (EventPlayer.TryCreate(m_BasePath, m_FileNamePattern, out m_EventPlayer))
            {
                m_EventPlayer.TaskStarted += new EventHandler(EventPlayer_TaskStarted);
                m_EventPlayer.TaskCompleted += new EventHandler(EventPlayer_TaskCompleted);

                //progressBar.Visible = true;
                //progressBar.MarqueeAnimationSpeed = 30;
            }
        }
        public void BeginLoad(DateTime startTime, double hours)
        {
            m_EventPlayer.BeginLoad(startTime, hours);
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
        //
        // ****             Update InstrumentName List              ****
        /// <summary>
        /// Whenever new events have been read by EventPlayer, call this
        /// to update the list of InstrumentNames.
        /// Called by UI thread.
        /// </summary>
        /// <param name="player"></param>
        private void UpdateInstrumentNameList(EventPlayer player)
        {
            object o = comboBoxInstrumentNames.SelectedItem;            // originally selected item

            // Add any new names to the combo box.
            List<InstrumentName> nameList = new List<InstrumentName>(player.SeriesList.Keys);
            nameList.Sort(new InstrumentNameOrder());
            foreach (InstrumentName name in nameList)
                if (!comboBoxInstrumentNames.Items.Contains(name))
                    comboBoxInstrumentNames.Items.Add(name);
            // Keep the user's selected instrument selected.
            if (o != null)
                comboBoxInstrumentNames.SelectedItem = o;               // reset the selected instrument.
            else if (comboBoxInstrumentNames.Items.Count > 0)
                comboBoxInstrumentNames.SelectedIndex = 0;               // select the first instrument.

            // Update the Start and End dates
            textStartDate.Text = string.Format("{0:ddd dd MMM yyyy}", player.StartDate);
            textStartTime.Text = string.Format("{0:hh:mm:ss tt}", player.StartDate);
            textEndDate.Text = string.Format("{0:ddd dd MMM yyyy}", player.EndDate);
            textEndTime.Text = string.Format("{0:hh:mm:ss tt}", player.EndDate);
        }// UpdateInstrumentNameList()
        //
        //
        //
        //
        // ****             Update Event List               ****
        //
        private void UpdateEventList()
        {
            if (comboBoxInstrumentNames.SelectedItem == null)
                return;
            InstrumentName name = (InstrumentName)comboBoxInstrumentNames.SelectedItem; // Determine selected instrument.
            DateTime selectedDateTime = DateTime.MinValue;                              // DateTime that is currently selected...
            object selectedItem = listBoxFillEvents.SelectedItem;                       // Determine selected item.
            if (selectedItem != null)
            {
                string s1 = selectedItem.ToString();
                DateTime.TryParse(s1.Substring(0, s1.IndexOf('\t', 0)), out selectedDateTime);
            }
            // Update the listbox.
            listBoxFillEvents.SuspendLayout();
            listBoxFillEvents.Items.Clear();

            EventSeries series = m_EventPlayer.SeriesList[name];
            this.textBoxInitialState.Text = string.Format(FillFormat, series.InitialState.LocalTime.ToString(Misty.Lib.Utilities.Strings.FormatDateTimeFull), series.InitialState);
            
            int selectedIndex = 0;            
            for (int i = 0; i < series.Series.Count; ++i)
            {
                Fill fill = series.Series[i];
                string s = string.Format(FillFormat, fill.LocalTime.ToString(Misty.Lib.Utilities.Strings.FormatDateTimeFull), fill);
                listBoxFillEvents.Items.Add(s);
                //m_SelectedPosQty += fill.Qty;
                if (fill.LocalTime.CompareTo(selectedDateTime) >= 0)
                    selectedIndex = i;
            }
            listBoxFillEvents.ResumeLayout();

            // Select a fill.
            if (selectedIndex < listBoxFillEvents.Items.Count)
                listBoxFillEvents.SelectedIndex = selectedIndex;
        }
        private const string FillFormat = "{0} \t{1}";
        //
        //
        //
        //
        // **********************************************************
        // ****             Update Selected Event()             *****
        // **********************************************************
        //
        private void UpdateSelectedEvent()
        {
            // Validate
            if (comboBoxInstrumentNames.SelectedItem == null)
                return;
            InstrumentName name = (InstrumentName)comboBoxInstrumentNames.SelectedItem; // Determine selected instrument.
            int selectedIndex = listBoxFillEvents.SelectedIndex;                        // Selected event index
            if ( selectedIndex < 0 )
            {
                if (m_EventPlayer.SeriesList[name].InitialState == null)
                {
                    this.textSelectedDate.Text = " ";
                    this.textSelectedTime.Text = " ";
                    this.textSelectedEXTime.Text = " ";
                    this.textSettletime.Text = " ";
                    this.textSelectedFill.Text = " ";
                    this.textSelectedPosition.Text = " ";
                }
                else
                {
                    this.textSelectedDate.Text = string.Format("{0:ddd dd MMM yyyy}", m_EventPlayer.SeriesList[name].InitialState.LocalTime);
                    this.textSelectedTime.Text = string.Format("{0:hh:mm:ss.fff tt}", m_EventPlayer.SeriesList[name].InitialState.LocalTime);
                    this.textSelectedEXTime.Text = string.Format("{0:hh:mm:ss.fff tt}", m_EventPlayer.SeriesList[name].InitialState.LocalTime);
                    this.textSettletime.Text = "";
                    this.textSelectedFill.Text = " ";
                    this.textSelectedPosition.Text = string.Format("{0}", m_EventPlayer.SeriesList[name].InitialState);
                }
                return;
            }
            else if (selectedIndex >= m_EventPlayer.SeriesList[name].Series.Count)
                listBoxFillEvents.SelectedIndex = m_EventPlayer.SeriesList[name].Series.Count - 1;

            EventSeries series = m_EventPlayer.SeriesList[name];
            int m_SelectedPosQty = 0;
            double m_SelectedPosCost = 0;
            m_SelectedPosQty = series.InitialState.Qty;
            for (int i = 0; i <= selectedIndex; ++i)
            {
                m_SelectedPosQty += series.Series[i].Qty;
            }

            // Fill
            Fill selectedFill = series.Series[selectedIndex];
            this.textSelectedDate.Text = string.Format("{0:ddd dd MMM yyyy}", selectedFill.LocalTime);
            
            // prepare settlement information
            ProductSettlementEntry SettlementEntry;
            m_ProductSettlementTable.TryFindMatchingEntry(series.Name, out SettlementEntry);
            DateTime m_SettleDate = m_SettlementDate.Add(SettlementEntry.SettleTime);
            DateTime settleTimeLocal = TimeZoneInfo.ConvertTime(m_SettleDate, SettlementEntry.TZInfo, TimeZoneInfo.Local);
            this.textSettletime.Text = string.Format("{0:hh:mm:ss.fff tt}", settleTimeLocal);
            this.EXSettle.Text = SettlementEntry.SettleTime.ToString();
            // settlement information ends here
            this.textSelectedTime.Text = string.Format("{0:hh:mm:ss.fff tt}", selectedFill.LocalTime);
            this.textSelectedEXTime.Text = string.Format("{0:hh:mm:ss.fff tt}", selectedFill.ExchangeTime);
            this.textSelectedFill.Text = string.Format("{0} @ {1}", selectedFill.Qty, selectedFill.Price);
            this.textSelectedPosition.Text = string.Format("{0} @ {1}", m_SelectedPosQty, m_SelectedPosCost);
        }
        //

        //
        //
        //
        //
        private class InstrumentNameOrder : IComparer<InstrumentName>
        {
            public int Compare(InstrumentName nameA, InstrumentName nameB)
            {
                int compare = nameA.Product.Type.CompareTo(nameB.Product.Type);
                if (compare == 0)
                {
                    compare = nameA.Product.Exchange.CompareTo(nameB.Product.Exchange);
                    if (compare == 0)
                    {
                        compare = nameA.Product.ProductName.CompareTo(nameB.Product.ProductName);
                        if (compare == 0)
                            return nameA.SeriesName.CompareTo(nameB.SeriesName);
                    }
                    else
                        return compare;
                }
                return compare;
            }
        }
        //
        //
        #endregion//Private Methods


        #region no External Event Handlers
        // *****************************************************************
        // ****            External Event Handlers                     ****
        // *****************************************************************
        
        //
        private void EventPlayer_TaskStarted(object sender, EventArgs eventArgs)
        {
            if (this.InvokeRequired)
                this.Invoke(new EventHandler(EventPlayer_TaskStarted), new object[] { sender, eventArgs });
            else
            {
                progressBar.Visible = true;
                progressBar.MarqueeAnimationSpeed = 30;
                textMessageBox.Visible = false;
            }
        }
        //
        private void EventPlayer_TaskCompleted(object sender, EventArgs eventArgs)
        {
            if (this.InvokeRequired)
                this.Invoke(new EventHandler(EventPlayer_TaskCompleted), new object[] { sender, eventArgs });
            else
            {
                UpdateInstrumentNameList(m_EventPlayer);
                UpdateEventList();
                progressBar.MarqueeAnimationSpeed = 0;
                progressBar.Visible = false;
                textMessageBox.Visible = true;
            }
        }
        //
        #endregion//Event Handlers


        #region Form Event Handlers
        // *****************************************************************
        // ****                 Form Event Handlers                     ****
        // *****************************************************************
        //
        private void Control_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sender.Equals(listBoxFillEvents))
            {
                UpdateSelectedEvent();
            }
            else if (sender.Equals(comboBoxInstrumentNames))
            {
                UpdateEventList();
            }
            else if (sender.Equals(textBoxInitialState))
            {
                listBoxFillEvents.SelectedIndex = -1;   // deselect all fills when user clicks initial state.
                UpdateSelectedEvent();
            }
        }
        //
        //
        // ****             Button_Clicked              ****
        //
        /// <summary>
        /// Generic event handler for Button clicks.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Clicked(object sender, EventArgs e)
        {
            if (sender.Equals(buttonLoadEarlierFile))
            {
                DateTime dt = m_EventPlayer.StartDate.AddDays(-0.5);
                m_EventPlayer.BeginTryInsert(dt);
            }
            else if (sender.Equals(buttonLoadLaterFile))
            {
                DateTime dt = m_EventPlayer.StartDate.AddDays(0.5);
                m_EventPlayer.BeginTryInsert(dt);
            }
            else if (sender.Equals(buttonLoadEarlierEnd))
            {
                DateTime dt = m_EventPlayer.EndDate.AddDays(-0.5);
                m_EventPlayer.BeginTryAppend(dt);
            }
            else if(sender.Equals(buttonLoadLaterEnd))
            {
                DateTime dt = m_EventPlayer.EndDate.AddDays(0.5);
                m_EventPlayer.BeginTryAppend(dt);
            }
        }

        //
        #endregion//Event Handlers

    }
}
