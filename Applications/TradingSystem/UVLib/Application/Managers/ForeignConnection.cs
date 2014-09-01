using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.Application.Managers
{
    using UV.Lib.IO.Xml;

    /// <summary>
    /// A description of how to connect to a foreign server, their location 
    /// and configuration.
    /// </summary>
    public class ForeignConnection : IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        // Connection parameters
        //
        public string m_UserCredFilename = string.Empty;       // name of encrypted file containing: user pw
        public string m_ConfigFilename = string.Empty;          // name of xml file with desired remote config

        public string IpAddress = "localhost";                // ip address to connect.
        public string Port = "6001";                          // port to connect

        // Foreign Server
        public ForeignServer Server = null;

        //
        // 
        //
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public ForeignConnection()
        {

        }
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


        #region no Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
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


        #region IStringifiable 
        // *****************************************************************
        // ****                     IStringifiable                      ****
        // *****************************************************************
        string IStringifiable.GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            return s.ToString();
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            List<IStringifiable> elements = null;
            return elements;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            foreach (KeyValuePair<string,string>a in attributes)
            {
                if (a.Key.Equals("IpAddress", StringComparison.CurrentCultureIgnoreCase))
                    IpAddress = a.Value;
                else if (a.Key.Equals("Port", StringComparison.CurrentCultureIgnoreCase))
                    Port = a.Value;
                else if (a.Key.Equals("Config", StringComparison.CurrentCultureIgnoreCase))
                    m_ConfigFilename = a.Value;
                else if (a.Key.Equals("UserCred", StringComparison.CurrentCultureIgnoreCase))
                    m_UserCredFilename = a.Value;
            }            
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
        }
        //
        //
        #endregion// IStringifiable





    }//end class
}
