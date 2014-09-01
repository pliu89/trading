using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;

namespace UV.Strategies.ExecutionHubs.ExecutionContainers
{
    using UV.Lib.Engines;
    using UV.Lib.Hubs;

    using UV.Strategies.ExecutionEngines.OrderEngines;

    /// <summary>
    /// This object can be used to aggregate serveral thread containers into a single entity 
    /// allowing an execution "strategy" to be multi threaded while obscuring that fact from the 
    /// pricing strategy on the client side.
    /// </summary>
    public class MultiThreadContainer : ThreadContainer
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        // Collections and mappings - concurrent just for safety currently...not sure if multiple threads will call 
        //
        private ConcurrentDictionary<int, ThreadContainer> m_EngineIdToThreadContainer = new ConcurrentDictionary<int, ThreadContainer>();    // EgineId --> ThreadContainer for engines now associated with another thread


        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
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
        // ********************************************************************
        // ****                 TryAddEngineIdToManagingContainer()        ****
        // ********************************************************************
        /// <summary>
        /// Caller would like to add an engine Id mapping it to the container that 
        /// will be controlling that engine.  This is a threadsafe call and can be made
        /// from any thread.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="engineID"></param>
        /// <returns></returns>
        public bool TryAddEngineIdToManagingContainer(ThreadContainer container, int engineID)
        {
            if (!m_EngineIdToThreadContainer.ContainsKey(engineID))
                return m_EngineIdToThreadContainer.TryAdd(engineID, container);
            else
                return false;
        }

        //
        // *****************************************************
        // ****                 ProcessEvent()              ****
        // *****************************************************
        //
        /// <summary>
        /// Caller would like an Event arg to be processed by the correct engine and thread
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override bool ProcessEvent(EventArgs e)
        {
            if (e is EngineEventArgs)
            {
                EngineEventArgs engEvent = (EngineEventArgs)e;
                ThreadContainer threadContainer;
                if (m_EngineIdToThreadContainer.TryGetValue(engEvent.EngineID, out threadContainer))
                {   // I found the container controlling this engine now.
                    if (this == threadContainer)
                    {   // if it is me, just process event now.
                        base.ProcessEvent(e);
                    }
                    else
                    {   // otherwise call the other container to process
                        threadContainer.ProcessEvent(e);
                    }
                        
                }
                else
                    Log.NewEntry(LogLevel.Error, "ProcessEvent: Could not find container responsible for that engine! Message will not be processed.");
            }
            return true;
        }
        //
        //
        //
        // *****************************************************
        // ****                 TryAddEngine()              ****
        // *****************************************************
        /// <summary>
        /// A nice way for hub to add engines to this container.
        /// </summary>
        /// <param name="oEngine"></param>
        /// <returns></returns>
        public override bool TryAddEngine(Engine oEngine)
        {
            if (m_IsLaunched)
                return false;
            if (oEngine is IOrderEngine)
            {
                if (this.IOrderEngine == null)
                    this.IOrderEngine = (IOrderEngine)oEngine;
            }
            if (oEngine is Engine)
            {
                Engine engine = (Engine)oEngine;
                this.EngineList.Add(engine.EngineID, engine);

                this.m_IEngineList.Add(engine);
            }
            return true;
        }// TryAddEngine()
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
