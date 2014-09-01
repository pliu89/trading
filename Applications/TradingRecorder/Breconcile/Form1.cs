using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;


namespace Ambre.Breconcile
{
    using Misty.Lib.Application;
    using Misty.Lib.Hubs;

    public partial class Form1 : Form
    {


        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // Services
        private LogHub Log = null;
        private Ftp.FtpReader m_FtpReader = null;

        // private variables
        private bool m_IsShuttingDown = false;                  

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public Form1(string[] args)
        {
            InitializeComponent();
            AppInfo m_AppInfo = AppInfo.GetInstance("Breconcile", true);

            



            m_FtpReader = new Ftp.FtpReader(true);
            Log = m_FtpReader.Log;                                          // I will use his Log.
            m_FtpReader.RequestCompleted += new EventHandler(FtpReader_RequestCompleted);

        }
        //
        //       
        #endregion//Constructors



        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
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
        // ******************************************************************
        // ****             FtpReader RequestCompleted()                *****
        // ******************************************************************
        private void FtpReader_RequestCompleted(object sender, EventArgs eventArgs)
        {
            Ftp.RequestEventArgs request = (Ftp.RequestEventArgs)eventArgs;

        }
        //
        //
        //
        //
        // *************************************
        // ****         ShutDown()          ****
        // *************************************
        private void ShutDown()
        {
            if (!m_IsShuttingDown)
            {
                m_IsShuttingDown = true;
                if (Log != null)
                {
                    Log.RequestStop();
                    Log = null;
                }

            }
        }// ShutDown()
        //
        #endregion//Private Methods


        #region Form Event Handlers
        // *****************************************************************
        // ****                 Form Event Handlers                     ****
        // *****************************************************************
        private void Button_Click(object sender, EventArgs e)
        {
            if (sender is Button)
            {
                Button button = (Button)sender;
                if (button == this.buttonGetStatements)
                {
                    if (m_FtpReader != null)
                        m_FtpReader.Request(new Ftp.RequestEventArgs(Ftp.RequestType.GetNewFiles));
                }
                else if (button == this.buttonExit)
                {
                    m_IsClosing = true;
                    this.Close();
                }
                else
                    Log.NewEntry(LogLevel.Warning, "Form: Unknown button click {0}.", button.Name);
            }            
        }//Button_Click()
        //
        //
        private bool m_IsClosing = false;
        private static int WM_Close = 0x0010;
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_Close && ! m_IsClosing)
            {   // We can grab the Close notification, and stop it from carrying it out.

            }
            else
                base.WndProc(ref m);
        }
        //
        //
        //
        private void Form_Closing(object sender, FormClosingEventArgs e)
        {
            ShutDown();

        }
        //
        #endregion// Form Event Handlers

    }
}
