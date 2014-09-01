using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.FrontEnds.GuiTemplates
{
    using UV.Lib.IO.Xml;
    using UV.Lib.Engines;

    public class EngineGui : IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Engine name and parameters
        public int EngineID = -1;								                    // unique engine ID (unique per EngineContainer).        
        public string DisplayName = string.Empty;                                   // Nice name for this engine.
        public string EngineFullName = string.Empty;                                // Full type name of ENGINE.

        // My Engine's parameters
        public List<ParameterInfo> ParameterList = new List<ParameterInfo>();       // my parameters

        // Display control object list
        public string HeaderControlFullName = string.Empty;                         // control for Header popups.
        public string LowerHudFullName = string.Empty;                              // empty means no control panel for this engine.

        //
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public EngineGui()
        {
            // Set the default values.
            this.HeaderControlFullName = "UV.Lib.FrontEnds.PopUps.EngineControl";   // default popup control.

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
        // ****         ToString()          ****
        //
        public override string ToString()
        {
            return string.Format("{0}",this.DisplayName);
        }

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
        //
        string IStringifiable.GetAttributes()
        {
            StringBuilder s = new StringBuilder();            
            s.AppendFormat("EngineID={0}",this.EngineID);
            s.AppendFormat(" DisplayName={0}",this.DisplayName);
            s.AppendFormat(" FullName={0}",this.EngineFullName);
            if (! string.IsNullOrEmpty(this.HeaderControlFullName))
              s.AppendFormat(" HeaderControlFullName={0}", this.HeaderControlFullName);
            if (! string.IsNullOrEmpty(this.LowerHudFullName))
                s.AppendFormat(" LowerHudFullName={0}", this.LowerHudFullName);
            return s.ToString();
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            List<IStringifiable> elements = new List<IStringifiable>();
            foreach (ParameterInfo info in this.ParameterList)
                elements.Add(info);
            return elements;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            int n;
            foreach (KeyValuePair<string,string> keyValuePair in attributes)
            {
                if (keyValuePair.Key.Equals("EngineID") && int.TryParse(keyValuePair.Value, out n))
                    this.EngineID = n;
                else if (keyValuePair.Key.Equals("DisplayName"))
                    this.DisplayName = keyValuePair.Value;
                else if (keyValuePair.Key.Equals("FullName"))
                    this.EngineFullName = keyValuePair.Value;
                else if (keyValuePair.Key.Equals("HeaderControlFullName"))
                    this.HeaderControlFullName = keyValuePair.Value;
                else if (keyValuePair.Key.Equals("LowerHudFullName"))
                    this.LowerHudFullName = keyValuePair.Value;
            
            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
            if (subElement is ParameterInfo)
                this.ParameterList.Add((ParameterInfo)subElement);
        }
        //
        //
        #endregion//Event Handlers


    }
}
