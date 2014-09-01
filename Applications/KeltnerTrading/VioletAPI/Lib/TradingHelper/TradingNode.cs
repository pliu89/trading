using System;
using System.Collections.Generic;

namespace VioletAPI.Lib.TradingHelper
{
    using UV.Lib.IO.Xml;

    public class TradingNode : IStringifiable
    {

        #region Members
        private int m_TradeLevel = -1;
        private string m_TradeLevelName = null;
        private TradeSide m_TradeSide = TradeSide.Unknown;
        private bool m_IsEnterEnable = false;

        private double m_WorkingEnterPrice = double.NaN;
        private double m_WorkingStopEnterPrice = double.NaN;
        private int m_WorkingEnterQty = 0;

        private double m_WorkingStopLossPrice = double.NaN;
        private double m_WorkingStopProfitPrice = double.NaN;
        private double m_WorkingTrailingStopPrice = double.NaN;
        private int m_WorkingExitQty = 0;
        #endregion


        #region Properties
        public bool IsEnterEnable
        {
            get { return m_IsEnterEnable; }
            set { m_IsEnterEnable = value; }
        }
        public string TradeLevelName
        {
            get { return m_TradeLevelName; }
            set { m_TradeLevelName = value; }
        }
        public int TradeLevel
        {
            get { return m_TradeLevel; }
            set { m_TradeLevel = value; }
        }
        public TradeSide TradeSide
        {
            get { return m_TradeSide; }
            set { m_TradeSide = value; }
        }
        public double WorkingEnterPrice
        {
            get { return m_WorkingEnterPrice; }
            set { m_WorkingEnterPrice = value; }
        }
        public double WorkingStopEnterPrice
        {
            get { return m_WorkingStopEnterPrice; }
            set { m_WorkingStopEnterPrice = value; }
        }
        public int WorkingEnterQty
        {
            get { return m_WorkingEnterQty; }
            set { m_WorkingEnterQty = value; }
        }
        public double WorkingStopLossPrice
        {
            get { return m_WorkingStopLossPrice; }
            set { m_WorkingStopLossPrice = value; }
        }
        public double WorkingStopProfitPrice
        {
            get { return m_WorkingStopProfitPrice; }
            set { m_WorkingStopProfitPrice = value; }
        }
        public double WorkingTrailingStopPrice
        {
            get { return m_WorkingTrailingStopPrice; }
            set { m_WorkingTrailingStopPrice = value; }
        }
        public int WorkingExitQty
        {
            get { return m_WorkingExitQty; }
            set { m_WorkingExitQty = value; }
        }
        #endregion


        #region Constructors
        public TradingNode()
        {

        }

        public TradingNode(string name)
        {
            m_TradeLevelName = name;
        }
        #endregion


        #region IStringifiable
        public string GetAttributes()
        {
            return string.Format("TradeLevelName={0} IsEnterEnable={1} TradeLevel={2} TradeSide={3} WorkingEnterQty={4} WorkingExitQty={5}"
                , m_TradeLevelName, m_IsEnterEnable, m_TradeLevel, m_TradeSide, m_WorkingEnterQty, m_WorkingExitQty);
        }
        public List<IStringifiable> GetElements()
        {
            return null;
        }
        public void SetAttributes(Dictionary<string, string> attributes)
        {
            TradeSide tradeSide;
            int qty;
            foreach (KeyValuePair<string, string> att in attributes)
            {
                if (att.Key.Equals("TradeLevelName"))
                    m_TradeLevelName = att.Value;
                else if (att.Key.Equals("IsEnterEnable") && att.Value.Equals(bool.TrueString))
                    m_IsEnterEnable = true;
                else if (att.Key.Equals("TradeLevel") && int.TryParse(att.Value, out qty))
                    m_TradeLevel = qty;
                else if (att.Key.Equals("TradeSide") && Enum.TryParse(att.Value, out tradeSide))
                    m_TradeSide = tradeSide;
                else if (att.Key.Equals("WorkingEnterQty") && int.TryParse(att.Value, out qty))
                    m_WorkingEnterQty = qty;
                else if (att.Key.Equals("WorkingExitQty") && int.TryParse(att.Value, out qty))
                    m_WorkingExitQty = qty;
            }
        }
        public void AddSubElement(IStringifiable subElement) { }
        #endregion

    }
}
