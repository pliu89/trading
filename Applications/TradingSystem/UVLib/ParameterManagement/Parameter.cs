using System;
using System.Collections.Generic;
using System.Text;

using System.Reflection;

namespace UV.Lib.ParameterManagement
{
    /// <summary>
    /// TODO: make this stringifiable!
    /// </summary>
    public class Parameter : UV.Lib.IO.Xml.IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //       
        //
        // Parameter information
        public string Name;                     // exact name of variable.
        public string DisplayName;              // nice user display name. // TODO also have short name?
        public Type ValueType;

        // Information about owner.
        public object Owner = null;             // owner for this parameter.
        public bool IsProperty;                 // Is parameter a property or member?

        #endregion// members



        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public Parameter()
        {
            this.IsProperty = false;        
        }
        public static bool TryCreate(PropertyInfo info, object owner, out Parameter newParameter)
        {
            newParameter = null;
            if (info == null || info.CanRead == false)
                return false;
            // Create the parameter
            newParameter = new Parameter();
            newParameter.Name = info.Name;
            newParameter.ValueType = info.PropertyType;
            newParameter.Owner = owner;
            newParameter.IsProperty = true;
            return true;
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
        //
        //
        //
        //
        // *************************************************
        // ****             ToString()                  ****         
        // *************************************************
        public override string ToString()
        {
            return string.Format("ParameterInfo {0}", Name);
        }
        //
        //
        // Todo: craete a serialization procedure.
        /*
        public static string Serialize(Parameter param)
        {
            StringBuilder s = new StringBuilder();
            Type myType = typeof(Parameter);
            System.Reflection.FieldInfo[] fields = myType.GetFields();
            foreach (System.Reflection.FieldInfo field in fields)
            {
                if (field.Name == "Owner" || field.Name == "IsProperty")
                    continue;
                s.AppendFormat("{0}={1},"
            }
            return s.ToString();
        }//Serialize()
        */
        //
        //
        #endregion//Public Methods


        #region IStringifiable Implementation
        // *****************************************************************
        // ****                IStringifiable Implementation            ****
        // *****************************************************************
        public string GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            s.AppendFormat("Name={0} DisplayName={1}", this.Name, this.DisplayName);
            s.AppendFormat(" Type={0}", this.ValueType.FullName);
            return s.ToString();
        }
        public List<IO.Xml.IStringifiable> GetElements()
        {
            return null;                // no elements 
        }
        public void SetAttributes(Dictionary<string, string> attributes)
        {
            foreach (KeyValuePair<string,string> pair in attributes)
            {
                if (pair.Key == "Name")
                    this.Name = pair.Value;
                else if (pair.Key == "DisplayName")
                    this.DisplayName = pair.Value;
                else if (pair.Key == "Type")
                {
                    Type type;
                    if (UV.Lib.IO.Xml.Stringifiable.TryGetType(pair.Value, out type))
                        this.ValueType = type;
                }
            }
        }
        public void AddSubElement(IO.Xml.IStringifiable subElement)
        {            
        }
        //
        #endregion//IStringifiable Implementation




 
    }
}
