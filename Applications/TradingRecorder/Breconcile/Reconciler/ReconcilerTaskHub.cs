using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;

namespace Ambre.Breconcile.Reconciler
{
    using Misty.Lib.Application;
    using Misty.Lib.Hubs;
    using Misty.Lib.TaskHubs;
    using Misty.Lib.OrderHubs;
    using Misty.Lib.Products;
    using Misty.Lib.IO.Xml;

    using Renci.SshNet;

    public class ReconcilerTaskHub : TaskHub , IStringifiable
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Services
        private AppInfo m_AppInfo = null;
        private Ftp.SimpleFtpReader m_FtpReader = null;
        
        // RCG:
        //private string m_RcgStatementPath;                                                                    // path to local RCG clearing statement csv files.
        private string[] m_RcgFilePatterns = new string[] { "POS", "ST4", "MNY" };                              // desired file name patterns.

        // ABN:
        //private string m_AbnStatementPath;                                                                    // path to local ABN clearing statements.
        private string[] m_AbnFilePatterns = new string[] { "futpos", "futtran", "futcash", "futbal", };        //, "futtran", "futintraday" };

        // Ambre controls:
        //private string m_DropPath;                                                                            // path to ambre drop files.
        private DateTime m_SettlementDate;
        private double m_SettlementHourOffsetDefault = 16.25;                                                   // and we presume our positions settled at 4:15pm.
        ProductSettlementTable m_ProductSettlementTable = null;

        private string m_AccountTagFilePath = "AccountTags.csv";                                                // internal AMbre account descriptions....
        private List<string> m_UnReconciledAmbreUserNames = null;                                               // place to remove ambre user names as we examine them, left overs are unexamined!


        // Reporting controls:
        private string[] m_EmailRecipients = new string[0];                                                     // these are loaded in as a ReconcilerTaskHub parameter!
        private string m_EmailSender = "DVtrading1@gmail.com";                                                  // "BreTrading@rcgdirect.com"; original email sender!
        private string m_EmailTo = "mpichowsky@dvtglobal.com";
        private int m_EmailsWarningsSent = 0;

        
        // User variables
        private UserInformation m_UserInformation;
        //private string FTPUserNameABN;
        //private string FTPPasswordABN;
        //private string FTPUserName;

        // private ftp path
        private string m_PrivatePath = "\\\\fileserver\\Users\\DV_Ambre\\AmbreUsers\\";
        //private string m_FTPKeyPath;

        // google name and google password used to get user account tag from google docs
        //private string m_googleName = null;
        //private string m_googlePassword = null;
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public ReconcilerTaskHub() : base("Reconciler", AppInfo.GetInstance().LogPath, true)
        {
            m_AppInfo = AppInfo.GetInstance();
            // Create the default data to perform settlement.
            // The user may also change this from its default value.

            DateTime dt = Log.GetTime();
            if (dt.TimeOfDay.CompareTo(new TimeSpan(17, 0, 0)) > 0)
                m_SettlementDate = dt.Date;                                                                                     // default settlement date to use is today, if its after 5pm.
            else
            {
                m_SettlementDate = dt.AddDays(-1).Date;                                                                         // otherwise, during the day use yesterday's date to reconcile.
            }

            while (m_SettlementDate.DayOfWeek == DayOfWeek.Sunday || m_SettlementDate.DayOfWeek == DayOfWeek.Saturday)
                m_SettlementDate = m_SettlementDate.AddDays(-1);

            // Ambre information
            // m_DropPath = string.Format("{0}{1}", m_AppInfo.UserPath, "Drops\\");  // original code
            //
            // Load exchange settlement table
            //
            string settlementTable = string.Format("{0}ProductSettleTimes.txt",m_AppInfo.UserPath);
            if (!ProductSettlementTable.TryCreate(settlementTable, out m_ProductSettlementTable, m_SettlementDate))
            {   // Failed.
                this.Log.NewEntry(LogLevel.Major, "ReconcilerTaskHub: Failed to create table from {0}.", settlementTable);
                this.RequestStop();
            }
            else
                Log.NewEntry(LogLevel.Minor, "Product Settlement Table:\r\n{0}", m_ProductSettlementTable.ToString());
            
            // Rcg info and connectivity
            //m_RcgStatementPath = string.Format("{0}{1}", m_AppInfo.UserPath, "Statements\\");

            m_FtpReader = new Ftp.SimpleFtpReader(this.Log);
            m_FtpReader.m_FilePatterns = new string[this.m_RcgFilePatterns.Length];
            this.m_RcgFilePatterns.CopyTo(m_FtpReader.m_FilePatterns, 0);                                                       // Load all the filebase names we want to read.
            
            // ABN information and connectivity
            //m_AbnStatementPath = string.Format("{0}{1}", m_AppInfo.UserPath, "StatementsABN\\");

        }
        //
        //       
        #endregion//Constructors


        #region Send Email
        // *****************************************************************
        // ****                         Send Email                      ****
        // *****************************************************************
        //
        //
        public void EmailReport(object sender, TaskEventArg eventArg)
        {
            TaskEventArg previous = null;                           // task that ran just before email report request - we are emailing a report about this task.
            TaskEventArg parent;                                    // to work, this task and the previous MUST have a common parent.
            if (eventArg.TryGetParent(out parent))
            {
                List<TaskEventArg> children = parent.GetChildren();
                for (int i = 0; i < children.Count; ++i)
                {
                    if (children[i] == eventArg && i > 0)
                        previous = children[i - 1];
                }
            }
            bool isSuccess = true;                                  // default is success, even when previous task does NOT want to produce and email (so it gives no OutData).
            if (previous != null && previous.Status == TaskStatus.Success && previous.OutData != null && previous.OutData.Count >= 2)
            {   // If we found the previous task, and it has a message, send it.

                StringBuilder subjectMessage = (StringBuilder)previous.OutData[0];          // This is required
                StringBuilder bodyMessage = (StringBuilder)previous.OutData[1];             // this is required                
                List<StringBuilder> attachments = new List<StringBuilder>();                // Now extract optional attachments.
                for (int i = 2; i < previous.OutData.Count; ++i)
                {
                    if (previous.OutData[i] is StringBuilder)
                        attachments.Add((StringBuilder)previous.OutData[i]);
                }

                // Output full report:
                if (subjectMessage.Length == 0)
                    subjectMessage.Append("No Warnings");

                // Save the attachments
                List<System.Net.Mail.Attachment> attList = new List<System.Net.Mail.Attachment>();
                string EmailReportPath = string.Format("{0}{1}{2}", m_PrivatePath, m_UserInformation.Name, "\\Reports\\");
                for (int i = 0; i < attachments.Count; ++i)
                {
                    if (attachments[i].Length < 1)
                        continue;
                    // TODO: Write this to the User directory - Reports/
                    string fileName = string.Format("EmailReport_{0}_{1}_{2}.txt", (i + 1).ToString(), DateTime.Now.ToString("yyyy_MMM_dd"), DateTime.Now.ToString("HHmm"));
                    string filepath = string.Format("{0}{1}", EmailReportPath, fileName);
                    using (System.IO.StreamWriter stream = new System.IO.StreamWriter(filepath, false))
                    {
                        stream.WriteLine(attachments[i].ToString());
                        stream.WriteLine(" ");
                        stream.WriteLine("End");
                        stream.Close();
                    }
                    attList.Add(new System.Net.Mail.Attachment(filepath));
                }//next attachment i

                // Send email
                Log.BeginEntry(LogLevel.Major, "EmailReport: Sending email to {0}.", m_EmailTo);
                if (m_EmailRecipients != null && m_EmailRecipients.Length > 0)
                {
                    foreach (string s in m_EmailRecipients)
                        Log.AppendEntry(" {0}", s);
                }
                Log.EndEntry();
                string subjectHeader = string.Format("Breconciler: {0}", subjectMessage.ToString());
                isSuccess = TrySendEmail(bodyMessage, subjectHeader, m_EmailRecipients, attList);    // send message if there's a non-empty message.
            }
            else
            {   // There is no previous task to report on.
                Log.BeginEntry(LogLevel.Major, "EmailReport: Sending email with no report.");
                if (m_EmailRecipients != null && m_EmailRecipients.Length > 0)
                {
                    foreach (string s in m_EmailRecipients)
                        Log.AppendEntry(" {0}", s);
                }
                Log.EndEntry();
                string subjectHeader = string.Format("Breconciler: {0}", "No report");
                StringBuilder bodyMessage = new StringBuilder();
                bodyMessage.Append("No reports generated.");
                isSuccess = TrySendEmail(bodyMessage, subjectHeader, m_EmailRecipients, null);    // send message if there's a non-empty message.

            }

            if (isSuccess)
                eventArg.Status = TaskStatus.Success;
            else
                eventArg.Status = TaskStatus.Failed;
        }//EmailReport()
        //
        //
        //
        //
        private bool TrySendEmail(StringBuilder messageBody, string subject, string[] recipients, List<System.Net.Mail.Attachment> attachments = null)
        {
            //
            // Create email
            //
            System.Net.Mail.MailMessage email = new System.Net.Mail.MailMessage();
            email.From = new System.Net.Mail.MailAddress(m_EmailSender, "dv_Ambre");
            recipients = m_UserInformation.EmailTo;             // here is a bad idea to over write email address. Will change later
            //m_EmailTo = m_EmailTo + "," + m_UserInformation.EmailTo;
            email.To.Add(m_EmailTo);
            //email.To.Add(m_UserInformation.EmailTo);
            foreach (string recipient in recipients)
                email.CC.Add(recipient);
            email.Subject = subject;

            email.IsBodyHtml = true;           
            email.Body = string.Format("<!DOCTYPE html><html><body>{0}</body></html>",messageBody);

            if (attachments != null)
                foreach (System.Net.Mail.Attachment att in attachments)
                    email.Attachments.Add(att);           

            // Save copy of email.
            // TODO: write the email body message string to a file in the user directory Reports/"Email_yyy_MM_etc...
            string EmailReportPath = string.Format("{0}{1}{2}", m_PrivatePath, m_UserInformation.Name, "\\Reports\\");
            if (!System.IO.Directory.Exists(EmailReportPath))
                System.IO.Directory.CreateDirectory(EmailReportPath);
            string messageBodyFileName = string.Format("EmailBody_{0}_{1}.html", DateTime.Now.ToString("yyyy_MMM_dd"), DateTime.Now.ToString("HHmmss"));
            string messageBodyFilePath = string.Format("{0}{1}", EmailReportPath, messageBodyFileName);
            using (StreamWriter writer = new StreamWriter(messageBodyFilePath, false))
            {
                writer.WriteLine(email.Body);
                writer.Close();
            }

            //
            // Send message.
            //
            bool isSuccessful = true;
            System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient("smtp.gmail.com", 587);        //Port ID 25 originally. Smtp client is webmail.rcgdirect.com.
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.Credentials = new NetworkCredential("DVtrading1", "Trading1");

            //System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient("webmail.rcgdirect.com", 25);        //Port ID 25 originally. Smtp client is webmail.rcgdirect.com.
            //client.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;
            try
            {
                client.Send(email);
                Log.NewEntry(LogLevel.Minor, "TrySendEmail: Successfully sent email.");
            }
            catch (Exception ex)
            {
                isSuccessful = false;
                Log.NewEntry(LogLevel.Error, "TrySendEmail: Exception={0}", ex.Message);
            }

            // Exit
            return isSuccessful;        
        }//TrySendEmail()
        //
        //
        #endregion//Send Email


