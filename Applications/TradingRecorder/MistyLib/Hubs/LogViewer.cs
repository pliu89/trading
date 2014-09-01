using System;
using System.Runtime.InteropServices;   // needed for DLL load
using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
//using System.Drawing;
//using System.Linq;
//using System.Text;
using System.Windows.Forms;

namespace Misty.Lib.Hubs
{
    public partial class LogViewer : Form
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //   
        // My object pointers.
        private LogHub m_LogHub = null;
        private bool m_IsMouseHover = false;
        private bool m_IsClosing = false;                                   // set to true once we get a request to close.
        private System.Drawing.Color[] m_LastUpdateColor = new System.Drawing.Color[2];
        private int m_LastUpdateColorIndex = 0;

        // Queue
        private object m_QueueLock = new object();
        private Queue<string> m_Queue = new Queue<string>();

        // update throttle
        private double m_UpdateIntervalSeconds = -2.0;
        private DateTime m_NextUpdate = DateTime.Now;
        private Timer m_Timer;

        // Formatting
        const int MaxLinesOfHistory = 1000;

        //
        //
        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        public LogViewer(LogHub loghub)
        {
            InitializeComponent();
            InitializeLogHub(loghub, loghub.LogName + " Log");
        }
        public LogViewer(LogHub loghub, bool disableUserControls)
        {
            InitializeComponent();
            InitializeLogHub(loghub, loghub.LogName + " Log");
            if (disableUserControls)
            {
                this.comboLogLevel.Visible = false;
            }

        }
        //
        private void InitializeLogHub(LogHub logHub, string windowName)
        {
            m_LogHub = logHub;
            this.Text = windowName;

            // Initialize my timer.
            m_Timer = new Timer();
            m_Timer.Interval = 100;// (int)(1000 * m_UpdateIntervalSeconds);
            m_Timer.Tick += new EventHandler(Timer_Tick);
            if (m_UpdateIntervalSeconds > 0)
            {
                m_Timer.Interval = (int)(1000 * m_UpdateIntervalSeconds);
                //m_Timer.Enabled = true;
                m_Timer.Start();
            }

            //
            // Create our combo box for message levels
            //
            string currentMsgLevel = Enum.GetName(typeof(LogLevel), m_LogHub.AllowedMessages);
            comboLogLevel.BeginUpdate();
            foreach (string level in Enum.GetNames(typeof(LogLevel)))
            {
                comboLogLevel.Items.Add(level);
            }
            comboLogLevel.SelectedItem = currentMsgLevel;
            comboLogLevel.EndUpdate();

            m_LastUpdateColor[0] = textLastUpdate.BackColor;
            m_LastUpdateColor[1] = System.Drawing.Color.Yellow;
        }
        //
        //
        // ****				System 32			****
        //
        [DllImport("user32.dll")]
        static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetScrollPos(IntPtr hWnd, int nBar);
        private const int SB_VERT = 0x1;
        //
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        public double UpdateIntervalSeconds
        {
            get
            {
                return m_UpdateIntervalSeconds;
            }
            set
            {
                if (value > 0)
                {
                    m_UpdateIntervalSeconds = value;
                    m_Timer.Interval = (int)(1000 * m_UpdateIntervalSeconds);  // update this value.
                }
            }
        }
        //
        //
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        // ****             AddMesssage()               ****
        /// <summary>
        /// Called by external thread to add a new message to be displayed.
        /// </summary>
        /// <param name="msg"></param>
        public void AddMessage(string msg)
        {
            if (this.IsDisposed || this.m_IsClosing) { return; }
            // Store the message
            if (!string.IsNullOrEmpty(msg))
            {
                lock (m_QueueLock)
                {
                    m_Queue.Enqueue(msg);
                }
            }

            // Request an update for gui, but throttled by a timer.            
            if (DateTime.Compare(DateTime.Now, m_NextUpdate) > 0.0)
            {   // if now is later than NextUpdate, and no pending timer events, then update now!
                LogUpdate(null, null);  // buzz the gui to update.
            }
            else
            {   // if we havent passed enough time, 
                if (!m_Timer.Enabled) { m_Timer.Enabled = true; }
            }
        }//end AddMessage().
        //
        //
        //
        //
        // ****             LogUpdate()                 ****
        //
        /// <summary>
        /// Called by external or GUI thread.  This forces the logviewer to update itself
        /// and reflect the loghub.  
        /// </summary>
        /// <param name="msg"></param>
        private void LogUpdate(object sender, EventArgs args)
        {
            // Validation check
            if (m_IsClosing) { return; }

            // Process the event callback.
            if (this.InvokeRequired)
            {   // If another thread has called me, set up an invoke.
                EventHandler d = new EventHandler(LogUpdate);
                // TODO: fix this on exiting, we can be disposing of the viewer, and invoke will fail.
                // perhaps, request that Log thread exits viewer nicely to avoid collisions between threads.
                try
                {
                    if (!m_IsClosing) this.Invoke(d, new object[]{this, EventArgs.Empty});
                }
                catch (Exception) { return; }
            }
            else
            {

                // Check and update message queue.
                if (!m_IsMouseHover)
                {
                    this.SuspendLayout();
                    if (m_Queue.Count > 200 || checkBoxAutoScroll.Checked)
                    {
                        lock (m_QueueLock)
                        {
                            while (m_Queue.Count > 0)
                            {
                                string msg = string.Format("{0}\r\n", m_Queue.Dequeue());
                                try
                                {
                                    textBox1.AppendText(msg);               // TODO: fix this! exception here when main form is killed.
                                    textBox1.Text.Insert(textBox1.TextLength, msg);
                                }
                                catch (Exception)
                                {
                                }
                            }
                        }//end QueueLock
                    }
                    //
                    // Remove very old messages                    
                    //
                    if (textBox1.Lines.Length > MaxLinesOfHistory)
                    {
                        //const int linesToSave = 100;
                        //int startOfCut = textBox1.Lines.Length - linesToSave;
                        //string[] savedLines = new string[linesToSave];
                        //savedLines[0] = "....<cut>.....";
                        //for (int i = 0; i < linesToSave; i++) { savedLines[1 + i] = textBox1.Lines[startOfCut + i]; }
                        textBox1.Clear();
                        //textBox1.Lines = savedLines;
                    }
                    this.ResumeLayout();
                }
                else
                {   // mouse hovering!
                    m_LastUpdateColorIndex = (1 + m_LastUpdateColorIndex) % 2;
                    textLastUpdate.BackColor = m_LastUpdateColor[m_LastUpdateColorIndex];

                }

                // Update the log state
                if (!comboLogLevel.IsDisposed)
                {
                    string currentMsgLevel = Enum.GetName(typeof(LogLevel), m_LogHub.AllowedMessages);
                    if (!String.Equals((string)comboLogLevel.SelectedItem, currentMsgLevel))
                    {   // update the combo box.
                        comboLogLevel.SelectedItem = currentMsgLevel;
                    }

                }
                textLastUpdate.Text = m_NextUpdate.ToLongTimeString();
                if (m_UpdateIntervalSeconds > 0)
                    m_NextUpdate = DateTime.Now.AddSeconds(m_UpdateIntervalSeconds);
            }
        }//end LogUpdate().
        //
        //
        //
        // ****             LogClose()                 ****
        //
        /// <summary>
        /// Called by external or GUI thread.  This forces the logviewer to update itself
        /// and reflect the loghub.  
        /// </summary>
        /// <param name="msg"></param>
        public void LogClose(object sender, EventArgs args)
        {
            // Validation check  
            m_IsClosing = true;
            if (this.IsDisposed ) { return; }
            m_LogHub.RemoveLogViewer(this);
            if (!m_Timer.Enabled) { m_Timer.Enabled = true; }               // wake up timer, which will turn us off.

            /*
            // Process the event callback.
            if (this.InvokeRequired)
            {   // If another thread has called me, set up an invoke.
                EventHandler d = new EventHandler(LogClose);
                // TODO: fix this on exiting, we can be disposing of the viewer, and invoke will fail.
                // perhaps, request that Log thread exits viewer nicely to avoid collisions between threads.
                try { this.Invoke(d, EventArgs.Empty); }
                catch (Exception) { return; }
            }
            else
            {
                this.Close();
            }
            */ 
        }
        //
        //
        #endregion//Public Methods







