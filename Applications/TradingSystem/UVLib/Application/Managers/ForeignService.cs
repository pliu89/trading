using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.Application.Managers
{
    using UV.Lib.IO.Xml;

    /// <summary>
    /// A description of a single service located on a foreign server.
    /// </summary>
    public class ForeignService : IService, IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Identification
        //
        public string ClassName;
        protected string m_RemoteServiceName;                 // Name of service on remote machine.
        protected string m_LocalServiceName;
        
        //
        public ForeignServer Parent = null;




        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public ForeignService()
        {
        }
        public ForeignService(IService iService)
        {
            m_RemoteServiceName = iService.ServiceName;
            ClassName = iService.GetType().FullName;
        }
        //
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        public string ServiceName
        {
            get { return m_LocalServiceName; }
            set { m_LocalServiceName = value; }
        }
        public string RemoteServiceName
        {
            get { return m_RemoteServiceName; }
        }
        //
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        //
        //
        //
        public override string ToString()
        {
            if (string.IsNullOrEmpty(m_LocalServiceName))
                return string.Format("{0}", m_RemoteServiceName);
            else
                return string.Format("{0}", m_LocalServiceName);
        }
        #endregion//Public Methods


        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        #endregion//Private Methods


        #region IStringifiable 
        // *****************************************************************
        // ****                     IStringifiable                      ****
        // *****************************************************************
        string IStringifiable.GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            s.AppendFormat(" ServiceName={0}", this.m_RemoteServiceName);
            s.AppendFormat(" ClassName={0}", this.ClassName);
            return s.ToString();
        }
        System.Collections.Generic.List<IStringifiable> IStringifiable.GetElements()
        {
            return null;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            foreach (KeyValuePair<string,string>a in attributes)
            {
                if (a.Key.Equals("ServiceName", StringComparison.CurrentCultureIgnoreCase))
                    m_RemoteServiceName = a.Value;
                else if (a.Key.Equals("ClassName", StringComparison.CurrentCultureIgnoreCase))
                    this.ClassName = a.Value;
            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {            
        }
        //
        //
        #endregion// IStringifiable




        #region IService
        public void Start()
        {            
        }
        public void Connect()
        {
        }
        public void RequestStop()
        {
            if (this.Stopping != null)
                return;
            if (this.ServiceStateChanged != null)
                return;
        }
        public event EventHandler ServiceStateChanged;
        public event EventHandler Stopping;
        #endregion// IService


    }//end class
}