        #region Finalize Reconciliation
        // *****************************************************************
        // ****                 Finalize Reconciliation                 ****
        // *****************************************************************
        /// <summary>
        /// Purpose of this task is to do additional tests AFTER all reconciliation tasks
        /// are completed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArg"></param>
        public void FinalizeReconciliation(object sender, TaskEventArg eventArg)
        {
            //
            // Initialize outgoing data.
            //
            StringBuilder subject = new StringBuilder();
            subject.AppendFormat("Ambre Final Report");
            StringBuilder body = new StringBuilder();
            body.AppendFormat("\r\n<p>");
            body.AppendFormat("\r\n<br>-------------------------- Reconcile Finalization --------------------------");
            body.AppendFormat("\r\n<br><b>Final reconciliation tests:</b>");
            body.AppendFormat("\r\n<br>Reconcile User Name: {0}</b>", m_UserInformation.Name);
            body.AppendFormat("\r\n<br>Reconciliation Date: {0}", m_SettlementDate.ToLongDateString());
            body.AppendFormat("\r\n<br>Runtime Date: {0}", Log.GetTime().ToLongDateString());
            body.AppendFormat("\r\n<br>Runtime Time: {0}", Log.GetTime().ToLongTimeString());

            bool somethingToReport = false;                    // some part of the finalization routine must change this flag to have email sent.

            // Test #1.
            if (m_UnReconciledAmbreUserNames != null && m_UnReconciledAmbreUserNames.Count > 0)
            {
                // Load exchange settlement time offsets.
                Dictionary<List<string>, double> exchangeSettlementOffset = GetExchangeSettlementDates();

                // Load ambre accounts into table
                Dictionary<string, PosTable> posTableList = new Dictionary<string, PosTable>(); ;
                int ambreUserNamePtr = 0;
                while (ambreUserNamePtr < m_UnReconciledAmbreUserNames.Count)
                {
                    string ambreUserName = m_UnReconciledAmbreUserNames[ambreUserNamePtr];

                    //
                    // Load Ambre Settles
                    //
                    Dictionary<InstrumentName, int> ambreSettle = new Dictionary<InstrumentName, int>();
                    Dictionary<InstrumentName, List<Fill>> ambreFills = new Dictionary<InstrumentName, List<Fill>>();
                    List<string> exchangeList = new List<string>();                     // Load ambre positions for default exchanges.
                    foreach (List<string> anExchangeList in exchangeSettlementOffset.Keys)
                        exchangeList.AddRange(anExchangeList);                          // Add all exchanges with non-default settlements.
                    DateTime settlementTime = m_SettlementDate.AddHours(m_SettlementHourOffsetDefault);
                    DateTime startTime = settlementTime.AddHours(-1.0);
                    TryLoadAmbreStatement(ambreUserName, startTime, settlementTime, exchangeList, ExchangeOptions.ExcludeExchangesInList, ref ambreSettle, ref ambreFills);
                    foreach (List<string> anExchangeList in exchangeSettlementOffset.Keys)// Load special-case exchanges           
                    {
                        exchangeList.Clear();
                        exchangeList.AddRange(anExchangeList);                          // Now, lets only try to reconcile these exchanges
                        double offset = exchangeSettlementOffset[anExchangeList];       // This is the hour offset these exchanges use.
                        settlementTime = m_SettlementDate.AddHours(offset);
                        startTime = settlementTime.AddHours(-1.0);
                        TryLoadAmbreStatement(ambreUserName, startTime, settlementTime, exchangeList, ExchangeOptions.IncludeOnlyExchangesInList, ref ambreSettle, ref ambreFills);
                    }//next exchangeList

                    // Create a position table, if its not totally empty.
                    PosTable posTable = null;
                    foreach (InstrumentName ambreName in ambreSettle.Keys)
                    {
                        if (ambreSettle[ambreName] != 0)
                        {
                            if (posTable == null)
                                posTable = new PosTable(new string[] { ambreUserName });
                            Pos[] newPos = posTable.CreateRow();
                            newPos[0] = new Pos(ambreName, ambreSettle[ambreName]);
                        }
                    }
                    if (posTable == null)
                    {   // No pos table created means: no outright positions observed
                        m_UnReconciledAmbreUserNames.RemoveAt(ambreUserNamePtr);    // remove name from list as being flat anyway.
                    }
                    else
                    {   // Store the created position table for output display.
                        posTableList[ambreUserName] = posTable;                     // 
                        ambreUserNamePtr++;                                         // go to next name on the list.
                    }
                }//next ambreUserName

                //
                // Write email report
                //
                if (m_UnReconciledAmbreUserNames.Count > 0)
                {
                    body.AppendFormat("\r\n<P><b>Unused Ambre files</b>");
                    body.AppendFormat("\r\n<br>No clearing entries were found for these Ambre drop files:");

                    // Load account info/spoofing and tags about remaining ambreUserNames.
                    Dictionary<string, List<BreUserTags>> allUserTags;
                    BreUserTags.TryCreate(m_AccountTagFilePath, out allUserTags, m_UserInformation.googleName, m_UserInformation.googlePassword, Log);


                    string[] clearingKeys = new string[allUserTags.Count];
                    allUserTags.Keys.CopyTo(clearingKeys, 0);
                    foreach (string ambreUserName in m_UnReconciledAmbreUserNames)
                    {
                        // See if this ambreUserName has a tag.  Display it if possible.
                        BreUserTags ambreTag = null;
                        int clearingFirmID = 0;
                        while (ambreTag == null && clearingFirmID < allUserTags.Count)
                        {
                            string clearingFirm = clearingKeys[clearingFirmID];
                            foreach (BreUserTags tag in allUserTags[clearingFirm])
                            {
                                string ambreUserPart;
                                int n = tag.Number.Length;          // this table only contains the trailing few numbers of account number.
                                if (ambreUserName.Length - n >= 0)
                                    ambreUserPart = ambreUserName.Substring(ambreUserName.Length - n, n);
                                else
                                    ambreUserPart = ambreUserName;
                                if (tag.Number.Contains(ambreUserPart))
                                {
                                    ambreTag = tag;
                                    break;
                                }
                            }
                            clearingFirmID++;
                        }
                        // Write report
                        if (ambreTag != null && ambreTag.Main.Contains("NA"))
                        {   // Skip these since they are marked as belonging to "NA"
                            Log.NewEntry(LogLevel.Major, "FinalizeReconciliation: No clearing for {0} - {1} {2} ({3})", ambreUserName, ambreTag.Main, ambreTag.Tag, ambreTag.SpoofStr);
                            body.AppendFormat("\r\n<BR>  No clearing entry for {0} - {1} {2} ({3}).", ambreUserName, ambreTag.Main, ambreTag.Tag, ambreTag.SpoofStr);
                        }
                        else if (ambreTag == null)
                        {
                            Log.NewEntry(LogLevel.Major, "FinalizeReconciliation:  Not Tag found for {0}", ambreUserName);
                            body.AppendFormat("\r\n<BR>  No clearing entry for {0}. No Tag found.", ambreUserName);
                            somethingToReport = true;
                        }
                        else
                        {   // Write message
                            Log.NewEntry(LogLevel.Major, "FinalizeReconciliation:  Not found in clearing {0} - {1} {2} ({3})", ambreUserName, ambreTag.Main, ambreTag.Tag, ambreTag.SpoofStr);
                            somethingToReport = true;
                            // Write position table.                            
                            PosTable posTable;
                            if (posTableList.TryGetValue(ambreUserName, out posTable))
                            {
                                body.Append("\r\n<table width=\"50%\">");
                                // Header #1
                                body.Append("\r\n<tr>");
                                body.AppendFormat("\r\n<td align=\"right\" colspan=\"2\">{0}</td>", posTable.ToStringHeader());
                                body.Append("\r\n<tr>");
                                // Header #2
                                body.Append("\r\n<tr>");
                                body.AppendFormat("\r\n<td align=\"right\" colspan=\"2\"><b>{0}</b>: {1}  - {2}</td>", ambreTag.Main, ambreTag.Tag, ambreTag.SpoofStr);
                                body.Append("\r\n<tr>");
                                // Table body
                                for (int row = 0; row < posTable.Rows.Count; ++row)
                                {
                                    body.Append("\r\n<tr>");
                                    body.AppendFormat("\r\n<td>{0}</td><td align=\"right\">{1}</td>", posTable.Rows[row][0].Instr, posTable.Rows[row][0].Qty);
                                    body.Append("\r\n</tr>");
                                }
                                body.Append("\r\n</table>");
                            }

                        }
                    }//next ambreUserName;
                    body.AppendFormat("\r\n<br> ");
                }
            }// if for test#1

            // Exit
            if (somethingToReport)
            {
                Log.NewEntry(LogLevel.Major, "FinalizeReconciliation:  Something to report.");
                if (eventArg.OutData == null)
                {
                    eventArg.OutData = new List<object>();
                    eventArg.OutData.Add(subject);
                    eventArg.OutData.Add(body);
                }
                else
                {
                    if (eventArg.OutData.Count > 1 && eventArg.OutData[1] is StringBuilder) // Append to body of any email.
                        ((StringBuilder)eventArg.OutData[1]).AppendFormat("\r\n<p>{0}", body.ToString());
                }
            }
            else
            {
                if (eventArg.OutData.Count > 1 && eventArg.OutData[1] is StringBuilder) // Append to body of any email.
                    ((StringBuilder)eventArg.OutData[1]).AppendFormat("\r\n<p>Final reconciler has nothing extra to report.");
                Log.NewEntry(LogLevel.Major, "FinalizeReconciliation:  Nothing to report.");
            }

            eventArg.Status = TaskStatus.Success;
        }// FinalizeReconciliation.
        //
        //
        //
        //
        #endregion//Finalize Reconciliation


        #region Reconcile Statement - Entry
        // *****************************************************************
        // ****                Reconcile Statement                      ****
        // *****************************************************************
        /// <summary>
        /// This version tries to simplify the code, and increase the throughput
        /// by considering only one account at a time.
        /// Procedure:
        /// 1.) Load all statements into embedded dictionaries
        ///     [acct# , [InstrName, Qty]]
        ///     
        /// </summary>
        public void ReconcileStatement(object sender, TaskEventArg eventArg)
        {
            // Create the output reports
            eventArg.OutData = new List<object>();
            eventArg.OutData.Add(new StringBuilder());  // Required report status: title of email message, leave blank interpreted as all is well.
            eventArg.OutData.Add(new StringBuilder());  // Required body of email. (Additional items added to OutData, they would be attachments for email.

            // Carry out reconciliation
            ReconcileStatementJuly(sender, eventArg, "RCG");        // TODO: What happens when a user doesn't have an RCG account?
            ReconcileStatementJuly(sender, eventArg, "ABN");
            FinalizeReconciliation(sender, eventArg);
            eventArg.Status = TaskStatus.Success;
        }
        //public void ReconcileStatementABN(object sender, TaskEventArg eventArg)
        //{
        //    //ReconcileStatement(sender, eventArg, "ABN");
        //    ReconcileStatementJuly(sender, eventArg, "ABN");
        //}
        #endregion // Reconcile statement


