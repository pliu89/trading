using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace VioletAPI.Lib.Plotting
{
    using UV.Lib.FrontEnds.Graphs;
    using UV.Strategies.StrategyEngines;
    using ZedGraph;

    public class PlottingTool
    {

        #region Public Methods
        /// <summary>
        /// Add a curve by definition.
        /// </summary>
        /// <param name="graphEngine"></param>
        /// <param name="graphName"></param>
        /// <param name="curveName"></param>
        /// <param name="graphID"></param>
        /// <param name="color"></param>
        /// <param name="isLineVisible"></param>
        /// <param name="symbolType"></param>
        /// <param name="symbolFillColor"></param>
        /// <returns></returns>
        public static void AddCurveToGraphEngine(ZGraphEngine graphEngine, string graphName, string curveName, int graphID, Color color, bool isLineVisible, SymbolType symbolType, Color symbolFillColor)
        {
            CurveDefinition curveDefinition = new CurveDefinition();
            curveDefinition.GraphName = graphName;
            curveDefinition.CurveName = curveName;
            curveDefinition.GraphID = graphID;
            curveDefinition.CurveColor = color;
            curveDefinition.IsLineVisible = isLineVisible;
            curveDefinition.Symbol = symbolType;
            curveDefinition.SymbolFillColor = symbolFillColor;
            graphEngine.AddDefinition(curveDefinition);
        }
        //
        //
        /// <summary>
        /// Add a curve by definition.
        /// </summary>
        /// <param name="graphEngine"></param>
        /// <param name="graphName"></param>
        /// <param name="curveName"></param>
        /// <param name="graphID"></param>
        /// <param name="color"></param>
        /// <param name="dashStyle"></param>
        /// <param name="symbolType"></param>
        /// <returns></returns>
        public static void AddCurveToGraphEngine(ZGraphEngine graphEngine, string graphName, string curveName, int graphID, Color color, DashStyle dashStyle, SymbolType symbolType)
        {
            CurveDefinition curveDefinition = new CurveDefinition();
            curveDefinition.GraphName = graphName;
            curveDefinition.CurveName = curveName;
            curveDefinition.GraphID = graphID;
            curveDefinition.CurveColor = color;
            curveDefinition.DashStyle = dashStyle;
            curveDefinition.Symbol = symbolType;
            graphEngine.AddDefinition(curveDefinition);
        }
        #endregion

    }
}
