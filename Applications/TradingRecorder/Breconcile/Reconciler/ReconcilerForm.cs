using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Google.GData.Client;

namespace Ambre.Breconcile.Reconciler
{
    using Misty.Lib.Application;
    using Misty.Lib.Hubs;

    using Misty.Lib.TaskHubs;

    using Misty.Lib.IO.Xml;

    /// <summary>
    /// This is the main entry point for the reconciler.
    /// </summary>
    public partial class ReconcilerForm : Form
    {
       
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // Services
        private LogHub Log = null;
        private List<ReconcilerTaskHub> m_ReconcilerTaskHubs = new List<ReconcilerTaskHub>();

        // private control variables
        private bool m_IsShuttingDown = false;



        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public ReconcilerForm(string[] cmdLineArgs)
        {
            InitializeComponent();
            AppInfo m_AppInfo = AppInfo.GetInstance("Breconcile", true);
            m_AppInfo.RequestShutdownAddHandler(new EventHandler(RequestShutDown));     // register my handler as the shutdown request handler.


            string filePath = string.Empty;
            if ( cmdLineArgs != null && cmdLineArgs.Length > 0 )
                filePath = string.Format("{0}{1}",m_AppInfo.UserConfigPath,cmdLineArgs[0].Trim());
            else
                filePath = string.Format("{0}ReconcilerConfig.txt",m_AppInfo.UserConfigPath);

            // here is temp hard code config file address
            // filePath = "\\\\fileserver\\Users\\DV_Ambre\\AmbreUsers\\dvbre\\Config\\ReconcilerConfig.txt";

            // Create the services defined in the config file.
            using (StringifiableReader reader = new StringifiableReader(filePath))
            {
                List<IStringifiable> objectList = reader.ReadToEnd();
                foreach (IStringifiable obj in objectList)
                {
                    if (obj is ReconcilerTaskHub)
                    {
                        ReconcilerTaskHub newHub = (ReconcilerTaskHub)obj;
                        m_ReconcilerTaskHubs.Add(newHub);
                        if (Log == null)
                            Log = newHub.Log;              // accept first Log as the form's log.
                        newHub.TaskCompleted += new EventHandler(Reconciler_RequestCompleted);
                        newHub.Stopping += new EventHandler(Reconciler_Stopping);
                    }
                }
            }

            // Log start up.
            if (Log != null)
            {
                Log.NewEntry(LogLevel.Minor, "ReconcilerForm: Running config file {0}", filePath);
                if (Log.BeginEntry(LogLevel.Minor, "ReconcilerForm: {0} TaskHubs: ", m_ReconcilerTaskHubs.Count))
                {
                    foreach (ReconcilerTaskHub hub in m_ReconcilerTaskHubs)
                        Log.AppendEntry("<{0}>", hub.GetAttributes());
                }
            }

            // Start hubs
            foreach (ReconcilerTaskHub hub in m_ReconcilerTaskHubs)
                hub.Start();
        }
        //
        //       
        #endregion//Constructors



        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        private void Reconciler_RequestCompleted(object sender, EventArgs eventArg)
        {
            if (eventArg is Misty.Lib.TaskHubs.TaskEventArg)
            {
                Misty.Lib.TaskHubs.TaskEventArg request = (Misty.Lib.TaskHubs.TaskEventArg)eventArg;
                if (Log != null)
                    Log.NewEntry(LogLevel.Major, "ReconcileForm: Request completed for {0}.  Requesting shut down", request);
                
                // Request a shutdown soon.             
                if (Log != null)
                    Log.NewEntry(LogLevel.Major, "ReconcileForm: Requesting a shut down.");
                TaskEventArg task = new TaskEventArg();
                task.RequestStop = true;
                task.StartTime = DateTime.Now.AddSeconds(20);
                if (m_ReconcilerTaskHubs.Count > 0)
                    m_ReconcilerTaskHubs[0].AddNewTask(task);
            }
        }//Reconciler_RequestCompleted()
        //
        //
        //
        private void Reconciler_Stopping(object sender, EventArgs eventArg)
        {
            if (sender is Reconciler.ReconcilerTaskHub)
            {
                ReconcilerTaskHub hub = (ReconcilerTaskHub)sender;
                if (m_ReconcilerTaskHubs.Contains(hub))
                    m_ReconcilerTaskHubs.Remove(hub);
            }
            // Check for closing condition
            if (m_ReconcilerTaskHubs.Count == 0 )
                RequestShutDown(this, eventArg);
        }// Reconciler_Stopping()
        //
        //
        //
        // *************************************************************
        // ****                     ShutDown()                      ****
        // *************************************************************
        private void ShutDown()
        {
            if (!m_IsShuttingDown)
            {
                m_IsShuttingDown = true;
                while (m_ReconcilerTaskHubs.Count > 0)
                {
                    m_ReconcilerTaskHubs[0].RequestStop();
                    m_ReconcilerTaskHubs.RemoveAt(0);
                }
            }
        }// ShutDown()
        //
        private void RequestShutDown(object sender, EventArgs e)
        {
            if (!m_IsShuttingDown)
            {
                if (this.InvokeRequired)
                    this.Invoke(new EventHandler(RequestShutDown), new object[] { sender, e });
                else
                    this.Close();
            }
        }// RequestShutDown()
        //
        //
        #endregion//Private Methods



        #region Form Event Handlers
        // *****************************************************************
        // ****                 Form Event Handlers                     ****
        // *****************************************************************
        private void Button_Click(object sender, EventArgs e)
        {
           
        }
        //
        //
        //private bool m_IsClosing = false;
        //private static int WM_Close = 0x0010;
        //protected override void WndProc(ref Message m)
        //{
        //    if (m.Msg == WM_Close && ! m_IsClosing)
        //    else
        //        base.WndProc(ref m);
        //}
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
