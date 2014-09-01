using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.Sockets
{
	public class SocketEventArgs : EventArgs
	{

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        public int ConversationId;
		public SocketEventType EventType;
		public string Message = string.Empty;
		//
		//
        #endregion// members

        
        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************		
		public SocketEventArgs(int conversationId , SocketEventType type, string newMessage)
		{
            this.ConversationId = conversationId;
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
            if ( this.ConversationId >= 0)  
    			return string.Format("[#{2} {0}] {1}", this.EventType.ToString(), this.Message,this.ConversationId);
            else
                return string.Format("[{0}] {1}", this.EventType.ToString(), this.Message, this.ConversationId);
		}
        //
        //
        //
        //
        //
        #endregion//Public Methods


    }//end class
}
