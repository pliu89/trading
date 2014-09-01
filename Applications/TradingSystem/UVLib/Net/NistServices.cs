using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

namespace UV.Lib.Net
{
	public class NistServices
	{
		#region Members
		// *****************************************************************
		// ****                     Members                             ****
		// *****************************************************************
		//
		// Static pointer for myself
		//		
		private static NistServices m_Instance = null;

		// List of servers.
		private string[] m_NistServers = new string[]{
			//"nist1-chi.ustiming.org"
			//"208.66.175.36"
			//,
			"38.106.177.10"
			//,"64.113.32.5"//
			//,"nist.expertsmi.com"
			,"66.219.116.140"
		};
		//"nist1-ny.ustiming.org","nist1-nj.ustiming.org",
		//"nist1-nj.ustiming.org",


		private bool m_IsDayLightSavingsInEffect = false;
		private TimeSpan m_SystemOffset;					// Best estimate Offset to correct PC time.
		private bool m_IsGood = false;

		private List<TimeSpan> m_SystemOffsetList = new List<TimeSpan>();
		private List<DateTime> m_SystemTimeList = new List<DateTime>();

		// Error logging.
		private List<string> m_Messages = new List<string>();
		private object m_MessagesLock = new object();

		#endregion//members



		#region  Private Constructor		
		// *****************************************************************
		// ***                  Constructor                             ****
		// *****************************************************************
		//
		protected NistServices(bool isConnectToNIST)
		{
			m_Instance = (NistServices)this;
			if ( isConnectToNIST ) Initialize();
		}
		//
		//
		//
		//
		private void Initialize()
		{
			//Random ran = new Random(DateTime.Now.Millisecond);
			DateTime date = DateTime.Today;
			string serverResponse = string.Empty;
			int serverIndex = 0;
			bool keepTrying = true;
			while (keepTrying && serverIndex < m_NistServers.Length)
			{
				try
				{
					// Open a StreamReader to server
					string serverName = m_NistServers[serverIndex];
					int serverPort = 13;
					System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient(serverName, serverPort);
					client.ReceiveTimeout = 1;
					System.Net.Sockets.NetworkStream stream = client.GetStream();
					stream.ReadTimeout = 1;
					StreamReader reader = new StreamReader(client.GetStream());
					serverResponse = reader.ReadToEnd();
					reader.Close();					
					
					DateTime systemTime = DateTime.Now;
					string[] elements = serverResponse.Split(new char[]{' ','\n'},StringSplitOptions.RemoveEmptyEntries);
					// Check to see that the signiture is there
					//if (serverResponse.Length > 47 && serverResponse.Substring(38, 9).Equals("UTC(NIST)"))
					if (elements!=null && elements.Length >= 7 && String.Equals(elements[7],"UTC(NIST)",StringComparison.CurrentCultureIgnoreCase))
					{
						
						int modJulianDate = int.Parse(elements[0]);
						// date - original method
						//string[] s = elements[1].Split(new char[]{'-'},StringSplitOptions.RemoveEmptyEntries);
						//int yr = int.Parse(s[0]);
						//int mo = int.Parse(s[1]);
						//int dd = int.Parse(s[2]);
						//if (modJulianDate > 51544) yr += 2000; else yr += 1999;
						// time
						//string[] s = elements[2].Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
						//int hr = int.Parse(s[0]);
						//int mm = int.Parse(s[1]);
						//int ss = int.Parse(s[2]);
						//DateTime nistTime = new DateTime(yr, mo, dd, hr, mm, ss);
						// New method
						DateTime nistTime = new DateTime(1858, 11, 17);	// base modified julian date.
						nistTime = nistTime.AddDays(modJulianDate);
						nistTime = nistTime.Add(TimeSpan.Parse(elements[2]));
						nistTime = nistTime.ToLocalTime();		//convert to local time.
						// flags
						int dayLightCode = int.Parse(elements[3]);
						int leapSecCode = int.Parse(elements[4]);
						int healthCode = int.Parse(elements[5]);
						double msAdvance = double.Parse(elements[6]);
						// elements[7] = "UTC(NIST)"
						// elements[8] = "*"; // on-time-messsage
						

						//
						// Now estimate correction.
						//
						if (dayLightCode > 0) { m_IsDayLightSavingsInEffect = true; }
						TimeSpan systemOffset = nistTime.Subtract(systemTime);
						if (healthCode == 0)
						{
							m_SystemTimeList.Add(systemTime);
							m_SystemOffsetList.Add(systemOffset);
							string msg = string.Format("NIST server #{1} at {2} is healthy: Offset = {0}.", systemOffset.ToString(), serverIndex.ToString(), m_NistServers[serverIndex]);
							lock (m_MessagesLock)
							{
								m_Messages.Add(string.Format("{0} {1}", DateTime.Now.ToShortTimeString(), msg));
							}
							Console.WriteLine(msg);

						}
						else
						{
							string msg = string.Format("NIST server #{1} at {2} is NOT healthy: Offset = {0}.", systemOffset.ToString(), serverIndex.ToString(), m_NistServers[serverIndex]);
							lock (m_MessagesLock)
							{
								m_Messages.Add(string.Format("{0} {1}", DateTime.Now.ToShortTimeString(), msg));
							}
							Console.WriteLine(msg);
						}
						// Usage:
						//DateTime correctTimeEstimate = m_SystemTime.Add(m_SystemOffset);	// use this!
					}
					else
					{
						string msg = string.Format("NIST server #{1} at {2} gave short response: Response = {0}.", serverResponse, serverIndex.ToString(), m_NistServers[serverIndex]);
						lock (m_MessagesLock)
						{
							m_Messages.Add(string.Format("{0} {1}", DateTime.Now.ToShortTimeString(), msg));
						}
						Console.WriteLine(msg);
					}
				}
				catch (Exception e)
				{
					string msg = string.Format("NIST server #{1} at {2} failed. \nException = {0}.", e.Message,serverIndex.ToString(), m_NistServers[serverIndex]);
					lock (m_MessagesLock)
					{
						m_Messages.Add(string.Format("{0} {1}", DateTime.Now.ToShortTimeString(), msg));
					}
					Console.WriteLine(msg);
					//System.Windows.Forms.MessageBox.Show(msg, "Failed to connect to NIST.");
				}
				serverIndex++;
			}//wend
			if (m_SystemOffsetList.Count > 0)
			{
				// Analyze results
				double aveOffset = 0;
				for (int i = 0; i < m_SystemOffsetList.Count; ++i)
				{
					aveOffset += m_SystemOffsetList[i].TotalSeconds;
				}
				aveOffset = aveOffset / m_SystemOffsetList.Count;
				int hours = (int)Math.Floor(aveOffset / 3600.0);
				aveOffset -= 3600.0 * hours;
				int mins = (int)Math.Floor(aveOffset / 60.0);
				aveOffset -= 60.0 * mins;
				int secs = (int)Math.Floor(aveOffset);
				aveOffset -= secs;
				int msecs = (int)Math.Floor(aveOffset * 1000.0);

				m_SystemOffset = new TimeSpan(0, hours, mins, secs, msecs);
				m_IsGood = true;
			}

		}//Initialize()
		//
		//
		#endregion//constructor


