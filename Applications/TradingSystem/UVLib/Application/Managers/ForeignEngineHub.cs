using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.Application.Managers
{
    using UV.Lib.Engines;                   // for engine event args
    using UV.Lib.FrontEnds.Clusters;        // for clusters event args
    using UV.Lib.IO.Xml;

    /// <summary>
    /// A place holder for a IEngineHub Service located on a foreign server.
    /// </summary>
    public class ForeignEngineHub : ForeignService, IEngineHub
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************





        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public ForeignEngineHub() : base()
        {
        }
        public ForeignEngineHub(IService iService) : base (iService)
        {
            
        }
        //
        //       
        #endregion//Constructors


        #region Properties
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
        #endregion//Public Methods


        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        #endregion//Private Methods


        #region IStringifiable
        //
        //
        #endregion// IStringifiable




        #region IService
        #endregion// IService


        #region IEngineHub
        public List<IEngineContainer> GetEngineContainers()
        {
            throw new NotImplementedException();
        }
        public bool HubEventEnqueue(EventArgs eventArgs)
        {
            if (eventArgs is EngineEventArgs)
            {
                EngineEventArgs engineEventArgs = (EngineEventArgs)eventArgs;
                if (engineEventArgs.EngineHubName.Equals(m_LocalServiceName))
                {   
                    // Is it true that only events that are meant for this hub
                    // are ever pushed onto the queue? 
                    //  Consider the case that a StrategyHub subscribes to remote engines
                    //  subscribes to their events.
                    engineEventArgs.EngineHubName = m_RemoteServiceName;
                    Message msg = new Message();
                    msg.State = MessageState.None;
                    msg.MessageType = MessageType.EngineEvent;
                    msg.Data.Add(engineEventArgs);
                    return Parent.TrySendMessage(msg);
                }
                else
                    return false;
            }
            else if (eventArgs is ClusterEventArgs)
            {   // Need to implement cluster event args, inherited from engine event args.
                //ClusterEventArgs engineEventArgs = (ClusterEventArgs)eventArgs;
                //engineEventArgs..EngineHubName = m_RemoteServiceName;
                //Message msg = new Message();
                //msg.State = MessageState.None;
                //msg.MessageType = MessageType.EngineEvent;
                //msg.Data.Add(engineEventArgs);
                //return Parent.TrySendMessage(msg);

            }
            else
            {
                //throw new NotImplementedException();
            }
            return false;
        }
        //
        //
        // ****         EngineChanged           ****
        //
        public event EventHandler EngineChanged;
        //
        public void OnEngineChanged(EventArgs engineEventArgs)
        {
            if (EngineChanged != null)
                this.EngineChanged(this, engineEventArgs);
        }
        #endregion // IEngineHub


    }//end class
}
