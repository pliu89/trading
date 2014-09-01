using System;
using System.Collections.Generic;
using System.Text;

namespace NakedRtdServer
{
    using System.Runtime.InteropServices;                       // com directives
    using Microsoft.Office.Interop.Excel;

    [ComVisible(true),ProgId("Bre.NakedRtd")]
    public class RtdServer : IRtdServer
    {
        // Members
        private IRTDUpdateEvent m_XLRTDUpdate = null;
        private Dictionary<int, MyData> m_TopicList = new Dictionary<int, MyData>();
        private object m_TopicListLock = new object();
        private System.Timers.Timer m_Timer = null;


        public RtdServer()
        {
        }


        
        //
        // IRtdServer interface
        //
        Array IRtdServer.RefreshData(ref int TopicCount)
        {
            int topicsUpdated = 0;
            object[,] updateInfo = null;
            lock (m_TopicListLock)
            {
                updateInfo = new object[2, m_TopicList.Count];      // array that will be returned to Excel.
            
                foreach (int topicID in m_TopicList.Keys)
                {
                    updateInfo[0, topicsUpdated] = topicID;
                    updateInfo[1, topicsUpdated] = m_TopicList[topicID].n.ToString();
                    topicsUpdated += 1;
                }
            }
            // Exit
            TopicCount = topicsUpdated;
            return updateInfo;
        }
        object IRtdServer.ConnectData(int TopicID, ref Array strings, ref bool GetNewValues)
        {
            

            // default response
            string responseStr = string.Empty;
            GetNewValues = true;
                
            // Read arguments provided by Excel user:
            string[] args = new string[strings.Length];
            strings.CopyTo(args, 0);
            if (args.Length > 0)
            {
                int startValue = 0;    
                if (Int32.TryParse(args[0], out startValue))
                {
                    GetNewValues = true;                    
                    responseStr = startValue.ToString();
                    MyData newData = new MyData();
                    newData.n = startValue;
                    lock (m_TopicListLock)
                    {
                        m_TopicList.Add(TopicID, newData);
                    }
                }
            }

            // Initialize model completely.
            if (!m_Timer.Enabled)
                m_Timer.Start();
            return responseStr;
        }
        //
        void IRtdServer.DisconnectData(int TopicID)
        {
            lock (m_TopicListLock)
            {
                if (m_TopicList.ContainsKey(TopicID))
                    m_TopicList.Remove(TopicID);

                if (m_TopicList.Count == 0 && m_Timer.Enabled)
                    m_Timer.Stop();
            }
        }// DisconnectData
        //
        int IRtdServer.ServerStart(IRTDUpdateEvent CallbackObject)
        {
            m_XLRTDUpdate = CallbackObject;                     // store reference to do callbacks.
            m_Timer = new System.Timers.Timer(2000);
            m_Timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Elapsed);
            return 1;                                           // returning 1 means all is well.
        }
        void IRtdServer.ServerTerminate()
        {
            m_XLRTDUpdate = null;                               // drop reference
            if (m_Timer != null && m_Timer.Enabled)
                m_Timer.Stop();
            m_Timer = null;
        }
        int IRtdServer.Heartbeat()
        {
            return 1;       // is healthy
        }


        private void Timer_Elapsed(object sender, EventArgs eventArgs)
        {
            lock (m_TopicListLock)
            {
                try
                {

                    foreach (MyData myData in m_TopicList.Values)
                    {
                        myData.n = myData.n + 1;
                    }
                }
                catch (Exception e)
                {
                    string msg = e.Message;
                }
            }
            m_XLRTDUpdate.UpdateNotify();

        }

        private class MyData
        {
            public int n = 0;
        }


    }
}
