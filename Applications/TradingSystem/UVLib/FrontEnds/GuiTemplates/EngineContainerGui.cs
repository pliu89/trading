using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.FrontEnds.GuiTemplates
{
    using UV.Lib.IO.Xml;

    /// <summary>
    /// 
    /// </summary>
    public class EngineContainerGui : IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        public int EngineContainerID = -1;
        public string DisplayName = string.Empty;

        // Basic Engine
        public List<EngineGui> m_Engines = new List<EngineGui>();           // Place for templates for my sub-components.

        //
        //
        //


        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public EngineContainerGui()
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


        #region no Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        #endregion//Event Handlers


        #region IStringifiable
        //
        //
        //
        string IStringifiable.GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            s.AppendFormat("EngineContainerID={0} ", this.EngineContainerID);
            s.AppendFormat("DisplayName={0} ", this.DisplayName);
            return s.ToString();
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            List<IStringifiable> elements = new List<IStringifiable>();
            foreach (EngineGui engineGui in m_Engines)
                elements.Add(engineGui);
            return elements;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            int n;
            foreach (KeyValuePair<string, string> keyVal in attributes)
            {
                if (keyVal.Key.Equals("EngineContainerID") && int.TryParse(keyVal.Value, out n))
                    this.EngineContainerID = n;
                else if (keyVal.Key.Equals("DisplayName"))
                    this.DisplayName = keyVal.Value;
            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
            if (subElement is EngineGui)
                m_Engines.Add( (EngineGui) subElement);
        }
        #endregion // IStringifiable


    }
}