        #region Reconcile Statement - 9 July 2013
        // *****************************************************************
        // ****                Reconcile Statement                      ****
        // *****************************************************************
        /// <summary>
        /// This version tries to simplify the code, and increase the throughput
        /// by considering only one account at a time.  
        /// ** Reconciliation process **
        /// This implements reconciliation by means of a table for each account;
        /// wherein there is a row for each instrument (in the account) and 
        /// each cell entry is the total open quantity for that instrument.
        /// There's a column for the clearing firm, then another column for each Ambre drop file.
        /// ** Procedure **
        /// 1.) Load all statements into an embedded dictionary: clearingStatement[acct# , [InstrName, Qty]]
        /// 2.) Loads ambre user names which have dropped files recently, loads BRE tag files.
        ///     
        public void ReconcileStatementJuly(object sender, TaskEventArg eventArg, string clearingFirmName)
        {
            //
            // Reporting storage - create reports
            //
            bool fireWarning = false;
            StringBuilder fullAttachedReport = new StringBuilder();
            StringBuilder bodyReport = new StringBuilder();          // new table based version
            StringBuilder summaryReport = new StringBuilder();       // place this after the warnings section!            
            fullAttachedReport.AppendFormat("\r\n<br>-------------------------- Reconcile {0} --------------------------", clearingFirmName);
            fullAttachedReport.AppendFormat("\r\nReconcile User Name: {0}", m_UserInformation.Name);
            fullAttachedReport.AppendFormat("\r\nRuntime Date: {0}", Log.GetTime().ToLongDateString());
            fullAttachedReport.AppendFormat("\r\nRuntime Time: {0}", Log.GetTime().ToLongTimeString());
            fullAttachedReport.AppendFormat("\r\nReconciliation Date: {0}", m_SettlementDate.ToLongDateString());
            fullAttachedReport.AppendFormat("\r\nProduct Settlement Table:\r\n{0}", m_ProductSettlementTable.ToString());
            bodyReport.AppendFormat("\r\n<br> ");
            bodyReport.AppendFormat("\r\n<br>-------------------------- Reconcile {0} --------------------------", clearingFirmName);
            bodyReport.AppendFormat("\r\n<br>Reconcile User Name: {0}</b>", m_UserInformation.Name);
            bodyReport.AppendFormat("\r\n<br><b>Reconcile: {0}</b>", clearingFirmName);
            bodyReport.AppendFormat("\r\n<br>Reconciliation Date: {0}", m_SettlementDate.ToLongDateString());
            bodyReport.AppendFormat("\r\n<br>Runtime Date: {0}", Log.GetTime().ToLongDateString());
            bodyReport.AppendFormat("\r\n<br>Runtime Time: {0}", Log.GetTime().ToLongTimeString());
            summaryReport.AppendFormat("\r\n<p><b>Summary:</b>");

            switch (m_UserInformation.ReconcileStatementType)
            {
                case ReconcileStatementType.None:
                    bodyReport.AppendFormat("\r\n<br>Reconcile User {0} does not input FTP user information for either RCG or ABN statement</b>", m_UserInformation.Name);
                    bodyReport.AppendFormat("\r\n<br>Please input your FTP user information at least for one of RCG and ABN clearing type in your configuration file</b>");
                    return;
                case ReconcileStatementType.RCG:
                    bodyReport.AppendFormat("\r\n<br>Reconcile User {0} only inputs FTP user information for RCG statement</b>", m_UserInformation.Name);
                    if (clearingFirmName.Equals("ABN"))
                        return;
                    break;
                case ReconcileStatementType.ABN:
                    bodyReport.AppendFormat("\r\n<br>Reconcile User {0} only inputs FTP user information for ABN statement</b>", m_UserInformation.Name);
                    if (clearingFirmName.Equals("RCG"))
                        return;
                    break;
                case ReconcileStatementType.Both:
                    bodyReport.AppendFormat("\r\n<br>Reconcile User {0} inputs FTP user information for {1} statement</b>", m_UserInformation.Name, clearingFirmName);
                    break;
            }

            string abnStatementPath = string.Format("{0}{1}{2}", m_PrivatePath, m_UserInformation.Name, "\\StatementsABN\\");
            string rcgStatementPath = string.Format("{0}{1}{2}", m_PrivatePath, m_UserInformation.Name, "\\Statements\\");

            switch (m_UserInformation.ReconfileLoadAccountTagsMethod)
            {
                case LoadAccountTagsMethod.GmailDrive:
                    bodyReport.AppendFormat("\r\n<br>Reconcile User {0} loads account tags from Gmail Drive</b>", m_UserInformation.Name);
                    //m_AccountTagFilePath = string.Format("{0}{1}{2}", m_PrivatePath, m_UserInformation.Name, "\\AccountTags.csv");
                    m_AccountTagFilePath = string.Empty;
                    break;
                case LoadAccountTagsMethod.LocalUserSpecifiedPath:
                    bodyReport.AppendFormat("\r\n<br>Reconcile User {0} loads account tags from user-specified full path</b>", m_UserInformation.Name);
                    m_AccountTagFilePath = m_UserInformation.FilePathToAccountTags;
                    break;
                case LoadAccountTagsMethod.LocalDefautPath:
                    bodyReport.AppendFormat("\r\n<br>Reconcile User {0} loads account tags from default path</b>", m_UserInformation.Name);
                    m_AccountTagFilePath = string.Format("{0}{1}{2}", m_PrivatePath, m_UserInformation.Name, "\\AccountTags.csv");
                    break;
                case LoadAccountTagsMethod.None:
                    bodyReport.AppendFormat("\r\n<br>Reconcile User {0} does not use account tag method and an empty account tag file will be created</b>", m_UserInformation.Name);
                    m_AccountTagFilePath = string.Format("{0}{1}{2}", m_PrivatePath, m_UserInformation.Name, "\\AccountTags.csv");

                    // If the user does not either provide account tags file path or google name/password, create a new blank one.
                    if (!System.IO.File.Exists(m_AccountTagFilePath))
                    {
                        FileStream fileStream = new FileStream(m_AccountTagFilePath, System.IO.FileMode.Create, System.IO.FileAccess.Write);
                        using (StreamWriter streamWriter = new StreamWriter(fileStream, System.Text.Encoding.Default))
                        {
                            streamWriter.WriteLine("RCG.number,ABN.number,main,tag,RCG.spoof,ABN.spoof");
                            streamWriter.Close();
                        }
                    }
                    break;
            }

            // If something is wrong, label the task status as failure.
            if (m_AccountTagFilePath != null)
            {
                if (m_UserInformation.ReconfileLoadAccountTagsMethod != LoadAccountTagsMethod.GmailDrive && !System.IO.File.Exists(m_AccountTagFilePath))
                {
                    // In loading account tags method other than google drive, the account tags file should not be empty.
                    eventArg.Status = TaskStatus.Failed;
                    Log.NewEntry(LogLevel.Warning, "The account tag path:{0} does not exist", m_AccountTagFilePath);
                    return;
                }
            }
            else
            {
                // In no circumstances does the account file path being null.
                eventArg.Status = TaskStatus.Failed;
                Log.NewEntry(LogLevel.Warning, "The account tags file member is null, error!");
                return;
            }

            string newDropPath = string.Format("{0}{1}{2}", m_PrivatePath, m_UserInformation.Name, "\\Drops\\");
            string originalDropPath = "\\\\fileserver\\Users\\dv_bre\\Ambre\\Drops\\";
            string newTodayDrop = string.Format("{0}{1}\\", newDropPath, m_SettlementDate.ToString("yyyyMMdd"));
            string originalTodayDrop = string.Format("{0}{1}\\", originalDropPath, m_SettlementDate.ToString("yyyyMMdd"));

            // Get the larger of the files in old/new paths to new path.
            if (!System.IO.Directory.Exists(newTodayDrop))
            {
                if (!System.IO.Directory.Exists(newDropPath))
                {
                    System.IO.Directory.CreateDirectory(newDropPath);
                }
                System.IO.Directory.CreateDirectory(newTodayDrop);
            }

            // This is to combine the time that old Ambre and new Ambre work. In the future, this block may be deleted because the drop files are to be loaded to new drop path.
            if (System.IO.Directory.Exists(originalTodayDrop))
            {
                List<string> fileNewPathList = new List<string>();
                fileNewPathList.AddRange(System.IO.Directory.GetFiles(newTodayDrop));
                List<string> fileOldPathList = new List<string>();
                fileOldPathList.AddRange(System.IO.Directory.GetFiles(originalTodayDrop));

                // Get the files that are needed to be copied.
                List<string> newFileNames = new List<string>();
                List<string> oldFileNames = new List<string>();
                foreach (string newFile in fileNewPathList)
                    newFileNames.Add(Path.GetFileName(newFile));
                foreach (string oldFile in fileOldPathList)
                    oldFileNames.Add(Path.GetFileName(oldFile));

                List<string> neededFiles = new List<string>(oldFileNames);
                foreach (string oldFile in oldFileNames)
                {
                    if (newFileNames.Contains(oldFile))
                    {
                        neededFiles.Remove(oldFile);
                    }
                }

                // Copy the needed files.
                if (neededFiles.Count > 0)
                {
                    foreach (string neededFileName in neededFiles)
                    {
                        string copyToPath = string.Format("{0}{1}", newTodayDrop, neededFileName);
                        string copyFromPath = string.Format("{0}{1}", originalTodayDrop, neededFileName);
                        System.IO.File.Copy(copyFromPath, copyToPath, true);
                    }
                }
            }

            // Add to check whether the directory of the new drop is null or empty. Fire warnings if it has problem.
            if (!System.IO.Directory.Exists(newTodayDrop) && System.IO.Directory.GetFiles(newTodayDrop).Length == 0)
            {
                Log.NewEntry(LogLevel.Warning, "The new drop path contains no files");
                bodyReport.AppendFormat("\r\n<br>There is no drop files contained in this path, error!</b>");
                fireWarning = true;
            }

            Log.NewEntry(LogLevel.Minor, "ReconcileStatementJuly: Will reconcile to date {0}", m_SettlementDate.ToLongDateString());
            //
            // Load the clearing firm statement.
            //
            DateTime dtProcessStartTime = Log.GetTime();
            IClearingStatementReader clearingStatement = null;
            List<InstrumentName> exceptionList = null;

            try
            {
                if (clearingFirmName.Contains("RCG"))
                {
                    RCG.StatementReader2 rcgStatement;
                    RCG.StatementReader2.TryReadStatement(m_SettlementDate, rcgStatementPath, out rcgStatement);
                    clearingStatement = rcgStatement;
                    string exceptFilePath = string.Format("{0}{1}{2}", m_PrivatePath, m_UserInformation.Name, "\\ExceptionTable.txt");
                    ExceptionTable exceptionListTableRCG = new ExceptionTable(exceptFilePath);
                    exceptionList = exceptionListTableRCG.m_ExceptionList;
                }
                else if (clearingFirmName.Contains("ABN"))
                {
                    ABN.StatementReader.TryReadStatement(m_SettlementDate, abnStatementPath, out clearingStatement);
                    string exceptFilePathABN = string.Format("{0}{1}{2}", m_PrivatePath, this.m_UserInformation.Name, "\\ExceptionTableABN.txt");
                    ExceptionTable exceptionListTableABN = new ExceptionTable(exceptFilePathABN);
                    exceptionList = exceptionListTableABN.m_ExceptionList;
                }
            }
            catch (Exception e)
            {
                bodyReport.AppendFormat("\r\n<br><b>Exception reading statement</b>");
                bodyReport.AppendFormat("\r\n<br>Exception: {0}", e.Message);
                bodyReport.AppendFormat("\r\n<br>Stopping. ");
                ((StringBuilder)eventArg.OutData[1]).Append(bodyReport.ToString());     // This is the email-body, add our report text to this object.                
                return;
            }

            //load the exception firm statement

            if (clearingStatement == null)
            {
                eventArg.Status = TaskStatus.Failed;
                Log.NewEntry(LogLevel.Major, "ReconcileStatement: Failed to load statement for clearing firm {0} for {1}.", clearingFirmName, m_SettlementDate.ToLongDateString());
                return;
            }
            else
            {
                string clearingPosFileName = clearingStatement.PositionFilePath.Substring(clearingStatement.PositionFilePath.LastIndexOf('\\') + 1);
                fullAttachedReport.AppendFormat("\r\nStatement file: {0}", clearingPosFileName);
                bodyReport.AppendFormat("\r\n<br>Statement file: {0}", clearingPosFileName);
            }

            //
            // Load Ambre user names and tags
            //
            List<string> allAmbreUserNames;                                     // store Ambre user names here.
            if (!TryGetAllAmbreUserNames(newDropPath, out allAmbreUserNames))    // search all drop directories, looking for all ambre users.
            {
                eventArg.Status = TaskStatus.Failed;
                Log.NewEntry(LogLevel.Minor, "ReconcileStatement: Failed to find Ambre users.");
                return;
            }
            if (m_UnReconciledAmbreUserNames == null)                           // Remove names from list as they are reconciled.
            {   // Load new list only on the first time thru. This function may be called for multiple clearing accounts.
                m_UnReconciledAmbreUserNames = new List<string>();
                m_UnReconciledAmbreUserNames.AddRange(allAmbreUserNames);
                m_UnReconciledAmbreUserNames.Sort();
            }

            // Load account info/spoofing and tags for AmbreUsers
            Dictionary<string, List<BreUserTags>> allUserTags;
            BreUserTags.TryCreate(m_AccountTagFilePath, out allUserTags, m_UserInformation.googleName, m_UserInformation.googlePassword, Log);


            // Storage for troubled reconciliation accounts.
            List<PosTable> unreconciledTables = new List<PosTable>();
            List<BreUserTags> unreconciledAmbreTags = new List<BreUserTags>();
            Dictionary<PosTable, StringBuilder> unreconciledFills = new Dictionary<PosTable, StringBuilder>();

            
            //
            // Loop thru each clearing account, reconciling it.
            //
            foreach (string clearingAcctName in clearingStatement.Position.Keys)
            {   
                //
                // Load AmbreUsers associated with this clearingAcctName
                //
                BreUserTags ambreUserTag = null;                                        // the Bre Account Tag for this user.
                List<string> ambreUserNames;                                            // ambre users associated with this clearning account.
                BreUserTags.GetUserNamesForThisClearing(clearingAcctName, allUserTags[clearingFirmName], allAmbreUserNames, out ambreUserTag, out ambreUserNames);

                if (ambreUserNames == null || ambreUserNames.Count < 1)                 // Note that we have to skip reconciling this account!
                {
                    if (string.IsNullOrWhiteSpace(ambreUserTag.SpoofStr))
                        fullAttachedReport.AppendFormat("\r\n{0}: Skipped ({1}) \r\n", clearingAcctName, ambreUserTag);
                    else
                        fullAttachedReport.AppendFormat("\r\n{0}: Skipped ({1} Spoof: {2}) \r\n", clearingAcctName, ambreUserTag, ambreUserTag.SpoofStr);
                    summaryReport.AppendFormat("\r\n<br>{0}: {1} has NO AMBRE drop. ({2})", clearingAcctName, clearingFirmName, ambreUserTag);
                    fireWarning = true;                                                 // Bosses want to fire WARNING email for missing Ambre drops.
                    continue;                                                           // skip accounts for which we have no Amber users
                }
                fullAttachedReport.AppendFormat("\r\n{0}: ({1} Spoof: {2}) \r\n", clearingAcctName, ambreUserTag, ambreUserTag.SpoofStr);

                //// Debug test!!!
                //if (clearingAcctName != "W1642")
                //    continue;
                //if (clearingAcctName.Contains("4096"))
                //   Console.Write(clearingAcctName);
                //// Debug test!!!

                //
                // Create the position Table
                //
                List<string> temp = new List<string>();
                temp.Add(clearingAcctName);
                foreach (string ambreUserName in ambreUserNames)
                    temp.Add(ambreUserName);
                PosTable posTable = new PosTable(temp);
                foreach (InstrumentName instrumentName in clearingStatement.Position[clearingAcctName].Keys)
                {   // Load clearing positions.
                    int accummulatedQty = 0;
                    foreach (Fill fill in clearingStatement.Position[clearingAcctName][instrumentName])
                        accummulatedQty += fill.Qty;

                    // Exclude the instruments specified from reconcilation report.
                    if (TryFindException(instrumentName, exceptionList))
                    {
                        fullAttachedReport.AppendFormat("\r\nReconciliation: exception instrument ignored : {0} , FillQty : {1}  \r\n{0}", instrumentName, accummulatedQty);
                        continue;
                    }

                    if (accummulatedQty != 0)
                    {
                        Pos[] aRow = posTable.CreateRow();
                        aRow[0] = new Pos(instrumentName, accummulatedQty);
                    }
                }//next instrumentName

                foreach (string ambreUserName in ambreUserNames)                // Load ambre accounts into table
                {
                    if (m_UnReconciledAmbreUserNames.Contains(ambreUserName))
                        m_UnReconciledAmbreUserNames.Remove(ambreUserName);     // remove these ambreUsers we will try to reconcile.
                    // Load this AmbreUserName drops.
                    Dictionary<InstrumentName, int> ambreSettle = new Dictionary<InstrumentName, int>();
                    Dictionary<InstrumentName, List<Fill>> ambreFills = new Dictionary<InstrumentName, List<Fill>>();
                    List<int> offsets = m_ProductSettlementTable.GetSettlementOffsets(); // Get unique settlement times.
                    foreach (int minuteOffset in offsets)                       // Load each group of settlements.
                        TryLoadAmbreStatement(ambreUserName, minuteOffset, 1.0, ref ambreSettle, ref ambreFills);

                    // Load into the table now.
                    int col = posTable.FindColumn(ambreUserName);
                    foreach (InstrumentName ambreName in ambreSettle.Keys)
                    {
                        if (ambreSettle[ambreName] == 0)                        // Skip 
                            continue;
                        Pos[] newRow = posTable.CreateRow();
                        newRow[col] = new Pos(ambreName, ambreSettle[ambreName]);// Store position for this instrument
                    }
                }//next ambreUserName

                //if (clearingAcctName.Equals("03256"))
                //    clearingFirmName = clearingFirmName + string.Empty;

                //
                // Pass #1 - moving entries that we recognize.
                //
                //MatchExceptions(posTable, exceptionList);
                MatchRows(posTable, clearingStatement.InstrumentNameMap);

                //
                // Pass #2 - Search for new or unknown products and re-reconcile.
                //      An unknown product is one that was left as unmatched (alone in a row).
                List<List<Pos>> unknownPos = null;
                for (int row = 0; row < posTable.Rows.Count; ++row)             // Check for single entries in rows; these are unmatched.
                    if (posTable.CountEntries(row) <= 1)
                    {
                        if (unknownPos == null)                                 // at first trouble, create  a list to hold stuff now, since we will need it.
                        {
                            unknownPos = new List<List<Pos>>();
                            for (int i = 0; i < posTable.ColNames.Count; i++)         // create entries for each account/column
                                unknownPos.Add(new List<Pos>());
                        }
                        for (int col = 0; col < posTable.ColNames.Count; ++col)
                        {
                            if (posTable.Rows[row][col] != null)
                                unknownPos[col].Add(posTable.Rows[row][col]); // since there is only one entry in this row, this must be it.
                        }
                    }
                if (unknownPos != null && unknownPos.Count > 0)
                {   // Some instruments are not matched!  Try to discover new products.
                    fullAttachedReport.AppendFormat(fmt3_1, "Encountered unmatched positions:");
                    for (int col = 0; col < unknownPos.Count; ++col)    //foreach (List<Pos> aPosList in unknownPos)
                        foreach (Pos anUnknownPos in unknownPos[col])
                            fullAttachedReport.AppendFormat(fmt3_4, string.Empty, anUnknownPos.Instr, string.Empty, posTable.ColNames[col]);

                    int newCount = -clearingStatement.InstrumentNameMap.Count;
                    if (TryDiscoverNewProducts(unknownPos, clearingStatement.InstrumentNameMap))
                    {
                        newCount += clearingStatement.InstrumentNameMap.Count;
                        List<InstrumentName> newEntry1 = null;
                        List<InstrumentName> newEntry2 = null;
                        if (clearingStatement.InstrumentNameMap.TryGetNewEntries(out newEntry1, out newEntry2))
                            for (int i = newEntry1.Count - newCount; i < newEntry1.Count; ++i)
                                fullAttachedReport.AppendFormat(fmt3_1, string.Format("Discovered new mapping: {0:22} --> {1:22}", newEntry1[i], newEntry2[i]));
                        // Now, try a second pass to reconcile newly discovered products.
                        MatchRows(posTable, clearingStatement.InstrumentNameMap);
                    }
                    fullAttachedReport.AppendFormat("\r\n ");
                }

                //
                // Write posTable to fullReport
                //
                fullAttachedReport.AppendFormat(posTable.ToStringHeader());
                for (int row = 0; row < posTable.Rows.Count; ++row)
                    fullAttachedReport.AppendFormat("\r\n{0}", posTable.ToStringRow(row));
                fullAttachedReport.Append("\r\n");

                //
                // Test if table Reconciled!
                //
                if (posTable.IsReconciled(0))                                   // test column 0 (which is clearing) equals sum of other columns (for each row).
                {   // This account is reconciled.
                    fullAttachedReport.AppendFormat(fmt3_1, "Reconciled.");              // Only add note to fullReport, nothing in summary.
                }
                else
                {
                    fullAttachedReport.AppendFormat(fmt3_1, "Not Reconciled.");
                    summaryReport.AppendFormat("\r\n<br>{0}: {1} statement UNRECONCILED. ({2})", clearingAcctName, clearingFirmName, ambreUserTag);
                    unreconciledTables.Add(posTable);                           // Store unreconciled tables, for additional analysis later.
                    unreconciledAmbreTags.Add(ambreUserTag);                    // Store ambre user tags too.
                    StringBuilder fillReport;                                   // Create the fill report for this table.
                    if (TryCreateFillReport(clearingStatement, clearingFirmName, clearingAcctName, ambreUserTag, posTable, ref fullAttachedReport, out fillReport))
                        unreconciledFills.Add(posTable, fillReport);
                }// if reconciled
            }//next clearingAccountName

            //
            // Save product table if new products discovered.
            //
            List<InstrumentName> keyList;
            List<InstrumentName> valList;
            if (clearingStatement.InstrumentNameMap.TryGetNewEntries(out keyList, out valList))
            {
                fullAttachedReport.AppendFormat("\r\nDiscovered products: ");
                bodyReport.AppendFormat("\r\n<P><b>Discovered products:</b>");
                for (int i = 0; i < keyList.Count; ++i)
                {
                    fullAttachedReport.AppendFormat("\r\n  {0:22} ---> {1:22}", InstrumentName.Serialize(keyList[i]), InstrumentName.Serialize(valList[i]));
                    bodyReport.AppendFormat("\r\n<br>{0:22} --->  {1:22}", InstrumentName.Serialize(keyList[i]), InstrumentName.Serialize(valList[i]));
                }
                bodyReport.AppendFormat("\r\n<br> ");
                clearingStatement.InstrumentNameMap.SaveTable();
            }

            //
            // Create warning report.
            //
            if (unreconciledTables.Count == 0)
            {
                bodyReport.AppendFormat("\r\n<p><b>All accounts reconciled for {0}.</b>", clearingFirmName);     // All accounts reconciled.
            }
            else
            {   // Create warnings for each unreconciled posTable.
                bodyReport.AppendFormat("\r\n<p><b>Unreconciled accounts:</b>");
                int nPositionColumns = 0;                                                   // max number of columns for all PosTables (for formatting table).
                foreach (PosTable pos in unreconciledTables)
                    nPositionColumns = Math.Max(nPositionColumns, pos.ColNames.Count);      // knowing max number of columns allows us to align all account reports.
                // Create the single table for all accounts reports.
                bodyReport.AppendFormat("\r\n<P><table border=\"1\" bordercolor=\"#808080\" width=\"100%\" cellpadding=\"0\" RULES=ROWS FRAME=HSIDES>");
                for (int posTableID = 0; posTableID < unreconciledTables.Count; ++posTableID)
                {
                    PosTable pos = unreconciledTables[posTableID];
                    BreUserTags ambreUserTag = unreconciledAmbreTags[posTableID];
                    // Title Row 1
                    bodyReport.AppendFormat("\r\n<tr>");
                    bodyReport.AppendFormat("\r\n<td align=\"left\">{0}</td>", " ");   // column for instrum,ent name
                    string tableTitle = string.Format("{0}: {1}", clearingFirmName, pos.ColNames[0]);
                    bodyReport.AppendFormat("\r\n<td align=\"left\" colspan=\"{1}\">{0}</td>", tableTitle, nPositionColumns + 1);
                    bodyReport.AppendFormat("\r\n</tr>");
                    // Title Row 2
                    bodyReport.AppendFormat("\r\n<tr>");
                    bodyReport.AppendFormat("\r\n<td align=\"left\">{0}</td>", " ");   // column for instrum,ent name
                    bodyReport.AppendFormat("\r\n<td align=\"left\" colspan=\"{1}\">{0}</td>", ambreUserTag, nPositionColumns + 1);
                    bodyReport.AppendFormat("\r\n</tr>");
                    // Header Row - Login name
                    bodyReport.AppendFormat("\r\n<tr>");
                    bodyReport.AppendFormat("\r\n<td align=\"left\">{0}</td>", " ");   // column for instrum,ent name
                    for (int col = 0; col < nPositionColumns; ++col)
                        if (col < pos.ColNames.Count)
                        {
                            int nLastPtr = pos.ColNames[col].LastIndexOf('_');
                            if (nLastPtr >= 0)
                                bodyReport.AppendFormat("\r\n<td align=\"right\">{0}</td>", pos.ColNames[col].Substring(0, nLastPtr));
                            else
                                bodyReport.AppendFormat("\r\n<td align=\"right\">{0}</td>", clearingFirmName);
                        }
                        else
                            bodyReport.AppendFormat("\r\n<td align=\"right\">{0}</td>", string.Empty);
                    bodyReport.AppendFormat("\r\n<td align=\"right\"> </td>");
                    bodyReport.AppendFormat("\r\n</tr>");
                    // Header Row - Account
                    bodyReport.AppendFormat("\r\n<tr>");
                    bodyReport.AppendFormat("\r\n<td align=\"left\">{0}</td>", " ");   // column for instrum,ent name
                    for (int col = 0; col < nPositionColumns; ++col)
                        if (col < pos.ColNames.Count)
                        {
                            int nLastPtr = pos.ColNames[col].LastIndexOf('_');
                            bodyReport.AppendFormat("\r\n<td align=\"right\">{0}</td>", pos.ColNames[col].Substring(nLastPtr + 1));
                        }
                        else
                            bodyReport.AppendFormat("\r\n<td align=\"right\">{0}</td>", string.Empty);
                    bodyReport.AppendFormat("\r\n<td align=\"right\">Net</td>");
                    bodyReport.AppendFormat("\r\n</tr>");
                    // Table entries - each row of position table.
                    for (int row = 0; row < pos.Rows.Count; ++row)
                    {
                        bodyReport.AppendFormat("\r\n<tr>");
                        // Instrument name for row.
                        Pos posForRowName = null;
                        int i = pos.ColNames.Count - 1;                                 // locate first instrument name used by Ambre (nicer to read than clearing names).
                        while (posForRowName == null && i >= 0)
                        {
                            if (pos.Rows[row][i] != null)
                                posForRowName = pos.Rows[row][i];
                            i--;
                        }
                        bodyReport.AppendFormat("\r\n<td align=\"left\">{0}</td>", posForRowName.Instr);
                        // Load each column with Qtys.
                        for (int col = 0; col < nPositionColumns; ++col)
                            if (col < pos.Rows[row].Length)
                            {
                                Pos aPos = pos.Rows[row][col];
                                if (aPos != null)
                                    bodyReport.AppendFormat("\r\n<td align=\"right\">{0}</td>", aPos.Qty);
                                else
                                    bodyReport.AppendFormat("\r\n<td align=\"right\">{0}</td>", " ");
                            }
                            else
                                bodyReport.AppendFormat("\r\n<td align=\"right\"> </td>"); // this account has fewer columns.
                        // Column for Net position.
                        if (!pos.IsReconciledRow(row, 0))
                        {
                            int net = 0;
                            if (pos.Rows[row][0] != null)
                                net = -pos.Rows[row][0].Qty;                                // clearing is -qty.
                            for (int col = 1; col < pos.Rows[row].Length; ++col)
                                if (pos.Rows[row][col] != null)
                                    net += pos.Rows[row][col].Qty;                          // add ambre +qty.
                            bodyReport.AppendFormat("\r\n<td align=\"right\">{0}</td>", net);
                        }
                        else
                            bodyReport.AppendFormat("\r\n<td align=\"right\">{0}</td>", string.Empty);// reconciled entry is blank
                        bodyReport.AppendFormat("\r\n</tr>");
                    }//next row
                    // Add some space between acounts.
                    if (posTableID < unreconciledTables.Count - 1)                         // Write a blank region after account-blocks, except for the last.
                        bodyReport.AppendFormat("\r\n<tr><td></td><td colspan=\"{0}\" height=\"50\"> <b></b> </td></tr>", nPositionColumns + 1);
                }// next unreconciled PosTable
                bodyReport.AppendFormat("\r\n</table>");
            }// if unReconciled tables.

            //
            // Report on unreconciled Fills.
            //
            if (unreconciledTables.Count > 0)
            {
                bodyReport.AppendFormat("\r\n<p><b>Unmatched/extra fills:</b>");
                bodyReport.AppendFormat("\r\n<table border=\"0\" bordercolor=\"#808080\" width=\"100%\" cellpadding=\"0\" RULES=ROWS FRAME=HSIDES>");
                foreach (PosTable pos in unreconciledTables)
                {
                    StringBuilder report;
                    if (unreconciledFills.TryGetValue(pos, out report) && report.Length > 0)
                        bodyReport.Append(report);
                }
                bodyReport.AppendFormat("\r\n</table>");
            }

            //
            // Exit - cleanup reports.
            //
            Log.NewEntry(LogLevel.Major, string.Format("Full report: \r\n{0}", fullAttachedReport.ToString()));
            double minutesElapsed = Log.GetTime().Subtract(dtProcessStartTime).TotalMinutes;
            Log.NewEntry(LogLevel.Major, "Reconciliation completed in {0:0.0} mins.", minutesElapsed);
            // Create final reports: fullAttachReport and bodyReport
            fullAttachedReport.AppendFormat("\r\nReconciliation completed in {0:0.0} minutes.", minutesElapsed);
            summaryReport.AppendFormat("\r\n<br>Reconciliation completed in {0:0.0} minutes.", minutesElapsed);
            bodyReport.AppendFormat("\r\n<br>{0}<br> ", summaryReport.ToString());   // Note that we add summaryReport to the bottom of the body!         

            //
            // Update eventArg OutData now!
            //
            // OutData[0] contains email subject 
            if (fireWarning || unreconciledTables.Count > 0)                    // Check to see if a warning needs to be issued
                if (((StringBuilder)eventArg.OutData[0]).Length == 0)           // if a warning was already generated, we need not add another to the email title.
                    ((StringBuilder)eventArg.OutData[0]).Append("Warnings");

            // OutData[1] contains email body.
            // Append our body to any body already there.
            StringBuilder body = (StringBuilder)eventArg.OutData[1];
            body.Append(bodyReport.ToString());                                 // This is the email-body, add our report text to this object.

            // OutData[2....] are all attachments
            eventArg.OutData.Add(fullAttachedReport);                           // Additional files will appear as email attachments.

            //List<StringBuilder> finalReports = new List<StringBuilder>();
            //StringBuilder messageTitle = new StringBuilder();
            //finalReports.Add(messageTitle);
            //if ( fireWarning || unreconciledTables.Count > 0 )
            //    messageTitle.AppendFormat("{0} Warnings", clearingFirmName);
            //else
            //    messageTitle.AppendFormat("{0} No Issues", clearingFirmName);
            //finalReports.Add(new StringBuilder());
            //eventArg.OutData = new List<object>();
            //eventArg.OutData.AddRange(finalReports);
            //eventArg.Status = TaskStatus.Success;
        }//ReconcileStatement()
        //
        //
        //
        private const string fmt3_1 = "       {0}\r\n";
        private const string fmt3_4 = "       {0,6} {1,-22} {2,6} {3,-22} \r\n";
        //
        //
        //
        // *****************************************************************
        // ****                 TryCreateFillReport()                   ****
        // *****************************************************************
        /// <summary>
        /// Reload ALL fills for the entire day for the ambre accounts. (We already know the clearing fills.)
        /// Currently, this procedure reloads 24 hours worth of fills for each account (in posTable columns), and examines fills.
        /// </summary>
        /// <param name="clearingStatement"></param>
        /// <param name="clearingFirmName"></param>
        /// <param name="clearingAcctName"></param>
        /// <param name="ambreUserTag"></param>
        /// <param name="clearing2BreProductMap"></param>
        /// <param name="posTable">Full PosTable that failed to reconcile</param>
        /// <param name="fullReport">Full report for email attachment</param>
        /// <param name="report">New report for this specific fill matching attempt</param>
        private bool TryCreateFillReport(IClearingStatementReader clearingStatement, string clearingFirmName, string clearingAcctName,
            BreUserTags ambreUserTag, PosTable posTable,
            ref StringBuilder fullReport, out StringBuilder report)
        {
            report = new StringBuilder();                                           // Create the output report - used in body of email (warnings)
            fullReport.AppendFormat(fmt3_1, "Fill Analysis Report:");
            //
            // Load all Ambre fills.
            //
            Dictionary<string, Dictionary<InstrumentName, List<Fill>>> allAmbreFills = new Dictionary<string, Dictionary<InstrumentName, List<Fill>>>();
            for (int col = 1; col < posTable.ColNames.Count; ++col)                 // Load the fills for each Ambre user.
            {
                string ambreUserName = posTable.ColNames[col];
                Dictionary<InstrumentName, int> ambreSettle = new Dictionary<InstrumentName, int>();
                Dictionary<InstrumentName, List<Fill>> ambreFills = new Dictionary<InstrumentName, List<Fill>>();
                List<int> offsets = m_ProductSettlementTable.GetSettlementOffsets();// Get unique settlement times.
                foreach (int minuteOffset in offsets)                               // Load each group of settlements.
                    TryLoadAmbreStatement(ambreUserName, minuteOffset, 24.0, ref ambreSettle, ref ambreFills);
                allAmbreFills[ambreUserName] = ambreFills;                          // store fills for this user.
            }
            // Analyze fills for instruments that failed to reconcile.
            for (int row = 0; row < posTable.Rows.Count; ++row)                     // Find rows to examine further.
            {
                if (!posTable.IsReconciledRow(row, 0))                              // This row did not reconcile. Examine it more closely.
                {   // First thing to do is determine the instruments associated with each column.                    
                    InstrumentName[] rowInstruments = new InstrumentName[posTable.ColNames.Count];
                    for (int i = 0; i < rowInstruments.Length; ++i)
                        if (posTable.Rows[row][i] != null)
                            rowInstruments[i] = posTable.Rows[row][i].Instr;

                    // Seek a "best name" for this row's instrument for use in reports.
                    InstrumentName rowInstrumentName = new InstrumentName();        // will hold the official name for this row.
                    for (int i = 1; i < rowInstruments.Length; ++i)                 // search thru Amber accounts, not clearing.
                        if (!rowInstruments[i].IsEmpty)
                            rowInstrumentName = rowInstruments[i];
                    if (rowInstrumentName.IsEmpty && !rowInstruments[0].IsEmpty)
                    {   // This is case then there is ONLY a clearing position (nothing in Ambre columns).
                        // Then, we still want to find a better name for the instrument.                        
                        List<InstrumentName> possibleMatches = new List<InstrumentName>();
                        if (clearingStatement.InstrumentNameMap.TryGetKey(rowInstruments[0], ref possibleMatches, false))
                        {   // There seem to be some instruments that match in our table.
                            DateTime expiryDate;
                            bool clearInstrHasExpiry = InstrumentNameMapTable.TryExtractExpiryFromSeriesName(rowInstruments[0], out expiryDate);
                            foreach (InstrumentName name in possibleMatches)
                            {
                                if (!name.IsProduct)                                // this is a perfect instr match, accept it.
                                {
                                    rowInstrumentName = name;
                                    break;
                                }
                                else if (clearInstrHasExpiry)
                                {
                                    rowInstrumentName = new InstrumentName(name.Product, rowInstruments[0].SeriesName);
                                    break;
                                }
                            }
                        }// if clearingInstr is in our map.
                        else
                            rowInstrumentName = rowInstruments[0];                  // We know nothing about this instrument.  User clearing name.                        
                    }
                    //
                    //Collect all the fills into a work area.
                    //
                    fullReport.AppendFormat(fmt3_1, string.Format("{0}: All Fills", rowInstrumentName));

                    Dictionary<int, List<Fill>> fillWork = new Dictionary<int, List<Fill>>();
                    for (int col = 0; col < rowInstruments.Length; ++col)
                    {
                        fillWork.Add(col, new List<Fill>());
                        List<Fill> fills;
                        if (col == 0)
                        {
                            if (clearingStatement.Fills.ContainsKey(clearingAcctName) && clearingStatement.Fills[clearingAcctName].TryGetValue(rowInstruments[0], out fills))
                                fillWork[col].AddRange(fills);
                        }
                        else
                        {
                            if (allAmbreFills.ContainsKey(posTable.ColNames[col]) && allAmbreFills[posTable.ColNames[col]].TryGetValue(rowInstruments[col], out fills))
                                fillWork[col].AddRange(fills);
                        }
                        // Write fills for this acct:
                        foreach (Fill fill in fillWork[col])
                            fullReport.AppendFormat(fmt3_4, " ", posTable.ColNames[col], string.Format("{0} @ {1}", fill.Qty, fill.Price), string.Empty);
                    }

                    AnnihilateFills(ref fillWork);          // Cancel out matching fills.
                    //
                    // Create reports of remaining extra/unmatched fills.
                    //
                    fullReport.AppendFormat(fmt3_1, "Excess/unmatched fills:");
                    for (int column = 0; column < fillWork.Count; ++column)
                    {
                        if (fillWork[column].Count == 0)
                            continue;                                           // skip accounts without extra fills.
                        else if (report.Length == 0)                            // First time thru, create the header for table.
                            report.AppendFormat("\r\n<tr><td></td><td align=\"left\">{0}: {1}</td><td colspan=\"3\" align=\"left\">{2}: {3}</td></tr>",
                                clearingFirmName, clearingAcctName, ambreUserTag.Main, ambreUserTag.Tag);
                        // Write rows for each fill.
                        foreach (Fill fill in fillWork[column])
                        {
                            // Create entry.
                            report.AppendFormat("\r\n<tr><td align=\"left\">{0}</td><td align=\"right\">{1} @ {2}</td>", rowInstrumentName, fill.Qty, fill.Price);
                            report.AppendFormat("<td align=\"right\">{0}</td>", posTable.ColNames[column]);
                            report.AppendFormat("<td align=\"right\">{0} CST</td><td align=\"right\"></td></tr>", fill.LocalTime.ToString("HH:mm:ss"));
                            fullReport.AppendFormat(fmt3_4, " ", rowInstrumentName, string.Format("{0} @ {1}", fill.Qty, fill.Price), posTable.ColNames[column]);
                        }//next fill
                    }//next column     
                }//if row reconciled
            }//next row
            return (report.Length != 0);
        }//TryCreateFillMatchReport();
        //
        //
        // *****************************************************
        // ****             AnnihilateFills()               ****
        // *****************************************************
        private void AnnihilateFills(ref Dictionary<int, List<Fill>> fillWork, int clearingCol = 0)
        {
            //
            // Set default price multiplier.
            //
            double[] multiplier = new double[fillWork.Count];
            for (int i = 0; i < multiplier.Length; ++i)
                multiplier[i] = 1.0;
            //
            // Determine a non-identity multiplier
            //
            double[] priceMean = new double[fillWork.Count];
            double[] priceRange = new double[fillWork.Count];
            foreach (int col in fillWork.Keys)
            {
                priceMean[col] = 0;
                foreach(Fill aFill in fillWork[col])
                    priceMean[col] += aFill.Price;
                if (fillWork[col].Count > 0)
                {
                    priceMean[col] = priceMean[col] / fillWork[col].Count;
                    foreach (Fill aFill in fillWork[col])
                    {
                        double x = Math.Abs(aFill.Price - priceMean[col]);
                        if (priceRange[col] < x)
                            priceRange[col] = x;
                    }
                }
                else
                {
                    priceRange[col] = double.PositiveInfinity;
                }
            }//next col
            // Version 1: Only allow corrections to clearing numbers.
            if ( priceRange[clearingCol] < double.PositiveInfinity )
            {
                List<double> factors = new List<double>();
                for (int col = 0; col < priceMean.Length; ++col)
                {
                    double x = 1.0;
                    if (col == clearingCol)
                        continue;
                    if (priceRange[col] == double.PositiveInfinity)
                        continue;
                    if (priceMean[clearingCol] < priceMean[col] - priceRange[col])
                    {
                        if (Math.Abs(priceMean[clearingCol]) > 0.00001)
                            x = (priceMean[col]/priceMean[clearingCol]);//x = (priceMean[clearingCol] / priceMean[col]);
                    }
                    else if (priceMean[clearingCol] < priceMean[col] + priceRange[col])
                    {
                        if (Math.Abs(priceMean[clearingCol]) > 0.00001)
                            x = (priceMean[col] / priceMean[clearingCol]);//x = (priceMean[clearingCol] / priceMean[col]);
                    }
                    // Nearest power of 10
                    double power = Math.Log(x, 10);
                    power = Math.Round(power);
                    factors.Add(Math.Pow(10, power));                    
                }
                if (factors.Count > 0)
                {
                    // Compare them
                    double multi = factors[0];
                    double sum = 0.0;
                    for (int i=0; i<factors.Count; ++i)
                    {
                        sum += factors[i] / multi;
                    }
                    if ( ((int)Math.Round(sum)) == factors.Count)
                        multiplier[clearingCol] = multi;

                }
            }
                
            //
            // Match fills, removing matches from each list.
            //
            int c = 0;                                                      // the clearing fill index is "c"
            while (c < fillWork[clearingCol].Count)                          // recall: column zero is the clearing column!
            {
                int sign = Math.Sign(fillWork[clearingCol][c].Qty);          // sign of the fill (B=+1, S=-1).
                double price = fillWork[clearingCol][c].Price * multiplier[clearingCol];
                for (int ambreCol = 0; ambreCol < fillWork.Count; ++ambreCol)// loop thru each ambre account.
                {
                    if (ambreCol == clearingCol)
                        continue;                                           // skip the clearing account column.
                    int a = 0;                                              // fill index.
                    while (a < fillWork[ambreCol].Count)
                    {
                        if (fillWork[ambreCol][a].Price * multiplier[ambreCol] == price && Math.Sign(fillWork[ambreCol][a].Qty) == sign)
                        {   // These two fills match price and side of market ==> they match.
                            int qtyToRemove = sign * Math.Min(Math.Abs(fillWork[ambreCol][a].Qty), Math.Abs(fillWork[clearingCol][c].Qty));
                            fillWork[ambreCol][a].Qty -= qtyToRemove;       // Reduce Ambre fill by the matched amount.
                            if (fillWork[ambreCol][a].Qty == 0)             // if ambre fill is now empty, remove it from list.
                                fillWork[ambreCol].RemoveAt(a);             // now, index a points to next entry anyway, so don't increment it.
                            else
                                a++;                                        // otherwise, increment index.
                            fillWork[clearingCol][c].Qty -= qtyToRemove;              // Reduce clearing fill by the matched amount.
                            if (fillWork[clearingCol][c].Qty == 0)
                                break;                                      // If this clearing fill has been spent, then stop inner loop.
                        }
                        else
                            a++;                                            // fill doesn't match, advance to next one.
                    }// next Ambre fill index "a"
                    if (fillWork[clearingCol][c].Qty == 0)                            // if clearing fill is completely spent, stop searching for Ambre matching fills.
                        break;
                }//next Ambre user
                if (fillWork[clearingCol][c].Qty == 0)                                // if clearing fill is completely spent, remove fill from clearing list.
                    fillWork[clearingCol].RemoveAt(c);
                else
                    c++;
            }//next clearing fill index "c"
        }// AnnihilateFills()
        //
        //
        //
        //
        // *********************************************************************
        // ****                 Do Instruments Match()                      ****
        // *********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name1">clearing instrument</param>
        /// <param name="name2">ambre instrument</param>
        /// <param name="productLookUpTable"></param>
        /// <returns>True if these instruments are the same.</returns>
        private bool DoInstrumentsMatch(InstrumentName name1, InstrumentName name2, InstrumentNameMapTable instrumentTable)
        {           
            m_WorkingInstrumentList.Clear();                                // clear working list
            if (instrumentTable.TryGetKey(name1,ref m_WorkingInstrumentList,false))
            {   // We found clearing name1 in our list of instruments, or products.
                // What is returned are all Ambre instruments (and products) that are mapped to name1.
                // We sometimes get multiple matches.  (A product might have multiple Ambre names mapped to it.)
                // And there are two types of matches we might get, those that match only the Product and those that 
                // perfectly match the instrument.
                foreach (InstrumentName possibleMatchName in m_WorkingInstrumentList)
                {
                    if (!possibleMatchName.IsProduct)                   // A matching explicit Instrument was found for name1 in the table.
                    {   // The matching map for name1 was for its specific instrument.
                        // So compare the specific instrument to name2.
                        if (possibleMatchName.Equals(name2))             
                            return true;
                        // If we fail, we might still find a different mapping that succeeds. Keep trying.
                    }
                    else
                    {   // We have a matching product for name1.
                        // Check whether this possibleMatchName product matches our name2.Product.
                        // But, we also must compare their SeriesName as well.
                        if (possibleMatchName.Product.Equals(name2.Product))
                        {   // The products match perfectly.  Now we need to examine the SeriesNames.
                            // Extract their SeriesName date for name1.
                            DateTime name1Expiry = DateTime.MinValue;
                            DateTime name2Expiry = DateTime.MinValue;
                            bool name1SeriesNameIsDate = InstrumentNameMapTable.TryExtractExpiryFromSeriesName(name1,out name1Expiry);
                            bool name2SeriesNameIsDate = InstrumentNameMapTable.TryExtractExpiryFromSeriesName(name2,out name2Expiry);
                            if (name1SeriesNameIsDate)
                            {
                                if (name2SeriesNameIsDate && name1Expiry.Equals(name2Expiry))  // both have an encoded date, if they are equal, then its the same instrument.
                                    return true;
                            }
                            else
                            {   // name 1 is NOT an obvious date.
                                if (!name2SeriesNameIsDate && name1.SeriesName.Equals(name2.SeriesName))
                                    return true;                    // neither are dates and they perfectly match series name!
                            }
                        }//if products match.
                    }
                }
                return false;
            }
            else
                return false;
        }//DoInstrumentsMatch
        private List<InstrumentName> m_WorkingInstrumentList = new List<InstrumentName>();
        //
        //
                //
        //
        //
        //
        //
        // *********************************************************************
        // ****                 Do Exception Match()                        ****
        // *********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <returns>True if these instruments are the same.</returns>

