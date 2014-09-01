using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Forms;

namespace UV.Lib.Utilities
{
    public class KeyPressMessageFilter  : IMessageFilter
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        //
        //
        public Dictionary<Keys, bool> KeyState = new Dictionary<Keys, bool>();

        //
        // Constants
        //
        private const int WinMsg_KeyDown    = 0x0100;
        private const int WinMsg_KeyUp      = 0x0101;
        
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


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //     
        //  ****            Pre Filter Message()            ****
        //
        /// <summary>
        /// Implements IFilterMessage interface.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == WinMsg_KeyDown)
            {
                Keys currentKey = (Keys) m.WParam;
                KeyState[currentKey] = true;
            }
            else if (m.Msg == WinMsg_KeyUp)
            {
                Keys currentKey = (Keys)m.WParam;
                KeyState[currentKey] = false;
            }
            return false;       // Do not filter this message; allow event processing to proceed as usual.
        }// PreFilterMessage()
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
