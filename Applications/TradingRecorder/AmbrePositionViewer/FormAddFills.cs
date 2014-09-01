using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Ambre.PositionViewer
{
    using Ambre.TTServices.Fills;
    using Misty.Lib.Hubs;
    using Misty.Lib.Products;

    public partial class FormAddFills : Form
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        private Misty.Lib.Hubs.LogHub Log = null;
        private Ambre.TTServices.Fills.FillHub m_FillHub = null;
        private Ambre.TTServices.Markets.MarketTTAPI m_Market = null;

        //private Misty.Lib.Products.InstrumentBase m_CurrentInstrument = null;

        private Misty.Lib.Products.InstrumentName m_CurrentInstrument = new Misty.Lib.Products.InstrumentName();
        private Misty.Lib.Products.InstrumentName m_EmptyInstrument = new Misty.Lib.Products.InstrumentName();

        private bool m_IsConfirmMode = false;

        // Colors and text.
        private Color ColorBuy = Color.RoyalBlue;
        private Color ColorSell = Color.Crimson;
        private Color ColorSubmit = Color.LightGoldenrodYellow;
        private string TextSubmit = "Submit Fill";

        private string TextSubmitBuy = "Confirm Buy";
        private string TextSubmitSell = "Confirm Sell";



        //
        //
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public FormAddFills(Ambre.TTServices.Markets.MarketTTAPI priceHub, FillHub aFillHub)
        {
            InitializeComponent();
            Log = aFillHub.Log;
            m_FillHub = aFillHub;
            m_Market = priceHub;

            this.buttonDeleteBook.Text = ButtonDeleteBook_Normal;           // set this to normal
            this.buttonDeleteBook.Enabled = true;                           // TODO: disable until i can work out the "dupe key" problems.
            //#if (DEBUG)
            //    this.buttonDeleteBook.Enabled = false;
            //#endif
        }

        //
        //       
        #endregion//Constructors


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        /// <summary>
        /// When an instrument is clicked on in the position viewer window, that window
        /// calls this routine letting us know which instrument is active.
        /// </summary>
        /*
        public void SetInstrument(FillHub fillHub, Misty.Lib.Products.InstrumentBase instrument)
        {
            m_CurrentInstrument = instrument;               // set current instrument
            m_FillHub = fillHub;                            // set current fill hub
            this.Text = string.Format("Add Fills - {0}", m_CurrentInstrument.FullName);
            this.labelInstrumentName.Text = m_CurrentInstrument.FullName;
            this.labelExpirationDate.Text = string.Format("{0:ddd dd MMM yyyy}", m_CurrentInstrument.ExpirationDate);

            // Update Markets
            Misty.Lib.BookHubs.Book aBook;
            if (m_Market.TryEnterReadBook(out aBook))
            {
                foreach (Misty.Lib.BookHubs.MarketInstrument mktInstr in aBook.Instruments.Values)
                {
                    if (mktInstr.Name.Equals(m_CurrentInstrument.FullName))
                    {
                        labelAskPrice.Text = mktInstr.Price[Misty.Lib.Utilities.QTMath.AskSide][0].ToString();
                        labelBidPrice.Text = mktInstr.Price[Misty.Lib.Utilities.QTMath.BidSide][0].ToString();
                        labelAskQty.Text = mktInstr.Qty[Misty.Lib.Utilities.QTMath.AskSide][0].ToString();
                        labelBidQty.Text = mktInstr.Qty[Misty.Lib.Utilities.QTMath.BidSide][0].ToString();
                        break;
                    }
                }
                m_Market.ExitReadBook(aBook);
            }
            // Reset defaults
            SetConfirmMode(buttonSubmitFill,false,0);
        }//SetInstrument()
        //
        */
        //
        //
        public void SetInstrument(FillHub fillHub, Misty.Lib.Products.InstrumentName instrument)
        {
            m_CurrentInstrument = instrument;               // set current instrument
            m_FillHub = fillHub;                            // set current fill hub
            this.Text = string.Format("Add Fills - {0}", m_CurrentInstrument.FullName);
            this.labelInstrumentName.Text = m_CurrentInstrument.FullName;

            /*
            Misty.Lib.Products.InstrumentBase instrBase;
            if (m_Market.TryGetInstrument(instrument, out instrBase))
            {
                this.labelExpirationDate.Text = string.Format("{0:ddd dd MMM yyyy}", instrBase.ExpirationDate);
            }
            else
                this.labelExpirationDate.Text = "unknown market instr";     
            */
            TradingTechnologies.TTAPI.InstrumentDetails details;
            if (m_Market.TryLookupInstrumentDetails(instrument, out details))
                this.labelExpirationDate.Text = string.Format("{0:ddd dd MMM yyyy}", details.ExpirationDate.ToDateTime());
            else
                this.labelExpirationDate.Text = "unknown market instr";

            // Update Markets
            Misty.Lib.BookHubs.Book aBook;
            if (m_Market.TryEnterReadBook(out aBook))
            {
                foreach (Misty.Lib.BookHubs.Market mktInstr in aBook.Instruments.Values)
                {
                    if (mktInstr.Name.Equals(m_CurrentInstrument))
                    {
                        labelAskPrice.Text = mktInstr.Price[Misty.Lib.Utilities.QTMath.AskSide][0].ToString();
                        labelBidPrice.Text = mktInstr.Price[Misty.Lib.Utilities.QTMath.BidSide][0].ToString();
                        labelAskQty.Text = mktInstr.Qty[Misty.Lib.Utilities.QTMath.AskSide][0].ToString();
                        labelBidQty.Text = mktInstr.Qty[Misty.Lib.Utilities.QTMath.BidSide][0].ToString();
                        break;
                    }
                }
                m_Market.ExitReadBook(aBook);
            }
            // Reset defaults
            SetConfirmMode(buttonSubmitFill, false, 0);
        }//SetInstrument()
        //
        //
        //
        //
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        private void SetConfirmMode(Button buttonToToggle, bool isConfirmingNow, int qty)
        {
            m_IsConfirmMode = isConfirmingNow;
            if (isConfirmingNow)
            {
                if (qty > 0)
                {
                    buttonToToggle.Text = TextSubmitBuy;
                    buttonToToggle.BackColor = ColorBuy;
                    buttonToToggle.ForeColor = Color.White;
                }
                else if (qty < 0)
                {
                    buttonToToggle.Text = TextSubmitSell;
                    buttonToToggle.BackColor = ColorSell;
                    buttonToToggle.ForeColor = Color.White;
                }
                else
                {
                    buttonToToggle.Text = TextSubmit;
                    buttonToToggle.BackColor = ColorSubmit;
                    buttonToToggle.ForeColor = Color.Black;
                }
            }
            else
            {
                buttonToToggle.Text = TextSubmit;
                buttonToToggle.BackColor = ColorSubmit;
                buttonToToggle.ForeColor = Color.Black;
            }
        }//SetConfirmMode()
        //
        #endregion//Private Methods


        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        private void buttonSubmitFill_Click(object sender, EventArgs e)
        {
            if (m_IsConfirmMode)
            {   // The user has confirmed to send this.
                // Read numbers again
                bool isGoodToSendOrder = true;
                int qty = 0;
                if (!Int32.TryParse(textBoxQty.Text, out qty))
                    isGoodToSendOrder = false;
                double price = 0;
                if (!Double.TryParse(textBoxPrice.Text, out price))
                    isGoodToSendOrder = false;

                if (isGoodToSendOrder)
                {
                    // Submit fill now
                    Misty.Lib.OrderHubs.Fill aFill = Misty.Lib.OrderHubs.Fill.Create();
                    aFill.Price = price;
                    aFill.Qty = qty;
                    aFill.LocalTime = Log.GetTime();

                    /*
                    //Misty.Lib.Products.InstrumentBase instrBase = null;
                    TradingTechnologies.TTAPI.InstrumentDetails details;
                    if (m_CurrentInstrument != m_EmptyInstrument && m_Market.TryGetInstrumentDetails(m_CurrentInstrument, out details) && details != null && details.Key != null)
                    {   // TODO: Somewhere around here a null exception was thrown.
                        if ( Log != null)
                            Log.NewEntry(Misty.Lib.Hubs.LogLevel.Major, "FormAddFills.SubmitFill_Click: Submitting fill request {0} or ForeignKey is null", m_CurrentInstrument);
                        TradingTechnologies.TTAPI.InstrumentKey instrKey = (TradingTechnologies.TTAPI.InstrumentKey)details.Key;
                        Ambre.TTServices.Fills.FillEventArgs fillEventArgs = new Ambre.TTServices.Fills.FillEventArgs(instrKey, Ambre.TTServices.Fills.FillType.UserAdjustment, aFill);
                        m_FillHub.HubEventEnqueue(fillEventArgs);
                    }
                    else if (Log != null)
                        Log.NewEntry(Misty.Lib.Hubs.LogLevel.Warning, "FormAddFills.SubmitFill_Click: Failed {0} or ForeignKey is null", m_CurrentInstrument);
                    */

                    // New version.
                    TradingTechnologies.TTAPI.InstrumentKey instrumentKey;
                    if (m_CurrentInstrument != m_EmptyInstrument && m_FillHub.TryGetInstrumentKey(m_CurrentInstrument, out instrumentKey))
                    {   // TODO: Somewhere around here a null exception was thrown.
                        if (Log != null)
                            Log.NewEntry(Misty.Lib.Hubs.LogLevel.Major, "FormAddFills.SubmitFill_Click: Submitting fill request {0}.", m_CurrentInstrument);
                        Ambre.TTServices.Fills.FillEventArgs fillEventArgs = new Ambre.TTServices.Fills.FillEventArgs(instrumentKey, Ambre.TTServices.Fills.FillType.UserAdjustment, aFill);
                        m_FillHub.HubEventEnqueue(fillEventArgs);
                    }
                    else if (Log != null)
                        Log.NewEntry(Misty.Lib.Hubs.LogLevel.Warning, "FormAddFills.SubmitFill_Click: Failed {0} or ForeignKey is null", m_CurrentInstrument);



                    // REset the colors
                    textBoxQty.Text = string.Empty;                         // protection against user's adding fill twice.
                    SetConfirmMode(buttonSubmitFill, false, 0);                               // reset colors and text on button
                }

            }
            else
            {
                bool isGood = true;
                int qty = 0;
                if (Int32.TryParse(textBoxQty.Text, out qty))
                {
                    textBoxQty.Text = string.Format("{0:+0;-0;0}", qty);
                }
                else
                {
                    textBoxQty.Text = "0";
                    isGood = false;
                }
                double price = 0;
                if (Double.TryParse(textBoxPrice.Text, out price))
                {
                    textBoxPrice.Text = string.Format("{0}", price);
                }
                else
                {
                    textBoxPrice.Text = "";
                    isGood = false;
                }
                // Set
                SetConfirmMode(buttonSubmitFill, isGood, qty);                            // set colors to "confirm" mode.
            }
        }
        //
        private const string ButtonDeleteBook_Normal = "Delete book";
        private const string ButtonDeleteBook_Confirm = "Confirm delete";
        private void buttonDeleteBook_Click(object sender, EventArgs e)
        {

            if (buttonDeleteBook.Text == ButtonDeleteBook_Normal)
            {   // We are in the normal state.
                buttonDeleteBook.Text = ButtonDeleteBook_Confirm;
            }
            else if (buttonDeleteBook.Text == ButtonDeleteBook_Confirm)
            {   // User has just confirmed the delete request.  
                // Submit the delete request

                if (Log != null)
                {
                    if (m_FillHub == null)
                    {
                        Log.NewEntry(Misty.Lib.Hubs.LogLevel.Major, "FormAddFill.buttonDeleteBook_Click: FillHub is null!");
                        buttonDeleteBook.Text = ButtonDeleteBook_Normal;
                        return;
                    }
                    else if (m_CurrentInstrument == null || m_CurrentInstrument == m_EmptyInstrument)
                    {
                        Log.NewEntry(Misty.Lib.Hubs.LogLevel.Major, "FormAddFill.buttonDeleteBook_Click: CurrentInsrument is null!");
                        buttonDeleteBook.Text = ButtonDeleteBook_Normal;
                        return;
                    }
                    //else if (m_CurrentInstrument.ForeignKey == null)
                    //{
                    //    Log.NewEntry(Misty.Lib.Hubs.LogLevel.Major,"FormAddFill.buttonDeleteBook_Click: CurrentInsrument.ForeignKey is null!");
                    //    buttonDeleteBook.Text = ButtonDeleteBook_Normal;
                    //    return;
                    //}
                    Log.NewEntry(Misty.Lib.Hubs.LogLevel.Major, "FormAddFill.buttonDeleteBook_Click: Request delete for {0} in FillHub {1}.", m_CurrentInstrument, m_FillHub.Name);
                }

                // Try to read the required values like realized PnL, average cost, net position.
                IFillBook book;
                if (m_FillHub.TryEnterReadBook(m_CurrentInstrument, out book))
                {
                    Log.NewEntry(LogLevel.Major, "The deleted book contains net pos:{0}, real PnL:{1}, start real PnL:{2}, avg price:{3}, currency name:{4}, currency rate:{5}.",
                        book.NetPosition, book.RealizedDollarGains, book.RealizedStartingDollarGains, book.AveragePrice, book.CurrencyName, book.CurrencyRate);

                    double netPosition = book.NetPosition;
                    double realizedPnL = book.RealizedDollarGains;
                    double startRealizedPnL = book.RealizedStartingDollarGains;
                    double avgPrice = book.AveragePrice;
                    string currencyName = book.CurrencyName;
                    double currencyRate = book.CurrencyRate;
                    m_FillHub.ExitReadBook(m_CurrentInstrument);
                    double settlementValue = double.NaN;

                    // Find the instrument detail for this instrument and load the cash book accordingly.
                    //TradingTechnologies.TTAPI.InstrumentDetails instrDetail;
                    //if (!m_Market.TryLookupInstrumentDetails(m_CurrentInstrument, out instrDetail))
                    //{
                    //    Log.NewEntry(LogLevel.Warning, "No instrument detail is found for {0}", m_CurrentInstrument);
                    //    return;
                    //}

                    if (netPosition != 0)
                    {
                        // Request the user to input a settlement value to calculate the PnL.
                        string input = string.Empty;
                        while (!double.TryParse(input, out settlementValue) && settlementValue != double.NaN)
                        {
                            input = Microsoft.VisualBasic.Interaction.InputBox("Please input the settlement price. Input nothing to exit.",
                                "Request settlement price", avgPrice.ToString(), 450, 450);

                            if (string.IsNullOrEmpty(input))
                                return;
                        }

                        // Calculate the PnL by the settlement price.
                        realizedPnL += (settlementValue - avgPrice) * netPosition * book.DollarPerPoint;
                    }

                    // Ask the user whether he likes to transfer the PnL to the corresponding cash instrument.
                    DialogResult result = MessageBox.Show(string.Format("I am going to transfer {0} ammount of PnL in currency {1} to the cash instrument",
                        startRealizedPnL + realizedPnL, currencyName), "PnL transfer to cash instrument", MessageBoxButtons.YesNo);

                    if (result == System.Windows.Forms.DialogResult.Yes)
                    {
                        // Get the cash book according to the instrument detail.
                        Product product = new Product("Cash", currencyName, ProductTypes.Cash, "");
                        InstrumentName newCashInstr = new InstrumentName(product, "");

                        Log.NewEntry(LogLevel.Minor, "Send request to create/update cash book.");
                        Misty.Lib.OrderHubs.OrderHubRequest cashBookCreateRequest = new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestCreateUpdateCashBook);
                        cashBookCreateRequest.Data = new object[5] {
                            newCashInstr, 
                            currencyName,
                            currencyRate,
                            realizedPnL, 
                            startRealizedPnL };
                        m_FillHub.Request(cashBookCreateRequest);

                        Misty.Lib.OrderHubs.OrderHubRequest instrumentDeleteRequest = new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestDeleteBook);
                        //TradingTechnologies.TTAPI.InstrumentKey instrKey = (TradingTechnologies.TTAPI.InstrumentKey)m_CurrentInstrument.ForeignKey;
                        //request.Data = new object[1] { instrKey };
                        Log.NewEntry(LogLevel.Minor, "Send request to delete fill book.");
                        instrumentDeleteRequest.Data = new object[1] { m_CurrentInstrument };
                        m_FillHub.Request(instrumentDeleteRequest);
                        buttonDeleteBook.Text = ButtonDeleteBook_Normal;
                    }
                    else
                    {
                        buttonDeleteBook.Text = ButtonDeleteBook_Normal;
                    }
                }
                else
                {
                    Log.NewEntry(LogLevel.Warning, "FormAddFill.buttonDeleteBook_Click:No instrument key found for {0}", m_CurrentInstrument);
                    buttonDeleteBook.Text = ButtonDeleteBook_Normal;
                }

                //Misty.Lib.OrderHubs.OrderHubRequest request = new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestDeleteBook);
                ////TradingTechnologies.TTAPI.InstrumentKey instrKey = (TradingTechnologies.TTAPI.InstrumentKey)m_CurrentInstrument.ForeignKey;
                ////request.Data = new object[1] { instrKey };
                //request.Data = new object[1] { m_CurrentInstrument };
                //m_FillHub.Request(request);
                //buttonDeleteBook.Text = ButtonDeleteBook_Normal;
            }
        }//
        //
        //
        //
        //
        private void FormAddFills_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_Market = null;
            m_FillHub = null;
            Log = null;
        }

        private void textBoxPriceQty_Enter(object sender, EventArgs e)
        {
            SetConfirmMode(buttonSubmitFill, false, 0);
        }
        //
        #endregion//Event Handlers

    }
}
