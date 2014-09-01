using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.FrontEnds.Graphs
{
    
    using UV.Lib.IO.Xml;
    using ZedGraph;

    /// <summary>
    /// This is a template for classes.
    /// </summary>
    public class ZGraphText : IStringifiable
	{

    

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
		//
        public string Text;
		public int GraphID = 0;			        // 0 is default graph

        // Location
		public double X;
		public double Y;


        // Font details
        public float FontSize = 9.0f;
        public float FontAngle = 90.0f;
        public bool FontFillIsVisible = false;
        public bool FontBorderIsVisible = false;
        public AlignH FontAlignH = AlignH.Center;
        public AlignV FontAlignV = AlignV.Center;

        #endregion


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
        string IStringifiable.GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            s.AppendFormat("Text={0}", this.Text);
            s.AppendFormat(" GraphID={0}", this.GraphID);
            s.AppendFormat(" X={0}", this.X);
            s.AppendFormat(" Y={0}", this.Y);
            return s.ToString();
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            return null;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            double x;
            //bool b;
            int n;
            foreach (KeyValuePair<string, string> a in attributes)
            {
                if (a.Key.Equals("Text", StringComparison.CurrentCultureIgnoreCase))
                    this.Text = a.Value;
                else if (a.Key.Equals("GraphID", StringComparison.CurrentCultureIgnoreCase) && int.TryParse(a.Value, out n))
                    this.GraphID = n;
                else if (a.Key.Equals("X", StringComparison.CurrentCultureIgnoreCase) && double.TryParse(a.Value, out x))
                    this.X = x;
                else if (a.Key.Equals("Y", StringComparison.CurrentCultureIgnoreCase) && double.TryParse(a.Value, out x))
                    this.Y = x;
            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
        }
        #endregion // IStringifiable

    }//end class
}
