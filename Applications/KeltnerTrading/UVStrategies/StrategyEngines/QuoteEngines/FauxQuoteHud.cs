using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
//using System.Data;
//using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UV.Strategies.StrategyEngines.QuoteEngines
{
    using UV.Lib.Engines;
    using UV.Lib.FrontEnds.GuiTemplates;
    using UV.Lib.FrontEnds.Huds;
    using UV.Lib.FrontEnds.PopUps;


    public partial class TradeEngineHud : HudPanel
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public TradeEngineHud(EngineGui engineGui) : base(engineGui)
        {
            InitializeComponent();

            pIsQuoteEnabled.Text = "Quote";
            base.Initialize(engineGui);
                        
            
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

    }//end class
}
