using System;
using System.Collections.Generic;
using System.Text;

namespace Misty.Lib.Sockets
{
	public class SocketEventArgs : EventArgs
	{

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
		public SocketEventType EventType;
		public string Message = string.Empty;
		//
		//
        #endregion// members

        
        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************		
		public SocketEventArgs(SocketEventType type, string newMessage)
		{
			this.EventType = type;
			this.Message = newMessage;
			
		}
        //
        //       
        #endregion//Constructors



        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
		public override string ToString()
		{
			return string.Format("[{0}] {1}", this.EventType.ToString(), this.Message);
		}
        //
        //
        //
        //
        //
        #endregion//Public Methods


    }//end class
}
