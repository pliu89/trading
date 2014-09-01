using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.FrontEnds.Graphs
{
    using UV.Lib.IO.Xml;

	public class ZGraphPoint : IStringifiable
	{
		public string CurveName;
		public int GraphID = 0;			// 0 is default graph

		public double X;
		public double Y;

		public bool IsReplaceAtX = false;	// if true, we try to find a plotted point with same x-value and replace with this one.
												// otherwise, we just add the new point serially.

        //
        //
        //
        public ZGraphPoint DeepCopy()
        {
            ZGraphPoint zpt = (ZGraphPoint)this.MemberwiseClone();
            return zpt;
        }


        #region IStringifiable 
        string IStringifiable.GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            s.AppendFormat("CurveName={0}", this.CurveName);
            s.AppendFormat(" GraphID={0}", this.GraphID);
            s.AppendFormat(" X={0}", this.X);
            s.AppendFormat(" Y={0}", this.Y);
            s.AppendFormat(" IsReplaceAtX={0}", this.IsReplaceAtX);
            return s.ToString();
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            return null;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            double x;
            bool b;
            int n;
            foreach (KeyValuePair<string,string>a in attributes)
            {
                if (a.Key.Equals("CurveName", StringComparison.CurrentCultureIgnoreCase))
                    this.CurveName = a.Value;
                else if (a.Key.Equals("GraphID", StringComparison.CurrentCultureIgnoreCase) && int.TryParse(a.Value, out n))
                    this.GraphID = n;
                else if (a.Key.Equals("X", StringComparison.CurrentCultureIgnoreCase) && double.TryParse(a.Value, out x))
                    this.X = x;
                else if (a.Key.Equals("Y", StringComparison.CurrentCultureIgnoreCase) && double.TryParse(a.Value, out x))
                    this.Y = x;
                else if (a.Key.Equals("IsReplaceAtX", StringComparison.CurrentCultureIgnoreCase) && bool.TryParse(a.Value, out b))
                    this.IsReplaceAtX = b;
            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {            
        }
        #endregion // IStringifiable


    }
}
