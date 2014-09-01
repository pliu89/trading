using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies.StrategyEngines
{

    using UV.Lib.Engines;
    using UV.Lib.Hubs;
    using UV.Strategies.StrategyHubs;

    /// <summary>
    /// Base class for all Model engines.
    /// </summary>
    public class ModelEngine : Engine
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // My Strategy and other resources
        //
        protected Strategy m_Parent = null;                                   // strategy that owns this
        protected LogHub Log = null;                                          // Log
        protected ZGraphEngine m_GraphEngine = null;

        // My internal variables
        protected int m_ModelEngineID = 0;

        #endregion// members



        #region Constructors and Setup
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public ModelEngine() : base()
        {
        }
        //
        //
        // ****         SetupBegin()            ****
        //
        public override void SetupBegin(IEngineHub myEngineHub, IEngineContainer engineContainer)
        {
            base.SetupBegin(myEngineHub, engineContainer);
            m_Parent = (Strategy)engineContainer;
            Log = ((Hub)myEngineHub).Log;

            // Get Engines of interest.
            int modelCounter = 0;
            foreach (IEngine ieng in m_Parent.GetEngines())
            {
                if (ieng is ZGraphEngine)
                {
                    if (m_GraphEngine == null)
                        m_GraphEngine = (ZGraphEngine)ieng;                 // take first found graph engine to draw to.
                }
                else if (ieng is ModelEngine)
                {
                    if (ieng == this) { m_ModelEngineID = modelCounter; }	// store the number of models in front of me (use as my id!)
                    modelCounter++;
                }
            } 
        }//SetupBegin()
        //
        //
        //
        // ****         SetupComplete()         ****
        //
        //public override void SetupComplete() 
        //{
        //    base.SetupComplete();
        //}//SetupComplete()
        //
        //       
        #endregion//Constructors and Setup


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

    }
}
