using System;
using System.Collections.Generic;
using System.Text;

using System.Reflection;

namespace UV.Lib.ParameterManagement
{
    public class ParameterManager
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //       
        // 
        protected Dictionary<string, Parameter> m_ParameterList = new Dictionary<string,Parameter>();

        //
        //
        // Work space
        private List<string> m_StringList = new List<string>();
        private List<object> m_ObjectList = new List<object>();
        private StringBuilder m_StringBuilder = new StringBuilder();

        // Storage for discovered type converters.
        private Dictionary<Type, System.ComponentModel.TypeConverter> m_TypeConverters = new Dictionary<Type, System.ComponentModel.TypeConverter>();
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public ParameterManager()
        {

        }
        //
        //
        /// <summary>
        /// Creates a Parameter Manager and collects all the 
        /// </summary>
        /// <param name="objectWithParameters"></param>
        public ParameterManager(object objectWithParameters)
        {
            this.AddAll(objectWithParameters);   
        }
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
        //
        //
        //
        //
        // *****************************************
        // ****             Add()               ****
        // *****************************************
        /// <summary>
        /// TODO: Can we check for uniqueness of name, and fix it?
        /// </summary>
        /// <param name="newParameter"></param>
        public void Add(Parameter newParameter)
        {
            m_ParameterList.Add(newParameter.Name, newParameter);
        }
        //
        //
        // *****************************************
        // ****             AddAll()            ****
        // *****************************************
        /// <summary>
        /// TODO: Can we check for uniqueness of name, and fix it?
        /// </summary>
        /// <param name="owner"></param>
        public void AddAll(object owner)
        {
            // Automatically add all public Properties to parameter list.
            PropertyInfo[] pList = owner.GetType().GetProperties();
            foreach (PropertyInfo info in pList)
            {
                Parameter parameter;
                if (Parameter.TryCreate(info, owner, out parameter))
                    m_ParameterList.Add(parameter.Name, parameter);
            }//next info
        }//AddAll()
        //
        //
        //
        // *****************************************
        // ****           Process()             ****
        // *****************************************
        /// <summary>
        /// When object receives a ParameterEventArg from outside regarding its
        /// parameters managed by this class, the request is processed here.
        /// The eventArg is overwritten with the appropriate response.
        /// </summary>
        /// <param name="eventArg"></param>
        public void ProcessEvent(ParameterEventArgs eventArg)
        {
            // Validate arguments
            if (eventArg.State != ParameterEventState.Request)
                return;                             // Parameter managers only respond to requests

            // Process the event request
            ParameterEventType requestType = eventArg.EventType;
            switch (requestType)
            {
                case ParameterEventType.AllParameterValues:
                    m_StringList.Clear();
                    m_ObjectList.Clear();
                    foreach (KeyValuePair<string,Parameter>pair in m_ParameterList)
                    {
                        object paramValue;
                        if (GetParameterValues(pair.Value, out paramValue))
                        {
                            m_StringList.Add(pair.Key);
                            m_ObjectList.Add(paramValue);
                        }
                    }
                    // Create outgoing confirmation event.
                    m_StringBuilder.Clear();
                    ParameterEventArgs.EncodeMessage(m_StringList, m_ObjectList, ref m_StringBuilder);
                    eventArg.Message = m_StringBuilder.ToString();
                    eventArg.State = ParameterEventState.Confirmed;
                    break;
                case ParameterEventType.ParameterValue:
                    m_StringList.Clear();
                    m_ObjectList.Clear();
                    ParameterEventArgs.DecodeMessage(eventArg.Message, ref m_StringList, ref m_ObjectList);
                    m_ObjectList.Clear();           // this will be filled with nulls.
                    this.GetParameterValues(ref m_StringList, ref m_ObjectList);
                    if (m_StringList.Count > 0)
                    {
                        m_StringBuilder.Clear();
                        ParameterEventArgs.EncodeMessage(m_StringList, m_ObjectList, ref m_StringBuilder);
                        eventArg.Message = m_StringBuilder.ToString();
                        eventArg.State = ParameterEventState.Confirmed;
                    }
                    break;
                case ParameterEventType.ParameterChange:
                    m_StringList.Clear();
                    m_ObjectList.Clear();
                    if (ParameterEventArgs.DecodeMessage(eventArg.Message, ref m_StringList, ref m_ObjectList))
                        this.SetParameterValues(m_StringList,m_ObjectList);
                    // Create confirmation.
                    m_ObjectList.Clear();
                    this.GetParameterValues(ref m_StringList, ref m_ObjectList);
                    if (m_StringList.Count > 0)
                    {
                        m_StringBuilder.Clear();
                        ParameterEventArgs.EncodeMessage(m_StringList, m_ObjectList, ref m_StringBuilder);
                        eventArg.Message = m_StringBuilder.ToString();
                        eventArg.State = ParameterEventState.Confirmed;                        
                    }
                    break;
                case ParameterEventType.AllParameterInfos:
                    eventArg.Message = this.StringifyParameters();
                    eventArg.State = ParameterEventState.Confirmed;
                    break;
                default:
                    break;
            }//requestType



            return;
        }// ProcessEvent()
        //
        // 
        //
        // *************************************************************
        // ****                 GetParameterValues()                ****
        // *************************************************************
        /// <summary>
        /// User provides list of parameter IDs and wants to know their current values.
        /// </summary>
        /// <param name="inputList">IDs of parameters</param>
        /// <param name="outputList">current values</param>
        protected void GetParameterValues(ref List<string> inputList, ref List<object> outputList)
        {
            foreach (string pName in inputList)
            {
                object val;
                if (GetParameterValues(pName, out val))
                    outputList.Add(val);
                else
                    outputList.Add(string.Empty);           // this is empty, but can be serialized into string
            }
        }//GetParameterValues()
        //
        /// <summary>
        /// User provides name of single parameter, and if found returns the value.
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="parameterValue"></param>
        /// <returns></returns>
        protected bool GetParameterValues(string parameterName, out object parameterValue)
        {
            parameterValue = null;                                              // set default value
            Parameter param;
            if (m_ParameterList.TryGetValue(parameterName, out param))          // search for this parameter.
                GetParameterValues(param, out parameterValue);
            return (parameterValue != null);
        }// GetParameterValues()
        //
        protected bool GetParameterValues(Parameter param, out object parameterValue)
        {
            parameterValue = null;
            if (param.IsProperty)
            {   // This parameter is associated with a Property
                PropertyInfo info = param.Owner.GetType().GetProperty(param.Name);
                if (info.CanRead)
                    parameterValue = info.GetValue(param.Owner, null);
            }
            else
            {   // This parameter is associated with an member field.
                FieldInfo fieldInfo = param.Owner.GetType().GetField(param.Name);
                if (fieldInfo != null && fieldInfo.FieldType == param.ValueType)
                    parameterValue = fieldInfo.GetValue(param.Owner);
            }
            return (parameterValue != null);
        }// GetParameterValues()
        //
        //
        //
        // *************************************************************
        // ****                 SetParameterValues()                ****
        // *************************************************************
        /// <summary>
        /// User provides list of parameter IDs and wants to know their current values.
        /// </summary>
        /// <param name="paramNameList">IDs of parameters</param>
        /// <param name="paramValueList">current values</param>
        protected void SetParameterValues(List<string> paramNameList, List<object> paramValueList)
        {
            if (paramNameList.Count != paramValueList.Count)
                return;
            Parameter param;
            for (int i = 0; i < paramValueList.Count; ++i)
            {
                if (m_ParameterList.TryGetValue(paramNameList[i], out param))
                    SetParameterValues(param, paramValueList[i]);
            }//next i
        }// SetParameterValues()
        //
        /// <summary>
        /// Provided a single parameter object, attempts to set its value to the paramValue object.
        /// </summary>
        protected bool SetParameterValues(Parameter param, object paramValue)
        {
            Type typeOfParamValue = paramValue.GetType();
            if (param.IsProperty)
            {   // This parameter is associated with a Property
                PropertyInfo info = param.Owner.GetType().GetProperty(param.Name);
                if (info != null && info.CanWrite)
                {                    
                    if (typeOfParamValue == info.PropertyType)
                        info.SetValue(param.Owner, param, null);
                    else if (typeOfParamValue == typeof(string))
                    {
                        object o = ConvertToObject((string)paramValue, info.PropertyType); // try to convert string into native type
                        info.SetValue(param.Owner, o, null);
                    }
                    else
                        return false;
                }
                else
                    return false;
            }
            else
            {   // This parameter is associated with an member field.
                FieldInfo fieldInfo = param.Owner.GetType().GetField(param.Name);
                if (fieldInfo != null)
                {
                    if (typeOfParamValue == fieldInfo.FieldType)
                        fieldInfo.SetValue(param.Owner, paramValue);                // no need for any type-casting.
                    else if (typeOfParamValue == typeof(string))                    // okay, he has handed us a string, try to convert it.
                    {
                        object o = ConvertToObject((string)paramValue, fieldInfo.FieldType); // try to convert string into native type
                        fieldInfo.SetValue(param.Owner, o);
                    }
                    else
                        return false;
                }
                else
                    return false;
            }
            return true;
        }// GetParameterValues()
        //
        //
        //
        // *********************************************************
        // ****                 Convert To Object()             ****
        // *********************************************************
        /// <summary>
        /// See: http://stackoverflow.com/questions/3965871/c-sharp-generic-string-parse-to-any-object
        /// </summary>
        /// <returns></returns>
        public object ConvertToObject(string text, Type type)
        {
            System.ComponentModel.TypeConverter tc;
            if ( ! m_TypeConverters.TryGetValue(type,out tc) )
            {
                tc = System.ComponentModel.TypeDescriptor.GetConverter(type);
                m_TypeConverters.Add(type, tc);
            }
            return tc.ConvertFromString(null, System.Globalization.CultureInfo.InvariantCulture, text);
        }// ConvertToObject

        //
        //
        //
        //
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        private string StringifyParameters()
        {
            StringBuilder s = new StringBuilder();
            foreach (KeyValuePair<string,Parameter> pair in m_ParameterList)
                s.AppendFormat("{0}", UV.Lib.IO.Xml.Stringifiable.Stringify(pair.Value));
            return s.ToString();
        }//StringifyParameters().
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
