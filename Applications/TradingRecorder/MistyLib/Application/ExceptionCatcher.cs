using System;
using System.Collections.Generic;
using System.Text;

namespace Misty.Lib.Application
{
    /// <summary>
    /// Some encapsulated code for exception processing.
    /// </summary>
    public static class ExceptionCatcher
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // 
        private static bool m_IsApplicationAborting = false;                           // set when user chooses to "abort"

        
        //
        //
        //
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        public static bool IsApplicationAborting
        {
            get { return m_IsApplicationAborting; }
        }
        //
        //
        //
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        // *********************************************************
        // ****             Query User Take Action()             ****
        // *********************************************************
        /// <summary>
        /// Opens a form asking the user whether exception should be retried or ignored.
        /// </summary>
        /// <param name="exceptionThrown">thrown exception object</param>
        /// <param name="hub">hub that experienced exception</param>
        /// <param name="userMessage">optional user text for message</param>
        /// <returns>DialogResult selected by user.</returns>
        public static System.Windows.Forms.DialogResult QueryUserTakeAction(Exception exceptionThrown, Misty.Lib.Hubs.Hub hub, string userMessage = null, EventArgs eventThatFailed = null)
        {
            if (m_IsApplicationAborting)
                return System.Windows.Forms.DialogResult.Ignore;            // once we are aborting, simply ignore all other exceptions

            
            //
            // Create message for window
            //            
            string caption = string.Format("{0} exception", System.Threading.Thread.CurrentThread.Name); // title bar
            
            System.Text.StringBuilder msg = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(userMessage))
                msg.Append(userMessage);                                                // use user's custom message
            else if (eventThatFailed != null && eventThatFailed != EventArgs.Empty)
                string.Format("Failed to process {0}.", eventThatFailed);               // else make a message using the event, if provided.
            else
            {
                eventThatFailed = EventArgs.Empty;
                msg.Append("Exception thrown.");                                        // else write generic message
            }
            msg.Append("\n\rSelect: Abort nicely, retry event, or ignore it.");
            msg.AppendFormat("\n\rException: {0} ", exceptionThrown.Message);
            msg.AppendFormat("{0}", exceptionThrown.StackTrace);

            System.Windows.Forms.DialogResult result = System.Windows.Forms.MessageBox.Show(
                msg.ToString(), caption,
                System.Windows.Forms.MessageBoxButtons.AbortRetryIgnore, System.Windows.Forms.MessageBoxIcon.Warning,
                System.Windows.Forms.MessageBoxDefaultButton.Button3);  // TODO:L would be nice to 
            //
            // Optionally take action.
            //
            if (hub != null)
            {
                hub.Log.NewEntry(Misty.Lib.Hubs.LogLevel.Major, "ExceptionCatcher: {0}", msg.ToString());
                hub.Log.NewEntry(Misty.Lib.Hubs.LogLevel.Major, "ExceptionCatcher: User chooses to {0}", result);
                if (result == System.Windows.Forms.DialogResult.Abort)
                {   // ABORT application
                    hub.Log.Flush();
                    Misty.Lib.Application.AppInfo.GetInstance().RequestShutdown(hub, eventThatFailed);
                }
                else if (result == System.Windows.Forms.DialogResult.Retry)
                {   // Resubmit the offending event
                    if (eventThatFailed!=null && eventThatFailed != EventArgs.Empty)
                        hub.HubEventEnqueue(eventThatFailed);
                }
                else if (result == System.Windows.Forms.DialogResult.Ignore)
                {   // 
                    hub.Log.Flush();
                }
                else
                {

                }
            }

            //
            // Exit
            //
            if (result == System.Windows.Forms.DialogResult.Abort)
                m_IsApplicationAborting = true;
            return result;
        } // 
        //
        //
        /// <summary>
        /// Simple over-loading useful for Hub objects.
        /// </summary>
        /// <param name="exceptionThrown">thrown exception</param>
        /// <param name="hub">hub that experienced exception</param>
        /// <param name="eventThatFailed">event that we were processing when the exception was thrown.</param>
        /// <returns>>DialogResult selected by user.</returns>
        public static System.Windows.Forms.DialogResult QueryUserTakeAction(Exception exceptionThrown, Misty.Lib.Hubs.Hub hub, EventArgs eventThatFailed)
        {
            return QueryUserTakeAction(exceptionThrown, hub, string.Format("Failed to process {0}.", eventThatFailed));
        }
        //
        //
        // *********************************************************
        // ****                 Query User()                    ****
        // *********************************************************
        /// <summary>
        /// Opens a form asking the user whether exception should be retried or ignored.
        /// </summary>
        /// <param name="exceptionThrown">thrown exception object</param>
        /// <param name="userMessage">Optional text containing information about what failed.</param>
        /// <returns>DialogResult selected by user.</returns>
        public static System.Windows.Forms.DialogResult QueryUser(Exception exceptionThrown, string userMessage = null)
        {            
            return QueryUserTakeAction(exceptionThrown,null,userMessage);
        } // 
        //
        //
        //
        /// <summary>
        /// Simple over-loading useful for Hub objects.
        /// </summary>
        /// <param name="exceptionThrown">thrown exception</param>
        /// <param name="eventThatFailed">event that we were processing when the exception was thrown.</param>
        /// <returns>>DialogResult selected by user.</returns>
        public static System.Windows.Forms.DialogResult QueryUser(Exception exceptionThrown, EventArgs eventThatFailed)
        {
            return QueryUserTakeAction(exceptionThrown,null,string.Format("Failed to process {0}.",eventThatFailed));
        }
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

    }
}
