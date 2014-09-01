using System;
using System.Collections.Generic;

namespace VioletAPI.Lib.Indicator
{
    public class IndicatorManagerBase
    {

        #region Members
        protected Dictionary<string, int> m_IndicatorIDByName = null;                                           // Indicator ID by name.
        protected Dictionary<int, string> m_IndicatorNameByID = null;                                           // Indicator name by ID.
        protected Dictionary<int, IndicatorBase> m_IndicatorByID = null;                                        // Indicator by ID.
        #endregion


        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        public IndicatorManagerBase()
        {
            m_IndicatorIDByName = new Dictionary<string, int>();
            m_IndicatorNameByID = new Dictionary<int, string>();
            m_IndicatorByID = new Dictionary<int, IndicatorBase>();
        }
        #endregion


        #region Public Methods
        /// <summary>
        /// Add or update the indicators in the dictionary.
        /// </summary>
        /// <param name="indicatorBase"></param>
        /// <param name="indicatorFullName"></param>
        /// <returns></returns>
        public bool AddIndicator(IndicatorBase indicatorBase, string indicatorFullName)
        {
            if (m_IndicatorByID.ContainsKey(indicatorBase.ID))
                m_IndicatorByID[indicatorBase.ID] = indicatorBase;
            else
                m_IndicatorByID.Add(indicatorBase.ID, indicatorBase);

            if (m_IndicatorIDByName.ContainsKey(indicatorFullName))
                m_IndicatorIDByName[indicatorFullName] = indicatorBase.ID;
            else
                m_IndicatorIDByName.Add(indicatorFullName, indicatorBase.ID);

            if (m_IndicatorNameByID.ContainsKey(indicatorBase.ID))
                m_IndicatorNameByID[indicatorBase.ID] = indicatorFullName;
            else
                m_IndicatorNameByID.Add(indicatorBase.ID, indicatorFullName);
            return true;
        }
        //
        //
        /// <summary>
        /// Feed all indicators.
        /// </summary>
        /// <param name="localTimeNow"></param>
        /// <param name="price"></param>
        public void FeedDataToAllIndicators(DateTime localTimeNow, double price)
        {
            foreach (int id in m_IndicatorByID.Keys)
            {
                FeedDataToIndicatorByID(id, localTimeNow, price);
            }
        }
        #endregion


        #region Private Methods
        /// <summary>
        /// Feed data to indicator by its ID.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="localTimeNow"></param>
        /// <param name="price"></param>
        protected void FeedDataToIndicatorByID(int id, DateTime localTimeNow, double price)
        {
            m_IndicatorByID[id].FeedIndicator(localTimeNow, price);
        }
        #endregion

    }
}