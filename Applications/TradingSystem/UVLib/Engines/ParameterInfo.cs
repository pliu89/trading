using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UV.Lib.Engines
{
    using UV.Lib.IO.Xml;

    #region public class ParameterInfo
    // *****************************************************************
    // ****                       ParameterInfo                     ****
    // *****************************************************************
    //
    /// <summary>
    /// Each of these objects contains information for formatting and displaying
    /// the engine's parameters.
    /// Note: A copy of these might be loading into controls so formatting and
    /// purposes.
    /// TODO: 1.  Ultimately make this a read-only object!
    /// </summary>
    public class ParameterInfo : IStringifiable
    {
        //
        // ****     Members     ****
        //
        public string Name;                 // name given by reflection for this property
        public string DisplayName;          // name for displaying on GUI.
        public bool IsReadOnly = false;     // is this property read-only?
        public Type ValueType;              // value type of property.

        //         Identification
        public int ParameterID = -1;
        public int EngineID = -1;
        public int EngineContainerID = -1;
        public string EngineHubName = string.Empty;
        //public IEngineHub EngineHub = null;

        //
        // ****     Constructor     ****
        //
        public ParameterInfo()
        {
        }

        //
        // ****     Methods     ****
        //
        public override string ToString()
        {
            return string.Format("{0}",this.DisplayName);
        }


        #region IStringifiable
        // *****************************************************************
        // ****                     IStringifiable                      ****
        //******************************************************************
        //
        string IStringifiable.GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            // Parameter details
            s.AppendFormat("DisplayName={0} ", this.DisplayName);
            s.AppendFormat("Name={0} ", this.Name);
            s.AppendFormat("IsReadOnly={0} ", this.IsReadOnly);
            s.AppendFormat("ValueType={0} ", this.ValueType.FullName);
            // Identification
            s.AppendFormat("EngineHubName={0} ", this.EngineHubName);
            s.AppendFormat("EngineContainerID={0} ", this.EngineContainerID);
            s.AppendFormat("EngineID={0} ", this.EngineID);
            s.AppendFormat("ParameterID={0} ", this.ParameterID);
            return s.ToString();
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            return null;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            foreach (KeyValuePair<string, string> keyVal in attributes)
            {
                bool b;
                int n;
                Type type;
                if (keyVal.Key == "Name")
                    this.Name = keyVal.Value;
                else if (keyVal.Key == "DisplayName")
                    this.DisplayName = keyVal.Value;
                else if (keyVal.Key == "IsReadOnly" && bool.TryParse(keyVal.Value,out b))
                    this.IsReadOnly = b;
                else if (keyVal.Key == "ValueType" && Stringifiable.TryGetType(keyVal.Value,out type))
                    this.ValueType = type;
                else if (keyVal.Key == "ParameterID" && int.TryParse(keyVal.Value,out n))
                    this.ParameterID = n;
                else if (keyVal.Key == "EngineID" && int.TryParse(keyVal.Value,out n))
                    this.EngineID = n;
                else if (keyVal.Key == "EngineContainerID" && int.TryParse(keyVal.Value,out n))
                    this.EngineContainerID = n;
                else if (keyVal.Key == "EngineHubName")
                    this.EngineHubName = keyVal.Value;

            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {            
        }
        //
        #endregion // IStringifiable

    }//end Parameter Info class
    //
    //

    #endregion//ParameterInfo

}
