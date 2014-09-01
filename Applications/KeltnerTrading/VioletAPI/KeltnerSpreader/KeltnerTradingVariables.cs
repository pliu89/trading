using System;
using System.Collections.Generic;

namespace VioletAPI.KeltnerSpreader
{
    using UV.Lib.IO.Xml;

    public class KeltnerTradingVariables : IStringifiable
    {

        #region Members
        public int BarIntervalInSeconds = 900;                                          // Bar interval to trade. It will be used to initialize the trading indicators.
        public double EconomicEventBlockingHour = 0.01;                                 // Economic blocking time measured in hour.
        public int EMALength = 20;                                                      // This is to specify the parameter for the exponential moving average.
        public int ATRLength = 15;                                                      // This is to specify the parameter for the average true range.
        public int MomentumLength = 12;                                                 // This is to specify the parameter for the momentum indicator.
        public double EntryWidth = 1.50;                                                // This is the entry width for user to enter the market if spread price deviates from fair price.
        public double FadeWidth = 3.00;                                                 // This is the fade width for user to enter the market if spread price deviates further from fair price.
        public double PukeWidth = 6.00;                                                 // This is the puke width for user to exit the market to stop loss if spread price deviates too much from fair price.
        public double MomentumEntryValue = 0.06;                                        // This is the entry puke momentum value to enter trades.
        public double MomentumPukeValue = 0.30;                                         // This is the momentum puke value if in short time the spread price moved in a bad direction for immediate stop loss.
        public int EntryQty = 15;                                                       // This is the quantity level at entry point.
        public int FadeQty = 15;                                                        // This is the quantity level at fade point.
        public int DripQty = 15;                                                        // Drip quantity to minimize execution risk.
        public int CurrentPos = 0;                                                      // Current position.
        public string MarketRunningDateTime = "08:30:00-13:15:00/19:00:00-07:45:00";    // Exchange open/close time ranges.
        public int StopLossTimeTrack = 60000;                                           // Stop loss trigger time tracker.
        public int MaxNetPosition;                                                      // Maximum spread position allowed.
        public int MaxTotalFills;                                                       // Maximum total fills allowed.
        #endregion


        #region Constructor
        public KeltnerTradingVariables()
        {
            MaxNetPosition = EntryQty + FadeQty;
            MaxTotalFills = 6 * MaxNetPosition;
        }
        #endregion


        #region IStringifiable Implementation
        public string GetAttributes()
        {
            return string.Format("BarIntervalInSeconds={0} EconomicEventBlockingHour={1} EMALength={2} ATRLength={3} MomentumLength={4} EntryWidth={5} FadeWidth={6} PukeWidth={7} MomentumEntryPukeValue={8} MomentumPukeValue={9} EntryQty={10} FadeQty={11} DripQty={12} CurrentPos={13} MarketRunningDateTime={14}"
                , BarIntervalInSeconds, EconomicEventBlockingHour, EMALength, ATRLength, MomentumLength, EntryWidth, FadeWidth, PukeWidth, MomentumEntryValue, MomentumPukeValue, EntryQty, FadeQty, DripQty, CurrentPos, MarketRunningDateTime);
        }

        public List<IStringifiable> GetElements() { return null; }

        public void SetAttributes(Dictionary<string, string> attributes)
        {
            double x;
            int y;
            foreach (string key in attributes.Keys)
            {
                if (key.Equals("BarIntervalInSeconds") && Int32.TryParse(attributes[key], out y))
                    this.BarIntervalInSeconds = y;
                else if (key.Equals("EconomicEventBlockingHour") && double.TryParse(attributes[key], out x))
                    EconomicEventBlockingHour = x;
                else if (key.Equals("EMALength") && Int32.TryParse(attributes[key], out y))
                    this.EMALength = y;
                else if (key.Equals("ATRLength") && Int32.TryParse(attributes[key], out y))
                    this.ATRLength = y;
                else if (key.Equals("MomentumLength") && Int32.TryParse(attributes[key], out y))
                    this.MomentumLength = y;
                else if (key.Equals("EntryWidth") && double.TryParse(attributes[key], out x))
                    this.EntryWidth = x;
                else if (key.Equals("FadeWidth") && double.TryParse(attributes[key], out x))
                    this.FadeWidth = x;
                else if (key.Equals("PukeWidth") && double.TryParse(attributes[key], out x))
                    this.PukeWidth = x;
                else if (key.Equals("MomentumEntryPukeValue") && double.TryParse(attributes[key], out x))
                    this.MomentumEntryValue = x;
                else if (key.Equals("MomentumPukeValue") && double.TryParse(attributes[key], out x))
                    this.MomentumPukeValue = x;
                else if (key.Equals("EntryQty") && Int32.TryParse(attributes[key], out y))
                    this.EntryQty = y;
                else if (key.Equals("FadeQty") && Int32.TryParse(attributes[key], out y))
                    this.FadeQty = y;
                else if (key.Equals("DripQty") && Int32.TryParse(attributes[key], out y))
                    this.DripQty = y;
                else if (key.Equals("CurrentPos") && Int32.TryParse(attributes[key], out y))
                    this.CurrentPos = y;
                else if (key.Equals("MarketRunningDateTime"))
                    this.MarketRunningDateTime = attributes[key];
            }
            this.MaxNetPosition = this.EntryQty + this.FadeQty;
            this.MaxTotalFills = 6 * this.MaxNetPosition;
        }

        public void AddSubElement(IStringifiable subElement) { }
        #endregion

    }
}