        //private void MatchExceptions(PosTable pos, List<InstrumentName> Exception)
        //{
        //    // Process: go thru each instruments, and then compare with exception list.
        //    int ambreCount = 0;
        //        while (ambreCount < pos.Rows.Count)                                   // Loop thru each row in column ambreCol.
        //        {
        //            if (pos.Rows[ambreCount][0] == null)
        //            {
        //                ambreCount++;
        //                continue;
        //            }
                    //                   if(Exception.Contains(pos.Rows[ambreCount][0].Instr))
        //            InstrumentName ClearInstr = pos.Rows[ambreCount][0].Instr;
        //           Log.NewEntry(LogLevel.Major, "Reconciliation: run here:{0}", ClearInstr.Product);
        //            if (TryFindException(ClearInstr, Exception))
        //            {
        //                Log.NewEntry(LogLevel.Major, "Reconciliation: removed");
        //                Log.NewEntry(LogLevel.Major, "Reconciliation: exception instrument ignored : {0} ", pos.Rows[ambreCount][0].Instr);
        //                pos.RemoveAt(ambreCount, 0);
        //            }
        //            ambreCount++;
        //        }
        //}// MatchException()

        private bool TryFindException(InstrumentName memberOfList, List<InstrumentName> list)
        {
            int nCount = list.Count;
            for (int i = 0; i < list.Count; ++i)
            {
                if (memberOfList.Product.Equals(list[i].Product))                     // we have located a matching in list1 at this index i.
                {
                    return true;         // this is the output of that entry of list1.
                }

            }
            // Exit
            return false;
        }// TryFindMatch()
        //
        // *********************************************************
        // ****                 Match Rows()                    ****
        // *********************************************************
        private void MatchRows(PosTable pos, InstrumentNameMapTable instrTable)
        {
            // Process: go thru each ambreUser, and then each non-empty row for an instrument.
            // Then, try to find the matching instrument in the clearing column.
            int ambreCol = 1;                                                       // loop thru each of the columns, one for each ambreUser.
            //MatchExceptions(List<InstrumentName> cleaningInstr, List<InstrumentName> Exception
            while (ambreCol < pos.ColNames.Count)
            {
                int ambreRow = 0;
                while (ambreRow < pos.Rows.Count)                                   // Loop thru each row in column ambreCol.
                {
                    if (pos.Rows[ambreRow][ambreCol] == null)
                    {
                        ambreRow++;
                        continue;
                    }
                    InstrumentName ambreInstr = pos.Rows[ambreRow][ambreCol].Instr; // See if this ambreInstr matches any clearing instrument.                    
                    int clearRow = 0;                                               // Search thru each row with clearing instrument to compare with.
                    while (clearRow < pos.Rows.Count)
                    {
                        if (pos.Rows[clearRow][0] == null)
                        {   // No clearing instrument in this row.
                            clearRow++;
                            continue;
                        }
                        InstrumentName clearInstr = pos.Rows[clearRow][0].Instr;    // clearing instrument in row clearRow
                        if (DoInstrumentsMatch(clearInstr, ambreInstr, instrTable))
                        {
                            if (pos.Rows[clearRow][ambreCol] == null)
                            {   // There is an open space in the clearing row to to place our ambreInstrument.
                                Pos ambrePos = pos.RemoveAt(ambreRow, ambreCol);    // Pull entry from ambreRow
                                pos.Rows[clearRow][ambreCol] = ambrePos;            // Stick in in the empty spot.
                            }
                            else if (clearRow == ambreRow)
                            {   // There is already an object occupying the space we want to place our instrument! 
                                // But this is because we are looking at ourselves, which can happen on second passes at reconciliation.
                                // But, here clearRow!=ambreRow, so it seems as those we have found two matching rows!
                                // That is a problem.  Write a report.
                                //fullReport.AppendFormat(fmt3_1, "Reconciliation instrument collision 1");
                                Log.NewEntry(LogLevel.Major, "Reconciliation: Non collision on 2nd pass {0}: {1} and {2} ", pos.ColNames[0], clearInstr, ambreInstr);
                            }
                            else
                            {
                                Log.NewEntry(LogLevel.Major, "Reconciliation: Collision2 in {0}: {1} and {2} ", pos.ColNames[0], clearInstr, ambreInstr);
                            }
                        }
                        clearRow++;
                    }// wend clearRow                
                    ambreRow++;
                }//wend ambreRow
                ambreCol++;
            }//wend ambreCol++
            pos.DeleteEmptyRows();
        }// MatchRows()
        //
        //
        // *****************************************************************
        // ****             Try Discover New Products()                 ****
        // *****************************************************************
        private bool TryDiscoverNewProducts(List<List<Pos>> unknownPos, InstrumentNameMapTable instrumentMap)
        {
            int nInstrumentMapCount = instrumentMap.Count;              // track changes to instrument map.
            // Validate input.  Remove any entries that are empty. Keep those that weren't matched up.
            for (int col = 0; col < unknownPos.Count; ++col)
            {
                int i = 0;
                while (i < unknownPos[col].Count)
                {
                    if (unknownPos[col][i] == null)
                        unknownPos[col].RemoveAt(i);
                    else if (unknownPos[col][i].Qty == 0)
                        unknownPos[col].RemoveAt(i);
                    else
                        i++;                                                        // this one looks good, move on to next entry.
                }
            }

            // Search for new product mappings between the clearing entry, and any Ambre account.
            foreach (Pos clearingPos in unknownPos[0])
            {
                // Extract type of Series name for this clearing instrument.
                DateTime clearingExpiryDate;
                bool isClearInstrumentNameDated = InstrumentNameMapTable.TryExtractExpiryFromSeriesName(clearingPos.Instr, out clearingExpiryDate);

                bool isMatch = false;
                InstrumentName matchAmbreInstrName = new InstrumentName();          // a place to hold the best match
                for (int ambreAcct = 1; ambreAcct < unknownPos.Count; ambreAcct++)  // Search for each account
                {
                    foreach (Pos ambrePos in unknownPos[ambreAcct])                 // Search thru each pos in this list.
                    {
                        bool seriesMatch = false;
                        // Determine whether or not the specific instruments series names look like they match.
                        // Here, we are trying to discover a new mapping of product names.                        
                        DateTime ambreExpiryDate;
                        if (InstrumentNameMapTable.TryExtractExpiryFromSeriesName(ambrePos.Instr, out ambreExpiryDate))
                        {   // This ambre instrument seems to have an expiry date in its name.
                            if (isClearInstrumentNameDated && clearingExpiryDate.Equals(ambreExpiryDate))
                                seriesMatch = true;                                // they both have dates AND dates match!
                        }
                        else
                        {
                            if (!isClearInstrumentNameDated)
                                seriesMatch = true;                               // Since neither have dates, they just have names, so they could be matching..  
                        }
                        if (seriesMatch && clearingPos.Qty == ambrePos.Qty 
                            && clearingPos.Instr.Product.Type == ambrePos.Instr.Product.Type)
                        {   // To try to make the identification between two instruments, we need to have the following:
                            // 1) The qty's match, product types match, contract names (may) match, and we don't recognize either product already.
                            if (!isMatch)
                            {   // First time we have found a possible match!
                                // You see, if multiple instruments seems to all match, then we can't be sure which pair to correctly match.
                                isMatch = true;
                                matchAmbreInstrName = ambrePos.Instr;
                            }
                            else
                            {   // Second time we found match. We are stymied!
                                isMatch = false;
                                break;
                            }
                        }
                    }// next ambrePos
                }//next ambreAcct
                if (isMatch)
                {   // Note: We resist removing matches here, so must run another reconciliation pass.  This allows for more uniform reporting.
                    if (isClearInstrumentNameDated)
                    {   // Since this instr is part of a term structure, the rule we discovered should be
                        // a mapping for the entire product family.
                        InstrumentName clearingProd = new InstrumentName(clearingPos.Instr.Product, "");
                        InstrumentName ambreProd = new InstrumentName(matchAmbreInstrName.Product, "");
                        instrumentMap.Add(ambreProd, clearingProd);
                    }
                    else
                    {   // Since clearing instr is NOT part of term structure, then this is a mapping of 
                        // the full instrument name, including the series name.
                        instrumentMap.Add( matchAmbreInstrName, clearingPos.Instr);
                    }
                    
                }
            }//next rcg instrument
            // Exit
            return instrumentMap.Count > nInstrumentMapCount;
        }
        //
        //
        //
        //
        #endregion//Reconcile Statement - July 2013