        #region GUI Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        //
        //
        // ****         Timer_Tick()            ****
        //
        private void Timer_Tick(object sender, EventArgs e)
        {
            m_Timer.Stop();
            if (m_IsClosing)
            {   // Start closing form
                this.Close();
            }
            else
            {            
                LogUpdate(null, null);      // buzz the gui to update.
            }
        }//Timer_Tick()
        //
        //
        //
        //
        private void comboLogLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            object selected = comboLogLevel.SelectedItem;
            if (selected != null)
            {
                string newLevel = (string)selected;
                string currentLevel = Enum.GetName(typeof(LogLevel), m_LogHub.AllowedMessages);
                if (!string.Equals(newLevel, currentLevel))
                {   // update the log's level
                    LogLevel newType = (LogLevel)Enum.Parse(typeof(LogLevel), newLevel);
                    m_LogHub.AllowedMessages = newType;
                }
            }

        }
        //
        //
        //
        private void checkBoxAutoScroll_CheckedChanged(object sender, EventArgs e)
        {
            if (this.m_IsClosing) { return; }
            if (checkBoxAutoScroll.Checked)
            {
                LogUpdate(this, EventArgs.Empty);
            }
        }

        private void textBox1_MouseHover(object sender, EventArgs e)
        {
            m_IsMouseHover = true;
            textLastUpdate.BackColor = m_LastUpdateColor[1];
        }

        private void textBox1_MouseLeave(object sender, EventArgs e)
        {
            m_IsMouseHover = false;
            textLastUpdate.BackColor = m_LastUpdateColor[0];
        }
        private void LogViewer_SizeChanged(object sender, EventArgs e)
        {
            //int ii = 0;
            if (this.WindowState != FormWindowState.Minimized)
                m_LogHub.IsViewActive = true;
            else if (this.WindowState == FormWindowState.Minimized)
                m_LogHub.IsViewActive = false;
        }
        private void LogViewer_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_LogHub.RemoveLogViewer(this);
            m_IsClosing = true;

        }
        private void LogViewer_Dispose(object sender, EventArgs e)
        {
            m_IsClosing = true;
        }
        //
        //
        //
        //
        #endregion//Event Handlers






    }//end class
}
