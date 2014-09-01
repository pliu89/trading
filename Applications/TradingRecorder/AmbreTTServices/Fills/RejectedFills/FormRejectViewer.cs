using System;
using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
//using System.Drawing;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ambre.TTServices.Fills.RejectedFills
{

    using Misty.Lib.Utilities;
    using Misty.Lib.Products;

    public partial class FormRejectViewer : Form
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // External services
        private FillHub m_FillHub = null;

        // Local variables
        private List<RejectedFills.RejectedFillEventArgs> m_RejectedFills = new List<RejectedFillEventArgs>();
        private Dictionary<InstrumentName, List<RejectedFillEventArgs>> m_RejectedFills2 = new Dictionary<InstrumentName, List<RejectedFillEventArgs>>(); 
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public FormRejectViewer(FillHub fillHub)
        {
            m_FillHub = fillHub;
            //m_FillHub.FillRejectionsUdated += new EventHandler(FillHub_FillRejectionsUpdate);
            InitializeComponent();

            UpdateSelectedItem(null);

        }
        //
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        public void UpdateNow(FillHub fillHub)
        {
            m_FillHub = fillHub;
            UpdateRejectList();
        }
        //
        #endregion//Properties


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        /// <summary>
        /// Must be called by GUI thread.
        /// </summary>
        private void UpdateSelectedItem(RejectedFillEventArgs selectedItem)
        {
            if (selectedItem == null)
            {
                // Clean all the boxes.
                textBoxRejectionMessage.Text = string.Empty;
                textReason.Text = string.Empty;
                // Fill details
                textInstrumentName.Text = string.Empty;
                textBoxFillDetails.Text = string.Empty;
                textDateTimeExchange.Text = string.Empty;
                textDateTimeLocal.Text = string.Empty;
            }
            else
            {
                textBoxRejectionMessage.Text = selectedItem.Message;
                textReason.Text = selectedItem.Reason.ToString();

                // Fill details
                textInstrumentName.Text = selectedItem.Name.FullName;
                textBoxFillDetails.Text = selectedItem.OriginalFillEventArg.Fill.ToString();
                textDateTimeExchange.Text = selectedItem.OriginalFillEventArg.Fill.ExchangeTime.ToString(Strings.FormatDateTimeZone);
                textDateTimeLocal.Text = selectedItem.OriginalFillEventArg.Fill.LocalTime.ToString(Strings.FormatDateTimeZone);
            }

        }
        /// <summary>
        /// Must be called by GUI thread.
        /// </summary>
        private void UpdateRejectList()
        {
            // Disconnect data source.
            this.dataGridView1.SuspendLayout();
            this.dataGridView1.DataSource = null;
            m_RejectedFills.Clear();

            // Create new list.
            List<InstrumentName> m_InstrumentNames = new List<InstrumentName>();
            m_FillHub.GetInstrumentNames(ref m_InstrumentNames);
            foreach (InstrumentName name in m_InstrumentNames)
            {
                IFillBook positionBook;
                if (m_FillHub.TryEnterReadBook(name,out positionBook))
                {
                    if (positionBook is BookLifo)
                    {
                        BookLifo book = (BookLifo)positionBook;
                        book.GetRejectedFills(ref m_RejectedFills);
                    }
                    m_FillHub.ExitReadBook(name);
                }
            }//next instrument name
            
            // Reconnect data source.
            this.dataGridView1.DataSource = m_RejectedFills;
            this.dataGridView1.ResumeLayout();
        }// UpdateRejectList()
        //
        //
        /// <summary>
        /// Must be called by GUI thread.
        /// </summary>
        private void ResubmitRejectedFills(RejectedFillEventArgs selectedItem)
        {
            selectedItem.Reason = RejectionReason.ResubmissionRequestedByUser;             // mark this as user resubmission request
            m_FillHub.HubEventEnqueue(selectedItem);               
            
        }
        //
        //
        private bool TryGetSelectedRow(out int selectedRow)
        {
            System.Drawing.Point point = dataGridView1.CurrentCellAddress;
            if (point.Y >= 0 && point.Y < m_RejectedFills.Count)
            {
                selectedRow = point.Y;
                return true;
            }
            else
            {
                selectedRow = -1;
                return false;
            }
        }
        //
        //
        //
        #endregion//Private Methods


        #region External Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        /*
        private void FillHub_FillRejectionsUpdate(object sender, EventArgs eventArgs)
        {
            if (this.InvokeRequired)
                this.Invoke(new EventHandler(FillHub_FillRejectionsUpdate), new object[] { sender, eventArgs });
            else
                UpdateRejectList();
        }
        */ 
        //
        //
        //
        #endregion//External Event Handlers


        #region Form Event Handlers
        // *****************************************************************
        // ****                 Form Event Handlers                     ****
        // *****************************************************************
        //
        //
        //
        private void Form_Load(object sender, EventArgs eventArgs)
        {
           
        }
        private void Form_Closing(object sender, FormClosingEventArgs e)
        {
            
        }
        private void Button_Click(object sender, EventArgs e)
        {
            if (sender is Button)
            {
                Button button = (Button)sender;
                if (button == buttonAcceptFill)
                {
                    int row;
                    if (TryGetSelectedRow(out row))
                    {
                        ResubmitRejectedFills(m_RejectedFills[row]);
                        if (this.dataGridView1.DataSource != null && this.dataGridView1.Rows.Count > 0)
                        {
                            dataGridView1.ClearSelection();
                            dataGridView1.CurrentCell = null;
                        }
                    }
                }

            }
        }
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {

        }
        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            int row;
            if (TryGetSelectedRow(out row) && row>=0 && row<m_RejectedFills.Count)
                UpdateSelectedItem(m_RejectedFills[row]);
            else
                UpdateSelectedItem(null);
        }
        private void dataGridView1_MouseUp(object sender, MouseEventArgs e)
        {
            if (dataGridView1.HitTest(e.X, e.Y) == DataGridView.HitTestInfo.Nowhere)
            {
                dataGridView1.ClearSelection();
                dataGridView1.CurrentCell = null;
            }
        }
        //
        //
        //
        #endregion//Form Event Handlers

    }
}
