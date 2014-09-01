using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UV.TTServices.AutoSpreaders
{
    using TradingTechnologies.TTAPI;
    using TradingTechnologies.TTAPI.Autospreader;
    
    using UVInstrName = UV.Lib.Products.InstrumentName;
    using UVSpreaderLeg = UV.Strategies.ExecutionEngines.OrderEngines.SpreaderLeg;

    using UV.Lib.IO.Xml;
    /// <summary>
    /// This is the begging of a class to try and utilize TT's autospreader.
    /// </summary>
    public class AutoSpreader : IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // external services
        //
        private TTApiService m_TTService = null;

        //
        // Spreads Components
        //
        //private List<Instrument> m_InstrumentLegs = new List<Instrument>();
        //private List<SpreadLegDetails> m_SpreadLegDetails = new List<SpreadLegDetails>();
        private List<UVSpreaderLeg> m_UVSpreaderLegs;
        //private SpreadDetails m_SpreadDetails = null;

        //private AutospreaderInstrument m_AutoSpreader;


        //
        // Lookup tables
        //
        private Dictionary<UVInstrName, Instrument> m_UVInstrToTTInstr = new Dictionary<UVInstrName, Instrument>();
        private Dictionary<UVInstrName, InstrumentKey> m_InstrumentNameToTTKey = new Dictionary<UVInstrName, InstrumentKey>();
        private Dictionary<InstrumentKey, FeedConnectionKey> m_DefaultFeedKey = new Dictionary<InstrumentKey, FeedConnectionKey>();
        
        //
        // flags
        //
        public bool IsReady = false;
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        public AutoSpreader()
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


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        public bool TryCreateAutoSpreader(List<UVSpreaderLeg> quoteLegsList)
        {
            SpreadDetails spreadDetails = new SpreadDetails();
            StringBuilder spreadName = new StringBuilder();
            m_UVSpreaderLegs = quoteLegsList;
            foreach (UVSpreaderLeg  spreadLeg in quoteLegsList)
            {

                UVInstrName instrName = spreadLeg.InstrumentDetails.InstrumentName;
                spreadName.AppendFormat("{0}x{1}.", instrName, spreadLeg.m_PriceLeg.Weight);

                InstrumentKey instrKey;
                FeedConnectionKey feedConnectionKey;
                if (!m_InstrumentNameToTTKey.TryGetValue(instrName, out instrKey))
                    return false; // we can't find the instrumentKey
                if (!m_DefaultFeedKey.TryGetValue(instrKey, out feedConnectionKey))
                    return false; // we can't find the instrumentKey

                // This needs to be fixed due to the newer API 7.17
                //SpreadLegDetails spreadLegDetails = new SpreadLegDetails(instrKey, feedConnectionKey);
                //spreadLegDetails.SpreadRatio = (int)spreadLeg.m_PriceLeg.Weight;
                //spreadLegDetails.PriceMultiplier = spreadLeg.m_PriceLeg.PriceMultiplier;
                //spreadLegDetails.CustomerName = instrName.ToString();

                //spreadDetails.Legs.Append(spreadLegDetails);
            }

            spreadDetails.Name = spreadName.ToString();

            CreateAutospreaderInstrumentRequest autoSpreaderRequest = new CreateAutospreaderInstrumentRequest(m_TTService.session, Dispatcher.Current, spreadDetails);
            autoSpreaderRequest.Completed += new EventHandler<CreateAutospreaderInstrumentRequestEventArgs>(AutoSpreaderInstrumentRequest_Completed);
            autoSpreaderRequest.Submit();

            return true;
        }
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


        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        private void AutoSpreaderInstrumentRequest_Completed(object sender, CreateAutospreaderInstrumentRequestEventArgs autoSpreaderEventArgs)
        {
            if (autoSpreaderEventArgs.Instrument != null)
            {
                AutospreaderInstrument m_AutoSpreader = (AutospreaderInstrument)autoSpreaderEventArgs.Instrument;
                LaunchReturnCode launchReturnCode = m_AutoSpreader.LaunchToOrderFeed(m_AutoSpreader.GetValidOrderFeeds()[0]); // launch to whatever orderfeed we find first
                if(launchReturnCode == LaunchReturnCode.Success)
                {
                    m_AutoSpreader.TradableStatusChanged += new EventHandler<TradableStatusChangedEventArgs>(AutoSpreaderInstrument_TradeableStatusChanged);
                }
            }
        }
        //
        private void AutoSpreaderInstrument_TradeableStatusChanged(object sender, TradableStatusChangedEventArgs tradeableStatusEventArgs)
        {
            if(tradeableStatusEventArgs.Value)
            { // 
                IsReady = true;
            }
            else
            {
                IsReady = false;
            }
        }
        #endregion//Event Handlers

        #region IStringifiable Implementation
        // *****************************************************************
        // ****               IStringifiable Implementation             ****
        // *****************************************************************
        //
        //
        public string GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            return s.ToString();
        }
        public List<IStringifiable> GetElements()
        {
            List<IStringifiable> elements = new List<IStringifiable>();
            foreach (UVSpreaderLeg spreadLeg in m_UVSpreaderLegs)
                elements.Add(spreadLeg);
            // Exit
            return elements;
        }
        public void SetAttributes(Dictionary<string, string> attributes)
        {
            foreach (KeyValuePair<string, string> attr in attributes)
            {
            }
        }
        public void AddSubElement(IStringifiable subElement)
        {
            if (subElement is UVSpreaderLeg)
            {// find all legs we are trying to use to create our spreader
                UVSpreaderLeg spreadLeg = (UVSpreaderLeg)subElement;
                m_UVSpreaderLegs.Add((UVSpreaderLeg)subElement);
            }
        }
        #endregion
    }//end class

}
