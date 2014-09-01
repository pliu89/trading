using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UV.Lib.FrontEnds.PopUps
{
    using UV.Lib.FrontEnds.Utilities;

    /// <summary>
    /// Form that holds a single IEngineControl object for displaying.
    /// </summary>
    public partial class PopUp1 : Form, IPopUp
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        public int HeaderHeight = 0; //16;
        public const int MinimumAllowedWidth = 230;
        public const int MarginSpace = 0; //1;
        private IEngineControl m_IEngineControl = null;	// the inside control to display .

        SystemMenuManager m_MenuManager = null;			// useful for controlling the grab-bar buttons.

        public Clusters.Header ParentHeader = null;

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        public PopUp1(Clusters.Header myparent)
        {
            ParentHeader = myparent;
            InitializeComponent();


            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
        }//PopUp1
        //
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        public string Title
        {
            get { return this.Text; }
            set
            {
                this.Text = value;
                bTitle.Text = value;
            }
        }
        //
        //
        //
        //
        public IEngineControl CustomControl { get { return m_IEngineControl; } }	// implements IPopUp
        //
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        // ****             ShowMe              ****
        //
        /// <summary>
        /// Show the control.  Implements IPopUp.
        /// Called by the GUI thread.
        /// </summary>
        /// <param name="spawnControl">Control onto which popup is painted.</param>
        public void ShowMe(Control spawnControl)
        {
            // Tell control we are showing it.
            if (m_IEngineControl.GetType() == typeof(Graphs.ZGraphControl))
            {
                Graphs.ZGraphControl zGraphControl = (Graphs.ZGraphControl)m_IEngineControl;
                Clusters.Header header = this.ParentHeader;		// grab Cluster header.
                Form displayForm = header.ParentForm;			// grab the form that cluster is in.
                Clusters.ClusterDisplay display = (Clusters.ClusterDisplay)displayForm;
                if (display.GraphDisplay != null)				// user has spawned a GraphDisplay.	
                    this.MdiParent = display.GraphDisplay;		// embed this into GraphDisplay.
                zGraphControl.ShowMe(this);
            }


            // Disable the "exit" button.
            this.m_MenuManager = new SystemMenuManager(this, true, SystemMenuManager.MenuItemState.Disabled);

            Control parentForm = spawnControl.TopLevelControl;
            this.SuspendLayout();
            this.Visible = false;

            //
            // Find a good position for the popup.
            //
            Rectangle parentRect = parentForm.RectangleToScreen(parentForm.ClientRectangle);
            int x = parentRect.Left;                        // parent inside edge coordinate.
            int y = parentRect.Top;                         // parent inside edge coordinate.

            Rectangle spawnRect = parentForm.RectangleToClient(spawnControl.ClientRectangle);
            int posX = spawnControl.Location.X + x;         // Starting guess position...
            int posY = spawnControl.Location.Y + y;         //

            // Make sure that the popup remains inside the parent form.
            if (posX + this.Width > parentRect.Right) { posX = parentRect.Right - this.Width; }
            if (posY + this.Height > parentRect.Bottom) { posY = parentRect.Bottom - this.Height; }
            // now set locateion
            posX = Math.Max(0, posX);                       // keep it from going off the screen to the left.
            posY = Math.Max(0, posY);
            this.Location = new Point(MarginSpace + posX, MarginSpace + posY);

            m_IEngineControl.Regenerate(this, null);        // updates values displayed, if needed

            this.Visible = true;
            this.ResumeLayout();

        }// ShowMe()
        //
        //
        // ****             AddControl()                ****
        //
        public void AddControl(IEngineControl engineControl)
        {
            this.m_IEngineControl = engineControl;
            engineControl.AcceptPopUp(this);

            Control newControl2 = (Control)engineControl;
            if (this.FormBorderStyle == FormBorderStyle.Sizable
                || this.FormBorderStyle == FormBorderStyle.SizableToolWindow)
            {   // This window has a grab bar - delete my home-made grab bar.
                this.HeaderHeight = 0;
            }
            else
                this.HeaderHeight = panel1.Height;
            newControl2.Location = new Point(0, HeaderHeight);
            this.Controls.Add(newControl2);
            int maxX = newControl2.Width;
            int maxY = newControl2.Height;
            // Resize popup.
            maxX = Math.Max(maxX, MinimumAllowedWidth);
            this.ClientSize = new Size(maxX, maxY + HeaderHeight);
        }
        //
        //
        //
        //
        //
        #endregion//Public Methods


        #region Form Event Handlers
        // *****************************************************************
        // ****                 Form Event Handlers                     ****
        // *****************************************************************
        //
        //
        // ****             bExit_Click()               ****
        //
        private void bExit_Click(object sender, EventArgs e)
        {
            this.Visible = false;
        }
        //
        // ****             PopUp_MouseClick()          ****
        //
        private void PopUp_MouseClick(object sender, MouseEventArgs e)
        {
            m_IEngineControl.TitleBar_Click(sender, e);
        }
        //
        //
        // ****             PopUp1_MouseDown()          ****
        //
        private void PopUp1_MouseDown(object sender, MouseEventArgs e)
        {

        }
        //
        //
        //
        // ****             PopUp1_FormClosing()        ****
        //
        private void PopUp1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (m_IEngineControl is IEngineControl)
            {
                e.Cancel = true;
                this.Visible = false;
            }
        }
        //
        //
        #endregion//Event Handlers
    }//end class
}
