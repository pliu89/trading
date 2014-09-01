using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.Engines
{
    using UV.Lib.IO.Xml;


    public class EngineEventArgs : EventArgs, IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
		// Note: When new data fields are added, update "CopyTo()" method!
		//
		//
        // Message type
        public EventType MsgType = EventType.None;
        public EventStatus Status = EventStatus.None;

        //
        // Responder or Target identity.
        //
        public string EngineHubName = string.Empty;
        public int EngineContainerID = -1;
        public int EngineID = -1;
        
        //
        // Message Data
        //
        public List<object> DataObjectList = null;
        
        public int[] DataIntA = null;
        public int[] DataIntB = null;
        public int[] DataIntC = null;

        public double[] DataA = null;
        public double[] DataB = null;
        public double[] DataC = null;

        //
        #endregion// members

        #region Constructors and Creators
        // *****************************************************************
        // ****             Constructors & Creator                      ****
        // *****************************************************************
        //
        public EngineEventArgs()
        {

        }
        //
        //
        // *****************************************
        // ****             Controls            ****
        // *****************************************
        public static EngineEventArgs RequestAllControls(string engineHubName)
        {
            EngineEventArgs req = new EngineEventArgs();
            req.MsgType = EventType.GetControls;
            req.Status = EventStatus.Request;
            req.EngineHubName = engineHubName;
            return req;
        }//end Request AllControls
        public static EngineEventArgs RequestControls(string engineHubName, int engineContainerId )
        {
            EngineEventArgs req = new EngineEventArgs();
            req.MsgType = EventType.GetControls;
            req.Status = EventStatus.Request;
            req.EngineHubName = engineHubName;
            req.EngineContainerID = engineContainerId;          // request controls for single engine container.
            return req;
        }//end Request AllControls
        public static EngineEventArgs ConfirmNewControls(string engineHubName, int engineContainerId = -1)
        {
            EngineEventArgs req = new EngineEventArgs();
            req.MsgType = EventType.GetControls;
            req.Status = EventStatus.Request;
            req.EngineHubName = engineHubName;
            req.EngineContainerID = engineContainerId;          // request controls for single engine container.
            return req;
        }
        // *****************************************
        // ****         Parameter Values        ****
        // *****************************************
        /// <summary>
        /// Requests all parameter values from one engine.
        /// </summary>
        public static EngineEventArgs RequestAllParameters(string engineHubName, int containerId, int engineId)
        {
            EngineEventArgs req = new EngineEventArgs();
            req.MsgType = EventType.ParameterValue;
            req.Status = EventStatus.Request;
            req.EngineHubName = engineHubName;
            req.EngineContainerID = containerId;
            req.EngineID = engineId;
            return req;
        }
        /// <summary>
        /// This is a convenient overload for the Guis.  They will have a container that 
        /// is one-to-one with the engines they are interested in getting parameters for.
        /// </summary>
        /// <param name="engineHubName">StrategyHub of engines with desired parameters</param>
        /// <param name="container">a container that has engineId's that match those you are interested in.</param>
        /// <returns></returns>
        public static List<EngineEventArgs> RequestAllParameters(string engineHubName, IEngineContainer container)
        {
            List<EngineEventArgs> requestList = new List<EngineEventArgs>();
            List<IEngine> engines = container.GetEngines();
            foreach (IEngine engine in engines)
                requestList.Add(RequestAllParameters(engineHubName, container.EngineContainerID, engine.EngineID));
            return requestList;
        }
        //       
        //
        //
        // *****************************************
        // ****         Parameter Change        ****
        // *****************************************
        public static EngineEventArgs RequestParameterChange(ParameterInfo pInfo, object newValue)//, int RequestingHubID)
        {
            EngineEventArgs req = new EngineEventArgs();
            req.EngineHubName = pInfo.EngineHubName;
            req.EngineContainerID = pInfo.EngineContainerID;
            req.EngineID = pInfo.EngineID;
            req.MsgType = EngineEventArgs.EventType.ParameterChange;
            req.Status = EngineEventArgs.EventStatus.Request;
            req.DataIntA = new int[] { pInfo.ParameterID };
            req.DataObjectList = new List<object>();
            req.DataObjectList.Add(newValue);
            return req;
        }//RequestParameterChange()
        //
        //
        // *****************************************
        // ****             NewEngine           ****
        // *****************************************
        public static EngineEventArgs ConfirmNewEngine(string engineHubName, int containerId, Engine newEngine)
        {
            EngineEventArgs e = new EngineEventArgs();
            e.EngineHubName = engineHubName;
            e.EngineContainerID = containerId;
            e.EngineID = newEngine.EngineID;
            e.MsgType = EngineEventArgs.EventType.NewEngine;
            e.Status = EngineEventArgs.EventStatus.Confirm;
            // Load necessary information for RemoteEngine back at the StrategyHub
            e.DataObjectList = new List<object>();
            e.DataObjectList.AddRange(newEngine.GetGuiTemplates());
            return e;
        }//end Request AllControls
        //
        //
        // *****************************************
        // ****        RequestSaveEngines       ****
        // *****************************************
        public static EngineEventArgs RequestSaveEngines(string engineHubName, int engineContainerId=-1)
        {
            EngineEventArgs req = new EngineEventArgs();
            req.MsgType = EventType.SaveEngines;
            req.Status = EventStatus.Request;
            req.EngineHubName = engineHubName;
            req.EngineContainerID = engineContainerId;          // request controls for single engine container.
            // TODO: In future, allow data with file path to be addded.
            return req;
        }//end Request AllControls

        //
        //
        #endregion//Constructors

        #region Enums
        // *****************************************************************
        // ****                     Enums                               ****
        // *****************************************************************
        //
        //
        //
        public enum EventType 
        {
            // Engine parameter:
            ParameterChange,        // request/confirm changes for parameter values
            ParameterValue,         // request/confirm for parameter values
            SaveEngines,            // request to export XML for engine.
            //
            // Execution requests
            //
            NewEngine,              // request/confirm to create new engines
            AddContainer,           // caller would like for hub to add engine container it has created.
            SyntheticOrder,         // Order submission/confirmation between strategy and execution hubs.  
            //
            // Gui controls:
            //
            GetControls,            // This is a request/confirm for a new complete collection of controls.            
            ClusterUpdate,          // updates for cluster GUI controls
			CustomEvent,			// eventArgs are loaded in DataObjectList for consumption by receiving engine.
            //
            // Alarm events
            //
            AlarmTriggered,
            None
        }
        //
        //
        public enum EventStatus
        {
            Confirm,
            Request,
            Failed,
            TradingStart,
            TradingEnd,
            EconomicEventStart,
            EconomicEventEnd,
            CustomEventStart,       // user defined times to avoid start
            CustomEventEnd,         // user defined times to avoid start
            None
        }
        //
        //
        #endregion//Event Handlers

		#region Public Methods
		// *************************************************************************
		// ****							Public Methods							****
		// *************************************************************************
		public EngineEventArgs Copy()
        {
            EngineEventArgs newEventArg = new EngineEventArgs();
            this.CopyTo(newEventArg);
            return newEventArg;
        }
		//
        protected void CopyTo(EngineEventArgs newArg)
		{            

			newArg.MsgType = this.MsgType;
			newArg.Status = this.Status;
            newArg.EngineHubName = this.EngineHubName;
			newArg.EngineContainerID = this.EngineContainerID;
			newArg.EngineID = this.EngineID;
			// Object data
			if ( this.DataObjectList != null )
			{
				object[] oArray = new object[DataObjectList.Count];
				this.DataObjectList.CopyTo(oArray);
				newArg.DataObjectList = new List<object>(oArray);
			}
			// Integer data.
			if (this.DataIntA != null)
			{
				newArg.DataIntA = new int[this.DataIntA.Length];
				this.DataIntA.CopyTo(newArg.DataIntA,0);
			}
			if (this.DataIntB != null)
			{
				newArg.DataIntB = new int[this.DataIntB.Length];
				this.DataIntB.CopyTo(newArg.DataIntB,0);
			}
			if (this.DataIntC != null)
			{
				newArg.DataIntC = new int[this.DataIntC.Length];
				this.DataIntC.CopyTo(newArg.DataIntC,0);
			}
			// double data.
			if (this.DataA != null)
			{
				newArg.DataA = new double[this.DataA.Length];
				this.DataA.CopyTo(newArg.DataA,0);
			}
			if (this.DataB != null)
			{
				newArg.DataB = new double[this.DataB.Length];
				this.DataB.CopyTo(newArg.DataB, 0);
			}
			if (this.DataC != null)
			{
				newArg.DataC = new double[this.DataC.Length];
				this.DataC.CopyTo(newArg.DataC, 0);
			}

		}// CopyTo()
		//
		//
		public override string ToString()
		{
			StringBuilder msg = new StringBuilder();
			//msg.Append("EngineEvent ");
			msg.AppendFormat("EngineEvent {0}:{1}:{2} ",this.EngineHubName,this.EngineContainerID,this.EngineID);
            msg.AppendFormat("[{0} {1}]", this.Status.ToString(), this.MsgType.ToString());
			//
			// Integers
			//
            /*
			msg.AppendFormat(" Integer:");
			if (DataIntA != null && DataIntA.Length > 0)
			{
				msg.AppendFormat("[");
				foreach (int x in DataIntA) { msg.AppendFormat("{0} ", x.ToString()); }
				msg.AppendFormat("] ");
			}
			else
				msg.AppendFormat("[] ");
			if (DataIntB != null && DataIntB.Length > 0)
			{
				msg.AppendFormat("[");
				foreach (int x in DataIntB) { msg.AppendFormat("{0} ", x.ToString()); }
				msg.AppendFormat("] ");
			}
			else
				msg.AppendFormat("[] ");
			if (DataIntC != null && DataIntC.Length > 0)
			{
				msg.AppendFormat("[");
				foreach (int x in DataIntC) { msg.AppendFormat("{0} ", x.ToString()); }
				msg.AppendFormat("] ");
			}
			else
				msg.AppendFormat("[] ");
			//
			// Doubles
			//
			msg.AppendFormat(" Double:");			
			if (DataA != null && DataA.Length > 0)
			{
				msg.AppendFormat("[");
				foreach (double x in DataA) { msg.AppendFormat("{0} ", x.ToString()); }
				msg.AppendFormat("] ");
			}
			else
				msg.AppendFormat("[] ");
			if (DataB != null && DataB.Length > 0)
			{
				msg.AppendFormat("[");
				foreach (double x in DataB) { msg.AppendFormat("{0} ", x.ToString()); }
				msg.AppendFormat("] ");
			}
			else
				msg.AppendFormat("[] ");
			if (DataC != null && DataC.Length > 0)
			{
				msg.AppendFormat("[");
				foreach (double x in DataC) { msg.AppendFormat("{0} ", x.ToString()); }
				msg.AppendFormat("] ");
			}
			else
				msg.AppendFormat("[] ");
			//
			// Objects
			//
			msg.AppendFormat(" Object:");
			if (DataObjectList != null && DataObjectList.Count > 0)
			{
				//msg.AppendFormat("[");
				foreach (object x in DataObjectList)
                    if ( x != null) 
    					msg.AppendFormat("[{0} {1}] ", x.GetType().Name, x.ToString()); 
			}
			else
				msg.AppendFormat("[] ");
            */
			return msg.ToString();
		}// ToString()
		//
		//
		//
		//
		#endregion//end public methods

        #region IStringifiable 
        public string GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            s.AppendFormat(" MsgType={0}",MsgType);
            s.AppendFormat(" Status={0}",Status);
            s.AppendFormat(" EngineHub={0}",EngineHubName);
            s.AppendFormat(" EngineContainerID={0}",EngineContainerID);
            if (this.EngineID >= 0)
                s.AppendFormat(" EngineID={0}",EngineID);
            return s.ToString();
        }
        public List<IStringifiable> GetElements()
        {
            List<IStringifiable> elements = new List<IStringifiable>();
            if (this.DataIntA != null)
            {
                IntegerArray a = new IntegerArray();
                a.Name = "A";
                a.Data = this.DataIntA;
                elements.Add(a);
            }
            if (this.DataIntB != null)
            {
                IntegerArray a = new IntegerArray();
                a.Name = "B";
                a.Data = this.DataIntB;
                elements.Add(a);
            }
            if (this.DataIntC != null)
            {
                IntegerArray a = new IntegerArray();
                a.Name = "C";
                a.Data = this.DataIntC;
                elements.Add(a);
            }
            if (this.DataA != null)
            {
                DoubleArray a = new DoubleArray();
                a.Name = "A";
                a.Data = this.DataA;
                elements.Add(a);
            }
            if (this.DataB != null)
            {
                DoubleArray a = new DoubleArray();
                a.Name = "B";
                a.Data = this.DataB;
                elements.Add(a);
            }
            if (this.DataC != null)
            {
                DoubleArray a = new DoubleArray();
                a.Name = "C";
                a.Data = this.DataC;
                elements.Add(a);
            }
            if (this.DataObjectList != null)
            {
                foreach (object o in this.DataObjectList)
                {
                    if ( o is IStringifiable)
                        elements.Add((IStringifiable) o);
                    else
                    {
                        ObjectString objStr = new ObjectString();
                        objStr.ClassName = o.GetType().FullName;
                        objStr.Data = o.ToString();
                        elements.Add(objStr);
                    }

                }
            }


            
            return elements;
        }
        public void SetAttributes(Dictionary<string, string> attributes)
        {
            EventType type;
            EventStatus status;
            int n;
            foreach(KeyValuePair<string,string>a in attributes)
            {
                if (a.Key.Equals("MsgType", StringComparison.CurrentCultureIgnoreCase) && Enum.TryParse<EventType>(a.Value, out type))
                    this.MsgType = type;
                else if (a.Key.Equals("Status", StringComparison.CurrentCultureIgnoreCase) && Enum.TryParse<EventStatus>(a.Value, out status))
                    this.Status = status;
                else if (a.Key.Equals("EngineHub"))
                    this.EngineHubName = a.Value;
                else if (a.Key.Equals("EngineContainerID") && int.TryParse(a.Value, out n))
                    this.EngineContainerID = n;
                else if (a.Key.Equals("EngineID") && int.TryParse(a.Value, out n))
                    this.EngineID = n;
            }
        }
        public void AddSubElement(IStringifiable subElement)
        {
            if (subElement is IntegerArray)
            {
                IntegerArray a = (IntegerArray)subElement;
                if (a.Name.Equals("A"))
                    this.DataIntA = a.Data;
                else if (a.Name.Equals("B"))
                    this.DataIntB = a.Data;
                else if (a.Name.Equals("C"))
                    this.DataIntC = a.Data;
            }
            else if (subElement is DoubleArray)
            {
                DoubleArray a = (DoubleArray)subElement;
                if (a.Name.Equals("A"))
                    this.DataA = a.Data;
                else if (a.Name.Equals("B"))
                    this.DataB = a.Data;
                else if (a.Name.Equals("C"))
                    this.DataC = a.Data;
            }
            else if (subElement is ObjectString)
            {
                if (this.DataObjectList == null)
                    this.DataObjectList = new List<object>();
                ObjectString obj = (ObjectString) subElement;
                Type type;
                if (Stringifiable.TryGetType(obj.ClassName,out type))
                {
                    object o = UV.Lib.Utilities.ConvertType.ChangeType(type, obj.Data);
                    this.DataObjectList.Add(o); 
                }
            }
            else if (subElement is IStringifiable)      // todo storage of values
            {
                if (this.DataObjectList == null)
                    this.DataObjectList = new List<object>();
                this.DataObjectList.Add(subElement);
            }
        }
        //
        //
        //
        #endregion // IStringifiable

        #region IStringifiable helper classes
        // *************************************************
        // ****             IntegerArray                ****
        // *************************************************
        private class IntegerArray : IStringifiable
        {
            public int[] Data = null;
            public string Name = "A";

            public string GetAttributes()
            {
                StringBuilder s = new StringBuilder();
                s.AppendFormat("Name={0}",Name);
                if ( this.Data != null)
                {
                    s.AppendFormat(" Data=");
                    foreach (int i in Data)
                        s.AppendFormat(" {0}", i.ToString());
                }
                return s.ToString();
            }
            public List<IStringifiable> GetElements()
            {
                return null;
            }
            public void SetAttributes(Dictionary<string, string> attributes)
            {
                foreach (KeyValuePair<string,string>a in attributes)
                {
                    if (a.Key.Equals("Name", StringComparison.CurrentCultureIgnoreCase))
                        this.Name = a.Value;
                    else if (a.Key.Equals("Data", StringComparison.CurrentCultureIgnoreCase))
                    {
                        string[] sList = a.Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        this.Data = new int[sList.Length];
                        for (int i = 0; i < sList.Length; ++i)
                            this.Data[i] = int.Parse(sList[i]);
                    }
                }
            }
            public void AddSubElement(IStringifiable subElement)
            {             
            }
        }
        //
        //
        // *************************************************
        // ****             DoubleArray                 ****
        // *************************************************
        private class DoubleArray : IStringifiable
        {
            public double[] Data = null;
            public string Name = "A";

            public string GetAttributes()
            {
                StringBuilder s = new StringBuilder();
                s.AppendFormat("Name={0}", Name);
                if (this.Data != null)
                {
                    s.AppendFormat(" Data=");
                    foreach (double x in Data)
                        s.AppendFormat(" {0}", x.ToString());
                }
                return s.ToString();
            }
            public List<IStringifiable> GetElements()
            {
                return null;
            }
            public void SetAttributes(Dictionary<string, string> attributes)
            {
                foreach (KeyValuePair<string, string> a in attributes)
                {
                    if (a.Key.Equals("Name", StringComparison.CurrentCultureIgnoreCase))
                        this.Name = a.Value;
                    else if (a.Key.Equals("Data", StringComparison.CurrentCultureIgnoreCase))
                    {
                        string[] sList = a.Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        this.Data = new double[sList.Length];
                        for (int i = 0; i < sList.Length; ++i)
                            this.Data[i] = double.Parse(sList[i]);
                    }
                }
            }
            public void AddSubElement(IStringifiable subElement)
            {
            }
        }
        //
        // *************************************************
        // ****             DoubleArray                 ****
        // *************************************************
        private class ObjectString : IStringifiable
        {
            public string Data = string.Empty;
            public string ClassName = string.Empty;

            public string GetAttributes()
            {
                StringBuilder s = new StringBuilder();
                s.AppendFormat("ClassName={0}", ClassName);

                string d = Data;
                d = d.Replace("\n", "");
                d = d.Replace("\r", "");
                d = d.Replace("<", "**,**");
                d = d.Replace(">", "**.**");
                d = d.Replace("=", "**+**");
                s.AppendFormat(" Data={0}", d);
                return s.ToString();
            }
            public List<IStringifiable> GetElements()
            {
                return null;
            }
            public void SetAttributes(Dictionary<string, string> attributes)
            {
                foreach (KeyValuePair<string, string> a in attributes)
                {
                    if (a.Key.Equals("ClassName", StringComparison.CurrentCultureIgnoreCase))
                        this.ClassName = a.Value;
                    else if (a.Key.Equals("Data", StringComparison.CurrentCultureIgnoreCase))
                    {
                        string d = a.Value;
                        d = d.Replace("**,**", "<");
                        d = d.Replace("**.**", ">");
                        d = d.Replace("**+**", "=");
                        this.Data = d;
                    }
                }
            }
            public void AddSubElement(IStringifiable subElement)
            {
            }
        }
        //
        //
        //
        //
        #endregion // IStringifiable helpers

    }
}
