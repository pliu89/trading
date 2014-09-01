using System;
using System.Collections.Generic;
using System.Text;

namespace Ambre.Lib.ExcelRTD
{
    public class TopicBase
    {


        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        public int TopicID = -1;                                        // topic ID# used by Excel to identify a specific topic.
        public string[] Arguments = null;

        // Value holder
        private string m_Value = string.Empty;
        private bool m_IsValueChangedSinceLastRead = false;                          // flag used to tell Excel that this topic has updated value.
        private object m_ValueLock = new object();
        //private string m_OldValue = string.Empty;                       // keep the last value as well....


        //
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public TopicBase(int topicID, string[] args, string currentValue = "")
        {
            this.TopicID = topicID;
            this.Arguments = args;
            if (!string.IsNullOrEmpty(currentValue))
            {
                m_Value = currentValue;
            }
        }
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************

        public bool IsChangedSinceLastRead
        {
            get { return m_IsValueChangedSinceLastRead; }
            set { m_IsValueChangedSinceLastRead = value; }
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
        /// <summary>
        /// This is the data that will be reported Excel on the next update.
        /// When this property is set, the topic is flagged as having changed; when the value is 
        /// read, the topic is IsValueChanged flagged is set to false. (We assume there is only 
        /// one read event when the value is loaded into the Excel array.)
        /// Threading: This locks the data when setting/getting.
        /// </summary>
        public void SetValue(string newValue)
        {
            lock (m_ValueLock)
            {
                m_Value = newValue;                                    // this should be a value type.
                m_IsValueChangedSinceLastRead = true;
            }
        }
        public string ReadValue()
        {
            lock (m_ValueLock)
            {
                m_IsValueChangedSinceLastRead = false;        // We only "Read" a value once, in RTDServerBase when its sent to Excel.
                return m_Value;                               // Anyone else who wants the value should user PeakAtValue().
            }
        }
        public string PeekAtValue()
        {
            string currentValue;
            lock (m_ValueLock)
            {
                currentValue = string.Copy(m_Value);
            }
            return currentValue;
        }
        //
        // *********************************************************************
        // ****                         Serialize()                         ****
        // *********************************************************************
        /// <summary>
        /// Only serializes the id and the Current Value.
        /// </summary>
        public string SerializeCurrent(MessageType msgType)
        {
            return string.Format("{0},{1},{2}\n", msgType, this.TopicID, this.PeekAtValue());
        }//SerializeCurrent
        //
        //
        /// <summary>
        /// Serializes the entire object.
        /// </summary>
        public string Serialize()
        {
            StringBuilder msg = new StringBuilder();
            msg.AppendFormat("{0},{1},{2}", MessageType.TopicArgs, this.TopicID, this.PeekAtValue());
            foreach (string arg in this.Arguments)
                msg.AppendFormat(",{0}", arg);
            msg.Append("\n");
            return msg.ToString();
        }//Serialize()
        //
        //
        //
        //
        //
        #endregion//Public Methods


        #region Static Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        public static bool TryReadSerialString(string serialString, out MessageType messageType, out int topicID, out string currentValue, out string[] args)
        {
            string[] elements = serialString.Split(',');
            MessageType msgType;
            int ID;
            if (elements.Length > 2 && Enum.TryParse<MessageType>(elements[0], out msgType) && int.TryParse(elements[1], out ID))
            {
                messageType = msgType;
                topicID = ID;
                currentValue = elements[2];
                if (elements.Length > 3)
                {
                    args = new string[elements.Length - 3];
                    for (int i = 3; i < elements.Length; ++i)
                        args[i - 3] = elements[i];
                }
                else
                    args = null;
                return true;
            }
            else
            {   // failed.
                messageType = MessageType.None;
                topicID = -1;
                currentValue = string.Empty;
                args = null;
                return false;
            }
        }//TryReadSerialString()
        //
        #endregion//Private Methods


    }
}
