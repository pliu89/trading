using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Misty.Lib.IO.Xml;

namespace Misty.Lib.Application
{
    public class UserInformation : IStringifiable
    {
        public string Name;
        public string FTPUserName = string.Empty;
        public string FTPUserNameABN = string.Empty;
        public string FTPPasswordABN;
        public string[] EmailTo;
        public string FilePathToAccountTags = string.Empty;
        public string googleName = string.Empty;
        public string googlePassword;
        public ReconcileStatementType ReconcileStatementType;
        public LoadAccountTagsMethod ReconfileLoadAccountTagsMethod;

        #region Public methods
        // *********************************************************
        // ****                 Public Methods                  ****
        // *********************************************************
        //
        public override string ToString()
        {
            return string.Format("{0}", Name);
        }

        /// <summary>
        /// This function will figure out which kind of the statements should be reconciled.
        /// </summary>
        public void DetermineReconcileStatementType()
        {
            ReconcileStatementType = ReconcileStatementType.None;

            if (!string.IsNullOrEmpty(FTPUserName) && string.IsNullOrEmpty(FTPUserNameABN))
                ReconcileStatementType = ReconcileStatementType.RCG;
            else if (string.IsNullOrEmpty(FTPUserName) && !string.IsNullOrEmpty(FTPUserNameABN))
                ReconcileStatementType = ReconcileStatementType.ABN;
            else if (!string.IsNullOrEmpty(FTPUserName) && !string.IsNullOrEmpty(FTPUserNameABN))
                ReconcileStatementType = ReconcileStatementType.Both;
        }

        /// <summary>
        /// This function will figure out which kind of way to load account tags.
        /// </summary>
        public void DetermineReconcileAccountTagsLoadMethod()
        {
            ReconfileLoadAccountTagsMethod = LoadAccountTagsMethod.None;

            if (!string.IsNullOrEmpty(FilePathToAccountTags))
            {
                if (FilePathToAccountTags.Contains("\\"))
                {
                    if (System.IO.File.Exists(FilePathToAccountTags))
                        ReconfileLoadAccountTagsMethod = LoadAccountTagsMethod.LocalUserSpecifiedPath;
                    else
                        ReconfileLoadAccountTagsMethod = LoadAccountTagsMethod.LocalDefautPath;
                }
                else
                    ReconfileLoadAccountTagsMethod = LoadAccountTagsMethod.LocalDefautPath;
            }
            else
            {
                if (!string.IsNullOrEmpty(googleName) && googlePassword != null)
                    ReconfileLoadAccountTagsMethod = LoadAccountTagsMethod.GmailDrive;
            }
        }
        #endregion//public methods


        #region IStringifiable
        string IStringifiable.GetAttributes()
        {
            StringBuilder msg = new StringBuilder();
            if (!string.IsNullOrEmpty(this.Name))
                msg.AppendFormat("Name={0}", this.Name);
            return msg.ToString();
        }

        List<IStringifiable> IStringifiable.GetElements()
        {
            return null;
        }

        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            foreach (KeyValuePair<string, string> att in attributes)
            {
                if (att.Key.Equals("Name"))
                    this.Name = att.Value.Trim();
                else if (att.Key.Equals("FTPUserName"))
                    this.FTPUserName = att.Value.Trim();
                else if (att.Key.Equals("FTPUserNameABN"))
                    this.FTPUserNameABN = att.Value.Trim();
                else if (att.Key.Equals("FTPPasswordABN"))
                    this.FTPPasswordABN = att.Value.Trim();
                else if (att.Key.Equals("EmailTo"))
                    this.EmailTo = attributes[att.Key].Split(',');
                else if (att.Key.Equals("FilePathToAccountTags"))
                    this.FilePathToAccountTags = att.Value.Trim();
                else if (att.Key.Equals("googleName"))
                    this.googleName = att.Value.Trim();
                else if (att.Key.Equals("googlePassword"))
                    this.googlePassword = att.Value.Trim();
            }
        }

        void IStringifiable.AddSubElement(IStringifiable subElement)
        {

        }
        #endregion IStringifiable
    }

    public enum ReconcileStatementType
    {
        None,
        RCG,
        ABN,
        Both
    }

    public enum LoadAccountTagsMethod
    {
        LocalDefautPath,
        LocalUserSpecifiedPath,
        GmailDrive,
        None
    }
}