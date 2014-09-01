using System;
using System.Collections.Generic;
using System.Text;

using System.Runtime.InteropServices;
using Microsoft.Office.Interop.Excel;

namespace ExcelRTDServer
{
    // To set the debugging settings, expand the Configuration Properties folder and click Debugging.
    // In the Start Action area, click Start External Program and enter the path to Microsoft Excel
    // (for example, C:\Program Files\Microsoft Office\Office10\EXCEL.EXE).
    // We will also need to change the build settings so that the assembly is registered for COM interop by Visual Studio .NET 
    // when it gets compiled. To do this, click Build under the Configuration Properties folder and check Register for COM Interop. 


    // We'll also need to add some attributes to the class for use by COM Interop services: ProgID and ComVisible.
    //[Guid("82D9DA6A-BBDD-3646-9CC7-5724B86C77F0")]
    //[Guid("ee99c4be-85b9-472b-b7a2-41119b39acfc")]

    [ComVisible(true), ProgId("BRE.StockQuote"),Guid("E790C1A5-D5EA-35B1-B84A-31162930E7A7")]
    public class StockQuote : IRtdServer
    {
        // RTD variables
        private IRTDUpdateEvent m_XLRTDUpdate = null;           //
        
        
        // my variables that simulate periodic update of data for RTD publication.
        private System.Timers.Timer m_Timer = null;
        private Dictionary<string, RTDConnection> m_Connections = new Dictionary<string, RTDConnection>();
        private Random m_Random = null;


        //
        // Constructor
        //
        public StockQuote()
        {
        }        
        private void Timer_Elapsed(object seneder, System.Timers.ElapsedEventArgs eventArgs)
        {
            if ( m_Random == null )
            {
                m_Random = new Random();
                
            }
            
            foreach (RTDConnection conn in m_Connections.Values)
            {
                conn.Price += (m_Random.NextDouble() - 0.5) / 10.0;
                conn.Price = Math.Round(conn.Price * 100) / 100;
            }
            m_XLRTDUpdate.UpdateNotify();                               // <---- TELL Excel that we have new values.
        }


        Array IRtdServer.RefreshData(ref int TopicCount)
        {
            object[,]updateInfo = new object[2,m_Connections.Count];      // array that will be returned to Excel.
            int topicsUpdated = 0;
            foreach(RTDConnection conn in m_Connections.Values)
            {
                if (conn.TopicID != -1)
                {
                    updateInfo[0, topicsUpdated] = conn.TopicID;
                    updateInfo[1, topicsUpdated] = conn.Price;
                    topicsUpdated += 1;
                }
            }
            // Exit
            TopicCount = topicsUpdated;
            return updateInfo;
        }



        //
        // These methods are the way excel will request new data from RTD server.
        //
        object IRtdServer.ConnectData(int TopicID, ref Array strings, ref bool GetNewValues)
        {
            // Initialize model completely.
            if (!m_Timer.Enabled)
                m_Timer.Start();

            
            // Read arguments provided by Excel user:
            string[] args = new string[strings.Length];
            string stockName;
            try
            {
                strings.CopyTo(args, 0);
                GetNewValues = true;
                stockName = args[0].ToLower();
            }
            catch (Exception)
            {
                return "Error in arguments";
            }
            RTDConnection conn = null;
            if (m_Connections.TryGetValue(stockName, out conn))
            {   // This stock was already subscribed to.

            }
            else
            {   // this is new stock name.
                conn = new RTDConnection();
                m_Connections.Add(stockName, conn);
                conn.TopicID = TopicID;
            }
            return conn.Price;
        }
        void IRtdServer.DisconnectData(int TopicID)
        {
            // User no longer wants to follow this topic.
            string quoteToRemove = string.Empty;
            foreach (string s in m_Connections.Keys)
            {
                if (m_Connections[s].TopicID == TopicID)
                {
                    quoteToRemove = s;
                    break;
                }
            }
            if (! string.IsNullOrEmpty(quoteToRemove))
            {
                m_Connections.Remove(quoteToRemove);
            }
            if (m_Connections.Count == 0 && m_Timer.Enabled)
                m_Timer.Stop();
        }// DisconnectData

        
        
        //
        // Server intialization.
        //
        int IRtdServer.ServerStart(IRTDUpdateEvent CallbackObject)// this is the initialization.
        {
            m_XLRTDUpdate = CallbackObject;                     // store reference to do callbacks.
            // Can do some model initialization here.
            // But request specific information gets passed in using ConnectData() call.
            // There we will finish intializing our model.
            m_Timer = new System.Timers.Timer(2000);
            m_Timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Elapsed);

            return 1;                                           // returning 1 means all is well.
        }
        void IRtdServer.ServerTerminate()
        {
            m_XLRTDUpdate = null;                               // drop reference
            // Dispose of the model components.
            if (m_Timer != null && m_Timer.Enabled)
                m_Timer.Stop();
            m_Timer = null;
        }
        int IRtdServer.Heartbeat()
        {
            return 1;       // is healthy
        }

    

        //
        // Private class to handle multiple calls
        //
        private class RTDConnection
        {            
            public int TopicID;
            public string StockName = string.Empty;
            public double Price = 100.0;



        }// end class RTDConnection

    
    
    }
}
