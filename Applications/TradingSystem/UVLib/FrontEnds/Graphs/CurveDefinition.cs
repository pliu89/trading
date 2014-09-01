using System;
using System.Collections.Generic;
using System.Text;


using ZedGraph;
using System.Drawing;			// Color enum
using System.Drawing.Drawing2D;

namespace UV.Lib.FrontEnds.Graphs
{
    using UV.Lib.IO.Xml;

    // *****************************************************
    // ****         Class CurveDefinition               ****
    // *****************************************************
	public class CurveDefinition : IStringifiable
    {
        #region Variables
        public string CurveName;					// Unique identifier in graph.
		public int GraphID = 0;						// Unique id# for graph in each popup.

		public string GraphName = string.Empty;		// this is the name of graph that appears in popup menu.
		public CurveType Type = CurveType.Curve;	// Type of curve, bar, etc.
		

		//
		// ****	Curve Type variables ****
		//
		// Line
		public Color CurveColor = Color.Black;
		public bool IsLineVisible = true;
		public DashStyle DashStyle = DashStyle.Solid;
        public float CurveWidth = 1.0F;

		// Symbol
		public SymbolType Symbol = SymbolType.None;
		public Color SymbolFillColor = Color.White;
		public float SymbolSize = 8.0f;

		//
		// **** Bar type variables ****
        //
        #endregion // Variables

        #region Curve Type Enum
        // ****  Enums ****
		public enum CurveType
		{
			Curve, 
			Bar,
			None
		}
		#endregion//enums

        #region IStringifiable 
        string IStringifiable.GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            if (!string.IsNullOrEmpty(this.CurveName))
                s.AppendFormat("CurveName={0}", this.CurveName);
            s.AppendFormat(" GraphID={0}", this.GraphID);
            if (! string.IsNullOrEmpty(this.GraphName))
                s.AppendFormat(" GraphName={0}", this.GraphName);
            s.AppendFormat(" CurveType={0}", this.Type);

            s.AppendFormat(" CurveColor={0}", this.CurveColor.Name);
            s.AppendFormat(" CurveWidth={0}", this.CurveWidth);
            s.AppendFormat(" IsLineVisible={0}", this.IsLineVisible);
            s.AppendFormat(" DashStyle={0}", this.DashStyle);

            s.AppendFormat(" Symbol={0}", this.Symbol.ToString());
            s.AppendFormat(" SymbolFillColor={0}", this.SymbolFillColor.Name);
            s.AppendFormat(" SymbolSize={0}", this.SymbolSize);

            return s.ToString();
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            return null;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {

            int n;
            bool b;
            float f;
            CurveType curveType;
            DashStyle dashStyle;
		    SymbolType symbolType;		
            foreach (KeyValuePair<string,string>a in attributes)
            {
                if (a.Key.Equals("CurveName", StringComparison.CurrentCultureIgnoreCase))
                    this.CurveName = a.Value;
                else if (a.Key.Equals("GraphID", StringComparison.CurrentCultureIgnoreCase) && int.TryParse(a.Value, out n))
                    this.GraphID = n;
                else if (a.Key.Equals("GraphName", StringComparison.CurrentCultureIgnoreCase))
                    this.GraphName = a.Value;
                else if (a.Key.Equals("CurveType", StringComparison.CurrentCultureIgnoreCase) && Enum.TryParse<CurveType>(a.Value, out curveType))
                    this.Type = curveType;
                else if (a.Key.Equals("CurveColor", StringComparison.CurrentCultureIgnoreCase))
                {
                    this.CurveColor = Color.FromName(a.Value);
                }
                else if (a.Key.Equals("CurveWidth", StringComparison.CurrentCultureIgnoreCase) && float.TryParse(a.Value, out f))
                    this.CurveWidth = f;
                else if (a.Key.Equals("IsLineVisible", StringComparison.CurrentCultureIgnoreCase) && bool.TryParse(a.Value, out b))
                    this.IsLineVisible = b;
                else if (a.Key.Equals("DashStyle", StringComparison.CurrentCultureIgnoreCase) && Enum.TryParse<DashStyle>(a.Value, out dashStyle))
                    this.DashStyle = dashStyle;
                else if (a.Key.Equals("Symbol", StringComparison.CurrentCultureIgnoreCase) && Enum.TryParse<SymbolType>(a.Value, out symbolType))
                    this.Symbol = symbolType;
                else if (a.Key.Equals("SymbolFillColor", StringComparison.CurrentCultureIgnoreCase))
                {
                    this.SymbolFillColor = Color.FromName(a.Value);
                }
                else if (a.Key.Equals("SymbolSize", StringComparison.CurrentCultureIgnoreCase) && float.TryParse(a.Value, out f))
                    this.SymbolSize = f;
            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {            
        }
        #endregion // IStringifiable 

    }//end CurveDefinition class



    // *****************************************************
    // ****         Class CurveDefinitionList           ****
    // *****************************************************
    public class CurveDefinitionList : IStringifiable
    {
        
        #region Variables 
        public List<CurveDefinition> CurveDefinitions = new List<CurveDefinition>();
        #endregion Variables

        #region Contructors 
        public CurveDefinitionList()
        {

        }
        public CurveDefinitionList(List<CurveDefinition> list)
        {
            foreach (CurveDefinition pt in list)
                this.CurveDefinitions.Add(pt);
        }
        public CurveDefinitionList(CurveDefinitionList curveList)
        {
            foreach (CurveDefinition pt in curveList.CurveDefinitions)
                this.CurveDefinitions.Add(pt);
        }
        #endregion// Constructor

        #region IStringifiable
        string IStringifiable.GetAttributes()
        {
            return string.Empty;
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            List<IStringifiable> elems = new List<IStringifiable>();
            elems.AddRange(this.CurveDefinitions);
            return elems;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
            if (subElement is CurveDefinition)
                this.CurveDefinitions.Add((CurveDefinition)subElement);
        }
        #endregion // IStringifiable


    }//end CurveDefinitionList class









}//namespace
