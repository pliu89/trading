using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UV.Lib.FrontEnds.PopUps
{
    using UV.Lib.FrontEnds.Utilities;

    /// <summary>
    /// This version of an IPopUp form is not in use now.
    /// </summary>
    public partial class PopUp2 : Form, IPopUp
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************

        //
        // Constants
        //
        public int HeaderHeight = 16;
        public const int MinimumAllowedWidth = 230;
        public const int MarginSpace = 1;
        private IEngineControl m_CustomControl = null;

        SystemMenuManager m_MenuManager = null;

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        public PopUp2()
        {
            //HeaderHeight = panel1.Height;

            InitializeComponent();
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Title = "";

            // debug


        }
        //
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        //
        // ***      Title       ***
        //
        public string Title
        {
            get { return bTitle.Text; }
            //get { return this.Text; }
            //set{ this.Text = value; }
            set { bTitle.Text = value; }
        }
        //
        //
        /// <summary>
        /// Implements IPopUp.
        /// </summary>
        public IEngineControl CustomControl
        {
            get { return m_CustomControl; }
        }
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
        /// </summary>
        /// <param name="spawnControl">Control onto which popup is painted.</param>
        public void ShowMe(Control spawnControl)
        {
            // Tell control we are showing it. TODO: Put this in IEngineControl interface!
            if (m_CustomControl.GetType() == typeof(Graphs.ZGraphControl))
            {
                ((Graphs.ZGraphControl)m_CustomControl).ShowMe(this);
            }

            this.m_MenuManager = new SystemMenuManager(this, true, SystemMenuManager.MenuItemState.Greyed);

            Control parentForm = spawnControl.TopLevelControl;
            this.SuspendLayout();
            this.Visible = false;

            //
            // Find a good position for the popup.
            //
            Rectangle parentRect = parentForm.RectangleToScreen(parentForm.ClientRectangle);
            int x = parentRect.Left;        // parent inside edge coordinate.
            int y = parentRect.Top;         // parent inside edge coordinate.
            //int posX = spawningControl.Left + x;     // Starting guess position...
            //int posY = spawningControl.Top + y;      //

            Rectangle spawnRect = parentForm.RectangleToClient(spawnControl.ClientRectangle);
            int posX = spawnControl.Location.X + x;     // Starting guess position...
            int posY = spawnControl.Location.Y + y;      //

            // Make sure that the popup remains inside the parent form.
            if (posX + this.Width > parentRect.Right) { posX = parentRect.Right - this.Width; }
            if (posY + this.Height > parentRect.Bottom) { posY = parentRect.Bottom - this.Height; }
            // now set locateion
            posX = Math.Max(0, posX);   // keep it from going off the screen to the left.
            posY = Math.Max(0, posY);
            this.Location = new Point(MarginSpace + posX, MarginSpace + posY);

            m_CustomControl.Regenerate(this, null);   // updates values displayed, if needed

            this.Visible = true;
            this.ResumeLayout();

        }// ShowMe()
        //
        //
        // ****             AddControl()                ****
        //
        public void AddControl(IEngineControl engineControl)
        {
            this.m_CustomControl = engineControl;
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


        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        #endregion//Private Methods


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
        //
        //

        private void PopUp_MouseClick(object sender, MouseEventArgs e)
        {
            m_CustomControl.TitleBar_Click(sender, e);
        }


        //
        #endregion//Event Handlers
    }//end class
}
