using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies.ExecutionHubs
{
    using UV.Lib.BookHubs;
    /// <summary>
    /// This interface is meant to mimic the ITimerSubscriber in the
    /// StrategyHubs namespace, however this one doesn't use books in its methods
    /// </summary>
    public interface ITimerSubscriber
    {
        void TimerSubscriberUpdate();
    }
}
