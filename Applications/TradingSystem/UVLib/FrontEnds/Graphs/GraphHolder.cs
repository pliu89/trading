using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace UV.Lib.FrontEnds.Graphs
{
    /// <summary>
    /// This is a form that holds multiple PopUp forms, allowing them
    /// to be easily tiled and controlled collectively.
    /// </summary>
    [DefaultPropertyAttribute("NumberOfColumns")]			// property to get focus in property grids.
    
    public partial class GraphHolder : Form
    {


        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        //
        public int m_NumberOfColumns = 4;
        public MdiLayout m_CurrentLayout = MdiLayout.TileHorizontal;


        // notes:
        //public MdiClient;	// what goodies are in here?
        //
        //
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public GraphHolder()
        {
            InitializeComponent();
            if (UV.Lib.Application.AppServices.GetInstance().AppIcon != null)
                this.Icon = UV.Lib.Application.AppServices.GetInstance().AppIcon;
        }

        //
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        [CategoryAttribute("Chimera"), DescriptionAttribute("Number of columns")]
        public int NumberOfColumns
        {
            get { return m_NumberOfColumns; }
            set { m_NumberOfColumns = value; }
        }
        [CategoryAttribute("Chimera"), DescriptionAttribute("Rule for tiling of child forms")]
        public MdiLayout CurrentTileLayout
        {
            get { return m_CurrentLayout; }
            set { m_CurrentLayout = value; }
        }

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
        // ****			TileChildForms( )				****
        //
        private void TileChildForms()
        {
            if (this.MdiChildren == null) return;

            // First determine the previous order these forms were displayed.			
            List<Form> allChildren = new List<Form>(this.MdiChildren);			// Get child forms
            if (allChildren.Count <= 0)
                return;
            List<Form> orderedChildren = GetOrderedChildren(allChildren);		// 
            double width = this.ClientSize.Width;								// size of my client space.
            double height = this.ClientSize.Height;

            if (MainMenuStrip != null)
            {
                height -= MainMenuStrip.Bounds.Height;
                //height -= MainMenuStrip.Size.Height + 1;
            }

            this.SuspendLayout();
            width -= 5;		// fudge tell children outer window is smaller, to give it some extra space!
            height -= 5;
            switch (m_CurrentLayout)
            {
                case MdiLayout.TileHorizontal:
                    int nRows = (int)Math.Ceiling(orderedChildren.Count / (1.0 * m_NumberOfColumns));

                    int childWidth = (int)Math.Floor(width / m_NumberOfColumns - 1);
                    int childHeight = (int)Math.Floor(height / nRows) - 1;
                    int row = 0;
                    int col = 0;
                    foreach (Form form in orderedChildren)
                    {
                        form.Location = new Point(col * childWidth, row * childHeight);
                        form.Width = childWidth;
                        form.Height = childHeight;
                        col++;
                        if (col >= m_NumberOfColumns)
                        {
                            col = 0;
                            row++;
                        }
                    }
                    break;
                default:
                    break;
            }// switch current layout
            this.ResumeLayout(true);
        }//TileChildForms().
        // 
        //
        //
        // ****			LocateTopMostForm()				****
        //
        /// <summary>
        /// Returns the form that is most closest to the upper-left corner.
        /// </summary>
        /// <param name="formList">List of forms to compare</param>
        /// <returns>form closest to upper left corner</returns>
        private Form LocateTopMostForm(List<Form> formList)
        {
            if (formList.Count < 1) return null;
            Form topmostForm = formList[0];
            int minX = topmostForm.Width;
            int mixY = topmostForm.Height;
            // Search thru remaining children, try to find more topmost form.
            for (int i = 1; i < formList.Count; ++i)	// skip the zeroth element.
            {
                Form aChild = formList[i];
                if (aChild.Location.Y == topmostForm.Location.Y)
                {
                    if (aChild.Location.X < topmostForm.Location.X)
                        topmostForm = aChild;
                }
                else if (aChild.Location.Y < topmostForm.Location.Y)
                    topmostForm = aChild;
            }//next i
            return topmostForm;
        }//LocateTopMostForm()
        //
        //
        //
        // ****				GetOrderedChildren()			****
        //
        /// <summary>
        /// Returns a list of forms ordered according to their approximate locations.
        /// The algorithm divides the area into horizontal strips, and orders the 
        /// forms from left to right, then top to bottom.
        /// </summary>
        /// <param name="allChildren">list of forms to compare</param>
        /// <returns>list of same forms, ordered.</returns>
        private List<Form> GetOrderedChildren(List<Form> allChildren)
        {
            List<Form> remainingChildren = new List<Form>(allChildren);
            List<Form> orderedChildren = new List<Form>();
            // Estimate window sizes			
            int aveHeight = 0;
            int n = 0;
            foreach (Form child in remainingChildren)
                if (child.WindowState == FormWindowState.Normal)
                {
                    aveHeight += child.Height;
                    n++;
                }
            if (n > 0) aveHeight = aveHeight / n; else aveHeight = 300;

            Form topMostChild = LocateTopMostForm(remainingChildren);
            int yBand = topMostChild.Location.Y + aveHeight / 2;	// band used to identify rows

            // find top row of forms
            List<Form> row = new List<Form>();
            while (remainingChildren.Count > 0)
            {
                // Collect all children located in a y-band from top, called a row.				
                foreach (Form child in remainingChildren)			// find children in this row-band.
                    if (child.Location.Y < yBand) row.Add(child);
                foreach (Form child in row)
                    remainingChildren.Remove(child);				// remove them from list of remaining children.
                //
                while (row.Count > 0)
                {
                    Form leftMostChild = row[0];
                    for (int i = 1; i < row.Count; ++i)
                        if (row[i].Location.X < leftMostChild.Location.X)
                            leftMostChild = row[i];
                        else if (row[i].Location.X == leftMostChild.Location.X && row[i].Location.Y < leftMostChild.Location.Y)
                            leftMostChild = row[i];
                    orderedChildren.Add(leftMostChild);				// add winner to ordered list.
                    row.Remove(leftMostChild);						// remove from row-list.						
                }// next child in this row
                row.Clear();
                yBand += aveHeight;
            }
            // Exit
            return orderedChildren;
        }// GetOrderedChildren()
        //
        //
        //
        // ****				Release Children()			****
        //
        private void ReleaseChildren()
        {
            Point p = this.Location;
            int yOffset = this.DesktopLocation.Y + this.DesktopBounds.Height - ClientSize.Height; // - Margin.Bottom?
            int xOffset = this.DesktopLocation.X + this.DesktopBounds.Width - ClientSize.Width; // - Margin.Right?
            List<Form> allChildren = new List<Form>(this.MdiChildren);			// Get child forms
            this.SuspendLayout();
            foreach (Form child in allChildren)
            {
                child.Location = new Point(child.Location.X + xOffset, child.Location.Y + yOffset);
                child.Visible = true;
                child.MdiParent = null;
            }
            this.ResumeLayout();


        }//ReleaseChildren()
        //
        //
        #endregion//Private Methods


        #region Form Event Handlers
        // *****************************************************************
        // ****                 Form Event Handlers                     ****
        // *****************************************************************
        //
        //
        //
        private void GraphHolder_ResizeEnd(object sender, EventArgs e)
        {
            if (this.MdiChildren != null) TileChildForms();
        }
        //
        //
        private void propertiesToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            UV.Lib.FrontEnds.Utilities.PropertyForm propForm = new UV.Lib.FrontEnds.Utilities.PropertyForm(this);
            propForm.Show();

        }
        //
        //
        private void GraphHolder_FormClosing(object sender, FormClosingEventArgs e)
        {
            ReleaseChildren();
            this.Visible = false;
        }

        private void releaseChildrenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReleaseChildren();
        }
        //
        //
        //		
        //
        //
        #endregion//Event Handlers

    }//end class
}
