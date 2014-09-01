using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UV.Lib.FrontEnds.PopUps;
using UV.Lib.FrontEnds.Huds;

namespace UV.Lib.Engines
{
    //
    //
    /// <summary>
    /// 
    /// </summary>
    public interface IEngine
    {
        //
        // Identification for engine
        //  
        int EngineID{ get; }            // unique index for this engine's position in parent EngineContainer's list.
        string EngineName { get; }      // non-unique user-friendly name.
        
        //bool IsReady { get; }           // denotes whether the engine is prepared to execute its functions.

        bool IsUpdateRequired { get; set; }

        void ProcessEvent(EventArgs eArgs);   // process engine events.

        //IEngineControl GetControl();     // returns a GUI control 
        //HudPanel GetHudPanel();         // returns a GUI control 


        //
        // ****             Setup Complete()             ****
        //
        // <summary>
        // Finalizes the initialization of this engine. It is called only after all engines in the same 
        // engine container exist (and have well-defined ID numbers) so that communications links between 
        // them may be initialized.
        // </summary>
        //void SetupComplete();   

		//
		// ****				ToLongString()					****
		// 
		// <summary>
		// Returns engine name and all its parameters.
		// </summary>
		//string ToLongString();


     

    }//end IEngine interface
}
