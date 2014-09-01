using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.FrontEnds.PopUps
{
    /// <summary>
    /// This represents a control that fills a popup form on which 
    /// various controls are contained for a particular engine.
    /// </summary>
    public interface IEngineControl
    {

        //bool IsRegenerationRequired
        //{
        //    get;
        //}

        void Regenerate(object sender, EventArgs e);



        void TitleBar_Click(object sender, EventArgs e);

        void AcceptPopUp(System.Windows.Forms.Form parentForm);
    }
}
