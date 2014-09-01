using System;
using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
using System.Drawing;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ambre.Breconcile.BookReaders
{
    using Misty.Lib.OrderHubs;                  // Fills
    using Misty.Lib.Products;                   // InstrumentName
    using Misty.Lib.IO.Xml;                     // Nodes.


    /// <summary>
    /// This form allows us to read FillBook xml files, and reconstruct a fill timeline.
    /// </summary>
    public partial class FormBookViewer : Form
    {


        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        private EventPlayer m_EventPlayer;



        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public FormBookViewer(string[] args)
        {
            InitializeComponent();

            string basePath = @"\\fileserver\Users\dv_bre\Ambre\Drops\";
            string baseFileName = "FillBooks_DVBRE4_82804115";
            DateTime startTime = new DateTime(2013, 6, 11, 16, 30, 0);
            //m_EventPlayer = new EventPlayer(basePath, baseFileName, startTime);
            if (EventPlayer.TryCreate(basePath, baseFileName, out m_EventPlayer))
            {
                m_EventPlayer.Load(startTime);
                UpdateInstrumentNames(m_EventPlayer);
            }

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


        #region no Private Methods
        // *****************************************************************
        // ****                    Private Methods                      ****
        // *****************************************************************
        //
        //
        //
        //
        //
        //
        #endregion//Private Methods


        #region Private Form Update Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        /// <summary>
        /// Check and update list of InstrumentNames.
        /// Must be called by UI thread.
        /// </summary>
        /// <param name="player"></param>
        private void UpdateInstrumentNames(EventPlayer player)
        {
            object o = comboBoxInstrumentNames.SelectedItem;            // originally selected item

            // Add any new names to the combo box - dont check if any are missing...
            List<InstrumentName> nameList = new List<InstrumentName>(player.SeriesList.Keys);
            nameList.Sort(new InstrumentNameOrder());
            foreach (InstrumentName name in nameList)
                if (!comboBoxInstrumentNames.Items.Contains(name))
                    comboBoxInstrumentNames.Items.Add(name);
            //else
            //    Console.WriteLine("Error!");

            if (o != null)
                comboBoxInstrumentNames.SelectedItem = o;               // reset the selected item.

            // Update the Start and End dates
            textStartDate.Text = player.StartDate.ToString(Misty.Lib.Utilities.Strings.FormatDateTimeZone);
            textEndDate.Text = player.EndDate.ToString(Misty.Lib.Utilities.Strings.FormatDateTimeZone);

        }// UpdateInstrumentNames()
        //
        //
        public class InstrumentNameOrder : IComparer<InstrumentName>
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
        //
        //
        //
        private void UpdateEventList()
        {
            if (comboBoxInstrumentNames.SelectedItem == null)
                return;
            InstrumentName name = (InstrumentName)comboBoxInstrumentNames.SelectedItem;

            listBoxFillEvents.SuspendLayout();
            listBoxFillEvents.Items.Clear();

            string s;
            EventSeries series = m_EventPlayer.SeriesList[name];
            for (int i = 0; i < series.Series.Count; ++i)
            {
                Fill fill = series.Series[i];
                s = string.Format("{0} \t{1}", fill.LocalTime.ToString(Misty.Lib.Utilities.Strings.FormatDateTimeZone), fill);
                listBoxFillEvents.Items.Add(s);
            }
            listBoxFillEvents.ResumeLayout();
            // Update the initial book state.
            s = string.Format("{0}", series.InitialState.LocalTime.ToString(Misty.Lib.Utilities.Strings.FormatDateTimeZone));
            textStartingBookDateTime.Text = s;
            s = string.Format("{0}", series.InitialState);
            textStartingBookFill.Text = s;
            // update the final book state.
            s = string.Format("{0}", series.FinalState.LocalTime.ToString(Misty.Lib.Utilities.Strings.FormatDateTimeZone));
            textFinalBookDateTime.Text = s;
            s = string.Format("{0}", series.FinalState);
            textFinalBookFill.Text = s;

        }
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
        // ****             Control_SelectedIndexChanged()              ****
        //
        /// <summary>
        /// General method to handle selection changes from any control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Control_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sender.Equals(listBoxFillEvents))
            {

            }
            else if (sender.Equals(comboBoxInstrumentNames))
            {
                UpdateEventList();

            }

        }
        //
        //
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
            if (sender.Equals(buttonLoadEarlier))
            {
                DateTime dt = m_EventPlayer.StartDate.AddDays(-0.5);
                if (m_EventPlayer.TryInsert(dt))
                {
                    UpdateInstrumentNames(m_EventPlayer);
                    UpdateEventList();
                }
            }
            else if (sender.Equals(buttonLoadLater))
            {
                DateTime dt = m_EventPlayer.EndDate.AddDays(0.5);
                if (m_EventPlayer.TryAppend(dt))
                {
                    UpdateInstrumentNames(m_EventPlayer);
                    UpdateEventList();
                }
            }
        }
        //
        //
        #endregion//Event Handlers

    }
}
