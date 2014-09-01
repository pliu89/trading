using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.FrontEnds.Graphs
{
    using UV.Lib.IO.Xml;

    public class ZGraphPoints : IStringifiable
    {
        //public string CurveName;
        //public int GraphID = 0;			// 0 is default graph

        public List<ZGraphPoint> Points = new List<ZGraphPoint>();


        //
        // Constructors
        //
        public ZGraphPoints()
        {

        }
        public ZGraphPoints( List<ZGraphPoint> points )
        {
            foreach (ZGraphPoint pt in points)
                this.Points.Add(pt.DeepCopy());
        }
        public ZGraphPoints(ZGraphPoints zGraphPoints)
        {
            foreach (ZGraphPoint pt in zGraphPoints.Points)
                this.Points.Add(pt.DeepCopy());
        }



        #region IStringifiable
        string IStringifiable.GetAttributes()
        {
            //StringBuilder s = new StringBuilder();
            //s.AppendFormat("CurveName={0}", this.CurveName);
            //s.AppendFormat(" GraphID={0}", this.GraphID);
            //return s.ToString();
            return string.Empty;
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            List<IStringifiable> elems = new List<IStringifiable>();
            elems.AddRange(Points);
            return elems;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            //int n;
            //foreach (KeyValuePair<string, string> a in attributes)
            //{
                //if (a.Key.Equals("CurveName", StringComparison.CurrentCultureIgnoreCase))
                //    this.CurveName = a.Value;
                //else if (a.Key.Equals("GraphID", StringComparison.CurrentCultureIgnoreCase) && int.TryParse(a.Value, out n))
                //    this.GraphID = n;                
            //}
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
            if (subElement is ZGraphPoint)
                Points.Add( (ZGraphPoint) subElement);
        }
        #endregion // IStringifiable


    }
}
