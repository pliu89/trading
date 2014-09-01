using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MistyTests.GUIs
{
    public partial class NotifyTest : Form
    {


        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        

        // Context menu

        private Icon m_Icon = null;
            


        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public NotifyTest()
        {
            InitializeComponent();

            m_Icon = MistyTests.Properties.Resources.user_female;

            //string dirPath = System.IO.Directory.GetCurrentDirectory();
            //int ptr = dirPath.LastIndexOf(@"Ambre\");
            //string iconPath = string.Format("{0}MistyTests\\GUIs\\user_female.ico", dirPath.Substring(0, ptr+6));
            //m_Icon = Icon.ExtractAssociatedIcon(iconPath);
            
            this.Icon = m_Icon;
            InitializeContextMenu();

        }
        private void InitializeContextMenu()
        {
            // Start notify icon
            this.notifyIcon.Visible = true;
            this.notifyIcon.Text = "Ambre";
            this.notifyIcon.Icon = m_Icon;
            this.notifyIcon.DoubleClick += new EventHandler(NotifyIcon_DoubleClick);
            this.notifyIcon.BalloonTipTitle = "Ambre";

            // Create its context menu.
            ContextMenu contextMenu;
            contextMenu = new ContextMenu();
            contextMenu.Name = "NotifyContextMenu";
            this.notifyIcon.ContextMenu = contextMenu;

            //
            // Add menu items.
            //
            MenuItem contextMenuItem;

            contextMenuItem = new MenuItem();
            //contextMenuItem.Index = 0;
            contextMenuItem.Name = "Hide/Restore";
            contextMenuItem.Text = "Hide";  // Hide/Restore
            contextMenuItem.Click += contextMenuItem_Click;
            contextMenu.MenuItems.Add(contextMenuItem);

            contextMenuItem = new MenuItem();
            //contextMenuItem.Index = 0;
            contextMenuItem.Text = "Balloon";
            contextMenuItem.Click += contextMenuItem_Click;
            contextMenu.MenuItems.Add(contextMenuItem);

            contextMenuItem = new MenuItem();
            //contextMenuItem.Index = 0;
            contextMenuItem.Text = "Exit";
            contextMenuItem.Click += contextMenuItem_Click;
            contextMenu.MenuItems.Add(contextMenuItem);


            // Show start up message.
            this.notifyIcon.BalloonTipText = "Starting...";
            this.notifyIcon.ShowBalloonTip(2000);

        }

        //
        //       
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


        #region no Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        #endregion//Event Handlers


        #region Form Event Handlers
        // *****************************************************************
        // ****                Form Event Handlers                     ****
        // *****************************************************************
        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = FormWindowState.Normal;
            else if (this.WindowState == FormWindowState.Normal || this.WindowState == FormWindowState.Maximized)
                this.WindowState = FormWindowState.Minimized;
        }
        //
        //
        private void contextMenuItem_Click(object sender, EventArgs e)
        {
            Type eventType = e.GetType();
            Type senderType = sender.GetType();
            if (senderType == typeof(MenuItem))
            {
                MenuItem menuItem = (MenuItem)sender;
                string selectedMenu = menuItem.Text;
                switch (selectedMenu)
                {
                    case "Exit":
                        //this.notifyIcon.BalloonTipText = "Exiting";
                        //this.notifyIcon.ShowBalloonTip(1000);    
                        this.Close();
                        break;
                    case "Hide":
                        /*
                        if (this.Visible)
                        {
                            menuItem.Text = "Restore";
                            this.Visible = false;
                            this.ShowInTaskbar = false;

                        }
                        */
                        this.WindowState = FormWindowState.Minimized;
                        break;
                    case "Restore":
                        /*
                         * if (!this.Visible)
                        {
                            menuItem.Text = "Hide";
                            this.Visible = true;
                            this.ShowInTaskbar = true;
                        }
                         */
                        this.WindowState = FormWindowState.Normal;
                        break;
                    case "Balloon":
                        this.notifyIcon.BalloonTipText = "Status update";
                        this.notifyIcon.ShowBalloonTip(2000);           
                        break;
                    default:
                        break;
                }
            }            
        }

        private void NotifyTest_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.ShowInTaskbar = false;
                foreach (MenuItem menu in this.notifyIcon.ContextMenu.MenuItems)
                    if (menu.Name.Equals("Hide/Restore"))
                    {
                        menu.Text = "Restore";
                        break;
                    }
            }
            else if (this.WindowState == FormWindowState.Normal || this.WindowState == FormWindowState.Maximized)
            {
                this.ShowInTaskbar = true;
                foreach (MenuItem menu in this.notifyIcon.ContextMenu.MenuItems)
                    if (menu.Name.Equals("Hide/Restore"))
                    {
                        menu.Text = "Hide";
                        break;
                    }

            }
        }
        //
        //
        #endregion//Event Handlers


    }
}
