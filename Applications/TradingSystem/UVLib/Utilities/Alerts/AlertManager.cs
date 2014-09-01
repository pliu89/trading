using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UV.Lib.Utilities.Alerts
{
    using UV.Lib.Hubs;
    using UV.Lib.Application;
    using UV.Lib.IO.Xml;
    using UV.Lib.Engines;

    using System.Collections.Concurrent;
    /// <summary>
    /// AlertManager service that can send emails, sms, and control other user alerts.  This is a singleton service
    /// that any object can utilize.
    /// </summary>
    public class AlertManager : Hub, IService, IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //    
        private AppServices m_AppServices = null;
        private ServiceStates m_ServiceState = ServiceStates.Unstarted;
        private string m_FromEmailAddress = string.Empty;
        private string m_FromEmailPassword = string.Empty;
        private string m_FromEmailSMTPServer = "smtp.gmail.com";
        private int m_FromEmailSMTPPort = 587;
        private List<AlertUser> m_AlertUsers = new List<AlertUser>();           // list of registerted users who want to recieve alerts

        private ConcurrentQueue<AlertManagerRequest> m_EmailQueue = new ConcurrentQueue<AlertManagerRequest>();  //FIFO queue for requests that need emails sent 
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        public AlertManager()
            : base("AlertManager", AppServices.GetInstance().Info.LogPath, false, LogLevel.ShowAllMessages)
        {
            m_AppServices = AppServices.GetInstance();
            base.m_WaitListenUpdatePeriod = 1000;

        }
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
        //
        /// <summary>
        /// Threadsafe method to create email alert and push onto the hub thread to be processed.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="alertMessage"></param>
        /// <param name="arg"></param>
        public void RequestEmailAlert(AlertLevel level, string alertMessage, object arg)
        {
            AlertManagerRequest newReq = new AlertManagerRequest();
            string msg = string.Format(alertMessage, arg);
            newReq.Level = level;
            newReq.AlertString = msg;
            newReq.Type = AlertManagerRequest.Request.SendEmail;
            HubEventEnqueue(newReq);
        }
        public void RequestEmailAlert(AlertLevel level, string alertMessage, params object[] args)
        {
            AlertManagerRequest newReq = new AlertManagerRequest();
            string msg = string.Format(alertMessage, args);
            newReq.Level = level;
            newReq.AlertString = msg;
            newReq.Type = AlertManagerRequest.Request.SendEmail;
            HubEventEnqueue(newReq);
        }
        /// <summary>
        /// Threadsafe method to create email alert and push onto the hub thread to be processed.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="alertMessage"></param>
        public void RequestEmailAlert(AlertLevel level, string alertMessage)
        {
            AlertManagerRequest newReq = new AlertManagerRequest();
            newReq.Level = level;
            newReq.AlertString = alertMessage;
            newReq.Type = AlertManagerRequest.Request.SendEmail;
            HubEventEnqueue(newReq);
        }
        //
        //
        //
        //
        //
        // *************************************************************
        // ****                     Hub Event Handler               ****
        // *************************************************************
        /// <summary>
        /// Main request handling routine processing all events originating from 
        /// external and internal sources.
        /// Called only by the internal hub thread.
        /// </summary>
        /// <param name="eventArgList">Array of EventArgs to be processed.</param>
        protected override void HubEventHandler(EventArgs[] eventArgList)
        {
            foreach (EventArgs eventArg in eventArgList)	                // process each event
            {
                if (eventArg is AlertManagerRequest)
                {
                    AlertManagerRequest alertRequest = (AlertManagerRequest)eventArg;
                    switch (alertRequest.Type)
                    {
                        case AlertManagerRequest.Request.Stop:
                            if (m_EmailQueue.IsEmpty)   // we have no more emails to send out just shutdown
                                base.Stop();
                            else
                            {   // we have more email to send out, send them and then shutdown
                                UpdatePeriodic();
                                base.Stop();
                            }
                            break;
                        case AlertManagerRequest.Request.SendEmail:
                            if (m_FromEmailAddress != string.Empty && m_FromEmailPassword != string.Empty)
                                m_EmailQueue.Enqueue(alertRequest);
                            else
                                Log.NewEntry(LogLevel.Error, "AlertManager: No send email adress has been set up. Ignoring email alert request.");
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        //
        //
        //
        //
        // *************************************************************
        // ****                      UpdatePeriodic                 ****
        // *************************************************************
        protected override void UpdatePeriodic()
        {
            AlertManagerRequest alertRequest;
            while (m_EmailQueue.TryDequeue(out alertRequest))
            {
                foreach (AlertUser aUser in m_AlertUsers)
                {
                    if (alertRequest.Level >= aUser.UserAlertLevel)
                    {
                        SendEmailAlert(aUser.UserEmail, alertRequest);
                    }
                }
            }
        }
        //
        //
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        // *************************************************************
        // ****                      SendEmailAlert                 ****
        // *************************************************************
        /// <summary>
        /// Method to send out email from the AlertManager thread.
        /// </summary>
        /// <param name="emailAddress"></param>
        /// <param name="alertRequest"></param>
        private void SendEmailAlert(string emailAddress, AlertManagerRequest alertRequest)
        {
            string subject = String.Empty;
            DateTime thisTime = Log.GetTime();
            StringBuilder msg = new StringBuilder();
            msg.AppendFormat("TimeStamp: {0}  {1} \n", thisTime.ToShortDateString(), thisTime.ToString("HH:mm:ss.fff"));
            msg.Append(alertRequest.AlertString);

            //
            // Create email
            //
            System.Net.Mail.MailMessage email = new System.Net.Mail.MailMessage();
            email.From = new System.Net.Mail.MailAddress(m_FromEmailAddress, "VioletAlert");//"Tramp@bgtradingllc.com", "Tramp");
            email.To.Add(emailAddress);
            email.Subject = string.Format("VioletAlert Level - {0}", alertRequest.Level);
            email.Body = msg.ToString();


            //
            // Send message.
            //
            DateTime dt = Log.GetTime();
            bool isSuccessful = true;
            System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient(m_FromEmailSMTPServer, m_FromEmailSMTPPort);
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.Credentials = new System.Net.NetworkCredential(m_FromEmailAddress, m_FromEmailPassword);
            client.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
            try
            {
                client.Send(email);
            }
            catch (Exception ex)
            {
                isSuccessful = false;
                Log.NewEntry(LogLevel.Error, "SendEmailNow: Exception={0}", ex.Message);
            }
            TimeSpan ts = Log.GetTime().Subtract(dt);
            Log.NewEntry(LogLevel.Minor, "SendEmailNow: Success = {0}. ElapsedTime = {1:0.000} ", isSuccessful.ToString(), ts.TotalSeconds);
        }
        #endregion//Private Methods

        #region no Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        #endregion//Event Handlers

        #region IService
        // *********************************************************
        // ****                     IService                    ****
        // *********************************************************
        string IService.ServiceName
        {
            get { return m_HubName; }
        }
        void IService.Connect()
        {
        }
        // *****************************************************
        // ****             Request Stop()                  ****
        // *****************************************************
        public override void RequestStop()
        {
            AlertManagerRequest request = new AlertManagerRequest();
            request.Type = AlertManagerRequest.Request.Stop;
            this.HubEventEnqueue(request);
        }
        //public event EventHandler Stopping; - part of Hub base class.
        //
        //
        // 
        public event EventHandler ServiceStateChanged;
        //
        //
        private void OnServiceStateChanged()
        {
            Log.NewEntry(LogLevel.Major, "StateChanged {0}", m_ServiceState);
            if (this.ServiceStateChanged != null)
            {
                ServiceStateEventArgs e = new ServiceStateEventArgs(this, m_ServiceState, ServiceStates.None);
                this.ServiceStateChanged(this, e);
            }
        }//OnServiceStateChanged()
        //
        // 
        #endregion//IService

        #region IStringifiable
        // *************************************************************
        // ****                     IStringifiable                  ****
        // *************************************************************
        string IStringifiable.GetAttributes()
        {
            return string.Empty;
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            return null;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            bool isTrue;
            int i;
            foreach (KeyValuePair<string, string> keyVal in attributes)
            {
                if (keyVal.Key.Equals("ShowLog") && bool.TryParse(keyVal.Value, out isTrue))
                    Log.IsViewActive = isTrue;
                if (keyVal.Key.ToUpper().Equals("SMTP"))
                    m_FromEmailSMTPServer = keyVal.Value;
                if (keyVal.Key.ToUpper().Equals("EMAIL"))
                    m_FromEmailAddress = keyVal.Value;
                if (keyVal.Key.ToUpper().Equals("PASSWORD"))
                    m_FromEmailPassword = keyVal.Value;
                if (keyVal.Key.ToUpper().Equals("PORT") && int.TryParse(keyVal.Value, out i))
                    m_FromEmailSMTPPort = i;
            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
            if (subElement is AlertUser)
            {
                AlertUser newAlertUser = (AlertUser)subElement;
                if (!m_AlertUsers.Contains(newAlertUser))
                    m_AlertUsers.Add(newAlertUser);
            }
        }
        #endregion//IStringifiable

    }//end class
}
