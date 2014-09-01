using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;

namespace Ambre.XL
{
    using System.Runtime.InteropServices;           // needed for com arguments.
    using Microsoft.Office.Interop.Excel;           // needed for RTD interface and objects.

    using Ambre.Lib.ExcelRTD;
    /// <summary>
    /// This is a base class for an RTD server.   
    /// Threading: The IRtdServer methods are called by the Excel thread, and the IRTDUpdateEvent delegate is
    /// triggered periodically by a thread from the Timer ThreadPool.  They may collide when accessing the TopicList dictionary.
    /// </summary>
    public class RTDServerBase : IRtdServer
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // 
        // Internal book keeping
        protected IRTDUpdateEvent m_RtdUpdateEvent = null;                    // delegate for call back into excel.
        protected ConcurrentDictionary<int, TopicBase> m_Topics = new ConcurrentDictionary<int, TopicBase>();
        protected System.Timers.Timer m_Timer = null;                         // timer for udpate throttling.
        protected int ThrottleTime = 2000;                                    // default update throttle in msecs.


        #endregion// members



        #region Methods to override
        // *****************************************************************
        // ****                 Virtual Methods                         ****
        // *****************************************************************
        /// <summary>
        /// These OnRtdServer...() methods are triggered within the IRtdServer interface
        /// implementation below, and are all called by the Excel.
        /// The purpose is to allow the super-object that overwrites these methods to 
        /// know what excel is doing (without using event handlers).
        /// The first is fired from  IRtdServer.ServerStart() when the RTD server is started; 
        /// that is, when the excel workbook is started.
        /// </summary>
        protected virtual void OnRtdServerStart()
        {
        }
        //
        /// <summary>
        /// Fired by excel thread when excel workbook shuts down, along with its RTD server.
        /// </summary>
        protected virtual void OnRtdServerTerminate()
        {
        }
        /// <summary>
        /// After startup, when excel detects a new RTD link in a cell, this event is fired
        /// by excel thread.  
        /// This method must create a new topic and add it to the list.
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns>value to be displayed immediately in excel cell.</returns>
        protected virtual string OnRtdConnectData(ref TopicBase newTopic)
        {
            return string.Empty;                            // what appears in cell immediately.
        }
        /// <summary>
        /// Called when a excel rtd link is deleted thereby removing the need to 
        /// continue updating this topic.
        /// </summary>
        /// <param name="topicToRemove"></param>
        protected virtual void OnRtdDisconnect(TopicBase topicToRemove)
        {
        }
        //
        /// <summary>
        /// This is called when the throttle timer has fired its Tick event, 
        /// just prior to the update notification being sent to excel. 
        /// </summary>
        protected virtual void OnUpdateNotify()
        {
        }
        #endregion // virtual methods