        #region Ambre Reading Utilities
        // *********************************************************************
        // ****                     Private Utilities                       ****
        // *********************************************************************
        //
        //
        //
        //
        //
        // **********************************************************
        // ****             TryLoadAmbreStatement()              ****
        // **********************************************************
        /// <summary>
        /// This adds to ambreSettles all settling positions discovered for each ambreUserName
        /// at the settlementDateTime.  
        /// </summary>
        /// <param name="settlementDateTime"></param>
        /// <param name="exchangeList"></param>
        /// <param name="notInExchangeList"></param>
        /// <param name="ambreUserNames"></param>
        /// <param name="ambreSettles"></param>
        /// <returns></returns>
        private bool TryLoadAmbreStatement(string ambreUserName, DateTime startDateTime, DateTime settlementDateTime, List<string> exchangeList, ExchangeOptions options
            , ref Dictionary<InstrumentName, int> ambreSettles
            , ref Dictionary<InstrumentName, List<Fill>> ambreFills)
        {

            // Load book for this ambreUserName.
            string dropPath = string.Format("{0}{1}{2}", m_PrivatePath, m_UserInformation.Name, "\\Drops\\");
            //m_DropPath = "\\\\fileserver\\Users\\dv_bre\\Ambre\\Drops\\";
            BookReaders.EventPlayer eventPlayer = null;
            try
            {
                double hours = settlementDateTime.Subtract(startDateTime).TotalHours;
                if (BookReaders.EventPlayer.TryCreate(dropPath, ambreUserName, out eventPlayer))
                {
                    eventPlayer.Load(startDateTime, hours);
                }
            }
            catch (Exception e)
            {
                Log.AppendEntry("Exception thrown by BookReader.EventPlayer {0}", e.Message);
                return false;
            }
            if (eventPlayer == null || eventPlayer.SeriesList.Count == 0)                   // No information about this account in drop files.
                return false;

            // 
            foreach (InstrumentName instrName in eventPlayer.SeriesList.Keys)                // loop thru each found instr.
            {
                if (options == ExchangeOptions.ExcludeExchangesInList && exchangeList != null && exchangeList.Contains(instrName.Product.Exchange))
                    continue;                                                               // user wants to skip this exchange; its in the list.
                else if (options == ExchangeOptions.IncludeOnlyExchangesInList && exchangeList != null && !exchangeList.Contains(instrName.Product.Exchange))
                    continue;                                                               // user wants to skip this exchange; its NOT in the list.

                BookReaders.EventSeries series;
                Fill fill;
                List<Fill> playedFills;
                if (eventPlayer.SeriesList.TryGetValue(instrName, out series) && series.TryGetStateAt(settlementDateTime, out fill, out playedFills))
                {
                    InstrumentName newName = CorrectAmbreInstrumentName(instrName);
                    if (fill.Qty != 0)
                        ambreSettles.Add(newName, fill.Qty);
                    if (playedFills.Count > 0)
                        ambreFills.Add(newName, new List<Misty.Lib.OrderHubs.Fill>(playedFills));
                }
            }

            // Exit.
            Log.EndEntry();
            return true;
        }//TryLoadAmbreSettles()
        //
        //
        //
        // **********************************************************
        // ****             TryLoadAmbreStatement()              ****
        // **********************************************************
        /// <summary>
        /// This adds to ambreSettles all settling positions discovered for each ambreUserName
        /// at the settlementDateTime.  
        /// </summary>
        /// <param name="settlementDateTime"></param>
        /// <param name="exchangeList"></param>
        /// <param name="notInExchangeList"></param>
        /// <param name="ambreUserNames"></param>
        /// <param name="ambreSettles"></param>
        /// <returns></returns>
        private bool TryLoadAmbreStatement(string ambreUserName, int settleMinuteOffset, double hoursToLoad
            , ref Dictionary<InstrumentName, int> ambreSettles
            , ref Dictionary<InstrumentName, List<Fill>> ambreFills)
        {
            // Set up start/end for window to load.
            DateTime settlementDateTime = m_SettlementDate.AddMinutes(settleMinuteOffset);      // End timestamp of our loaded range.
            DateTime startDateTime = settlementDateTime;
            startDateTime = startDateTime.AddHours(-hoursToLoad);
            string dropPath = string.Format("{0}{1}{2}", m_PrivatePath, m_UserInformation.Name, "\\Drops\\");
            //m_DropPath = "\\\\fileserver\\Users\\dv_bre\\Ambre\\Drops\\";
            // Load book for this ambreUserName.
            BookReaders.EventPlayer eventPlayer = null;
            try
            {
                if (BookReaders.EventPlayer.TryCreate(dropPath, ambreUserName, out eventPlayer))
                {
                    eventPlayer.Load(startDateTime, hoursToLoad);
                }
            }
            catch (Exception e)
            {
                Log.AppendEntry("Exception thrown by BookReader.EventPlayer {0}", e.Message);
                return false;
            }
            if (eventPlayer == null || eventPlayer.SeriesList.Count == 0)                   // No information about this account in drop files.
                return false;

            // Search thru each loaded instrument, determine if it settles at this time.
            foreach (InstrumentName instrName in eventPlayer.SeriesList.Keys)                // loop thru each found instr.
            {
                ProductSettlementEntry entry;
                if (m_ProductSettlementTable.TryFindMatchingEntry(instrName, out entry) && entry.MinuteOffset == settleMinuteOffset)
                {   // Product has a rule, and it matches!
                    BookReaders.EventSeries series;
                    Fill fill;
                    List<Fill> playedFills;
                    if (eventPlayer.SeriesList.TryGetValue(instrName, out series) && series.TryGetStateAt(settlementDateTime, out fill, out playedFills))
                    {
                        InstrumentName newName = CorrectAmbreInstrumentName(instrName);
                        if (fill.Qty != 0)
                            ambreSettles.Add(newName, fill.Qty);
                        if (playedFills.Count > 0)
                            ambreFills.Add(newName, new List<Fill>(playedFills));
                    }
                }
            }//next instrument

            // Exit.
            Log.EndEntry();
            return true;
        }//TryLoadAmbreSettles()
        //
        //

