using System;
using System.Collections.Generic;
using System.Text;

namespace UV.TTServices.Execution
{

    using UV.Strategies.ExecutionHubs;

    /// <summary>
    /// </summary>
    public class ExecutionHubTT : ExecutionHub
    {

        #region no Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        #endregion// members


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


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        protected override ExecutionListener CreateListener(string listenerThreadName)
        {
            ExecutionListenerTT listener = new ExecutionListenerTT(listenerThreadName, this);
            return listener;
        }
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
