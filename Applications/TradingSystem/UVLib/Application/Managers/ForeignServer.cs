using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.Application.Managers
{
    using UV.Lib.IO.Xml;

    /// <summary>
    /// </summary>
    public class ForeignServer : IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        // Identification
        //
        private static int m_LastServerId = -1;
        public readonly int ServerId;                       // This is a locally unique id for each foreign server.
        public string UniqueTag = string.Empty;             // This is unique name, set by server that initiated connection.
        public int ConversationId = -1;                     // This is the current conversation server is sitting on.

        // Parent
        public ServiceManager m_Manager = null;

        // Internal variables
        public Dictionary<string, ForeignService> Services = new Dictionary<string, ForeignService>();


        #endregion// members


        #region Constructors 
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public ForeignServer()
        {

        }
        public ForeignServer(ServiceManager myManager)
        {
            this.ServerId = System.Threading.Interlocked.Increment(ref m_LastServerId);
            this.m_Manager = myManager;
        }
        //
        //
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


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        // *************************************************
        // ****             TryAddService()             ****
        // *************************************************
        /// <summary>
        /// This ForeignServer will try to register this ForeignSerivce with AppServices.
        /// It will try to create a new LocalServiceName = "Remote Service Name" + "@" + "ServerId"
        /// so that it will be unique in the *local* application service list.
        /// </summary>
        /// <param name="foreignService"></param>
        /// <returns></returns>
        public bool TryAddService(ForeignService foreignService)
        {
            bool isSuccess = true;

            // Store service inside this server.
            foreignService.Parent = this;            // Connect events to the service before registering it.
            if ( this.Services.ContainsKey( foreignService.RemoteServiceName ) )
            {
                return false;
            }
            else
            {
                this.Services.Add(foreignService.RemoteServiceName, foreignService);
            }            

            // Create unique name and register with AppServices.
            AppServices app = AppServices.GetInstance();
            /*
            foreignService.ServiceName = string.Format("{0}@{1}",foreignService.RemoteServiceName,this.ServerId);   // Usually this is enough for uniqueness.
            if (! app.TryAddService(foreignService) )
            {   // This should never happen since the remote name is unique on the foreign server, 
                // and my server id is unique.
                int n = (int)'A';
                foreignService.ServiceName = string.Format("{0}@{1}{2}", foreignService.RemoteServiceName, this.ServerId, Convert.ToChar(n));
                while ( ! app.TryAddService(foreignService) && n < 123)
                    foreignService.ServiceName = string.Format("{0}@{1}{2}", foreignService.RemoteServiceName, this.ServerId, Convert.ToChar(++n));
                if (n >= 123)
                    isSuccess = false;
            }
            */ 
            // new approach is to assume unique names are employed.
            foreignService.ServiceName = foreignService.RemoteServiceName;
            if (!app.TryAddService(foreignService))
            {
                isSuccess = false;
            }
            return isSuccess;
        }// TryAddService()
        //
        //
        public bool TrySendMessage( Message msg )
        {
            if (ConversationId >= 0 && m_Manager!=null)
                return m_Manager.SendMessage(ConversationId, msg);
            else 
                return false;
        }// TrySendMessage()
        //
        public bool TryGetService(string serviceName, out ForeignService iService)
        {
            return Services.TryGetValue(serviceName, out iService);
        }
        //
        //
        public static string CreateUniqueTag(string baseTag, DateTime dt)
        {
            return string.Format("{0}{1}", baseTag, dt.ToString("yyMMdd.HHmmss.fff"));
        }
        public override string ToString()
        {
            return string.Format("@{1}[#{0}]", this.ConversationId,this.ServerId);
        }
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


        #region IStringifiable
        // *****************************************************************
        // ****                     IStringifiable                      ****
        // *****************************************************************
        public string GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            if (!string.IsNullOrEmpty(this.UniqueTag))
                s.AppendFormat(" UniqueTag={0}", this.UniqueTag);
            return string.Empty;
        }
        public List<IStringifiable> GetElements()
        {
            return null;
        }
        public void SetAttributes(Dictionary<string, string> attributes)
        {
            foreach (KeyValuePair<string, string> a in attributes)
            {
                if (a.Key.Equals("UniqueTag", StringComparison.CurrentCultureIgnoreCase))
                    UniqueTag = a.Value;
            }
        }
        public void AddSubElement(IStringifiable subElement)
        {
        }
        //
        //
        #endregion// IStringifiable

    }//end class
}
