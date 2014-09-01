using System;

namespace UV.Lib.Excel.RTD
{
    public enum MessageType : ushort
    {
        None = 0
            //
            // Msgs BreTalker -> socket
            //
        ,Current = 1                    // From BreTalker: Current value of this Topic
        ,TopicArgs = 2                  // From BreTalker: all arguments originally passed in from Excel 
            //,AcceptChange = 3
        ,RejectChange = 4               // From BreTalker: failed to change TopicBase
        ,TopicRemoved = 5               // From BreTalker: Excel has ended subscription to this TopicBase
            //
            // Msgs socket -> BreTalker
            //
        ,RequestCurrent = 101           // From socket user: user wants current value of Topic.
        ,RequestTopicArgs = 102         // From socket user: 
        ,RequestChange = 103           // From socket user: user wants to change current value of topic.

    }
}
