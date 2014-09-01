using System;
using System.Collections.Generic;
using System.Text;

namespace Ambre.TTServices.Talker
{
    public class TalkerHubEventArg : EventArgs
    {


        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        public TalkerHubRequest Request ;
        public List<object> Data = null;
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public TalkerHubEventArg(TalkerHubRequest request)
        {
            this.Request = request;
        }
        //
        //       
        #endregion//Constructors


    }
}
