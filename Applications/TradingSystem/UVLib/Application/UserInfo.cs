using System;
using System.Collections.Generic;
using System.Text;


namespace UV.Lib.Application
{
    using Lib.IO.Xml;

    public class UserInfo : IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //     
        //
        public string UserName = string.Empty;
        public RunType RunType = RunType.Debug;



        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public UserInfo()
        {

        }
        public UserInfo(UserInfo infoToCopy)
        {
            this.UserName = infoToCopy.UserName;
            this.RunType = infoToCopy.RunType;
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


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        public override string ToString()
        {
            return string.Format("{0} {1}", this.UserName, this.RunType);
        }
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
            throw new NotImplementedException();
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            throw new NotImplementedException();
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            RunType runtype;
            foreach (KeyValuePair<string, string> attr in attributes)
            {
                if (attr.Key.Equals("UserName"))
                    this.UserName = attr.Value;
                else if ( attr.Key.Equals("RunType") && Enum.TryParse<RunType>(attr.Value,out runtype) )
                    this.RunType = runtype;
            }
            
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {            
        }
        //
        //
        #endregion//Event Handlers

    }
}
