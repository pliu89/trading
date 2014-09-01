using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

namespace BRE.Tests.DatabaseTest
{
    using BRE.Lib;
    using BRE.Lib.TermStructures;
    using BRE.Lib.Utilities;

    using UV.Lib.Hubs;
    using UV.Lib.MarketHubs;
    using UV.Lib.Products;
    using UV.Lib.DatabaseReaderWriters;
    using UV.Lib.DatabaseReaderWriters.Queries;

    using UV.TTServices;
    using UV.TTServices.Markets;

    public partial class HedgeOptionsDatabaseWriterTest : Form
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        private DatabaseReaderWriter m_DatabaseReaderWriter = null;
        private HedgeOptionsWriter m_HedgeOptionsWriter = null;
        private List<InstrumentName> m_InstrumentExistInDatabase = null;
        private List<InstrumentName> m_InstrumentPendingWriting = null;
        private Dictionary<InstrumentName, InstrumentInfoItem> m_BREInstrumentInfoItems = null;
        private InstrumentInfoItem m_BREInstrumentInfoItem = null;
        private TTApiService m_TTAPIService;
        private MarketHub m_Market;
        private Product m_Product;
        private Dispatcher m_UIDispatcher;
        private LogHub m_Log = null;
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //       
        public HedgeOptionsDatabaseWriterTest()
        {
            m_Log = new LogHub("BRELibLog", string.Format("{0}{1}", UV.Lib.Application.AppInfo.GetInstance().BasePath,
                UV.Lib.Application.AppInfo.GetInstance().LogDirectory), true, LogLevel.ShowAllMessages);
            InitializeComponent();
            m_UIDispatcher = Dispatcher.CurrentDispatcher;

            DatabaseInfo dbInfo = DatabaseInfo.Create(DatabaseInfo.DatabaseLocation.bredev);
            dbInfo.UserName = "root";
            dbInfo.UserPW = "test";
            m_DatabaseReaderWriter = new DatabaseReaderWriter(dbInfo);
            m_DatabaseReaderWriter.QueryResponse += new EventHandler(DatabaseReaderWriter_QueryResponse);

            this.textBoxExchangeName.Text = "CME";
            this.textBoxProductName.Text = "GE";
            this.textBoxProductType.Text = "Spread";

            m_TTAPIService = TTApiService.GetInstance();
            m_TTAPIService.ServiceStateChanged += new EventHandler(TTApiService_ServiceStateChanged);
            m_TTAPIService.Start(true);

            m_Market = new MarketTTAPI();
            m_Market.MarketStatusChanged += new EventHandler(MarketTTAPI_MarketStatusChanged);
            m_Market.FoundResource += new EventHandler(MarketTTAPI_MarketFoundServiceResource);
            m_Market.Start();
            m_Market.Connect();                             // initialize connection to API.
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


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        private void buttonStart_Click(object sender, EventArgs e)
        {
            m_HedgeOptionsWriter = new HedgeOptionsWriter(m_Log);
            m_InstrumentExistInDatabase = new List<InstrumentName>();
            m_InstrumentPendingWriting = new List<InstrumentName>();
            m_BREInstrumentInfoItems = new Dictionary<InstrumentName, InstrumentInfoItem>();

            // Generate query to get all instruments from the data base.
            InstrumentInfoQuery BREInstrumentInfoQuery = new InstrumentInfoQuery();
            ProductTypes productTypes;
            if (Enum.TryParse<ProductTypes>(textBoxProductType.Text, out productTypes))
            {
                m_Product = new Product(textBoxExchangeName.Text, textBoxProductName.Text, productTypes);
                BREInstrumentInfoQuery.InstrumentName = new InstrumentName(m_Product, string.Empty);
                BREInstrumentInfoQuery.IsRead = true;
                BREInstrumentInfoQuery.Status = QueryStatus.New;
            }
            else
            {
                m_Log.NewEntry(LogLevel.Error, "Product type parse failed");
                return;
            }

            // Submit query and store existing instruments from the data base.
            m_DatabaseReaderWriter.SubmitSync(BREInstrumentInfoQuery);
            List<InstrumentInfoItem> BREInstrumentInfoItems = BREInstrumentInfoQuery.Results;
            foreach (InstrumentInfoItem BREInstrumentInfoItem in BREInstrumentInfoItems)
            {
                if (m_BREInstrumentInfoItem == null)
                    m_BREInstrumentInfoItem = BREInstrumentInfoItem;                                // Need one of this only.

                string instrumentNameString = BREInstrumentInfoItem.InstrumentNameTT;
                InstrumentName instrumentName;
                if (instrumentNameString != null && InstrumentName.TryDeserialize(instrumentNameString, out instrumentName))
                {
                    m_InstrumentExistInDatabase.Add(instrumentName);
                    m_BREInstrumentInfoItems.Add(instrumentName, BREInstrumentInfoItem);
                }
            }

            // Launch market instruments request.
            m_Market.RequestProducts(new List<Product>() { m_Product });
            m_Market.RequestInstruments(m_Product);
        }
        //
        //
        private void DatabaseReaderWriter_QueryResponse(object sender, EventArgs e)
        {

        }
        //
        //
        private void TTApiService_ServiceStateChanged(object sender, EventArgs e)
        {
            if (e is TTApiService.ServiceStatusChangeEventArgs)
            {
                TTApiService.ServiceStatusChangeEventArgs eventArgs = (TTApiService.ServiceStatusChangeEventArgs)e;
                if (eventArgs.IsConnected)
                {
                    // We have a good connection to a user session now.
                    m_UIDispatcher.Invoke(
                        new Action(() =>
                        {
                            this.buttonStart.Enabled = true;
                        })
                        );
                }
            }
        }
        //
        //
        private void MarketTTAPI_MarketFoundServiceResource(object sender, EventArgs e)
        {
            FoundServiceEventArg foundServiceEventArg = (FoundServiceEventArg)e;
            if (foundServiceEventArg.FoundInstruments != null)
            {
                foreach (InstrumentName instrumentFromTT in foundServiceEventArg.FoundInstruments)
                {
                    if (!m_InstrumentPendingWriting.Contains(instrumentFromTT))
                    {
                        InstrumentInfoItem BREInstrumentInfoItem;
                        if (m_InstrumentExistInDatabase.Contains(instrumentFromTT))
                        {
                            BREInstrumentInfoItem = m_BREInstrumentInfoItems[instrumentFromTT];
                        }
                        else
                        {
                            BREInstrumentInfoItem = m_BREInstrumentInfoItem;
                            InstrumentDetails instrumentDetails;
                            if (m_Market.TryGetInstrumentDetails(instrumentFromTT, out instrumentDetails))
                            {
                                BREInstrumentInfoItem.LastTradeDate = instrumentDetails.ExpirationDate;
                                BREInstrumentInfoItem.Type = instrumentDetails.Type;
                                string expiryCode;
                                if (!BRE.Lib.Utilities.QTMath.TryParseMMMYYToMonthCode(instrumentDetails.InstrumentName.SeriesName, out expiryCode))
                                {
                                    m_Log.NewEntry(LogLevel.Error, "Failed to parse instrument series name for instrument {0}.", instrumentFromTT);
                                    continue;
                                }
                                BREInstrumentInfoItem.Expiry = expiryCode;
                            }
                            else
                            {
                                m_Log.NewEntry(LogLevel.Error, "Failed to get instrument details for instrument {0}.", instrumentFromTT);
                                continue;
                            }
                            m_BREInstrumentInfoItems.Add(instrumentFromTT, BREInstrumentInfoItem);
                        }
                        BREInstrumentInfoItem.InstrumentNameTT = instrumentFromTT.FullName;

                        string instrumentNameDatabase;
                        string hedgeOptions;
                        if (QTMath.TryConvertInstrumentNameToInstrumentNameDatabase(instrumentFromTT, m_Log, out instrumentNameDatabase))
                        {
                            BREInstrumentInfoItem.InstrumentNameDatabase = instrumentNameDatabase;
                        }
                        else
                        {
                            m_Log.NewEntry(LogLevel.Error, "Instrument name database generated failed for instrument {0}.", instrumentFromTT);
                            continue;
                        }

                        if (m_HedgeOptionsWriter.TryWriteHedgeOptionsForInstrument(instrumentFromTT, out hedgeOptions))
                        {
                            BREInstrumentInfoItem.HedgeOptions = hedgeOptions;
                        }
                        else
                        {
                            m_Log.NewEntry(LogLevel.Error, "Hedge options generated failed for instrument {0}.", instrumentFromTT);
                            continue;
                        }

                        m_InstrumentPendingWriting.Add(instrumentFromTT);
                    }
                }
            }
            m_UIDispatcher.Invoke(
                         new Action(() =>
                         {
                             this.buttonWrite.Enabled = true;
                         })
                         );
        }
        //
        //
        private void MarketTTAPI_MarketStatusChanged(object sender, EventArgs e)
        {

        }
        //
        //
        private void buttonWrite_Click(object sender, EventArgs e)
        {
            List<InstrumentInfoQuery> BREInstrumentInfoQuerys = new List<InstrumentInfoQuery>();
            foreach (InstrumentName instrumentName in m_InstrumentPendingWriting)
            {
                if (m_BREInstrumentInfoItems.ContainsKey(instrumentName))
                {
                    InstrumentInfoItem BREInstrumentInfoItem = m_BREInstrumentInfoItems[instrumentName];
                    InstrumentInfoQuery BREInstrumentInfoQuery = new InstrumentInfoQuery();
                    BREInstrumentInfoQuery.InstrumentName = instrumentName;
                    BREInstrumentInfoQuery.IsRead = false;
                    BREInstrumentInfoQuery.Status = QueryStatus.New;
                    BREInstrumentInfoQuery.Results.Add(BREInstrumentInfoItem);
                    if (!m_InstrumentExistInDatabase.Contains(instrumentName))
                    {
                        BREInstrumentInfoQuerys.Add(BREInstrumentInfoQuery);
                    }
                }
                else
                {
                    m_Log.NewEntry(LogLevel.Error, "No instrument found for instrument {0}.", instrumentName);
                    return;
                }
            }

            foreach (InstrumentInfoQuery BREInstrumentInfoQuery in BREInstrumentInfoQuerys)
                m_DatabaseReaderWriter.SubmitAsync(BREInstrumentInfoQuery);

            m_UIDispatcher.Invoke(
                         new Action(() =>
                         {
                             this.buttonWrite.Enabled = false;
                         })
                         );
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
