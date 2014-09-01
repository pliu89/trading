using System;
using System.Collections.Generic;
using System.Text;

namespace Ambre.Breconcile.Reconciler
{
    public class RequestEventArg : EventArgs
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        //
        public RequestType Type;
        public RequestStatus Status = RequestStatus.New;
        public DateTime GiveUpTime = DateTime.MinValue;             // (optional) keep trying to process request until this time, then we give up.

        // Returned data
        public List<object> Data = null;                            // data returned on success to user
        

        // Linking of Requests
        public List<RequestEventArg> m_Children = null;
        private RequestEventArg m_Parent = null;


        //
        //
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public RequestEventArg(RequestType type)
        {
            this.Type = type;
        }
        //
        //       
        #endregion//Constructors


        #region Public Methods
        // *****************************************************************
        // ****                   Public Methods                        ****
        // *****************************************************************
        //
        //
        public bool IsParentOf(RequestEventArg possibleChild)
        {
            return (Type == RequestType.MultipleSequentialRequests && this.m_Children != null && this.m_Children.Contains(possibleChild));
        }
        public bool IsChild
        {
            get { return m_Parent != null; }
        }
        public bool TryGetParent(out RequestEventArg parent)
        {
            parent = m_Parent;
            return (parent != null);
        }
        public bool TryToAddChild(RequestEventArg newChild)
        {
            if (this.Type != RequestType.MultipleSequentialRequests)
                return false;
            else
            {
                if (m_Children == null)
                    m_Children = new List<RequestEventArg>();
                if (m_Children.Contains(newChild))
                    return false;
                m_Children.Add(newChild);
                newChild.m_Parent = this;
            }
            return true;
        }// TryToAddChild()
        //
        //
        public bool TryGetNextUnsuccessfulChild(out RequestEventArg currentChild)
        {
            currentChild = null;
            if (m_Children == null || m_Children.Count == 0)
                return false;
            int n = 0;
            while (n < m_Children.Count && currentChild == null)    // its the first event that is not "sucessful"
            {
                RequestEventArg child = (RequestEventArg)m_Children[n];
                if (child.Status != RequestStatus.Success)          // look for first child that is not completed!
                    currentChild = child;                           // If found, he is the current working task
                n++;
            }
            if (currentChild == null)
                currentChild = m_Children[m_Children.Count-1];      // return last child at least.
            // Exit.
            return true;
        }//TryGetCurrentChild()
        //
        //
        //
        #endregion // public methods



        #region Public methods
        public override string ToString()
        {
            if (m_Children == null || m_Children.Count == 0)
                return string.Format("[{0} ({1})]", this.Type,this.Status);
            else
            {
                StringBuilder msg = new StringBuilder();
                msg.AppendFormat("[Request {0} ({1}) Children=", this.Type,this.Status);
                foreach (RequestEventArg e in m_Children)
                    msg.AppendFormat("{0}", e);
                msg.Append("]");
                return msg.ToString();
            }
        }
        //
        //
        #endregion // public methods


    }
}
