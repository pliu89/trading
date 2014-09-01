using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Misty.Lib.IO
{

    #region  Drop Queue Writer EventArgs
    // *****************************************************************************
    // ****             Drop Queue Writer EventArgs                             ****
    // *****************************************************************************
    //
    public class DropQueueWriterEventArgs : EventArgs
    {
        public DropQueueWriterRequestType Request;
        public StringBuilder Message = new StringBuilder(128);              // big string space for messages.
        public StringBuilder Message2 = new StringBuilder(128);             // path to output directory with trailing "\\"            
        public DropQueueWriterEventArgs()
        {
        }
        //
        // ****             Clear()             ****
        /// <summary>
        /// Clear all information stored during last use.
        /// </summary>
        public void Clear()
        {
            Message.Clear();
            Message2.Clear();
            Request = DropQueueWriterRequestType.None;
        }
        //
        // ****             ToString()          ****
        public override string ToString()
        {
            return string.Format("{0}", Request);
        }
    }
    //
    #endregion // end of class
   
    #region DropQueueWriterRequestType
    //
    //
    public enum DropQueueWriterRequestType
    {
        None = 0
        ,        Stop                               // begins the exit process
        ,        FlushNow                           // flushes the buffer now.
        ,        CopyTo                             // saves a copy of out file to file name provided.
        ,        CopyAllFiles                       // copies all files in current path to path provided.
        ,        ChangeFileName                     // updates target file name and directory
        ,        MoveTo                             // renames a file, over-writing the out file name if present.
        ,        WriteLine
    }
    #endregion // event args

}
