using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;

namespace EconomicBloombergProject
{
    public static class Logging
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //   
        // Text body for the error log and logs.
        private static List<string> m_ErrorLogs = null;
        private static List<string> m_Logs = null;
        private static List<string> m_QueryLogs = null;
        private static readonly object m_LockObject = new object();

        // Email logger.
        private static string m_EmailFrom = "suesandy0@gmail.com";
        private static string m_UserName = "suesandy0";
        private static string m_Credential = "suesandy000";
        private static string m_EmailTo = "cliu@rcgdirect.com";
        //private static string m_EmailCC = "mpichowsky@dvtglobal.com";
        private static string m_SMTPHost = "smtp.gmail.com";
        private static int m_Port = 587;
        private static SmtpClient m_SmtpClient = null;

        // Text logger.
        private static string m_ErrorOutputPath = null;
        private static string m_LogOutputPath = null;
        private static string m_QueryLogPath = null;
        private static StreamWriter m_ErrorStreamWriter = null;
        private static StreamWriter m_StreamWriter = null;
        private static StreamWriter m_QueryStreamWriter = null;

        // Status of bloomberg.
        public static bool m_BloombergConnectionStatus = false;
        public static string m_EmptySign = "N/A";
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //     
        public static void InitiateLogging(string countryCode)
        {
            m_ErrorLogs = new List<string>();
            m_Logs = new List<string>();
            m_QueryLogs = new List<string>();

            // Get the current working directory and append sub directory and file name for error logs.
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(Environment.CurrentDirectory);
            stringBuilder.AppendFormat(@"\ErrorLogs{0}.txt", countryCode);
            m_ErrorOutputPath = stringBuilder.ToString();
            try
            {
                if (!File.Exists(m_ErrorOutputPath))
                {
                    m_ErrorStreamWriter = File.CreateText(m_ErrorOutputPath);
                }
                m_ErrorStreamWriter = new StreamWriter(m_ErrorOutputPath, false);
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.ToString());
            }

            // Get the current working directory and append sub directory and file name for logs.
            stringBuilder = new StringBuilder();
            stringBuilder.Append(Environment.CurrentDirectory);
            stringBuilder.AppendFormat(@"\Logs{0}.txt", countryCode);
            m_LogOutputPath = stringBuilder.ToString();
            try
            {
                if (!File.Exists(m_LogOutputPath))
                {
                    m_StreamWriter = File.CreateText(m_LogOutputPath);
                }
                m_StreamWriter = new StreamWriter(m_LogOutputPath, false);
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.ToString());
            }

            // Get the current working directory and append sub directory and file name for query logs.
            stringBuilder = new StringBuilder();
            stringBuilder.Append(Environment.CurrentDirectory);
            stringBuilder.AppendFormat(@"\Query{0}.txt", countryCode);
            m_QueryLogPath = stringBuilder.ToString();
            try
            {
                if (!File.Exists(m_QueryLogPath))
                {
                    m_QueryStreamWriter = File.CreateText(m_QueryLogPath);
                }
                m_QueryStreamWriter = new StreamWriter(m_QueryLogPath, false);
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.ToString());
            }

            // Instantiate the SmtpClient for sending email.
            m_SmtpClient = new SmtpClient();
            m_SmtpClient.Host = m_SMTPHost;
            m_SmtpClient.Port = m_Port;
            m_SmtpClient.EnableSsl = true;
            m_SmtpClient.UseDefaultCredentials = false;
            m_SmtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            m_SmtpClient.Credentials = new NetworkCredential(m_UserName, m_Credential);
        }

        /// <summary>
        /// This function closes all the stream writers.
        /// </summary>
        public static void CloseStreamWriters()
        {
            m_StreamWriter.Close();
            m_ErrorStreamWriter.Close();
            m_QueryStreamWriter.Close();
        }
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
        //
        //
        //
        /// <summary>
        /// This function writes error logs.
        /// </summary>
        public static void WriteErrorLog(string text)
        {
            lock (m_LockObject)
            {
                m_ErrorLogs.Add(text);
                try
                {
                    m_ErrorStreamWriter.WriteLine(text);
                }
                catch (IOException ex)
                {
                    Console.WriteLine(ex.ToString());
                    m_ErrorLogs.Add(ex.ToString());
                    return;
                }
            }
        }

        /// <summary>
        /// This function writes success logs.
        /// </summary>
        public static void WriteLog(string text)
        {
            lock (m_LockObject)
            {
                m_Logs.Add(text);
                try
                {
                    m_StreamWriter.WriteLine(text);
                }
                catch (IOException ex)
                {
                    Console.WriteLine(ex.ToString());
                    m_ErrorLogs.Add(ex.ToString());
                    return;
                }
            }
        }

        /// <summary>
        /// This function writes query logs.
        /// </summary>
        /// <param name="text"></param>
        public static void WriteQueryLog(string text)
        {
            lock (m_LockObject)
            {
                m_QueryLogs.Add(text);
                try
                {
                    m_QueryStreamWriter.WriteLine(text);
                }
                catch (IOException ex)
                {
                    Console.WriteLine(ex.ToString());
                    m_ErrorLogs.Add(ex.ToString());
                    return;
                }
            }
        }

        /// <summary>
        /// Send email by subject and email text body.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="message"></param>
        public static void SendingEmail(string subject, string message, bool attachmentMode)
        {
            lock (m_LockObject)
            {
                // Instantiate the email message.
                MailMessage email = new MailMessage();
                email.Subject = subject;
                email.Body = message;
                email.BodyEncoding = Encoding.GetEncoding("Windows-1254");
                email.From = new MailAddress(m_EmailFrom, m_UserName);
                email.To.Add(new MailAddress(m_EmailTo));
                //email.CC.Add(new MailAddress(m_EmailCC));

                if (attachmentMode)
                {
                    m_StreamWriter.Close();
                    m_ErrorStreamWriter.Close();
                    m_QueryStreamWriter.Close();
                    email.Attachments.Add(new Attachment(m_ErrorOutputPath));
                    email.Attachments.Add(new Attachment(m_LogOutputPath));
                    email.Attachments.Add(new Attachment(m_QueryLogPath));
                }

                // Send the email out.
                m_SmtpClient.Send(email);
            }
        }

        /// <summary>
        /// This function dump all the log information to string.
        /// </summary>
        public static string LogToString()
        {
            lock (m_LockObject)
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (string info in m_Logs)
                    stringBuilder.AppendLine(info);
                return stringBuilder.ToString();
            }
        }

        /// <summary>
        /// This function dump all the error log information to string.
        /// </summary>
        public static string ErrorLogToString()
        {
            lock (m_LockObject)
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (string info in m_ErrorLogs)
                    stringBuilder.AppendLine(info);
                return stringBuilder.ToString();
            }
        }

        /// <summary>
        /// This function dump all the query log information to string.
        /// </summary>
        public static string QueryLogToString()
        {
            lock (m_LockObject)
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (string info in m_QueryLogs)
                    stringBuilder.AppendLine(info);
                return stringBuilder.ToString();
            }
        }
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
    }
}