        //
        /// <summary>
        /// Useful little enum for TryLoadAmbreSettles() method.
        /// </summary>
        private enum ExchangeOptions
        {
            None,
            ExcludeExchangesInList,
            IncludeOnlyExchangesInList
        }
        //
        //
        //
        //
        // *********************************************************
        // ***          TryGetAllAmbreUserNames()               ****
        // *********************************************************
        private bool TryGetAllAmbreUserNames(string dropPath, out List<string> ambreUserNames)
        {
            ambreUserNames = new List<string>();
            dropPath = string.Format("{0}{1}{2}", m_PrivatePath, m_UserInformation.Name, "\\Drops\\");
            //m_DropPath = "\\\\fileserver\\Users\\dv_bre\\Ambre\\Drops\\";
            const int NumberOfDaysToSearchUsersFor = 2; 
            try
            {
                List<string> dirPaths = new List<string>(System.IO.Directory.GetDirectories(dropPath));
                if (dirPaths.Count == 0)
                {
                    Log.NewEntry(LogLevel.Major, "TryGetAllAmbreUserNames: No drop directories found.");
                    return false;
                }
                dirPaths.Sort();                                                        // Sort these since  dir names are dates.
                int dirPtr = Math.Max(0, (dirPaths.Count - NumberOfDaysToSearchUsersFor));// search only most recent, two day.
                while (dirPtr < dirPaths.Count)                                         // Here we are collecting ambre user names, only.
                {
                    string[] filePaths = System.IO.Directory.GetFiles(dirPaths[dirPtr]);
                    foreach (string filePath in filePaths)
                    {
                        int fileNameStart = filePath.LastIndexOf('\\') + 1;             // first char position of file name
                        fileNameStart = filePath.IndexOf('_', fileNameStart) + 1;       // point after "HHmmss_"
                        fileNameStart = filePath.IndexOf('_', fileNameStart) + 1;       // point after "FillBooks_"
                        string baseName = filePath.Substring(fileNameStart);
                        baseName = baseName.Substring(0, baseName.Length - 4);
                        if (baseName.Contains("SIM"))
                            continue;                                                   // Skip sim accounts
                        if (!ambreUserNames.Contains(baseName))
                            ambreUserNames.Add(baseName);                               // store name - this is how ambre identifies users/fill hubs.
                    }
                    dirPtr++;
                }//wend dirPtr
            }// try
            catch (Exception e)
            {
                Log.NewEntry(LogLevel.Error, "TryGetAllAmbreUserNames: Loading ambre drops. Exception: {0}", e.Message);
                return false;
            }
            return true;
        }// TryGetAllAmbreUserNames()
        //
        //
        //
        //
        //
        //
        //
        //
        // *********************************************************
        // ****             CorrectInstrumentName()             ****
        // *********************************************************
        private InstrumentName CorrectAmbreInstrumentName(InstrumentName oldName)
        {
            // Correct abnormal names in Ambre drop files.
            InstrumentName newName;
            if (oldName.Product.Exchange.Equals("TOCOM"))
            {
                DateTime date;
                if (DateTime.TryParseExact(oldName.SeriesName, "yyyy/MM", new System.Globalization.DateTimeFormatInfo(), System.Globalization.DateTimeStyles.None, out date))
                    newName = new InstrumentName(oldName.Product, string.Format("{0}", date.ToString("MMMyy")));
                else
                    newName = oldName;
            }
            else
                newName = oldName;
            return newName;
        }// CorrectInstrumentName()
        //
        //
        //
        //
        // *****************************************************************
        // ****             GetExchangeSettlementDates()                ****
        // *****************************************************************
        /// <summary>
        /// Returns the table of day/hour offsets for each exchange.
        /// The exchange symbol is the one used by Ambre.
        /// </summary>
        /// <returns>Dictionary[hour offset] ---> List{Ambre exchanges} </returns>
        private Dictionary<List<string>, double> GetExchangeSettlementDates()
        {
            Dictionary<List<string>, double> hourOffset = new Dictionary<List<string>, double>();
            // TOCOM-like exchanges
            List<string> aList = new List<string>();
            aList.Add("TOCOM");
            aList.Add("SGX");
            aList.Add("TFX");           // This might not be correct Ambre name.
            aList.Add("SFE");            
            hourOffset.Add(aList, 1.5);
            // Paris closing
            aList = new List<string>();
            aList.Add("MTF");
            hourOffset.Add(aList, 11.5);
            // Sydney-like exchanges
            //aList = new List<string>();
            //aList.Add("SFE");
            //hourOffset.Add(aList, 0.5);

            // Exit
            return hourOffset;
        }// GetExchangeSettlementDates()
        //
        //
        #endregion// private utilities


