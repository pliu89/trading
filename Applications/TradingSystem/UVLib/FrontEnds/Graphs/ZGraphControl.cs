using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UV.Lib.FrontEnds.Graphs
{
    using ZedGraph;
    using UV.Lib.Engines;
    using UV.Lib.FrontEnds.PopUps;
    using UV.Lib.FrontEnds.GuiTemplates;


    public partial class ZGraphControl : UserControl, IEngine, IEngineControl
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Engine control
        //public List<Engine.ParameterInfo> m_ParamInfoList = null;  // parameter information
        //private List<ParamControlBase> m_ParamControl = new List<ParamControlBase>(); // list of parameter sub-controls
        private int m_EngineID = -1;                                // my id
        private string m_EngineName = string.Empty;                 // my name
        private bool m_IsUpdateRequired = false;
        

        private bool m_LoadedHistory = false;
        private int m_GraphPaintCount = 0;

        //
        // Zed Graph
        //
        //public ZedGraph.ZedGraphControl zg1 = null;
        private int m_CurrentZedGraphID = 0;
        private bool m_IsGraphsInitialized = false;
        private object GraphListLock = new object();
        private List<int> m_ZedGraphIDRequested = new List<int>(new int[] { 0 });
        private Dictionary<int, ZedGraph.ZedGraphControl> m_GraphList = new Dictionary<int, ZedGraph.ZedGraphControl>();
        private int m_LeftMargin = 0;					// space to left of zgraph.
        private int m_TopMargin = 0;					// space above zgraph.
        private int m_BottomMargin = 0;					// space below zgraph.
        private List<EventArgs> m_InitialEvents = new List<EventArgs>();	// events stored before zg1 initialized.

        private Color[] m_DefaultColors = new Color[] { Color.Black, Color.Blue, Color.Red, Color.Green, Color.SandyBrown };


        private EventHandler m_RegenerateDelegate = null;
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public ZGraphControl(int engineID, List<ParameterInfo> pInfoList)
        {
            m_EngineID = engineID;
            //m_ParamInfoList = pInfoList;
            InitializeComponent();

            m_RegenerateDelegate = new EventHandler(this.Regenerate);
        }
        public ZGraphControl(EngineGui engineGui)
        {
            m_EngineID = engineGui.EngineID;
            InitializeComponent();

            m_RegenerateDelegate = new EventHandler(this.Regenerate);
        }

        public ZGraphControl() { InitializeComponent(); }
        //
        //
        #endregion//Constructors


        #region no Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        //
        // ****				ShowMe()			****
        //
        /// <summary>
        /// Currently, this is necessary to resolve two issues.
        /// First, the zed graph control, zg1 below, must be created by this windows
        /// thread that will repaint it.
        /// Secondly, I want a zed graph to have different form-border properties than
        /// other pop-up forms.  On being openned for the first time, this method will 
        /// change the properties of its parent form.
        /// </summary>
        /// <param name="parentControl"></param>
        public void ShowMe(Form parentControl)
        {
            if (!m_IsGraphsInitialized)
            {	// On first time opening...
                /*
                lock (GraphListLock)
                {				
                    //
                    // Create a new ZedGraphControl - always create this default one immediately.
                    //
                    ZedGraphControl zg = new ZedGraphControl();
                    m_GraphList.Add(0, zg);
                    ConstructZedGraphControl(zg);
                    InitializeZedGraphControl(zg);
                    zg.Anchor = AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;


                    for (int i = 1; i < m_MaxZedGraphID; i++)
                    {
                        // make second zg
                        zg = new ZedGraphControl();
                        m_GraphList.Add(i, zg);
                        ConstructZedGraphControl(zg);
                        InitializeZedGraphControl(zg);
                        zg.Anchor = AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                        zg.Visible = false;
                    }
	
                }// GraphListLock
                */
                AddGraph(this, EventArgs.Empty);

                //
                // Update parent form style
                //
                parentControl.FormBorderStyle = FormBorderStyle.Sizable;
                parentControl.TopMost = false;
                this.Anchor = AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                // Set that we are initialized!
                m_IsGraphsInitialized = true;	// this variable is accessed across threads!
            }
            //ProcessEvent(EventArgs.Empty);
            m_IsUpdateRequired = true;
        }// ShowMe()
        //
        //
        private void AddGraph(object s, EventArgs e)
        {
            if (this.InvokeRequired)
                this.Invoke(new EventHandler(AddGraph), e);
            else
            {
                lock (GraphListLock)
                {
                    foreach (int id in m_ZedGraphIDRequested)
                    {
                        // make new zg
                        if (!m_GraphList.ContainsKey(id))
                        {
                            ZedGraphControl zg = new ZedGraphControl();
                            m_GraphList.Add(id, zg);
                            ConstructZedGraphControl(zg);
                            InitializeZedGraphControl(zg);
                            zg.Anchor = AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                            if (m_GraphList.Count == 1)
                                zg.Visible = true;	            // first graph must be visible by default.
                            else 
                                zg.Visible = false;
                        }
                    }
                    m_ZedGraphIDRequested.Clear();
                }
                ProcessEvent(e);
            }
        }//end AddGraph()
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
        // ****					Initialize ZG()					****
        //
        /// <summary>
        /// This is called by the windows thread that will display this
        /// control.
        /// </summary>
        private void ConstructZedGraphControl(ZedGraphControl zg1)
        {

            //this.zg1 = new ZedGraph.ZedGraphControl();
            //ZedGraph.ZedGraphControl zg1 = new ZedGraphControl();

            this.SuspendLayout();
            // 
            // zg1
            //
            zg1.EditButtons = System.Windows.Forms.MouseButtons.Left;
            zg1.EditModifierKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.None)));
            zg1.IsAutoScrollRange = false;
            zg1.IsEnableHEdit = false;
            zg1.IsEnableHPan = true;
            zg1.IsEnableHZoom = true;
            zg1.IsEnableVEdit = false;
            zg1.IsEnableVPan = true;
            zg1.IsEnableVZoom = true;
            zg1.IsPrintFillPage = true;
            zg1.IsPrintKeepAspectRatio = true;
            zg1.IsScrollY2 = false;
            zg1.IsShowContextMenu = true;
            zg1.IsShowCopyMessage = true;
            zg1.IsShowCursorValues = false;
            zg1.IsShowHScrollBar = false;
            zg1.IsShowPointValues = false;
            zg1.IsShowVScrollBar = false;
            zg1.IsSynchronizeXAxes = false;
            zg1.IsSynchronizeYAxes = false;
            zg1.IsZoomOnMouseCenter = false;
            zg1.LinkButtons = System.Windows.Forms.MouseButtons.Left;
            zg1.LinkModifierKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.None)));
            //
            zg1.Location = new System.Drawing.Point(m_LeftMargin, m_TopMargin);
            //zg1.Size = new System.Drawing.Size(499, 333);
            zg1.Size = new System.Drawing.Size(this.ClientSize.Width - 2 * m_LeftMargin, this.ClientSize.Height - m_TopMargin - m_BottomMargin);


            zg1.Name = "zg1";
            zg1.PanButtons = System.Windows.Forms.MouseButtons.Left;
            zg1.PanButtons2 = System.Windows.Forms.MouseButtons.Middle;
            zg1.PanModifierKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.None)));
            zg1.PanModifierKeys2 = System.Windows.Forms.Keys.None;
            zg1.PointDateFormat = "g";
            zg1.PointValueFormat = "G";
            zg1.ScrollMaxX = 0;
            zg1.ScrollMaxY = 0;
            zg1.ScrollMaxY2 = 0;
            zg1.ScrollMinX = 0;
            zg1.ScrollMinY = 0;
            zg1.ScrollMinY2 = 0;
            zg1.TabIndex = 0;
            zg1.ZoomButtons = System.Windows.Forms.MouseButtons.Left;
            zg1.ZoomButtons2 = System.Windows.Forms.MouseButtons.None;
            zg1.ZoomModifierKeys = System.Windows.Forms.Keys.None;
            zg1.ZoomModifierKeys2 = System.Windows.Forms.Keys.None;
            zg1.ZoomStepFraction = 0.1;

            this.Controls.Add(zg1);
            this.ResumeLayout();
        }//ConstructZedGraphControl
        //
        //       
        //
        //
        // ****					InitializeZedGraphControl				****
        //
        /// <summary>
        /// After the zed graph is created, we can initialize its look here.
        /// </summary>
        private void InitializeZedGraphControl(ZedGraphControl zg1)
        {
            // Get a reference to the GraphPane instance in the ZedGraphControl
            GraphPane myPane = zg1.GraphPane;
            // Set the titles and axis labels
            myPane.Title.Text = "";
            myPane.XAxis.Title.Text = "Time (hrs)";
            myPane.YAxis.Title.Text = "";
            myPane.Y2Axis.Title.Text = "";

            myPane.Legend.IsVisible = false;
            /*
            // Make up some data points based on the Sine function
            PointPairList list = new PointPairList();
            PointPairList list2 = new PointPairList();
            //for (int i = 0; i < 36; i++)
            //{
            //    double x = (double)i * 5.0;
            //    double y = Math.Sin((double)i * Math.PI / 15.0) * 16.0;
            //    double y2 = y * 13.5;
            //    list.Add(x, y);
            //    list2.Add(x, y2);
            //}

            LineItem myCurve = myPane.AddCurve("Alpha", list, Color.Red, SymbolType.Diamond);	// Generate a curve
            myCurve.Symbol.Fill = new Fill(Color.White);            // Fill the symbols with white

            myCurve = myPane.AddCurve("Beta", list2, Color.Blue, SymbolType.Circle);
            myCurve.Symbol.Fill = new Fill(Color.White);
            myCurve.IsY2Axis = true;								// Associate this curve with the Y2 axis
            */

            myPane.XAxis.MajorGrid.IsVisible = true;				// Show the x axis grid

            // Y-axis display
            myPane.YAxis.Scale.FontSpec.FontColor = Color.Black;		// Make the Y axis scale red
            myPane.YAxis.Title.FontSpec.FontColor = Color.Black;
            myPane.YAxis.MajorTic.IsOpposite = true;	// turn off the opposite tics so the Y tics don't show up on the Y2 axis
            myPane.YAxis.MinorTic.IsOpposite = true;
            myPane.YAxis.Scale.MaxAuto = true;
            myPane.YAxis.Scale.MinAuto = true;

            myPane.YAxis.MajorGrid.IsZeroLine = false;	// Don't display the Y zero line
            myPane.YAxis.Scale.Align = AlignP.Inside;	// Align the Y axis labels so they are flush to the axis
            //myPane.YAxis.Scale.Min = -30;				// Manually set the axis range
            //myPane.YAxis.Scale.Max = 30;

            /*
            // Y2-axis display
            myPane.Y2Axis.IsVisible = true;
            myPane.Y2Axis.Scale.FontSpec.FontColor = Color.Blue;    // Make the Y2 axis scale blue
            myPane.Y2Axis.Title.FontSpec.FontColor = Color.Blue;
            myPane.Y2Axis.MajorTic.IsOpposite = false;				// turn off the opposite tics so the Y2 tics don't show up on the Y axis
            myPane.Y2Axis.MinorTic.IsOpposite = false;
            myPane.Y2Axis.MajorGrid.IsZeroLine = false;
            myPane.Y2Axis.MajorGrid.IsVisible = true;				// Display the Y2 axis grid lines
            myPane.Y2Axis.Scale.Align = AlignP.Inside;				// Align the Y2 axis labels so they are flush to the axis
            */

            myPane.Chart.Fill = new Fill(Color.White, Color.LightGray, 45.0f);	// Fill the axis background with a gradient

            // Add a text box with instructions
            //TextObj text = new TextObj(
            //	"Zoom: left mouse & drag\nPan: middle mouse & drag\nContext Menu: right mouse",
            //	0.02f, 0.98f, CoordType.ChartFraction, AlignH.Left, AlignV.Bottom);
            //text.FontSpec.StringAlignment = StringAlignment.Near;
            //myPane.GraphObjList.Add(text);

            // Enable scrollbars if needed
            zg1.IsShowHScrollBar = true;
            zg1.IsShowVScrollBar = true;
            zg1.IsAutoScrollRange = true;
            //zg1.IsScrollY2 = true;

            // OPTIONAL: Show tooltips when the mouse hovers over a point
            zg1.IsShowPointValues = false;          // disable on Jan 2 2014 - to avoid illegal operation in some list when first openning
            zg1.PointValueEvent += new ZedGraphControl.PointValueHandler(ZedGraph_PointValueHandler);

            // OPTIONAL: Add a custom context menu item
            zg1.ContextMenuBuilder += new ZedGraphControl.ContextMenuBuilderEventHandler(
                            ZedGraph_ContextMenuBuilder);

            // OPTIONAL: Handle the Zoom Event
            zg1.ZoomEvent += new ZedGraphControl.ZoomEventHandler(ZedGraph_ZoomEvent);

            // Size the control to fit the window
            SetSize();




            // Tell ZedGraph to calculate the axis ranges
            // Note that you MUST call this after enabling IsAutoScrollRange, since AxisChange() sets
            // up the proper scrolling parameters
            zg1.AxisChange();
            zg1.Invalidate();	// Make sure the Graph gets redrawn
        }// InitializeZedGraphControl().
        //
        // ****				SetSize()				****
        private void SetSize()
        {
            //zg1.Location = new Point(10, 10);// Leave a small margin around the outside of the control            
            //zg1.Size = new Size(this.ClientRectangle.Width - 20,this.ClientRectangle.Height - 20);
        }
        //
        //
        //
        //
        // ****             Update Curve Definition                 ****
        //
        private void UpdateCurveDefinition(ZedGraphControl zg1, CurveDefinition zNewCurve)
        {
            CurveItem curve = zg1.GraphPane.CurveList[zNewCurve.CurveName];
            if (curve == null)
            {	// curve name does not yet exist.  User wants us to create a new one.				
                if (zNewCurve.Type == CurveDefinition.CurveType.Curve)
                    curve = zg1.GraphPane.AddCurve(zNewCurve.CurveName, new PointPairList(), zNewCurve.CurveColor, zNewCurve.Symbol);
                //else if (zNewCurve.Type == CurveDefinition.CurveType.Bar)
                //	curve = zg1.GraphPane.AddBar(zNewCurve.CurveName, new PointPairList(), zNewCurve.CurveColor, zNewCurve.Symbol);
                else
                    return;			// failed to create new curve
            }
            // Update properties of found curve.
            if (zNewCurve.Type == CurveDefinition.CurveType.Curve)
            {
                LineItem li = (LineItem)curve;
                li.Line.Style = zNewCurve.DashStyle;
                li.Line.Width = zNewCurve.CurveWidth;
                li.Symbol.Type = zNewCurve.Symbol;
                li.Symbol.Fill = new Fill(zNewCurve.SymbolFillColor);
                li.Symbol.Size = zNewCurve.SymbolSize;
                li.Line.IsVisible = zNewCurve.IsLineVisible;
                curve.Color = zNewCurve.CurveColor;
                if (!String.IsNullOrEmpty(zNewCurve.GraphName))
                    zg1.Name = zNewCurve.GraphName;
            }
            else if (zNewCurve.Type == CurveDefinition.CurveType.Bar)
            {
                // TODO: implement bars
                if (!String.IsNullOrEmpty(zNewCurve.GraphName))
                    zg1.Name = zNewCurve.GraphName;
            }
        }//UpdateCurveDefintion()
        //
        //
        //		
        //
        //
        #endregion//Private Methods


        #region IEngine implementation
        // *****************************************************************************
        // ****                         IEngine Implementation                      ****
        // *****************************************************************************
        //
        public int EngineID { get { return m_EngineID; } }
        public string EngineName { get { return m_EngineName; } }
        public bool IsReady { get { return true; } }
        //public string ToLongString() { return ToString(); }
        //public IEngineControl GetControl() { return null; }
        //public Huds.HudPanel GetHudPanel() { return null; }
        public bool IsUpdateRequired { get { return m_IsUpdateRequired; } set { m_IsUpdateRequired = value; } }
        //
        public void SetupComplete()
        {

        }// InitializeEngine()        
        //
        //
        // ****          Process Event           ****
        //
        public void ProcessEvent(EventArgs e)
        {
            // Initialize and validate
            if (!m_IsGraphsInitialized)
            {	// Graphs not initialized yet. Just store these events.
                m_InitialEvents.Add(e);
                return;
            }
            else if (m_InitialEvents.Count > 0)
            {   // The graphs have just been initialized, so 
                // lets extract all the previously stored initial events.
                List<EventArgs> events = new List<EventArgs>();
                events.AddRange(m_InitialEvents);		// add already acrued events.
                m_InitialEvents.Clear();                // clearing this signals we are ready for normal operation.
                events.Add(e);							// add the latest event also.
                foreach (EventArgs anEvent in events) { ProcessEvent(anEvent); }
            }
            if (e.GetType() != typeof(EngineEventArgs))
                return;
            //
            // Process Engine Events
            //
            EngineEventArgs eArgs = (EngineEventArgs)e;
            EngineEventArgs.EventType eventType = eArgs.MsgType;
            EngineEventArgs.EventStatus eventStatus = eArgs.Status;
            if (eventStatus != EngineEventArgs.EventStatus.Confirm)
                return;                                 // we only process confirmations.

            if (eArgs.DataObjectList != null)
            {
                foreach (object o in eArgs.DataObjectList)
                {
                    // *****************************************
                    // ****		Curve Definitiion List		****
                    // *****************************************
                    Type dataType = o.GetType();
                    //if (dataType == typeof(List<CurveDefinition>))
                    if (dataType == typeof(CurveDefinitionList))
                    {   // Given a list of curve definitions, update pre-defined curves, 
                        // or create new curves.  These must be created by the windows thread.
                        // These are usually only received at the start of the run when all parameters are broadcasted.                        
                        List<CurveDefinition> list = ((CurveDefinitionList)o).CurveDefinitions;
                        Dictionary<int, EngineEventArgs> requestedGraphRequests = new Dictionary<int, EngineEventArgs>(); // place to put CurveDefs for not yet created graphs.                        
                        foreach (CurveDefinition cDef in list)
                        {
                            ZedGraphControl zg1;
                            lock (GraphListLock)
                            {
                                if (!m_GraphList.TryGetValue(cDef.GraphID, out zg1))
                                {	// Since we can't find this GraphID, request it to be created below.                                    
                                    m_ZedGraphIDRequested.Add(cDef.GraphID);
                                    zg1 = null;         // this signals that graph doesn't exit.
                                }
                            }
                            if (zg1 == null && !requestedGraphRequests.ContainsKey(cDef.GraphID))
                            {	// We couldn't find GraphID, so request it to be created it now!
                                // Also make sure we only request it once thru this loop, just to save time.
                                AddGraph(this, eArgs);	                            // Asynch call will call ProcessEvent() again.
                                EngineEventArgs newEvent = new EngineEventArgs();   // Event we will restack until after graph exists.
                                newEvent.DataObjectList = new List<object>();
                                newEvent.Status = EngineEventArgs.EventStatus.Confirm;
                                requestedGraphRequests.Add(cDef.GraphID, newEvent);// Mark this ID as having just been requested.
                            }
                            if (requestedGraphRequests.ContainsKey(cDef.GraphID))
                            {   // This is a curve for a graph we just requested... push onto wait list.
                                requestedGraphRequests[cDef.GraphID].DataObjectList.Add(cDef);
                                continue;           // skip 
                            }

                            UpdateCurveDefinition(zg1, cDef);

                        }//next curveDefn
                        // Push any unfulfilled requests back onto queue.
                        foreach (EngineEventArgs eeArgs in requestedGraphRequests.Values)
                            m_InitialEvents.Add(eeArgs);                       
                    }
                    // *****************************************
                    // ****			ZGraphPoints		    ****
                    // *****************************************
                    else if (dataType == typeof(ZGraphPoints))
                    {   // This is a list of ZGraphPoint objects. This message is from the AllParameter broadcast
                        // at the beginning of the run.  A new ZGraphControl will request all parameter values
                        // only when first created.   Therefore, if we are an old ZGraphControl, it probably is not
                        // for us.
                        if (m_LoadedHistory)
                            return;
                        m_LoadedHistory = true;
                        List<ZGraphPoint> pointList = ((ZGraphPoints)o).Points;
                        foreach (ZGraphPoint zpt in pointList)
                        {
                            ZedGraphControl zg1;
                            if (!m_GraphList.TryGetValue(zpt.GraphID, out zg1))
                            {	// No currently existing graph with this id.							
                                continue;
                            }
                            CurveItem curve = null;
                            if (zg1.GraphPane != null)
                            {
                                curve = zg1.GraphPane.CurveList[zpt.CurveName];
                                if (curve == null)
                                {	// a new curve showed up - create a new curve, and plot this point anyway.
                                    Color newColor = m_DefaultColors[zg1.GraphPane.CurveList.Count % m_DefaultColors.Length];
                                    curve = zg1.GraphPane.AddCurve(zpt.CurveName, new PointPairList(), newColor, SymbolType.None);
                                }
                            }
                            // Add/replace points in the plot.
                            IPointListEdit ip = null;
                            if (curve != null)
                                ip = curve.Points as IPointListEdit;
                            if (ip != null)
                            {
                                if (zpt.IsReplaceAtX)
                                {	// This is a request to replace point with new one.
                                    int foundIndex = -1;
                                    for (int i = 0; i < ip.Count; ++i)	// search for X-value of point to replace
                                        if (ip[i].X == zpt.X)			// very unsafe
                                        {
                                            foundIndex = i;
                                            break;						// stop looking
                                        }
                                    if (foundIndex > -1)
                                        ip[foundIndex].Y = zpt.Y;
                                    else
                                    {
                                        ip.Add(zpt.X, zpt.Y);
                                        // sort?
                                    }

                                }
                                else
                                    ip.Add(zpt.X, zpt.Y);		// simple serial addition of new data point.
                            }
                        }//next zpt
                    }
                    // *****************************************
                    // ****			List<ZGraphPoint>		****
                    // *****************************************
                    /*
                    else if (dataType == typeof(List<ZGraphPoint>))
                    {   // This is a list of ZGraphPoints.  This message is from the AllParameter broadcast
                        // at the beginning of the run.  A new ZGraphControl will request all parameter values
                        // only when first created.   Therefore, if we are an old ZGraphControl, it probably is not
                        // for us.
                        List<ZGraphPoint> pointList = (List<ZGraphPoint>) o;
                        foreach (ZGraphPoint zpt in pointList)
                        {
                            ZedGraphControl zg1;
                            if (!m_GraphList.TryGetValue(zpt.GraphID, out zg1))
                            {	// No currently existing graph with this id.							
                                continue;
                            }
                            CurveItem curve = null;
                            if (zg1.GraphPane != null)
                            {
                                curve = zg1.GraphPane.CurveList[zpt.CurveName];
                                if (curve == null)
                                {	// a new curve showed up - create a new curve, and plot this point anyway.
                                    Color newColor = m_DefaultColors[zg1.GraphPane.CurveList.Count % m_DefaultColors.Length];
                                    curve = zg1.GraphPane.AddCurve(zpt.CurveName, new PointPairList(), newColor, SymbolType.None);
                                }
                            }
                            // Add/replace points in the plot.
                            IPointListEdit ip = null;
                            if (curve != null)
                                ip = curve.Points as IPointListEdit;
                            if (ip != null)
                            {
                                if (zpt.IsReplaceAtX)
                                {	// This is a request to replace point with new one.
                                    int foundIndex = -1;
                                    for (int i = 0; i < ip.Count; ++i)	// search for X-value of point to replace
                                        if (ip[i].X == zpt.X)			// very unsafe
                                        {
                                            foundIndex = i;
                                            break;						// stop looking
                                        }
                                    if (foundIndex > -1)
                                        ip[foundIndex].Y = zpt.Y;
                                    else
                                    {
                                        ip.Add(zpt.X, zpt.Y);
                                        // sort?
                                    }

                                }
                                else
                                    ip.Add(zpt.X, zpt.Y);		// simple serial addition of new data point.
                            }
                        }//next zpt
                    }
                    */ 
                    // *****************************************
                    // ****			Curve Definition		****
                    // *****************************************
                    else if (dataType == typeof(CurveDefinition))
                    {
                        CurveDefinition zNewCurve = (CurveDefinition)o;
                        ZedGraphControl zg1;
                        if (!m_GraphList.TryGetValue(zNewCurve.GraphID, out zg1))
                        {	// New graph ID!
                            continue;	// TODO: Create new graph on the fly.
                        }
                        UpdateCurveDefinition(zg1, zNewCurve);
                    }
                    // *****************************************
                    // ****			ZGraphPoint			    ****
                    // *****************************************
                    else if (dataType == typeof(ZGraphText))
                    {
                        ZGraphText zpt = (ZGraphText)o;
                        ZedGraphControl zg1;
                        if (!m_GraphList.TryGetValue(zpt.GraphID, out zg1))
                        {	// No currently existing graph with this id.							
                            continue;
                        }

                        TextObj text = new TextObj(zpt.Text, zpt.X, zpt.Y);

                        text.FontSpec.Angle = zpt.FontAngle;
                        text.Location.AlignH = zpt.FontAlignH;
                        text.Location.AlignV = zpt.FontAlignV;

                        text.FontSpec.Size = zpt.FontSize;
                        text.FontSpec.Border.IsVisible = zpt.FontBorderIsVisible;
                        text.FontSpec.Fill.IsVisible = zpt.FontFillIsVisible;

                        zg1.GraphPane.GraphObjList.Add(text);

                    }
                    // *****************************************
                    // ****			ZGraphPoint			    ****
                    // *****************************************
                    else if (dataType == typeof(ZGraphPoint))
                    {
                        ZGraphPoint zpt = (ZGraphPoint)o;
                        ZedGraphControl zg1;
                        if (!m_GraphList.TryGetValue(zpt.GraphID, out zg1))
                        {	// No currently existing graph with this id.							
                            continue;
                        }
                        CurveItem curve = null;
                        if (zg1.GraphPane != null)
                        {
                            curve = zg1.GraphPane.CurveList[zpt.CurveName];
                            if (curve == null)
                            {	// a new curve showed up - create a new curve, and plot this point anyway.
                                Color newColor = m_DefaultColors[zg1.GraphPane.CurveList.Count % m_DefaultColors.Length];
                                curve = zg1.GraphPane.AddCurve(zpt.CurveName, new PointPairList(), newColor, SymbolType.None);
                            }
                        }
                        // Add/replace points in the plot.
                        IPointListEdit ip = null;
                        if (curve != null)
                            ip = curve.Points as IPointListEdit;
                        if (ip != null)
                        {
                            if (zpt.IsReplaceAtX)
                            {	// This is a request to replace point with new one.
                                int foundIndex = -1;
                                for (int i = 0; i < ip.Count; ++i)	// search for X-value of point to replace
                                    if (ip[i].X == zpt.X)			// very unsafe
                                    {
                                        foundIndex = i;
                                        break;						// stop looking
                                    }
                                if (foundIndex > -1)
                                    ip[foundIndex].Y = zpt.Y;
                                else
                                {
                                    ip.Add(zpt.X, zpt.Y);
                                    // sort?
                                }

                            }
                            else
                                ip.Add(zpt.X, zpt.Y);		// simple serial addition of new data point.
                        }
                    }// ZPoint
                }
                m_IsUpdateRequired = true;
                //Regenerate(this, EventArgs.Empty);
            }//if e.DataObjectList is empty
        }// ProcessEvent()
        //
        //
        //
        //
        #endregion// IEngine implementation


        #region IEngineControl Implementation
        // *****************************************************************
        // ****                     IEngineControl                      ****
        // *****************************************************************
        //
        public void Regenerate(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {   // Not the window thread.                
            }
            else
            {   // windows thread.

                // New: Update only periodically based on size of graph.
                int size = Math.Min(this.Size.Width, this.Size.Height);
                const int fullSize = 1000;
                if (size < fullSize)
                {
                    m_GraphPaintCount++;
                    int n = 1 + (fullSize - size) / 50;
                    m_GraphPaintCount = m_GraphPaintCount % n ; 
                }
                else
                    m_GraphPaintCount = 0;

                // Repaint the graphs now.
                bool isGraphExist = false;
                ZedGraphControl zg1 = null;
                lock (GraphListLock)
                {
                    isGraphExist = m_GraphList.TryGetValue(m_CurrentZedGraphID, out zg1);
                }
                if (isGraphExist && m_GraphPaintCount==0)
                {
                    zg1.AxisChange();
                    zg1.Refresh();                 
                }
            }

        }//Regenerate()
        //
        //
        public void AcceptPopUp(Form parentForm)
        {
            foreach (Control control in parentForm.Controls)
                if (control.Name == "panel1") { control.Visible = false; }
        }
        //
        public void TitleBar_Click(object sender, EventArgs e)
        {
            MouseEventArgs mouseArg = (MouseEventArgs)e;
            if (mouseArg.Button == MouseButtons.Left)
            {
                this.Parent.Visible = false;
            }
        }//TitleBar_Click()

        //
        //
        #endregion// IEngineControl


        #region ZedGraph Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        // ****				ZedGraph_Resize			****
        /// <summary>
        /// On resize action, resize the ZedGraphControl to fill most of the Form, with a small
        /// margin around the outside
        /// </summary>
        private void ZedGraph_Resize(object sender, EventArgs e)
        {
            SetSize();
        }
        //
        // ****			ZedGraph_PointValueHandler			****
        //
        /// <summary>
        /// Display customized tooltips when the mouse hovers over a point
        /// </summary>
        private string ZedGraph_PointValueHandler(ZedGraphControl control, GraphPane pane, CurveItem curve, int iPt)
        {
            PointPair pt = curve[iPt];            // Get the PointPair that is under the mouse
            //return curve.Label.Text + " " + pt.Y.ToString("f2") + " at t=" + pt.X.ToString("f1");
            return curve.Label.Text + "=" + pt.Y.ToString("f2") + " at t=" + pt.X.ToString("f1");
        }// ZedGraph_PointValueHandler()
        //
        //
        //
        // ****				ZedGraph_ContextMenuBuilder				****
        //
        /// <summary>
        /// Customize the context menu by adding a new item to the end of the menu
        /// </summary>
        private void ZedGraph_ContextMenuBuilder(ZedGraphControl control, ContextMenuStrip menuStrip,
                        Point mousePt, ZedGraphControl.ContextMenuObjectState objState)
        {
            ToolStripMenuItem item;
            /*
            // Example:
            ToolStripMenuItem item = new ToolStripMenuItem();
            item.Name = "add-beta";
            item.Tag = "add-beta";
            item.Text = "Add a new Beta Point";
            item.Click += new System.EventHandler(AddBetaPoint);

            menuStrip.Items.Add(item);
            */

            //
            // Hide this Graph
            //
            /*
            item = new ToolStripMenuItem();
            item.Name = "hide-Graph";				
            item.Text = "Hide this Graph";
            item.Click += new System.EventHandler(HideThisGraph);
            menuStrip.Items.Add(item);
            */

            //
            // Show Graph
            //
            ToolStripSeparator separator = new ToolStripSeparator();
            menuStrip.Items.Add(separator);
            lock (GraphListLock)
            {
                foreach (int id in m_GraphList.Keys)
                {
                    // IEngine iengine = (IEngine)m_GraphList[id];
                    item = new ToolStripMenuItem();
                    item.Name = "ShowGraph" + id.ToString();		// not visible to user
                    //item.Text = string.Format("Show Graph {0}", id.ToString());	// shown to user.
                    item.Text = m_GraphList[id].Name;
                    item.Click += new System.EventHandler(ShowGraph);
                    if (id == m_CurrentZedGraphID) { item.Checked = true; }
                    menuStrip.Items.Add(item);
                }
            }
        }//ZedGraph_ContextMenuBuilder()
        //
        //
        // ****				ShowGraph()				****
        //
        /// <summary>
        /// The user has selected a new graph to show from the context menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ShowGraph(object sender, EventArgs e)
        {
            bool isRegenRequired = false;
            lock (GraphListLock)
            {
                ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;
                // Format: "ShowGraph 4 graphname...."
                if (menuItem.Name.StartsWith("ShowGraph"))
                {	// User has selected to display a second graph.
                    if (menuItem.Checked  == false )
                    {	// User requests that we show another graph.
                        string s = menuItem.Name.Substring(9);
                        int id = Convert.ToInt16(s);						// id# of selected graph
                        m_GraphList[m_CurrentZedGraphID].Visible = false;	// hide current graph
                        m_GraphList[id].Visible = true;						// show selected graph
                        m_CurrentZedGraphID = id;

                        isRegenRequired = false;                            // Request update graphs!
                    }
                    //else
                    //    this.Parent.Visible = false;		// choose already visible graph --> hide all the graphs
                    
                }
            }//lock GraphList
            
            if (isRegenRequired)
            {
                this.m_IsUpdateRequired = true;
                this.Regenerate(this, EventArgs.Empty);
            }

        }//end ShowGraph()
        //
        /*
        /// <summary>
        /// Handle the "Add New Beta Point" context menu item.  This finds the curve with
        /// the CurveItem.Label = "Beta", and adds a new point to it.
        /// </summary>
        private void AddBetaPoint(object sender, EventArgs args)
        {
            // Get a reference to the "Beta" curve IPointListEdit
            IPointListEdit ip = zg1.GraphPane.CurveList["Beta"].Points as IPointListEdit;
            if (ip != null)
            {
                double x = ip.Count * 5.0;
                double y = Math.Sin(ip.Count * Math.PI / 15.0) * 16.0 * 13.5;
                ip.Add(x, y);
                zg1.AxisChange();
                zg1.Refresh();
            }
        }
        */
        //
        //
        //
        // ****						ZedGraph_ZoomEvent					****
        /// <summary>
        /// Respond to a Zoom Event.
        /// </summary>
        private void ZedGraph_ZoomEvent(ZedGraphControl control, ZoomState oldState, ZoomState newState)
        {

        }//ZedGraph_ZoomEvent()
        //
        //
        //
        //
        #endregion//ZedGraph Event Handlers


    }//end class
}
