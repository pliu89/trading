using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Misty.Lib.Hubs
{
    /// <summary>
    /// This method is a helper class for LogViewer.  A single instance of it
    /// is created when the LogHub is created (by the GUI thread).
    /// From then on, a LogHub can call CreateInvoke() to create new LogViewer forms 
    /// from a non-GUI thread!
    /// TODO:  This can be generalized!
    /// 1. Allow features of the LogViewer to be determined by the caller.
    /// 2. Generalize this to a Form factory service that once created can create
    /// asynchronously ANY 
    /// </summary>
    public partial class LogViewCreator : Form
    {

        #region no Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        /// <summary>
        /// This object must be created by the GUI thread.
        /// </summary>
        public LogViewCreator()
        {
            InitializeComponent();
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
        /// <summary>
        /// Called by non-gui thread to create a LogViewer.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="msg"></param>
        public void CreateInvoke(object o, string msg)
        {
            CreatorEventArgs args = new CreatorEventArgs();
            args.Message = msg;
            this.CreateInvoke(o, args);
        }
        //
        //
        //
        //
        //
        #endregion//Public Methods



        #region Private Methods and Classes
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        private class CreatorEventArgs : EventArgs
        {
            public string Message;
        }
        private void CreateInvoke(object o, EventArgs eArgs)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new EventHandler(CreateInvoke), new object[] { o, eArgs });
                //IAsyncResult res = this.BeginInvoke(new EventHandler(CreateInvoke), new object[] { o, eArgs });
                //this.EndInvoke(res);
                /*
                LogHub logHub = (LogHub)o;
                if (logHub.m_ErrorView != null)
                {
                    logHub.m_ErrorView.AddMessage("Awesome!");
                }
                else
                {
                    int nn = 0;
                }
                */
            }
            else
            {
                LogHub logHub = (LogHub)o;
                CreatorEventArgs e = (CreatorEventArgs)eArgs;
                LogViewer v = new LogViewer(logHub, true);
                v.Text = logHub.LogName + " Error";
                v.TopMost = true;
                v.Show();
                v.AddMessage(e.Message);
                if (logHub.m_ErrorView == null)
                    logHub.m_ErrorView = v;

            }
        }
        //
        //
        //
        //
        //
        #endregion// Private Methods


    }
}