        #region RCG Monitor And Copy New Files
        // *****************************************************************
        // ****                 MonitorAndCopyNewFiles                  ****
        // *****************************************************************
        /// <summary>
        /// The new approach (May 7 2013) is to check the local statement path 
        /// to see if the desired statement is already there.  If not, we monitor the 
        /// ftp site until it appears there, then ftp it over to our local statement dir.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArg"></param>
        public void MonitorAndCopyNewFiles(object sender, TaskEventArg eventArg)
        {
            if (m_UserInformation.ReconcileStatementType == ReconcileStatementType.RCG || m_UserInformation.ReconcileStatementType == ReconcileStatementType.Both)
            {
                Log.NewEntry(LogLevel.Minor, "MonitorAndCopyNewFiles: {0} ", eventArg);
                string rcgStatementPath = string.Format("{0}{1}{2}", m_PrivatePath, m_UserInformation.Name, "\\Statements\\");

                // 1. First create local statement directory, if not already present.
                if (!System.IO.Directory.Exists(rcgStatementPath))
                    System.IO.Directory.CreateDirectory(rcgStatementPath);

                // 2. Next look to see whether the desired files already in local directory.
                List<string> neededFileNameList = GetStatementFileNameForRCG(m_SettlementDate);
                int fileNamePtr = 0;
                while (fileNamePtr < neededFileNameList.Count)
                {
                    string desiredFilePath = string.Format("{0}{1}", rcgStatementPath, neededFileNameList[fileNamePtr]);
                    if (System.IO.File.Exists(desiredFilePath))
                        neededFileNameList.RemoveAt(fileNamePtr);         // this necessary filename is found, remove it from list.
                    else
                        fileNamePtr++;                                    // failed to find the file, keep its name in list, now consider next filename.
                }

                // 3. If not, call FTP routine to download it.
                if (neededFileNameList.Count == 0)
                    eventArg.Status = TaskStatus.Success;               // All necessary files have been found.
                else
                    GetFTPFiles(eventArg, neededFileNameList, null);          // Need to do FTP to find missing files... this call sets eventArg.Status approapriately.
                //GetFTPFiles(eventArg, neededFileNameList, new List<string>(new string[]{m_AccountTagFileName}) );          // Need to do FTP to find missing files... this call sets eventArg.Status approapriately.
            }
            else
            {
                Log.NewEntry(LogLevel.Major, "No RCG statement reconcilation is needed for user {0}", m_UserInformation.Name);
                eventArg.Status = TaskStatus.Success;
            }
        }// MonitorAndCopyNewFiles()
        //
        //
        // ****                 GetFTPFiles()                   ****
        /// <summary>
        /// </summary>
        /// <param name="eventArg">Current task we are working</param>
        /// <param name="requiredFileNameList">filenames we seek from ftp site.</param>
        private void GetFTPFiles(TaskEventArg eventArg, List<string> requiredFileNameList, List<string> optionalFileNameList)
        {
            Log.NewEntry(LogLevel.Minor, "GetFTPFiles: {0} ", eventArg);
            string rcgStatementPath = string.Format("{0}{1}{2}", m_PrivatePath, m_UserInformation.Name, "\\Statements\\");
            string ftpKeyPath = string.Format("{0}{1}{2}{3}{4}", m_PrivatePath, m_UserInformation.Name, "\\", m_UserInformation.Name, ".priv");
            List<string> localFileNamesCopied;                          // names of files discovered on FTP site.
            if (m_FtpReader.TryCopyNewRemoteFilesToLocal(m_UserInformation.FTPUserName, rcgStatementPath, out localFileNamesCopied, ftpKeyPath))
            {   // Success making FTP connection!
                //
                // Attempt to find all neccessary files on remote server.
                //
                bool allFilesFound = true;                              // assume the best
                foreach (string fileName in requiredFileNameList)
                {
                    bool fileFound = false;
                    foreach (string s in localFileNamesCopied)
                        if (s.ToUpper().Contains(fileName.ToUpper()))
                        {
                            fileFound = true;
                            break;      
                        }
                    allFilesFound = allFilesFound && fileFound;
                }
                if (allFilesFound)                                      // we consider a success that the files we waiting for are there now.
                {
                    Log.NewEntry(LogLevel.Major, "GetFtpFiles: Successfully downloaded all needed files.");
                    eventArg.Status = TaskStatus.Success;               // update request state
                    try
                    {
                        //
                        // Copy optional files too.
                        //
                        List<Renci.SshNet.Sftp.SftpFile> fileList;
                        if (optionalFileNameList != null && m_FtpReader.TryGetRemoteFileNames(m_UserInformation.FTPUserName, out fileList))
                        {   // User has provided an list of optionalFiles to load, and we successfully got all remote files.
                            // Now lets compare and see if we find any files we are looking for.
                            int n = 0;
                            while (n < fileList.Count)
                            {
                                Renci.SshNet.Sftp.SftpFile file = fileList[n];
                                if (!optionalFileNameList.Contains(file.Name))
                                    fileList.RemoveAt(n);
                                else
                                    n++;
                            }
                            if (fileList.Count > 0)
                            {
                                List<string> fileNamesCopied = null;
                                m_FtpReader.TryCopyFilesWeWant(m_UserInformation.FTPUserName, fileList, m_AppInfo.UserPath, out fileNamesCopied);
                                if (fileNamesCopied != null && fileNamesCopied.Count > 0)
                                {
                                    Log.BeginEntry(LogLevel.Minor, "GetFTPFiles: Copied optional files:");
                                    foreach (string s in fileNamesCopied)
                                        Log.AppendEntry(" {0}", s);
                                    Log.AppendEntry(".");
                                    Log.EndEntry();
                                }
                            }
                            else
                                Log.NewEntry(LogLevel.Minor, "GetFTPFiles: No optional files copied.");
                        }
                    }
                    catch (Exception e)
                    {
                        Log.NewEntry(LogLevel.Minor, "GetFTPFiles: No optional files copied. Exception caught = {0}",e.Message);
                    }


                }
                else
                {
                    Log.NewEntry(LogLevel.Major, "GetFtpFiles: Failed to download all needed files.");
                    eventArg.Status = TaskStatus.WaitAndTryAgain;       // There were no new files yet. Try again later.
                }


            }
            else
            {   // Failed to connect properly to FTP
                Log.NewEntry(LogLevel.Minor, "GetFTPFiles: FTP Connection failed.");
                eventArg.Status = TaskStatus.WaitAndTryAgain;           // We will try this again.                
            }




            // Test for expired time - this should be done by base class.
            if (eventArg.Status == TaskStatus.WaitAndTryAgain && DateTime.Now.CompareTo(eventArg.StopTime) > 0)
            {
                Log.NewEntry(LogLevel.Minor, "GetFTPFiles: Task expiration time has passed.  Task failed.");
                eventArg.Status = TaskStatus.Failed;                // We are past the StopTime, need to give up.
                // Send message about failure.
                StringBuilder messageBody = new StringBuilder();
                messageBody.AppendFormat("Failed to find clearing report.");
                string subject = "Breconciler: Failed";
                if (!TrySendEmail(messageBody, subject, m_EmailRecipients))
                    Log.NewEntry(LogLevel.Major, "GetFTPFiles: Failed and failed to send email.");
            }
            else
            {   // We will continue to wait.
                if (DateTime.Now.CompareTo(eventArg.StopTime.AddHours(-2.0 + m_EmailsWarningsSent)) >= 0)
                {   // Send message about delay.
                    m_EmailsWarningsSent++;
                    StringBuilder messageBody = new StringBuilder();
                    messageBody.AppendFormat("Reconciler is delayed.  Waiting for clearing reports.");
                    string subject = "Breconciler: Delayed";
                    if (!TrySendEmail(messageBody, subject, m_EmailRecipients))
                        Log.NewEntry(LogLevel.Major, "GetFTPFiles: Delayed and failed to send email.");
                }
            }
            
        }// MonitorAndCopyNewFiles()
        //
        //
        // ****             GetStatementFileNameForRCG()                ****
        //
        private List<string> GetStatementFileNameForRCG(DateTime settlementDate)
        {
            List<string> desiredFileNames = new List<string>();
            foreach (string filePatternName in m_RcgFilePatterns)
            {
                string s = string.Format("{0}_{1:yyyyMMdd}.csv", filePatternName, settlementDate);
                desiredFileNames.Add(s);
            }
            return desiredFileNames;
        }// GetStatementFileNameForRCG()
        //
        //
        //
        //
        #endregion//Private Methods


