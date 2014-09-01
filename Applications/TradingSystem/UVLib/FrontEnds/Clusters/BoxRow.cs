using System;
using System.Windows.Forms;


namespace UV.Lib.FrontEnds.Clusters
{
    public class BoxRow : Control
    {

        #region Member variables
        // *********************************************************************
        // ****						Member variables						****
        // *********************************************************************
        //
        // 
        protected BoxNumeric[] m_Box;			// array containing the text box elements in this row.
        protected int m_RowSize = 6;			// number of elements; even to make row balanced.
		public ClusterEventArgs.BoxRowType RowType = ClusterEventArgs.BoxRowType.None;

        private BoxNumeric[] m_OverFlow;		// left/right boxes for overflow.

        private double m_MinTickSize;				// minimum tick size for my elements. 
        public int xPositionOffset = 0;
        private const int LeftSide = 0;
        private const int RightSide = 1;
        private int m_CenterBarWidth = 0;       // width of center bar, 2 is good.
        private int m_RightPadding = 1;          // blank space to left of cluster.
        private int m_BottomPadding = 0;        // "  " below cluster.

        //
        // Control parameters.
        // 
        public bool HideZeroOverflow = true;		// hides overflow boxes when they are zero.
        public bool EmbedZeroOverflowInRow = false;	// does not allocate space for the extra box for overflow; 
        // This embedded of overflow boxes is good for creating
        // a compact mode for a cluster; it saves space.

        //
        #endregion// variables.


        #region Constructor
        // *********************************************************************
        // ****							Constructor							****
        // *********************************************************************
        //
        //
        public BoxRow(int rowLength, double minTickSize, int xLocationOffset, bool useOverFlow, ColorPalette colorPreference)
        {
            xPositionOffset = xLocationOffset;
			Initialize(rowLength, minTickSize, useOverFlow, colorPreference);

        }//end constructor
        //
        //
        //
        //
        // ****							Initialize							****
        //
		private void Initialize(int rowLength, double minTickSize, bool useOverFlow, ColorPalette colorPreference)
        {
            m_RowSize = rowLength;
            m_MinTickSize = minTickSize;
            int xPosition = xPositionOffset;
            int yPosition = 0;
            this.SuspendLayout();

            // If the user wants an overflow box, but doesnt want to add a special
            // overflow box to the ends of this row, then set EmbedZeroOverflowInRow to true.
            if (useOverFlow && (xPositionOffset < BoxNumeric.DefaultWidth)) EmbedZeroOverflowInRow = true;


            // insert overflow box on left.
            if (useOverFlow && (!EmbedZeroOverflowInRow))
            {
                m_OverFlow = new BoxNumeric[2];
				m_OverFlow[LeftSide] = new BoxNumeric(1.0d, 0, colorPreference);
				m_OverFlow[RightSide] = new BoxNumeric(1.0d, 0, colorPreference);
                m_OverFlow[LeftSide].Location = new System.Drawing.Point(xPosition - m_OverFlow[LeftSide].Width, yPosition);
                this.Controls.Add(m_OverFlow[LeftSide]);
            }
            else
            {
                m_OverFlow = new BoxNumeric[0];				// zero length array.
            }

            // Construct main row.
            m_Box = new BoxNumeric[m_RowSize];
            //m_Box = new BoxNumClick[ m_RowSize ];		
            int tallestBoxHeight = 0;
            for (int i = 0; i < m_RowSize; ++i)
            {
                // create the row element.
                BoxNumeric aControl = new BoxNumeric(m_MinTickSize, i, colorPreference);		// create a new box.
                //BoxNumClick aControl = new BoxNumClick(MinTickSize);	// create a new box.
                aControl.Location = new System.Drawing.Point(xPosition, yPosition);
                m_Box[i] = aControl;
                this.Controls.Add(aControl);					// add this box to our collection.
                // 
                xPosition += aControl.Width;					// increment drawing position.				
                if (aControl.Height > tallestBoxHeight) tallestBoxHeight = aControl.Height;	// keep tallest box height.

                // Place the center bar. 
                if (i == (m_RowSize / 2) - 1) { xPosition += m_CenterBarWidth; }               
            }//i

            // place right-overflow box.
            if (useOverFlow && (!EmbedZeroOverflowInRow))
            {
                m_OverFlow[RightSide].Location = new System.Drawing.Point(xPosition, yPosition);
                xPosition += m_OverFlow[RightSide].Width;
                this.Controls.Add(m_OverFlow[RightSide]);
            }


            // Resize myself.
            this.ClientSize = new System.Drawing.Size(xPosition + m_RightPadding, yPosition + tallestBoxHeight + m_BottomPadding);
            this.Size = new System.Drawing.Size(this.ClientSize.Width, this.ClientSize.Height);
            this.ResumeLayout(false);

        }//end initialize()
        //
        //
        #endregion//constructors

