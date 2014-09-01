using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies.StrategyEngines
{
    using UV.Lib.IO.Xml;
    using UV.Lib.Hubs;
    using UV.Lib.Engines;
    using UV.Lib.Utilities;

    using UV.Lib.FrontEnds.Graphs;
    using UV.Strategies.StrategyHubs;

    public class ZGraphEngine : Engine, IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // 
        protected Strategy m_Parent = null;
        protected int m_ParentEngineContainerID = -1;
        protected LogHub Log = null;
        protected double m_StartHour = 0.0;			                                    // hour of the day system started. Used for plotting x-axis.

        protected DateTime m_StartTime;

        //
        // Data to plot
        //
        private List<ZGraphPoint> m_NewPoints = new List<ZGraphPoint>();	        // data to plot
        //private List<ZGraphPoint> m_HistoricPoints = new List<ZGraphPoint>();	    // data already broadcasted
        //private List<CurveDefinition> m_CurveDefList = new List<CurveDefinition>();// my curves
        private ZGraphPoints m_HistoricPoints = new ZGraphPoints();
        private CurveDefinitionList m_CurveDefintions = new CurveDefinitionList();

        private List<ZGraphText> m_NewText = new List<ZGraphText>();

        #endregion// members


        #region Constructors & Setup
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public ZGraphEngine() : base()
        {

        } 
        //
        // 
        // ******************************************************
        // ****             Setup Initialize()               ****
        // ******************************************************
        /// <summary>
        /// Since we are a specialized gui manager, we override SetupInitialize()
        /// and don't create a default popup gui.
        /// </summary>
        /// <param name="myEngineHub"></param>
        /// <param name="engineContainer"></param>
        /// <param name="engineID"></param>
        /// <param name="isSetupGui"></param>
        protected override void SetupInitialize(IEngineHub myEngineHub, IEngineContainer engineContainer, int engineID, bool isSetupGui)
        {
            base.SetupInitialize(myEngineHub, engineContainer, engineID, false);// call base class first.

            m_Parent = (Strategy)engineContainer;
            m_ParentEngineContainerID = engineContainer.EngineContainerID;
            Log = m_Parent.StrategyHub.Log;
            
            // Set the plotting hour offset.
            m_StartTime = m_Parent.StrategyHub.GetLocalTime();
            m_StartHour = m_StartTime.Hour + m_StartTime.Minute / 60.0 + m_StartTime.Second / 3600.0;
            if (m_StartHour >= 16.0)
                m_StartHour -= 24.0;				// after 4pm, shift to negative numbers (new trading day).



            //
            // Add our custom ZGraphControl template
            //
            UV.Lib.FrontEnds.GuiTemplates.EngineGui engineGui = new Lib.FrontEnds.GuiTemplates.EngineGui();
            engineGui.DisplayName = "Graph";
            engineGui.EngineID = this.m_EngineID;
            engineGui.EngineFullName = this.GetType().FullName;
            engineGui.HeaderControlFullName = typeof(UV.Lib.FrontEnds.Graphs.ZGraphControl).FullName;
            engineGui.ParameterList.AddRange(this.m_PInfo);
            m_EngineGuis.Add(engineGui);
        }//SetupInitialize()
        //
        //
        //
        //       
        #endregion//Constructors



        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        public CurveDefinitionList CurveDefinitions
        {
            get { return new CurveDefinitionList(m_CurveDefintions); }
        }
        public ZGraphPoints HistoricPoints
        {
            get { return new ZGraphPoints(m_HistoricPoints); }
        }
        //
        #endregion//Properties



        #region Public Methods
        // *************************************************
        // ****             Public Methods              ****
        // *************************************************
        //
        //
        //
        // *************************************************
        // ****             Add Definition()            ****
        // *************************************************
        public void AddDefinition( CurveDefinition cdef )
        {
            m_CurveDefintions.CurveDefinitions.Add(cdef);
        }
        //
        //
        // *************************************************
        // ****				Add Point()					****
        // *************************************************
        public void AddPoint(int graphID, string curveName, double x, double y, bool isReplaceAtX)
        {
            ZGraphPoint zpt = new ZGraphPoint();
            zpt.X = x;
            zpt.Y = y;
            zpt.CurveName = curveName;
            zpt.GraphID = graphID;
            zpt.IsReplaceAtX = isReplaceAtX;
            m_NewPoints.Add(zpt);
        }// AddPoint
        //
        // *************************************************
        // ****				Add Time Point()			****
        // *************************************************
        /// <summary>
        /// This overloading allows only a y-value to be entered.  The x-axis is assumed
        /// to be the current elapsed time in hours.
        /// </summary>
        /// <param name="graphID"></param>
        /// <param name="curveName"></param>
        /// <param name="y"></param>
        public void AddPoint(int graphID, string curveName, double y)
        {
            ZGraphPoint zpt = new ZGraphPoint();

            // Original way to deal with time.
            //double origX = Log.m_StopWatch.Elapsed.TotalHours + m_StartHour;	// fractional hours from our starting.
            // New way to handle time.
            TimeSpan elapsedTime = m_Parent.StrategyHub.GetLocalTime().Subtract(m_StartTime);
            double newX = elapsedTime.TotalHours + m_StartHour;
            
            zpt.X = newX;
            zpt.Y = y;
            zpt.CurveName = curveName;
            zpt.GraphID = graphID;
            m_NewPoints.Add(zpt);
        }// AddPoint
        //		
        //
        // *****************************************
        // ****         Add Text()              ****
        // *****************************************
        public void AddText(int graphID, string text, double yLoc)
        {
            ZGraphText zpt = GetText();
            zpt.Text = text;
            zpt.Y = yLoc;
            zpt.GraphID = graphID;
            //zpt.FontSize = 8.0f;
            m_NewText.Add(zpt);
        }// AddPoint
        //
        public ZGraphText GetText()
        {
            ZGraphText zpt = new ZGraphText();
            // Set defalt values
            TimeSpan elapsedTime = m_Parent.StrategyHub.GetLocalTime().Subtract(m_StartTime);
            double newX = elapsedTime.TotalHours + m_StartHour;
            zpt.X = newX;

            return zpt;
        }// GetText()
        //
        public void AddText( ZGraphText zpt )
        {
            m_NewText.Add(zpt);
        }
        //
        //
        // *********************************************************
        // ****			AddSpontaneousEngineEvents()			****
        // *********************************************************
        public override void AddSpontaneousEngineEvents(List<EngineEventArgs> eventList)
        {
            base.AddSpontaneousEngineEvents(eventList);           // add any previously queued events to list.

            // Add new x-y points to plot.
            if (m_NewPoints.Count > 0 )
            {
                EngineEventArgs eArgs = new EngineEventArgs();
                eArgs.MsgType = EngineEventArgs.EventType.ParameterValue;
                eArgs.Status = EngineEventArgs.EventStatus.Confirm;
                eArgs.EngineHubName = m_Parent.StrategyHub.ServiceName;     // name of my hub.
                eArgs.EngineContainerID = m_ParentEngineContainerID;        // this is my parent's Container ID.  
                eArgs.EngineID = this.EngineID;                             // this is my ID.

                // load market data   
                if (eArgs.DataObjectList == null)   
                    eArgs.DataObjectList = new List<object>();

                // Add points to data list
                if (m_NewPoints.Count > 0)
                {
                    eArgs.DataObjectList.AddRange(m_NewPoints);                 // Load event
                    m_HistoricPoints.Points.AddRange(m_NewPoints);              // update the historic list
                    m_NewPoints.Clear();                                        // clear the new points as processed.
                }               

                // Exit
                eventList.Add(eArgs);
            }

            // Add text
            if (m_NewText.Count > 0)
            {
                EngineEventArgs eArgs = new EngineEventArgs();
                eArgs.MsgType = EngineEventArgs.EventType.ParameterValue;
                eArgs.Status = EngineEventArgs.EventStatus.Confirm;
                eArgs.EngineHubName = m_Parent.StrategyHub.ServiceName;     // name of my hub.
                eArgs.EngineContainerID = m_ParentEngineContainerID;        // this is my parent's Container ID.  
                eArgs.EngineID = this.EngineID;                             // this is my ID.

                // load market data   
                if (eArgs.DataObjectList == null)
                    eArgs.DataObjectList = new List<object>();

                // Add points to data list
                if (m_NewText.Count > 0)
                {
                    eArgs.DataObjectList.AddRange(m_NewText);                 // Load event
                    //m_HistoricText.Points.AddRange(m_NewText);              // update the historic list
                    m_NewText.Clear();                                        // clear the new points as processed.
                }

                // Exit
                eventList.Add(eArgs);
            }



        }//end AddSpontaneousEvents()
        //

        //
        //
        public override void ProcessEvent(EventArgs e)
        {
            base.ProcessEvent(e);
        }
        //
        //
        //
        #endregion//Public Methods



        #region IStringifiable
        // *****************************************************************
        // ****                  IStringifiable                         ****
        // *****************************************************************
        //
        string IStringifiable.GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            s.Append(base.GetAttributes());
            return s.ToString();
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            return null;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
        }
        #endregion//IStringifiable

    }
}
