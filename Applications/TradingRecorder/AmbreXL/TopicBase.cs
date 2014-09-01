using System;
using System.Collections.Generic;
using System.Text;

namespace Ambre.XL
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
        //
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************

        public bool IsChangedSinceLastRead
        {
            get { return m_IsValueChangedSinceLastRead; }
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
