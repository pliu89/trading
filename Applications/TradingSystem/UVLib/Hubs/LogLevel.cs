using System;

namespace UV.Lib.Hubs
{
    // *********************************************************
    // ****                     QTLog Enums                 ****
    // *********************************************************
    /// <summary>
    /// This enum provides information regarding the types of log 
    /// entries.
    /// </summary>
    [Flags()]
    public enum LogLevel
    {

        //
        // Execution Events
        //
        None = 0x00000000,
        Error = 0x0001,           // indicates error - bit 1
        Warning = 0x0002,           // warnings - bit 2        
        Major = 0x0004,           // major execution event - bit 3
        Minor = 0x0008,           // minor execution event - bit 4

        //
        // Display filters
        //
        // These are the codes given to the log level *filters* to determine
        // which messages will be processed.
        ShowNoMessages = 0x00000000,
        ShowErrorMessages = LogLevel.Error,
        ShowWarningMessages = LogLevel.Warning | ShowErrorMessages,	// shows warnings AND errors.
        ShowMajorMessages = LogLevel.Major | ShowWarningMessages,
        ShowMinorMessages = LogLevel.Minor | ShowMajorMessages,

        ShowAllMessages = 0xffff
    }//end Message Type enum.
    //
    //
    //



}//end namespace.
