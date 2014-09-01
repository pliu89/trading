using System;
using System.Collections.Generic;
using System.Text;

using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace UV.Lib.Sockets
{
	// **********************************************************************
	// *****                    Conversation                            *****
	// **********************************************************************
	/// <summary>
	/// One instance of this class is created for each thread that is used 
	/// to READ on a socket.  Used to identify who the thread is talking to, 
	/// and whether we want him to disconnect.
	/// </summary>
	public class Conversation
	{

		#region Members
		//
		// ****						Members						****
		//
		private static int m_NextID = 0;
        public string Name = string.Empty;
		public readonly int ID;
		public readonly TcpClient Client = null;
		//public readonly IPEndPoint EndPoint = null;	// this object doesnt need end point, does it?
		public readonly Thread Thread = null;
		public NetworkStream m_OutStream = null;

		public bool IsStarted = false;      // True = thread started
		public bool IsContinuing = true;    // True = continue conversation, False = request to stop
		public bool IsStopping = false;     // True = thread has requested abort

		public delegate void StartFunction(object o);
		//private StartFunction m_StartFunction;
		
		#endregion//members


		#region Constructor
		// *********************************************************
		// ****                 Constructor                     ****
		// *********************************************************
		public Conversation(TcpClient client, StartFunction s)
		{
			// Store my TCP client
			this.Client = client;
			//this.EndPoint = ip;
            this.Name = client.Client.RemoteEndPoint.ToString();


			// prepare my read-thread.
            ID = System.Threading.Interlocked.Increment(ref m_NextID);
			//m_StartFunction = s;
			this.Thread = new Thread(new ParameterizedThreadStart(s));
			this.Thread.Name = string.Format("{0}#{1}",this.Name,ID.ToString());
		}
		//
		//
		#endregion // constructor


		#region Public Methods
		// *********************************************************
		// ****                 Public methods                  ****
		// *********************************************************
		public void Start()
		{
			if (!this.IsStarted)
			{
				this.IsStarted = true;      // We are gonna start our thread.
				this.IsContinuing = true;   // we set our desire to continue conversation.
				this.Thread.Start(this);    // spawn the thread.
			}
		}//Start()
		//
		//
		// ****             Close()             ****
		//
		/// <summary>
		/// Complete closing of the conversation and its socket clients.
		/// </summary>
		public void Close()
		{
			if (!this.IsStopping)
			{
				this.IsStopping = true;
				if (this.Client != null && this.Client.Client != null)
				{
					this.Client.Client.Close();
					this.Client.Close();
					
				}

			}
		}//Close().
		//
		//
		/// <summary>
		/// This is an attempt to allow outsiders to shut down the conversation, 
		/// even when the Conversation is sitting at a read block line.
		/// </summary>
		public void RequestClose()
		{
			NetworkStream stream = GetOutStream();
			if (stream != null)
			{
				this.IsContinuing = false;	// set my exiting flag to exit.
				stream.ReadTimeout = 10;	// give blocking read a short timeout. (will not kick out read block)
				stream.Close();
			}
			else
				this.Close();
		}//RequestClose().
		//
		public NetworkStream GetOutStream()
		{
			if (this.IsStopping || (!this.IsContinuing)) return null;
			if (this.m_OutStream == null)
			{
				if (this.Client != null && this.Client.Connected)
				{
					this.m_OutStream = this.Client.GetStream();
				}
			}
			return m_OutStream;
		}// GetOutStream().
		//
		//
		//
		#endregion//public methods

	}// end Conversation
}
