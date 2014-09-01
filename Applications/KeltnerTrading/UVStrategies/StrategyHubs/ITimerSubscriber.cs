using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies.StrategyHubs
{
    using UV.Lib.BookHubs;

    public interface ITimerSubscriber
    {

        void TimerSubscriberUpdate(Book aBook);

    }
}
