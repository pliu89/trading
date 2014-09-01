using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BRE.Tests.InstrumentMatrixTest
{
    using BRE.Lib;
    using BRE.Lib.TermStructures;
    using BRE.Lib.TermStructures.InstrumentMatrix;

    using UV.Lib.Hubs;
    using UV.Lib.Products;
    using UV.Lib.DatabaseReaderWriters;
    using UV.Lib.DatabaseReaderWriters.Queries;

    public partial class InstrumentMatrixTest : Form
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //    
        private DatabaseReaderWriter m_DatabaseReaderWriter = null;
        private InstrumentMatrix m_InstrumentMatrix = null;
        private HedgeOptionsReader m_HedgeOptionsReader = null;
        private InstrumentMatrixViewer m_InstrumentMatrixViewer = null;
        private List<InstrumentName> m_InstrumentNames = null;
        private List<string> m_InstrumentNameStrings = null;
        private LogHub m_Log = null;
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //     
        public InstrumentMatrixTest()
        {
            //BRELibLog.Log.NewEntry(LogLevel.Minor, "");
            m_Log = new LogHub("BRELibLog", string.Format("{0}{1}", UV.Lib.Application.AppInfo.GetInstance().BasePath,
                UV.Lib.Application.AppInfo.GetInstance().LogDirectory), true, LogLevel.ShowAllMessages);
            InitializeComponent();

            DatabaseInfo dbInfo = DatabaseInfo.Create(DatabaseInfo.DatabaseLocation.bredev);
            dbInfo.UserName = "root";
            dbInfo.UserPW = "test";
            m_DatabaseReaderWriter = new DatabaseReaderWriter(dbInfo);
            m_DatabaseReaderWriter.QueryResponse += new EventHandler(DatabaseReaderWriter_QueryResponse);

            this.textBoxInstrument1.Text = "CME.GE (Spread) Calendar: 1xGE Sep14:-1xJun15";
            this.textBoxInstrument2.Text = "CME.GE (Spread) Calendar: 1xGE Dec14:-1xMar15";
        }
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


        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        #endregion//Private Methods


        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        private void buttonSetupInstrumentMatrix_Click(object sender, EventArgs e)
        {
            // Process clicks of buttons.
            m_HedgeOptionsReader = new HedgeOptionsReader(m_Log);
            m_InstrumentNames = new List<InstrumentName>();
            m_InstrumentNameStrings = new List<string>();

            m_InstrumentNameStrings.Add("CME.GE (Spread) Calendar: 1xGE Sep14:-1xDec14");
            m_InstrumentNameStrings.Add("CME.GE (Spread) Calendar: 1xGE Dec14:-1xMar15");
            m_InstrumentNameStrings.Add("CME.GE (Spread) Calendar: 1xGE Mar15:-1xJun15");
            m_InstrumentNameStrings.Add("CME.GE (Spread) Calendar: 1xGE Jun15:-1xSep15");
            m_InstrumentNameStrings.Add("CME.GE (Spread) Calendar: 1xGE Sep15:-1xDec15");
            m_InstrumentNameStrings.Add("CME.GE (Spread) Calendar: 1xGE Dec15:-1xMar16");
            m_InstrumentNameStrings.Add("CME.GE (Spread) Calendar: 1xGE Mar16:-1xJun16");
            m_InstrumentNameStrings.Add("CME.GE (Spread) Calendar: 1xGE Jun16:-1xSep16");
            m_InstrumentNameStrings.Add("CME.GE (Spread) Calendar: 1xGE Sep16:-1xDec16");

            m_InstrumentNameStrings.Add("CME.GE (Spread) Calendar: 1xGE Sep14:-1xMar15");
            m_InstrumentNameStrings.Add("CME.GE (Spread) Calendar: 1xGE Dec14:-1xJun15");
            m_InstrumentNameStrings.Add("CME.GE (Spread) Calendar: 1xGE Mar15:-1xSep15");
            m_InstrumentNameStrings.Add("CME.GE (Spread) Calendar: 1xGE Jun15:-1xDec15");
            m_InstrumentNameStrings.Add("CME.GE (Spread) Calendar: 1xGE Sep15:-1xMar16");
            m_InstrumentNameStrings.Add("CME.GE (Spread) Calendar: 1xGE Dec15:-1xJun16");
            m_InstrumentNameStrings.Add("CME.GE (Spread) Calendar: 1xGE Mar16:-1xSep16");
            m_InstrumentNameStrings.Add("CME.GE (Spread) Calendar: 1xGE Jun16:-1xDec16");

            m_InstrumentNameStrings.Add("CME.GE (Spread) Calendar: 1xGE Sep14:-1xJun15");
            m_InstrumentNameStrings.Add("CME.GE (Spread) Calendar: 1xGE Dec14:-1xSep15");
            m_InstrumentNameStrings.Add("CME.GE (Spread) Calendar: 1xGE Mar15:-1xDec15");
            m_InstrumentNameStrings.Add("CME.GE (Spread) Calendar: 1xGE Jun15:-1xMar16");
            m_InstrumentNameStrings.Add("CME.GE (Spread) Calendar: 1xGE Sep15:-1xJun16");
            m_InstrumentNameStrings.Add("CME.GE (Spread) Calendar: 1xGE Dec15:-1xSep16");
            m_InstrumentNameStrings.Add("CME.GE (Spread) Calendar: 1xGE Mar16:-1xDec16");

            foreach (string instrumentString in m_InstrumentNameStrings)
            {
                InstrumentInfoQuery BREInstrumentInfoQuery = new InstrumentInfoQuery();
                InstrumentName instrumentName;
                if (InstrumentName.TryDeserialize(instrumentString, out instrumentName))
                {
                    m_InstrumentNames.Add(instrumentName);
                    BREInstrumentInfoQuery.InstrumentName = instrumentName;
                    BREInstrumentInfoQuery.IsRead = true;
                    BREInstrumentInfoQuery.Status = QueryStatus.New;
                    m_DatabaseReaderWriter.SubmitSync(BREInstrumentInfoQuery);
                    m_HedgeOptionsReader.TryReadHedgeOptionsInformation(BREInstrumentInfoQuery);
                }
                else
                    return;
            }

            Dictionary<InstrumentName, HedgeOptions> hedgeOptionsByInstrument = m_HedgeOptionsReader.GetHedgeOptionsByInstrumentName();
            m_InstrumentMatrix = new InstrumentMatrix(m_InstrumentNames, m_Log);
            if (m_InstrumentMatrix.TrySetupInstrumentMatrix(hedgeOptionsByInstrument))
            {
                m_Log.NewEntry(LogLevel.Minor, "Successfully setup the instrument matrix.");
                ProcessShowData();
            }
        }
        //
        //
        private void buttonFindResultingInstrument_Click(object sender, EventArgs e)
        {
            if (m_InstrumentMatrix != null)
            {
                ResultingInstrument resultingInstrument;
                InstrumentName instrumentName1;
                InstrumentName instrumentName2;
                if (InstrumentName.TryDeserialize(this.textBoxInstrument1.Text, out instrumentName1) &&
                    InstrumentName.TryDeserialize(this.textBoxInstrument2.Text, out instrumentName2))
                {
                    int filledQty1;
                    int filledQty2;
                    if (int.TryParse(this.textBoxQty1.Text, out filledQty1) && int.TryParse(this.textBoxQty2.Text, out filledQty2))
                    {
                        int resultingQty;
                        int qty1Remaining;
                        int qty2Remaining;
                        if (m_InstrumentMatrix.TryFindResultingInstrument(instrumentName1, filledQty1, instrumentName2, filledQty2,
                            out resultingInstrument, out resultingQty, out qty1Remaining, out qty2Remaining))
                        {
                            this.textBoxResultingInstrument.Text = resultingInstrument.ResultingInstrumentName.FullName;
                            this.textBoxResultingQty.Text = resultingQty.ToString();
                            this.textBoxQty1Remaining.Text = qty1Remaining.ToString();
                            this.textBoxQty2Remaining.Text = qty2Remaining.ToString();
                        }
                        else
                        {
                            this.textBoxResultingInstrument.Text = string.Empty;
                            this.textBoxResultingQty.Text = string.Empty;
                            this.textBoxQty1Remaining.Text = string.Empty;
                            this.textBoxQty2Remaining.Text = string.Empty;
                        }
                    }
                    else
                    {
                        this.textBoxResultingInstrument.Text = string.Empty;
                        this.textBoxResultingQty.Text = string.Empty;
                        this.textBoxQty1Remaining.Text = string.Empty;
                        this.textBoxQty2Remaining.Text = string.Empty;
                    }
                }
                else
                {
                    this.textBoxResultingInstrument.Text = string.Empty;
                    this.textBoxResultingQty.Text = string.Empty;
                    this.textBoxQty1Remaining.Text = string.Empty;
                    this.textBoxQty2Remaining.Text = string.Empty;
                }
            }
            else
            {
                m_Log.NewEntry(LogLevel.Minor, "Not setup the instrument matrix yet.");
            }
        }
        //
        //
        private void DatabaseReaderWriter_QueryResponse(object sender, EventArgs e)
        {

        }
        //
        //
        private void InstrumentMatrixTest_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (m_DatabaseReaderWriter != null)
            {
                m_DatabaseReaderWriter.RequestStop();
                m_DatabaseReaderWriter = null;
            }
            if (m_Log != null)
            {
                m_Log.RequestStop();
                m_Log = null;
            }
        }
        //
        //
        private void ProcessShowData()
        {
            if (m_InstrumentMatrixViewer == null || m_InstrumentMatrixViewer.IsShutDown)
            {
                m_InstrumentMatrixViewer = new InstrumentMatrixViewer();
                m_InstrumentMatrixViewer.ShowInstrumentMatrix(m_InstrumentNames, m_InstrumentMatrix);
            }
        }
        #endregion//Event Handlers

    }
}