		#region  Properties
		// *****************************************************************
		// ***                  Properties                             ****
		// *****************************************************************
		//
		public bool IsGood
		{
			get { return m_IsGood; }
		}
		//
		public bool IsDayLightSavingsInEffect
		{
			get{ return m_IsDayLightSavingsInEffect; }
		}
		//
		//
		/// <summary>
		/// This timespan should be added to system time to be closer to NIST time.
		/// For example: 
		/// DateTime correctNowEstimate = (DateTime.Now).Add(SystemTimeOffset)
		/// </summary>
		public TimeSpan SystemTimeOffset		
		{
			get { return m_SystemOffset; }
		}
		//
		//
		/// <summary>
		/// Allows user thread-safe access to any messages or warnings that this 
		/// services encountered.
		/// </summary>
		public string[] Messages
		{
			get
			{
				string[] messages;
				lock (m_MessagesLock)
				{
					messages = m_Messages.ToArray();
				}
				return messages;
			}
		}
		//
		//
		//
		#endregion//properties


		#region  Static Methods
		// *****************************************************************
		// ***                  Static Methods                          ****
		// *****************************************************************
		//
		/// <summary>
		/// For non-time-critical runs, the nist timer can be simply bypassed 
		/// by setting isConnectToNist = false.
		/// </summary>
        /// <param name="isConnectToNist">True to do contact nist, false bypasses everything.</param>
		/// <returns></returns>
		public static NistServices GetInstance(bool isConnectToNist)
		{
			if (m_Instance == null) 
				m_Instance = new NistServices( isConnectToNist );
			return m_Instance;
		}
		// Default call.
		public static NistServices GetInstance()
		{
			return GetInstance(true);
		}//GetInstance().
		//
		//
		//
		#endregion// Static Methods


	}//end class
}//end namespace