        #region Monitor And Copy New Files - ABN
        // *****************************************************************
        // ****                 MonitorAndCopyNewFiles                  ****
        // *****************************************************************
        /// <summary>
        /// The new approach (May 7 2013) is to check the local statement path 
        /// to see if the desired statement is already there.  If not, we monitor the 
        /// ftp site until it appears there, then ftp it over to our local statement dir.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArg"></param>
        public void MonitorAndCopyNewFilesABN(object sender, TaskEventArg eventArg)
        {
            if (m_UserInformation.ReconcileStatementType == ReconcileStatementType.ABN || m_UserInformation.ReconcileStatementType == ReconcileStatementType.Both)
            {
                DateTime settlementDate = m_SettlementDate;
                string abnStatementPath = string.Format("{0}{1}{2}", m_PrivatePath, m_UserInformation.Name, "\\StatementsABN\\");
                // Clearing firm specific variables; TODO: separating firm specific variables for future generalization.
                FtpConnectInfo ftpInfo = FtpConnectInfo.CreateAbn();
                ftpInfo.UserName = m_UserInformation.FTPUserNameABN;
                ftpInfo.Password = m_UserInformation.FTPPasswordABN;
                string localStatementPath = abnStatementPath;
                string[] fileNamePatterns = m_AbnFilePatterns;

                // General 
                Log.NewEntry(LogLevel.Minor, "MonitorAndCopyNewFiles: {0} for {1}.", eventArg, settlementDate.ToShortDateString());
                // 1. First create local statement directory, if not already present.
                if (!System.IO.Directory.Exists(localStatementPath))
                    System.IO.Directory.CreateDirectory(localStatementPath);

                // 2. Next look to see whether the desired files already in local directory.
                List<string> neededFileNameList = GetStatementFileNames(settlementDate, fileNamePatterns);
                if (Log.BeginEntry(LogLevel.Minor, "MonitorAndCopyNewFiles: Needed filenames are"))
                {
                    foreach (string s in neededFileNameList)
                        Log.AppendEntry(" {0}", s);
                    Log.AppendEntry(". ");
                    Log.EndEntry();
                }
                int fileNamePtr = 0;
                while (fileNamePtr < neededFileNameList.Count)
                {
                    string desiredFilePath = string.Format("{0}{1}", localStatementPath, neededFileNameList[fileNamePtr]);
                    if (System.IO.File.Exists(desiredFilePath))
                        neededFileNameList.RemoveAt(fileNamePtr);         // this necessary filename is found, remove it from list since we already have it.
                    else
                        fileNamePtr++;                                    // failed to find the file, keep its name in list, now consider next filename.
                }
                // 3. If not, call FTP routine to download it.
                if (neededFileNameList.Count == 0)
                    eventArg.Status = TaskStatus.Success;               // All necessary files have been found.
                else
                {
                    if (Log.BeginEntry(LogLevel.Minor, "MonitorAndCopyNewFiles: Filenames not found are"))
                    {
                        foreach (string s in neededFileNameList)
                            Log.AppendEntry(" {0}", s);
                        Log.AppendEntry(".  Will try to FTP these files.");
                        Log.EndEntry();
                    }
                    GetFTPFiles(eventArg, neededFileNameList, abnStatementPath, ftpInfo);// Need to do FTP to find missing files... this call sets eventArg.Status approapriately.
                }
            }
            else
            {
                Log.NewEntry(LogLevel.Major, "No ABN statement reconcilation is needed for user {0}", m_UserInformation.Name);
                eventArg.Status = TaskStatus.Success;
            }
        }// MonitorAndCopyNewFiles()
        //
        //
        // ****                 GetFTPFiles()                   ****
        /// <summary>
        /// </summary>
        /// <param name="eventArg">Current task we are working</param>
        /// <param name="neededFileNameList">filenames we seek from ftp site.</param>
        private void GetFTPFiles(TaskEventArg eventArg, List<string> neededFileNameList, string localDestinationPath, FtpConnectInfo ftpInfo)
        {
            Log.NewEntry(LogLevel.Minor, "GetFTPFiles: {0} ", eventArg);
            List<string> localFileNamesCopied = new List<string>();     // names of files actually copied to local.            

            // 
            // FTP
            //
            SftpClient client;
            List<Renci.SshNet.Sftp.SftpFile> remoteFiles = null;
            using ( (ftpInfo.PrivateKeyFileObject!=null ? client = new SftpClient(ftpInfo.HostName, ftpInfo.PortID, ftpInfo.UserName, ftpInfo.PrivateKeyFileObject) :
                client = new SftpClient(ftpInfo.HostName, ftpInfo.PortID, ftpInfo.UserName, ftpInfo.Password)))
            {
                // Connect to ftp server.
                try
                {
                    client.Connect();
                    string workingDir = client.WorkingDirectory;
                    var listDirectory = client.ListDirectory(workingDir);
                    remoteFiles = new List<Renci.SshNet.Sftp.SftpFile>(client.ListDirectory(workingDir));
                }
                catch (Exception ex)
                {
                    Log.NewEntry(LogLevel.Warning, "GetFTPFileNames: FTP connection exception {0}", ex.Message);
                    eventArg.Status = TaskStatus.Failed;
                    return;
                }
                // Determine if the desired files are available on remote.
                List<Renci.SshNet.Sftp.SftpFile> desiredFiles = new List<Renci.SshNet.Sftp.SftpFile>(); // These are the file we will download.
                foreach (Renci.SshNet.Sftp.SftpFile file in remoteFiles)
                {
                    string remoteFileName = file.Name.Substring(file.Name.LastIndexOf('\\') + 1);
                    if (neededFileNameList.Contains(remoteFileName))
                        desiredFiles.Add(file);                             // keep this file to download
                }//next file
                // Try to copy desired files to local statement path.
                try
                {
                    //List<string> fileLinesRead = new List<string>();
                    foreach (Renci.SshNet.Sftp.SftpFile file in desiredFiles)
                    {
                        string localFilePath = string.Format("{0}{1}", localDestinationPath, file.Name);
                        //fileLinesRead.Clear();
                        //fileLinesRead.AddRange(client.ReadLines(file.Name));
                        //System.IO.File.WriteAllLines(localFileName, fileLinesRead);   // Write to local file.
                        using (System.IO.FileStream localFile = System.IO.File.OpenWrite(localFilePath))
                        {
                            client.DownloadFile(file.FullName, localFile);
                        }
                        Log.NewEntry(LogLevel.Minor, "FtpReader: Downloaded file {0}.", localFilePath);
                        localFileNamesCopied.Add(localFilePath);
                    }
                    client.Disconnect();
                }
                catch (Exception ex)
                {
                    Log.NewEntry(LogLevel.Warning, "GetFtpFiles: Failed to read exception. {0}", ex.Message);
                    eventArg.Status = TaskStatus.Failed;
                    return;
                }
            }// using ftp client

            // Test for success.
            if (neededFileNameList.Count == localFileNamesCopied.Count)
            {
                Log.NewEntry(LogLevel.Minor, "GetFtpFiles: Successfully copied all {0} files.", neededFileNameList.Count);
                eventArg.Status = TaskStatus.Success;
            }
            else
            {
                Log.NewEntry(LogLevel.Minor, "GetFtpFiles: Copied {0} of {1} files.  Will try again later.", localFileNamesCopied.Count,neededFileNameList.Count);
                eventArg.Status = TaskStatus.WaitAndTryAgain;
            }
            if (eventArg.Status == TaskStatus.WaitAndTryAgain && DateTime.Now.CompareTo(eventArg.StopTime) > 0)
            {
                Log.NewEntry(LogLevel.Minor, "MonitorAndCopyNewFiles: Task expiration time has passed.  Task failed.");
                Log.NewEntry(LogLevel.Minor, "MonitorAndCopyNewFiles: Write emails for failure.");
                // 
                StringBuilder body = new StringBuilder();
                body.AppendFormat("\r\nFailed to locate the remote clearing reports.");
                body.AppendFormat("\r\nExceeded timeout period.  Giving up.");
                if (!TrySendEmail(body, "BREconciler: No report", m_EmailRecipients))
                {
                    Log.NewEntry(LogLevel.Minor, "MonitorAndCopyNewFiles: Failed to send email.");
                }
                eventArg.Status = TaskStatus.Failed;                // We are past the StopTime, need to give up.
            }
        }// GetFTPFiles()
        //
        //
        // ****             GetStatementFileNames()                ****
        //
        private List<string> GetStatementFileNames(DateTime settlementDate, string[] fileNamePatterns)
        {
            List<string> desiredFileNames = new List<string>();
            foreach (string fileNamePattern in fileNamePatterns)
            {
                string s = string.Format("{0}_{1:yyyyMMdd}.csv", fileNamePattern, settlementDate);
                desiredFileNames.Add(s);
            }
            return desiredFileNames;
        }// GetStatementFileNames()
        //
        //
        //
        public class FtpConnectInfo
        {
            public string HostName = string.Empty;
            public int PortID = 22;
            public string UserName = string.Empty;
            public string Password = string.Empty;
            public PrivateKeyFile PrivateKeyFileObject = null;
            private FtpConnectInfo()
            {
            }
            public static FtpConnectInfo CreateAbn()
            {
                FtpConnectInfo info = new FtpConnectInfo();
                info.HostName ="64.29.96.125";
                info.PortID = 22;
                //info.UserName = "breftp";
                //info.Password = "n5$YtSf3";
                
                return info;
            }
            public static FtpConnectInfo CreateRcg()
            {
                FtpConnectInfo info = new FtpConnectInfo();
                info.HostName = "10.64.33.164";
                info.PortID = 34022;
                info.UserName = "bretrading";
                // Load local 
                string userPath = Misty.Lib.Application.AppInfo.GetInstance().UserPath;
                string privateKeyFilePath = string.Format("{0}{1}",userPath,"bretrading.priv");
                if (System.IO.File.Exists(privateKeyFilePath))
                    info.PrivateKeyFileObject = new PrivateKeyFile(privateKeyFilePath);
                return info;
            }
        }
        //
        //
        #endregion//Private Methods


        #region IStringifiable
        // *****************************************************************
        // ****                     IStringifiable                      ****
        // *****************************************************************
        /// <summary>
        /// 
        /// </summary>
        public new string GetAttributes()
        {
            return string.Format("StatementPath={0}.", m_SettlementDate);
        }
        public new void SetAttributes(Dictionary<string, string> attributes)
        {
            base.SetAttributes(attributes);
            DateTime dt;
            foreach (string key in attributes.Keys)
            {
                if (key.Equals("SettlementDate") && DateTime.TryParse(attributes[key], out dt))
                    this.m_SettlementDate = dt;
            }
        }
        public new void AddSubElement(IStringifiable subElement)
        {
            base.AddSubElement(subElement);
            if (subElement is UserInformation)
            {
                if (m_UserInformation == null)
                {
                    m_UserInformation = (UserInformation)subElement;

                    // Try to figure out which type of the statements should be reconciled.
                    m_UserInformation.DetermineReconcileStatementType();
                    m_UserInformation.DetermineReconcileAccountTagsLoadMethod();
                }
            }
        }
        //
        //
        #endregion// IStringifiable interface


        #region StartUserReconciler
        // ******************************************************
        // ****     Start Reconciler for current user        ****
        // ******************************************************
        /// <summary>
        /// file created Dec. 02 2013 to start Reconciler for each user 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArg"></param>
        public void StartUserReconciler(object sender, TaskEventArg eventArg)
        {
            bool isAlreadyWorking = false;
            lock (m_WorkingTaskHubsLock)
            {
                isAlreadyWorking = (m_WorkingTaskHubs != null);
            }
            if (isAlreadyWorking)
                return;

            //
            // Locate the user information
            //
            UserInformation userInfo = null;
            if (eventArg.InData != null)                    // the userInfo should be somewhere in the InData list.
            {
                foreach (object o in eventArg.InData)
                    if (o is UserInformation)
                        userInfo = (UserInformation)o; 
            }
            if (userInfo == null)
            {
                // A UserInformation object is a required (sub-element) of this Task.
                Log.NewEntry(LogLevel.Minor, "No UserInformation object found! Cannot run Reconciliation with it.");
                eventArg.Status = TaskStatus.Success;       // flag this as success so system can continue to reconcile other users
                return;
            }
            string filePath = string.Format("\\\\fileserver\\Users\\DV_Ambre\\AmbreUsers\\{0}\\Config\\ReconcilerConfig.txt",userInfo.Name);

            try
            {
                // Create the services defined in the config file.
                using (StringifiableReader reader = new StringifiableReader(filePath))
                {
                    List<IStringifiable> objectList = reader.ReadToEnd();
                    foreach (IStringifiable obj in objectList)
                    {                                                
                        if (obj is ReconcilerTaskHub)
                        {
                            ReconcilerTaskHub newHub = (ReconcilerTaskHub) obj;
                            newHub.Log.AppendEntry("StartUserReconcile: Starting reconciler for user {0}.", userInfo);
                            if (Log == null)
                                Log = newHub.Log;              // accept first Log as the form's log.

                            lock (m_WorkingTaskHubsLock)
                            {
                                m_WorkingTaskHubs = newHub;
                                m_WorkingTaskHubArg = eventArg;
                            }

                            // Log start up.
                            if (Log != null)
                            {
                                Log.NewEntry(LogLevel.Minor, "ReconcilerForm: Running config file {0}", filePath);
                                if (Log.BeginEntry(LogLevel.Minor, "ReconcilerForm: {0} TaskHubs: ", 1))
                                {
                                    Log.AppendEntry("<{0}>", newHub.GetAttributes());
                                }
                            }
                            newHub.Stopping += new EventHandler(Reconciler_Stopping);
                            newHub.Start();
                        }
                    }
                }
                eventArg.Status = TaskStatus.WaitAndTryAgain;
            }
            catch (Exception)
            {
                eventArg.Status = TaskStatus.Failed;
            }

        }
        private object m_WorkingTaskHubsLock = new object();
        private ReconcilerTaskHub m_WorkingTaskHubs = null;
        private TaskEventArg m_WorkingTaskHubArg = null;
        //
        // 
        private void Reconciler_Stopping(object sender, EventArgs eventArg)
        {
            ReconcilerTaskHub hub = (ReconcilerTaskHub)sender;
            hub.Stopping -= new EventHandler(Reconciler_Stopping);

            TaskEventArg myWorkingTask = null;
            lock (m_WorkingTaskHubsLock)
            {
                myWorkingTask = m_WorkingTaskHubArg;
                m_WorkingTaskHubArg.Status = TaskStatus.Success;                
                m_WorkingTaskHubs = null;
                m_WorkingTaskHubArg = null;
            }

        }
        #endregion // StartUserReconciler
    }
}