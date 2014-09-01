using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
//using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UV.Lib.FrontEnds.Clusters
{
    using UV.Lib.Engines;
    using UV.Lib.BookHubs;
    using UV.Lib.Hubs;
    using UV.Lib.Utilities;
    using UV.Lib.FrontEnds.Huds;

    using UV.Lib.IO.Xml;        // for Stringify.TryGetType()

    public partial class Cluster : UserControl, IEngineContainer
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Parent display and Hub
        //        
        public ClusterDisplay ParentDisplay = null; // my owner display - not set at construction time, but later.
        private LogHub Log = null;                  // the log mangager (of my parent hub) for error messages.
        private bool IsRegenerateNeeded = false;    // communicates to parent hub that I need regenerating.

        // Engine interfaces        
        private ClusterConfiguration m_Configuration = null;	// controls layout of Cluster inside ClusterDisplay
        private int m_EngineContainerID = -1;       // my ID
        private string m_Name = string.Empty;       // my name

        // Header
        public Header m_Header = null;
        private MultiPanel m_LowerControl = null;



        //
        // Rows - This will be a BoxRowControl in future
        //
        private BoxRow[] m_Row = null;
        private int m_BoxRowRows = 5;
        //private int m_BoxRowColumns = 6;                      // now inside Config
        private int[][] m_Memory;
        private int[][] m_HiLiteMemory;
        private int[] m_PrevHiLite = new int[2];
        public double m_MinTickSize = 1.0;
        private int m_CurrentLeftBaseIndex = int.MinValue;      // integerized price at the LeftBase location.
        private int m_LeftBaseLoc = 0;                          // column index, just left of center.
        private bool[] m_BoxRowIsRegenerationNeeded = null;
        private int[][] m_RawDataQ;                             // [rowID][ith data] store raw qty data for each box row.
        private double[][] m_RawDataP;                          // store raw price data for each box row.

        // Row types
        private const int PriceRowID = 1;                       // these values denote the row ordering for display.
        private const int AskRowID = 2;
        private const int BidRowID = 3;
        private const int FillsRowID = 0;
        private const int OrderRowID = 4;
        // EngineIDs
        private int m_PricingEngineID = -1;                     // engineID numbers for each engine found.
        private int m_FillEngineID = -1;
        private int m_OrderEngineID = -1;


        //
        // Cluster mouse events.
        //
        private bool m_IsMouseOverClickableBox = false;     // indicates mouse is over a clickable region.

        //
        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        public Cluster(GuiTemplates.EngineContainerGui guiTemplate)
        {           
            InitializeComponent();

            m_Name = guiTemplate.DisplayName;
            m_EngineContainerID = guiTemplate.EngineContainerID;
            this.Name = string.Format("Cluster{0}", m_EngineContainerID.ToString());
            m_Configuration = new ClusterConfiguration();//clusterConfig.Clone();

            m_Header = new Header(this, guiTemplate);                       // creates PopUp controls for engines.
            m_LowerControl = new Huds.MultiPanel(this, guiTemplate);

            InitializeBoxRowControl(guiTemplate);						    // creates BoxRows       
            InitializeLayout();										        // does layout for all controls.

            RegisterMouseEvents();
            
        }
        //
        //
        //
        //
        //
        // *********************************************************
        // ****         Initialize BoxRow Control()            ****
        // *********************************************************
        /// <summary>
        /// Initializes the BoxRowControl object, which right now is hard-coding inside
        /// this object.
        /// </summary>
        private void InitializeBoxRowControl(GuiTemplates.EngineContainerGui guiTemplate)
        {
            //
            // Identify the types of engines we have.
            //
            List<GuiTemplates.EngineGui> engineList = guiTemplate.m_Engines;
            foreach (GuiTemplates.EngineGui engine in engineList)
            {
                int engineID = engine.EngineID;
                Type engineType;
                if (UV.Lib.IO.Xml.Stringifiable.TryGetType(engine.EngineFullName,out engineType))
                {
                    List<Type> interfaces = new List<Type>(engineType.GetInterfaces());
                    if (interfaces.Contains( typeof(IPricingEngine) ))
                    {
                        m_PricingEngineID = engineID;
                        
                        //m_MinTickSize = 1;
                    }
                    // test for order engines, fill engines, etc, too.

                }



                /*
                Type baseType = engine.GetType();
                while (baseType != null && baseType.BaseType != typeof(Engine))
                {   // if next next level down is NOT an Engine, then we need to dig deeper.
                    baseType = baseType.BaseType;
                }
                if (baseType.Name.Contains("Pricing"))
                {	// the main pricing engine is assumed to be listed first!
                    // TODO: In future, allow multiple pricing engines, or some other way to 
                    // indicate the "boss" pricing engine whose market is shown in the Cluster.
                    if (m_PricingEngineID < 0)
                    {
                        m_PricingEngineID = engine.EngineID;
                    }
                    else if (Log != null)
                    {
                        Log.NewEntry(LogLevel.Major, "{0} cluster won't display mkt depth of pricing engine {1}."
                            , container.EngineContainerName, engine.EngineName);
                    }
                }
                else if (baseType.Name.Contains("Fill"))
                    m_FillEngineID = engine.EngineID;
                else if (baseType.Name.Contains("Order"))
                    m_OrderEngineID = engine.EngineID;
                 */ 
            }//next engine


            //
            // Initialize the cluster's internal workings.                        
            //
            m_BoxRowIsRegenerationNeeded = new bool[m_BoxRowRows];
            m_Memory = new int[m_BoxRowRows][];
            m_HiLiteMemory = new int[m_BoxRowRows][];
            m_RawDataQ = new int[m_BoxRowRows][];
            m_RawDataP = new double[m_BoxRowRows][];

            for (int i = 0; i < m_Memory.Length; i++)
            {
                m_Memory[i] = new int[m_Configuration.BoxRowColumns];
                m_HiLiteMemory[i] = new int[m_Configuration.BoxRowColumns];
            }
            // Initialize the clusters controls.
            m_LeftBaseLoc = (int)(m_Configuration.BoxRowColumns / 2 - 1); // index offset for left-most (first) box.


            //
            // Create the rows
            //
            int nCols = m_Configuration.BoxRowColumns;
            if (nCols == 0)
            {
                m_Row = new BoxRow[0];
                return;
            }
            int xLocOffset = 0;                 // optional extra space for over-flow columns.
            m_Row = new BoxRow[m_BoxRowRows];
            for (int row = 0; row < m_Row.Length; ++row)
            {   // design each row property
                switch (row)
                {
                    case PriceRowID:     // price row
                        m_Row[row] = new BoxRow(nCols, m_MinTickSize, xLocOffset, false, ColorPalette.TextNormal);
                        m_Row[row].RowType = ClusterEventArgs.BoxRowType.PriceRow;
                        break;
                    case BidRowID:
                        m_Row[row] = new BoxRow(nCols, 1, xLocOffset, false, ColorPalette.BackSigned);
                        m_Row[row].RowType = ClusterEventArgs.BoxRowType.BidQtyRow;
                        break;
                    case AskRowID:
                        m_Row[row] = new BoxRow(nCols, 1, xLocOffset, false, ColorPalette.BackSigned);
                        m_Row[row].RowType = ClusterEventArgs.BoxRowType.AskQtyRow;
                        break;
                    case FillsRowID:
                        m_Row[row] = new BoxRow(nCols, 1, xLocOffset, false, ColorPalette.BackSigned);
                        m_Row[row].RowType = ClusterEventArgs.BoxRowType.FillsRow;
                        break;
                    case OrderRowID:
                        //m_Row[row] = new BoxRow(nCols, 1, xLocOffset, false, ColorPalette.ForeSigned);
                        m_Row[row] = new BoxRow(nCols, 1, xLocOffset, false, ColorPalette.TextSigned);
                        m_Row[row].RowType = ClusterEventArgs.BoxRowType.OrderRow;
                        break;
                    default:
                        m_Row[row] = new BoxRow(nCols, 1, xLocOffset, false, ColorPalette.BackSigned);
                        break;
                }//row switch
            }// row

        }//InitializeBoxRowControl().
        //
        /*
        private void InitializeBoxRowControl(IEngineContainer container)
        {
            //
            // Identify the types of engines we have.
            //
            List<IEngine> engineList = container.GetEngines();
            foreach (IEngine engine in engineList)
            {
                Type baseType = engine.GetType();
                while (baseType != null && baseType.BaseType != typeof(Engine))
                {   // if next next level down is NOT an Engine, then we need to dig deeper.
                    baseType = baseType.BaseType;
                }
                if (baseType.Name.Contains("Pricing"))
                {	// the main pricing engine is assumed to be listed first!
                    // TODO: In future, allow multiple pricing engines, or some other way to 
                    // indicate the "boss" pricing engine whose market is shown in the Cluster.
                    if (m_PricingEngineID < 0)
                    {
                        m_PricingEngineID = engine.EngineID;
                    }
                    else if (Log != null)
                    {
                        Log.NewEntry(LogLevel.Major, "{0} cluster won't display mkt depth of pricing engine {1}."
                            , container.EngineContainerName, engine.EngineName);
                    }
                }
                else if (baseType.Name.Contains("Fill"))
                    m_FillEngineID = engine.EngineID;
                else if (baseType.Name.Contains("Order"))
                    m_OrderEngineID = engine.EngineID;
            }//next engine


            //
            // Initialize the cluster's internal workings.                        
            //
            m_BoxRowIsRegenerationNeeded = new bool[m_BoxRowRows];
            m_Memory = new int[m_BoxRowRows][];
            m_HiLiteMemory = new int[m_BoxRowRows][];
            m_RawDataQ = new int[m_BoxRowRows][];
            m_RawDataP = new double[m_BoxRowRows][];

            for (int i = 0; i < m_Memory.Length; i++)
            {
                m_Memory[i] = new int[m_Configuration.BoxRowColumns];
                m_HiLiteMemory[i] = new int[m_Configuration.BoxRowColumns];
            }
            // Initialize the clusters controls.
            m_LeftBaseLoc = (int)(m_Configuration.BoxRowColumns / 2 - 1); // index offset for left-most (first) box.


            //
            // Create the rows
            //
            int nCols = m_Configuration.BoxRowColumns;
            if (nCols == 0)
            {
                m_Row = new BoxRow[0];
                return;
            }
            int xLocOffset = 0;                 // optional extra space for over-flow columns.
            m_Row = new BoxRow[m_BoxRowRows];
            for (int row = 0; row < m_Row.Length; ++row)
            {   // design each row property
                switch (row)
                {
                    case PriceRowID:     // price row
                        m_Row[row] = new BoxRow(nCols, m_MinTickSize, xLocOffset, false, ColorPalette.TextNormal);
                        m_Row[row].RowType = ClusterEventArgs.BoxRowType.PriceRow;
                        break;
                    case BidRowID:
                        m_Row[row] = new BoxRow(nCols, 1, xLocOffset, false, ColorPalette.BackSigned);
                        m_Row[row].RowType = ClusterEventArgs.BoxRowType.BidQtyRow;
                        break;
                    case AskRowID:
                        m_Row[row] = new BoxRow(nCols, 1, xLocOffset, false, ColorPalette.BackSigned);
                        m_Row[row].RowType = ClusterEventArgs.BoxRowType.AskQtyRow;
                        break;
                    case FillsRowID:
                        m_Row[row] = new BoxRow(nCols, 1, xLocOffset, false, ColorPalette.BackSigned);
                        m_Row[row].RowType = ClusterEventArgs.BoxRowType.FillsRow;
                        break;
                    case OrderRowID:
                        //m_Row[row] = new BoxRow(nCols, 1, xLocOffset, false, ColorPalette.ForeSigned);
                        m_Row[row] = new BoxRow(nCols, 1, xLocOffset, false, ColorPalette.TextSigned);
                        m_Row[row].RowType = ClusterEventArgs.BoxRowType.OrderRow;
                        break;
                    default:
                        m_Row[row] = new BoxRow(nCols, 1, xLocOffset, false, ColorPalette.BackSigned);
                        break;
                }//row switch
            }// row

        }//InitializeBoxRowControl().
        */
        //
        //
        // *********************************************************
        // ****                 Initialize Layout               ****
        // *********************************************************
        /// <summary>
        /// Places and paints all the controls in the Cluster.  
        /// This also creates the BoxRows.
        /// </summary>
        private void InitializeLayout()
        {
            this.SuspendLayout();
            int xPosition = 0;
            int yPosition = 0;
            int maxWidth = 0;

            //
            // Place down the header
            //
            m_Header.Location = new System.Drawing.Point(0, 0);
            yPosition += m_Header.Height;
            this.Controls.Add(m_Header);


            // Layout the BoxRow objects - in future, this will be one control to place.
            for (int row = 0; row < m_Row.Length; ++row)
            {
                m_Row[row].Location = new System.Drawing.Point(xPosition, yPosition);
                yPosition += m_Row[row].Height - 1;								// increment drawing position.
                if (m_Row[row].Width + xPosition > maxWidth) maxWidth = m_Row[row].Width + xPosition;	// keep track of widest row.
                this.Controls.Add(m_Row[row]);
            }//next row            

            //
            // Add lower controls
            //
            if (m_LowerControl != null)
            {
                m_LowerControl.Location = new System.Drawing.Point(0, yPosition);
                m_LowerControl.Size = new System.Drawing.Size(maxWidth - 1, m_LowerControl.Height); // stretch control width
                yPosition += m_LowerControl.Height + 1; // tack in its height.
                this.Controls.Add(m_LowerControl);
                if (m_LowerControl.Width > maxWidth) maxWidth = m_LowerControl.Width;
            }


            //
            // Resize myself and other components
            //
            maxWidth = Math.Max(m_Header.MinimumSize.Width, maxWidth);
            m_Header.Size = new System.Drawing.Size(maxWidth, m_Header.Height); // stretch header width

            this.ClientSize = new System.Drawing.Size(maxWidth, yPosition + 1); // cluster size
            this.Size = new System.Drawing.Size(this.ClientSize.Width, this.ClientSize.Height);
            this.ResumeLayout(false);
        }//InitializeLayout()
        //
        //       
        // *****************************************************
        // ****         Register Mouse Events               ****
        // *****************************************************
        /// <summary>
        /// Tells each (or any) BoxRow objects to send mouse-click events they receive
        /// to this Cluster, which in turn sends them to the StrategyHub associated 
        /// with this Cluster.  
        /// The BoxRow will call "BoxNumeric_Click()" event handler.
        /// </summary>
        private void RegisterMouseEvents()
        {
            for (int rowID = 0; rowID < m_Row.Length; ++rowID)
                m_Row[rowID].RegisterMouseEvents(this, rowID);
        }// RegisterMouseEvents()
        //
        //
        #endregion//Constructors

        #region Properties
        public ContextMenuStrip ClusterContextMenuStrip
        {
            get { return this.contextMenuStrip1;  }
        }
        #endregion

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        // ****             AcceptNewDisplay                **** 
        //
        /// <summary>
        /// Clusters are typically created by the strategies (EngineContainers) that
        /// they are meant to display information about.  However, the object that actually 
        /// does the work of updating/repainting them may be a different object entirely, like
        /// a "display hub".
        /// The creating strategy hub passes the cluster to the display hub, and the display hub 
        /// calls this method to tell the cluster it now has a new owner.
        /// </summary>
        /// <param name="display"></param>
        public void AcceptNewParentDisplay(ClusterDisplay display)
        {
            ParentDisplay = display;
            Log = ParentDisplay.ParentHub.Log;
        }
        //
        //  ****                RegenerateNow()             ****
        // 
        /// <summary>
        /// If any regeneration is needed, we loop thru each boxRow, checking whether
        /// that row in particular needs regenerating, if so we call its updateValue() method.
        /// By "regenerating", I mean that the memory value has been changed by the hub, but
        /// the displayed value needs updating by the windows thread.
        /// Note: This method must be called by the windows thread!
        /// </summary>
        public void RegenerateNow()
        {
            // Renegerate market depth of cluster.
            if (this.IsRegenerateNeeded)
            {
                for (int i = 0; i < m_BoxRowRows; ++i)
                {
                    if (m_BoxRowIsRegenerationNeeded[i])
                    {
                        m_Row[i].UpdateValue(m_Memory[i]);
                        m_Row[i].UpdateHiLite(m_HiLiteMemory[i]);
                    }
                }
                this.IsRegenerateNeeded = false;
            }

            // Regenerate header and its popups.
            if (m_Header.IsRegenerateRequired)
            {
                m_Header.RegenerateNow();
            }
            if (m_LowerControl.IsRegenerationRequired)
            {
                m_LowerControl.RegenerateNow();
            }
        }// RegenerateNow().
        //
        //
        //
        //
        //
        //
        // ****					ToString()					****
        //
        public override string ToString()
        {
            return string.Format("{0} ID={1}", m_Name, m_EngineContainerID.ToString());
        }
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
        // ****         Regenerate Memory               ****
        //
        /// <summary>
        /// Given a market depth from an "MarketChange" Engine Event, we recreate the market
        /// here.
        /// If something has changed, we set IsRegenRequired = true, so the windows thread
        /// can be invoked to repaint the controls.
        /// TODO: This needs major efficiency improvements.
        /// </summary>
        private void RegenerateMemory(double[] bidP, double[] askP, int[] bidQ, int[] askQ, double[] hiLitePrices)
        {
            double x;                                               // dummy var
            if (m_Memory[0].Length < 1) return;
            if (!m_IsMouseOverClickableBox)			                // when there's a mouse over, hold position.
            {
                //
                // Determine price center location.
                //           
                double leftBaseValue = bidP[0];                     // TODO: determine more sophisticated method!
                x = leftBaseValue / m_MinTickSize;
                int newLeftBaseIndex = (int)Math.Round(x);          // integerized tick-value of price to be new left-base.
                int newRightBaseIndex = (int)Math.Round(askP[0] / m_MinTickSize);
                int minShown = 2;		                            // minimum number of levels to keep displayed on screen.

                int locBid = newLeftBaseIndex - m_CurrentLeftBaseIndex + m_LeftBaseLoc;
                int locAsk = newRightBaseIndex - m_CurrentLeftBaseIndex + m_LeftBaseLoc;
                if ((locBid < minShown - 1) || (locAsk > (m_Configuration.BoxRowColumns - minShown)))
                {	// Market has moved to edge of screen.
                    m_CurrentLeftBaseIndex = newLeftBaseIndex;      // save this new value.
                    for (int i = 0; i < m_Configuration.BoxRowColumns; i++) { m_Memory[PriceRowID][i] = newLeftBaseIndex + (i - m_LeftBaseLoc); }
                    m_BoxRowIsRegenerationNeeded[PriceRowID] = true;
                }
            }
            // Determine market price offsets. TODO: make this more efficient!
            //
            // Bid and ask quantities
            //
            for (int i = 0; i < m_Memory[BidRowID].Length; ++i)
            {
                m_Memory[BidRowID][i] = 0;
                m_Memory[AskRowID][i] = 0;
            }
            for (int level = 0; level < bidP.Length; ++level)
            {   // compute the index offset.
                x = bidP[level] / m_MinTickSize;
                int pIndex = ((int)Math.Round(x)) - m_CurrentLeftBaseIndex + m_LeftBaseLoc;
                if (pIndex < 0 || pIndex >= m_Memory[BidRowID].Length) break;
                m_Memory[BidRowID][pIndex] = bidQ[level];
            }
            m_BoxRowIsRegenerationNeeded[BidRowID] = true;
            for (int level = 0; level < askP.Length; ++level)
            {   // compute the index offset.
                x = askP[level] / m_MinTickSize;
                int pIndex = ((int)Math.Round(x)) - m_CurrentLeftBaseIndex + m_LeftBaseLoc;
                if (pIndex < 0 || pIndex >= m_Memory[AskRowID].Length) break;
                m_Memory[AskRowID][pIndex] = -askQ[level];
            }
            m_BoxRowIsRegenerationNeeded[AskRowID] = true;

            //
            // Hi Lites - for price row
            //
            int[] mask = new int[] { 1, 2, 4, 8 };
            if (hiLitePrices != null)
            {
                for (int i = 0; i < hiLitePrices.Length; ++i)
                {
                    int hiLite0;
                    x = hiLitePrices[i] / m_MinTickSize;
                    if (double.IsInfinity(x))
                        hiLite0 = -1;       // mark hiLite as out-of-bounds
                    else
                        hiLite0 = ((int)Math.Round(x) - m_CurrentLeftBaseIndex + m_LeftBaseLoc);    // hiLite location.
                    if (hiLite0 < 0 || hiLite0 >= m_HiLiteMemory[PriceRowID].Length)
                        hiLite0 = -1;       // mark hiLite as out-of-bounds
                    // Now update the location of the HiLite, if needed.    
                    if (hiLite0 != m_PrevHiLite[i])
                    {   // The highlight has changed since last update!
                        if (m_PrevHiLite[i] != -1)
                        {   // turn off the ith bit in prevHiLite location.
                            //m_HiLiteMemory[PriceRowID][m_PrevHiLite[i]] = 0;    // clear out previous hightlight memory
                            int a = ~mask[i];
                            int b = m_HiLiteMemory[PriceRowID][m_PrevHiLite[i]];
                            int newValue = b & a;
                            m_HiLiteMemory[PriceRowID][m_PrevHiLite[i]] = newValue;
                        }
                        if (hiLite0 != -1)
                        {   // Turn on the ith bit in hiLite location.
                            int newValue = m_HiLiteMemory[PriceRowID][hiLite0] | mask[i];
                            m_HiLiteMemory[PriceRowID][hiLite0] = newValue;              // set hightlight memory                    
                        }
                        m_PrevHiLite[i] = hiLite0;                              // save this value for next update comparison.
                    }
                }//for each hiLite type.
            }

            this.IsRegenerateNeeded = true;
        }//end RegenerateMemory().
        //
        //
        // *************************************************
        // ****         Regenerate Memory               ****
        // *************************************************
        /// <summary>
        /// Main entry point for Cluster events. 
        /// Cluster events can come from any engine.  But when it comes from the pricing engine
        /// it could shift the entire cluster.
        /// </summary>
        private void RegenerateMemory(EngineEventArgs eArgs)
        {
            // Determine BoxRow:
            int rowID = 0;
            if (eArgs.EngineID == m_PricingEngineID)
            {
                int prevBasePriceIndex = m_CurrentLeftBaseIndex;            // remember where bid-price is located.
                RegenerateMemory(eArgs.DataB, eArgs.DataA, eArgs.DataIntB, eArgs.DataIntA, eArgs.DataC);
                if (m_CurrentLeftBaseIndex != prevBasePriceIndex)           
                {   // This means the price location has shifted - must realign all rows.
                    for (int i = 0; i < m_Memory.Length; ++i)
                        if (!(i == PriceRowID || i == BidRowID || i == AskRowID)) RegenerateBoxRow(i);
                }
                return;
            }
            else if (eArgs.EngineID == m_FillEngineID)
                rowID = FillsRowID;
            else if (eArgs.EngineID == m_OrderEngineID)
                rowID = OrderRowID;
            else
                return;
            if (rowID < 0 || rowID >= m_Memory.Length) { return; }  // unknown row ID.
            // Store the data.
            m_RawDataP[rowID] = eArgs.DataA;
            m_RawDataQ[rowID] = eArgs.DataIntA;
            RegenerateBoxRow(rowID);

        }//RegenerateMemory().  
        //
        //
        // 
        /// <summary>
        /// Updates the BoxRow.
        /// </summary>
        /// <param name="rowID">RowID of BoxRow to update.</param>
        private void RegenerateBoxRow(int rowID)
        {
            for (int i = 0; i < m_Memory[rowID].Length; ++i) { m_Memory[rowID][i] = 0; }    // zero-out old results.
            if (m_RawDataP[rowID] == null || m_RawDataP[rowID].Length == 0)
            {
                m_BoxRowIsRegenerationNeeded[rowID] = true;
                return;         // no data for this row, done!
            }
            int nFills = m_RawDataP[rowID].Length;
            int maxColID = m_Memory[rowID].Length - 1;        // largest column index.
            for (int fillID = 0; fillID < nFills; ++fillID)
            {
                int offset = 0;
                try
                {
                    offset = Convert.ToInt32(Math.Round(m_RawDataP[rowID][fillID] / m_MinTickSize));
                }
                catch
                {
                }
                int loc = m_LeftBaseLoc + offset - m_CurrentLeftBaseIndex;
                if (loc < 0)
                    m_Memory[rowID][0] += m_RawDataQ[rowID][fillID];        // left of leftmost price.
                else if (loc > maxColID)
                    m_Memory[rowID][maxColID] += m_RawDataQ[rowID][fillID]; // right of rightmost price.
                else
                    m_Memory[rowID][loc] += m_RawDataQ[rowID][fillID];      // inside price row.
            }
            m_BoxRowIsRegenerationNeeded[rowID] = true;

        }// RegenerateBoxRow().
        //
        //
        //
        #endregion//Private Methods

        #region Cluster Mouse Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //

        //
        //
        // ****             BoxNumeric_Click()              ****
        //
        /// <summary>
        /// The event handler for whenever the user Clicks on a Box that is 
        /// owned by this Cluster.  
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        public void BoxNumeric_Click(object sender, EventArgs eventArgs)
        {
            if (eventArgs.GetType() == typeof(ClusterEventArgs))
            {
                ClusterEventArgs clusterArgs = (ClusterEventArgs)eventArgs;
                // Store information needed to process this event.
                clusterArgs.BoxPriceValue = m_Memory[PriceRowID][clusterArgs.BoxID]; // price displayed in this same column.
                int engineID = -1;
                switch (clusterArgs.RowID)          // convert RowID# to EngineID#.
                {
                    case FillsRowID: engineID = m_FillEngineID; break;
                    case PriceRowID: engineID = m_PricingEngineID; break;
                    case BidRowID: engineID = m_PricingEngineID; break;
                    case AskRowID: engineID = m_PricingEngineID; break;
                    case OrderRowID: engineID = m_OrderEngineID; break;
                    default: break;
                }
                clusterArgs.ClusterEngineID = engineID;                     // engine associate with this row.
                clusterArgs.KeyPressed = (Keys)ParentDisplay.m_KeyPressed;
                clusterArgs.RowType = m_Row[clusterArgs.RowID].RowType;
                //if (ParentDisplay.m_KeyPressed != Keys.None)
                //{
                //    int ii = 0;
                //}
                ParentDisplay.AssocEngineHub.HubEventEnqueue(clusterArgs);  // Push this event onto StrategyHub queue.
            }
            else
            {   // I don't recognize this type of event arg.
                Log.NewEntry(LogLevel.Warning, "Cluster {0}: Unknown BoxNumeric_Click EventArg Type={1}. Ignored.", this.Name, eventArgs.GetType().Name);
            }
        }// BoxNumeric_Click().
        //
        //
        // ****             BoxNumeric_MouseEnter()         ****
        //
        /// <summary>
        /// For some BoxRow objects, the Cluster wants to know when they are moused-over.
        /// For example, when the mouse is over a BoxRow, we will not shift the prices, so that
        /// the user doesn't have price levels shifting beneath his mouse as he is clicking somewhere.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        public void BoxNumeric_MouseEnter(object sender, EventArgs eventArgs)
        {
            m_IsMouseOverClickableBox = true;
        }
        //
        //
        // ****             BoxNumeric_MouseEnter()         ****
        //
        public void BoxNumeric_MouseLeave(object sender, EventArgs eventArgs)
        {
            m_IsMouseOverClickableBox = false;
        }
        //
        //
        //
        //
        //

        //
        //
        #endregion//Event Handlers

        #region IEngineContainer implementation
        // *************************************************************
        // ****                 Engine Implementation               ****
        // *************************************************************
        //
        //
        // ****         IEngineContainer implementation         ****
        //
        public int EngineContainerID { get { return m_EngineContainerID; } set { m_EngineContainerID = value; } }
        public string EngineContainerName { get { return m_Name; } }
        public ClusterConfiguration ClusterConfiguation { get { return m_Configuration; } }
        public List<IEngine> GetEngines() { return m_Header.GetEngines(); }  // TODO: here can go my rows in the future.
        public Cluster GetCluster() { return this; }
        public PopUps.EngineControl GetControl() { return null; }
        //
        //
        // ****                 Process Engine Event            ****
        //
        /// <summary>
        /// Implements IEngineContainer.  This is called by my hub thread when it receives an 
        /// EngineEvent.  The engine event will be passed to the correct engines.
        /// </summary>
        /// <param name="eventArgs"></param>
        public bool ProcessEvent(EventArgs eventArgs)
        {
            bool regenerateNow = false;
            if (eventArgs is EngineEventArgs)
            {
                EngineEventArgs eArgs = (EngineEventArgs)eventArgs;
                EngineEventArgs.EventType eventType = eArgs.MsgType;

                if (eventType == EngineEventArgs.EventType.ClusterUpdate)
                    RegenerateMemory(eArgs);		                        // I intercept events for Cluster Updates!
                else
                {	// Other egine events, I will pass along to objects owned by this Cluster.
                    regenerateNow = m_Header.ProcessEngineEvent(eArgs) || regenerateNow;// Header holds all popup forms.
                    if (m_LowerControl != null)                             // Gui panels displayed below cluster.
                        regenerateNow = m_LowerControl.ProcessEngineEvent(eArgs) || regenerateNow;  
                }
            }

            return regenerateNow;       
        }//ProcessEngineEvent
        //
        //
        //
        //
        //
        #endregion

        #region Form events
        //
        //
        private void toolStripTextBoxTickSize_KeyUp(object sender, KeyEventArgs e)
        {
            if ((sender is ToolStripTextBox) == false)
                return;
            ToolStripTextBox tb = (ToolStripTextBox)sender;       // sender of this event is text box
            if (e.KeyCode == Keys.Enter)
            {   // "enter key" was pressed!
                // Generate a parameter change request for our associated engine.
                string newValueStr = tb.Text;
                try
                {
                    // Test the format!
                    double newValue = Convert.ToDouble(tb.Text);
                    m_MinTickSize = newValue;
                    m_Row[PriceRowID].MinTickSize = m_MinTickSize;
                    for (int i = 0; i < m_BoxRowIsRegenerationNeeded.Length; ++i )
                        m_BoxRowIsRegenerationNeeded[i] = true;                    
                }
                catch (Exception)
                {   // Failed restore old value
                    tb.Text = this.m_MinTickSize.ToString();
                }
                tb.Text = this.m_MinTickSize.ToString();
            }
            else if (e.KeyCode == Keys.Escape)
            {   // "escape key"
                tb.Text = this.m_MinTickSize.ToString();
            }
            else
            {   // the key pressed was NOT the enter key.
                //if (!IsUserInputActive)
                //{   // first time we started typing here.
                //    // tb.Clear();
                //    IsUserInputActive = true;   // flag that user is typing.
                //    tb.ForeColor = TextColorHiLite;
                //}
            }

        }
        #endregion


    }// end class
}
