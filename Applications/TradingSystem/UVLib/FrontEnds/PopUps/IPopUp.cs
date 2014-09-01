using System;
using System.Collections.Generic;
using System.Text;

using System.Windows.Forms;

namespace UV.Lib.FrontEnds.PopUps
{
    public interface IPopUp
    {

        //
        // Parameters
        //
        IEngineControl CustomControl
        {
            get;
        }

        bool Visible
        {
            get;
            set;
        }


        //
        // Methods
        //
        void ShowMe(Control ParentCluster);



    }
}