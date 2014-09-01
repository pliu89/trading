using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.Application.Managers
{
    using UV.Lib.IO.Xml;
    using UV.Lib.Utilities;

    /// <summary>
    /// Message object sent thru sockets providing communication between Application managers.
    /// </summary>
    public class Message : EventArgs , IStringifiable, IRecyclable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //   
        
        public MessageType MessageType = MessageType.None;
        public MessageState State = MessageState.None;

        public List<IStringifiable> Data = new List<IStringifiable>();

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
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


        #region no Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        //
        //
        public override string ToString()
        {
            return string.Format("{0} {1}", this.State, this.MessageType);
        }// ToString()
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
        // ****                     IStringifiable                     ****
        // *****************************************************************
        string IStringifiable.GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            s.AppendFormat(" Type={0}", this.MessageType);
            s.AppendFormat(" State={0}", this.State);
            return s.ToString();
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            if (this.Data != null && this.Data.Count > 0)
            {
                List<IStringifiable> elems = new List<IStringifiable>();
                elems.AddRange(Data);
                return elems;
            }
            else
                return null;           
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            MessageType t;
            MessageState s;
            foreach (KeyValuePair<string,string> a in attributes)
            {
                if (a.Key.Equals("Type", StringComparison.CurrentCultureIgnoreCase) && Enum.TryParse<MessageType>(a.Value, out t))
                    this.MessageType = t;
                else if (a.Key.Equals("State", StringComparison.CurrentCultureIgnoreCase) && Enum.TryParse<MessageState>(a.Value, out s))
                    this.State = s;
            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
            if (this.Data == null)
                Data = new List<IStringifiable>();
            this.Data.Add(subElement);
        }
        //
        //
        #endregion// IStringifiable

        #region IRecyclable
        void IRecyclable.Clear()
        {
            this.Data.Clear();
        }
        #endregion//IRecyclable



    }//end class
}