        #region Properties
        //
        public double MinTickSize
        {
            get { return m_MinTickSize;  }
            set 
            {
                m_MinTickSize = value;
                foreach (BoxNumeric box in m_Box)
                    box.m_MinTickSize = m_MinTickSize;
            }
        }
        //
        //
        #endregion//Properties

        #region Private Utilities
        // *********************************************************************
        // ****						Private Utilities						****
        // *********************************************************************
        //
        //
        //
        // ****				Set Color Palette				****
        //
        protected void SetColorPalette(ColorPalette colorSet)
        {
            foreach (BoxNumeric box in m_Box) { box.UseColorPalette(colorSet); }
            foreach (BoxNumeric box in m_OverFlow) { box.UseColorPalette(colorSet); }
        }//end SetColorPalette().

        #endregion//end private Utilities


        #region Public Methods
        // *********************************************************************
        // ****							Public Methods						****
        // *********************************************************************
        //
        //
        //
        //
        // ****				Update Value()					****
        //
        public void UpdateValue(int[] cellValues)
        {
            for (int i = 0; i < m_RowSize; ++i)
            {
                m_Box[i].TickValue = cellValues[i];
            }//i
        }//end Update()
        //
        //
        //
        public void UpdateValue(int[] cellValues, int leftOverflow, int rightOverflow)
        {	//
            // Update overflow boxes.
            //
            if (EmbedZeroOverflowInRow)
            {	// If this row is being display in the compact form, the user has set
                // "EmbedZeroOverflowInRow to true, then he wants overflows to be places inside
                // the leftmost and rightmost cells; rather than the default extra "overflow" boxes.
                // Add overflows on:
                cellValues[0] += leftOverflow;
                cellValues[m_RowSize - 1] += rightOverflow;
            }
            else
            {	// In normal configuration, we have to update the overflow boxes
                // separately now.
                UpdateOverFlow(leftOverflow, rightOverflow);
            }
            //
            // Update central grid of boxes.
            //
            for (int i = 0; i < m_RowSize; ++i)
            {
                m_Box[i].TickValue = cellValues[i];
            }//i
        }//end UpdateValue().
        //
        //
        //
        // ****				Update HighLite					****
        //
        /// <summary>
        /// Allows the application to apply HiLite colors to the forground.
        /// </summary>
        /// <param name="hiLiteCodes">Array of m_Box.Length that gives the "hi-lite value"
        ///		parameter.  A value of "0" = default, not hi-lited.</param>
        public void UpdateHiLite(int[] hiLiteCodes)
        {
            // Check validity of the value.
            if (hiLiteCodes.Length == m_Box.Length)
            {	// hiLiteCodes is valid.
                for (int i = 0; i < hiLiteCodes.Length; ++i)
                {
                    m_Box[i].ForeColorHiLite = hiLiteCodes[i];
                }
            }
        }//end UpdateHiLite().
        //
        //
        //
        // ****             Register Mouse Events               ****
        //
        /// <summary>
        /// This registers sets all my boxes to register mouse events with my parent
        /// cluster.
        /// </summary>
        /// <param name="aCluster">Parent cluster</param>
        /// <param name="rowID"></param>
        public void RegisterMouseEvents(Cluster aCluster, int rowID)
        {
            foreach (BoxNumeric box in m_Box) { box.RegisterMouseEvents(aCluster,rowID); }
        
        }// RegisterMouseEvents()
        //
        #endregion



        #region Private Utilities
        // *************************************************************************
        // ****							Private Utilities						****
        // *************************************************************************
        //
        //
        // ****				Update OverFlow()				****
        //
        private void UpdateOverFlow(int leftValue, int rightValue)
        {
            m_OverFlow[LeftSide].TickValue = leftValue;
            m_OverFlow[RightSide].TickValue = rightValue;

            // Show or suppress overflow cells.
            if (HideZeroOverflow)
            {	// user has flag for hiding overflows that are zero set to true.
                if (leftValue == 0 && m_OverFlow[LeftSide].Visible) m_OverFlow[LeftSide].Visible = false;
                else if (leftValue != 0 && !m_OverFlow[LeftSide].Visible) m_OverFlow[LeftSide].Visible = true;

                if (rightValue == 0 && m_OverFlow[RightSide].Visible) m_OverFlow[RightSide].Visible = false;
                else if (rightValue != 0 && !m_OverFlow[RightSide].Visible) m_OverFlow[RightSide].Visible = true;
            }
        }//end UpdateOverFlow()
        //
        //


        #endregion//end private utilities


    }
}
