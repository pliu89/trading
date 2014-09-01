using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.ParameterManagement
{
    /// <summary>
    /// Messages for updating, changing, broadcasting values of parameters
    /// </summary>
    public class ParameterEventArgs : EventArgs
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //     
        //
        public ParameterEventType EventType     = ParameterEventType.None;
        public ParameterEventState State        = ParameterEventState.Unknown;

        public string Message                   = string.Empty;

        // Delimiter
        private const char ElementDelim = ',';
        private const char PairDelim = '=';
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //       
        #endregion//Constructors



        #region Static Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        // *************************************************
        // ****             Decode Message              ****
        // *************************************************
        /// <summary>
        /// Create message for encoding parameter key/value pairs.
        /// </summary>
        public static bool DecodeMessage(string incomingMessage, ref List<string> m_StringList, ref List<object> m_ObjectList)
        {
            int currentElementPtr = 0;
            int nextElementPtr = 0;
            try
            {
                while (nextElementPtr >= 0)                             // quit once we find no more delimiters
                {
                    nextElementPtr = incomingMessage.IndexOf(ElementDelim, currentElementPtr);  // point to next delimiter.
                    int elementLength = nextElementPtr - currentElementPtr;             // length of current element
                    if (elementLength < 0)                                              // happens when we find no next element (nextElementPtr = -1).
                        elementLength = incomingMessage.Length - currentElementPtr;             // length of final element in message.
                    // Split element in key/value pairs
                    int keyValueDelimPtr = incomingMessage.IndexOf(PairDelim, currentElementPtr);
                    if (keyValueDelimPtr > 0 && (nextElementPtr<0 || keyValueDelimPtr < nextElementPtr))
                    {   // Current element is a pair, with proper pair delimiter.
                        int keyStrLen = keyValueDelimPtr - currentElementPtr;
                        string keyStr = incomingMessage.Substring(currentElementPtr, keyStrLen);
                        m_StringList.Add(keyStr);
                        int valPtr = keyValueDelimPtr + 1;                  // ptr to start of value, after delimiter
                        int valStrLen = elementLength - keyStrLen - 1;
                        if (valStrLen > 0)
                        {
                            string valStr = incomingMessage.Substring(valPtr, valStrLen);
                            m_ObjectList.Add(valStr);
                        }
                        else
                            m_ObjectList.Add(null);
                    }
                    else
                    {   // This current element doesn't seem to have a value pair.
                        // This happens for queries...
                        string keyStr = incomingMessage.Substring(currentElementPtr, elementLength);
                        m_StringList.Add(keyStr);
                        m_ObjectList.Add(null);
                    }                    
                    // Increment pointers
                    currentElementPtr = nextElementPtr + 1;
                }//while there is still another element to read.
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }// DecodeMessage()
        //
        //
        //
        //
        // *************************************************
        // ****             Encode Message              ****
        // *************************************************
        /// <summary>
        /// Message is "elements" separated by ElementDelims.
        /// Message come in two types only:
        /// 1) comma delimited list of items.
        ///     This option is acheived by passing a null for parameterValues, or passing in an empty list.
        /// 2) comma delimited list of pairs of items of the format "KeyName=Value"
        ///     This option must have equal count of the two lists.
        /// </summary>
        /// <param name="parameterNames"></param>
        /// <param name="parameterValues"></param>
        /// /// <param name="outMessage">Message is appended to this StringBuilder</param>
        /// <returns>true if message decoded successfully.</returns>
        public static bool EncodeMessage(List<string> parameterNames, List<object> parameterValues, ref StringBuilder outMessage)
        {
            try
            {
                if (parameterValues==null || parameterValues.Count != parameterNames.Count)
                {   // Mesas
                    for (int i = 0; i < parameterNames.Count; ++i)
                        outMessage.AppendFormat("{0}{1}", parameterNames[i], ElementDelim);
                    outMessage.Remove(outMessage.Length - 1, 1);        // remove the last ElementDelim            
                }
                else
                {
                    for (int i = 0; i < parameterNames.Count; ++i)
                        outMessage.AppendFormat("{0}{1}{2}{3}", parameterNames[i], PairDelim, parameterValues[i].ToString(), ElementDelim);
                    outMessage.Remove(outMessage.Length - 1, 1);        // remove the last ElementDelim            
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }// EncodeMessage()
        //
        //
        //
        // *********************************************
        // ****             To String()             ****
        // *********************************************
        public override string ToString()
        {
            return string.Format("{0} {1}:{2}", this.State, this.EventType, this.Message);
        }
        //
        #endregion//Public Methods

    }


    // *****************************************
    // *****        ParameterEventType      ****
    // *****************************************
    public enum ParameterEventType
    {
        None = 0
        ,AllParameterValues
        ,ParameterValue
        ,ParameterChange
        ,AllParameterInfos
    }
    //
    //
    // *****************************************
    // *****        MessageState            ****
    // *****************************************
    public enum ParameterEventState
    {
        Unknown = 0
        ,Request
        ,Confirmed
    }


}