        #region RTD Implementation Event Handlers
        // *****************************************************************
        // ****                 Server Start()                          ****
        // *****************************************************************
        /// <summary>
        ///  These methods implement the IRtdServer interface, allowing us to receive
        ///  calls from excel thread.
        ///  This is triggered when excel starts the new workbook.
        /// </summary>
        /// <param name="CallbackObject">Object we use to inform excel of updates</param>
        /// <returns>1 (healthy RTD server)</returns>
        int IRtdServer.ServerStart(IRTDUpdateEvent CallbackObject)          // this is the initialization.
        {
            m_RtdUpdateEvent = CallbackObject;                              // store reference to do callbacks.
            OnRtdServerStart();
            if (m_Timer == null)
            {
                m_Timer = new System.Timers.Timer();
                m_Timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Elapsed);
            }
            return 1;                                                       // returning 1 = "is healthy".
        }// ServerStart()
        //
        // *******************************************************************
        // ****                 Server Terminate()                        ****
        // *******************************************************************
        //
        void IRtdServer.ServerTerminate()
        {
            OnRtdServerTerminate();
            if (m_Timer != null)                                            // dispose of the timer.
            {
                m_Timer.Stop();
                m_Timer.Elapsed -= new System.Timers.ElapsedEventHandler(Timer_Elapsed);
                m_Timer = null;
            }
            m_RtdUpdateEvent = null;                                        // drop reference
        }//ServerTerminate()
        // *******************************************************************
        // ****                     HeartBeat()                           ****
        // *******************************************************************
        int IRtdServer.Heartbeat()
        {
            return 1;                                                       // returns 1 = "is healthy"
        }// HeartBeat()
        //
        // *******************************************************************
        // ****                     ConnectData()                         ****
        // *******************************************************************
        /// <summary>
        /// This is called for each cell with an RTD link. It's called only the first time they 
        /// are entered into the cell (or at startup).
        /// </summary>
        /// <param name="TopicID">Excel's id# for this RTD link.</param>
        /// <param name="strings">Arguments in RTD command passed by excel</param>
        /// <param name="GetNewValues"></param>
        /// <returns>Object to be displayed in cell now.</returns>
        object IRtdServer.ConnectData(int TopicID, ref Array strings, ref bool GetNewValues)
        {
            object returnObject = string.Empty;
            if (strings.Length > 0)
            {
                string[] arguments = new string[strings.Length];                // Read arguments provided by Excel user:
                strings.CopyTo(arguments, 0);
                TopicBase topic = new TopicBase(TopicID,arguments);
                m_Topics.TryAdd(topic.TopicID, topic);
                string response = OnRtdConnectData(ref topic);                     // create topic, add to list.
                double x;
                if (Double.TryParse(response, out x))
                    returnObject = x;
                else
                    returnObject = response;
                GetNewValues = true;                                            // tells Excel it will have to call back to get new values.
            }
            else
                GetNewValues = false;                                           // tells Excel it will have to call back to get new values.

            if ((!m_Topics.IsEmpty) && (!m_Timer.Enabled))                   // Make sure timer for message throttleing is running.            
            {
                m_Timer.Interval = ThrottleTime;
                m_Timer.Start();
            }
            return returnObject;
        }// ConnectData()
        //
        //
        // *******************************************************************
        // ****                     DisconnectData()                      ****
        // *******************************************************************
        /// <summary>
        /// Excel user no longer wants to follow this topic.
        /// </summary>
        /// <param name="TopicID">id of topic to drop.</param>
        void IRtdServer.DisconnectData(int TopicID)
        {
            TopicBase topicToRemove = null;
            if (m_Topics.TryRemove(TopicID, out topicToRemove))
            {
                OnRtdDisconnect(topicToRemove);
            }
            if (m_Topics.IsEmpty && m_Timer != null && m_Timer.Enabled)
                m_Timer.Stop();                                             // last RTD link on sheet is removed.  Shut off timer.
        }// DisconnectData()
        //
        // *******************************************************************
        // ****                     RefreshData()                         ****
        // *******************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="TopicCount">Number of elements in array containing updates.</param>
        /// <returns>array of objects passed back to excel.</returns>
        Array IRtdServer.RefreshData(ref int TopicCount)
        {
            object[,] updateInfo = new object[2, m_Topics.Count];           // array that will be returned to Excel.
            int topicsUpdated = 0;                                          // counter of which topics need updating.
            double x;
            foreach (TopicBase topic in m_Topics.Values)
            {
                if (topic.TopicID != -1 && topic.IsChangedSinceLastRead)
                {
                    updateInfo[0, topicsUpdated] = topic.TopicID;
                    string newValue = topic.ReadValue();       // this marks topic as "read" !!!
                    if (double.TryParse(newValue, out x))
                        updateInfo[1, topicsUpdated] = x;
                    else
                        updateInfo[1, topicsUpdated] = newValue;
                    topicsUpdated += 1;
                }
            }
            // Exit
            TopicCount = topicsUpdated;
            return updateInfo;
        }//RefreshData()
        //
        #endregion//RTD Event Handlers



        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        /// <summary>
        /// This is called periodically using a thread from a thread pool.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs eventArgs)
        {
            OnUpdateNotify();
            // Check if topics have changed.
            bool notifyExcel = false;
            foreach (TopicBase topic in m_Topics.Values)
                if (topic.IsChangedSinceLastRead)
                {
                    notifyExcel = true;
                    break;
                }
            if (notifyExcel)
                m_RtdUpdateEvent.UpdateNotify();
        }
        //
        //
        #endregion//Event Handlers

    }
}
