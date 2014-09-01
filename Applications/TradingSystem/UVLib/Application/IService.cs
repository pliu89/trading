using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UV.Lib.Application
{
    /// <summary>
    /// A Service represent an application resource that receives requests and processes
    /// them asynchronously.  
    /// This services are usually autonomous, being created via a deserialization process and
    /// will spontaneously appear as a new service held by the Application.AppServices singleton object.
    /// Application.AppServices manages them assuming they at IService objects.
    /// </summary>
    public interface IService 
    {
        //
        // Properties
        //
        string ServiceName
        {
            get;
            //set;
        }
        //
        // *****************************************
        // ****             Methods             ****
        // *****************************************
        //
        //
        //
        //
        // ****             Start()             ****
        /// <summary>
        /// The life cycle of a service begins with Start().  
        /// This begins the service's internal initialization.  It assumes that all other services
        /// (that it may need) have been created, but are not neccessarily running yet.
        /// Note that many services will depend on others (that is, require that another service exists) 
        /// to work properly.
        /// </summary>
        void Start();

        //
        // ****             Connect()           ****
        /// <summary>
        /// After being started, the service is asked to Connect().
        /// Here, it is allowed to connect to the outside world, and upon completing
        /// this phase, it triggers the Connected event.  Announcing that it is fully operational.
        /// </summary>
        void Connect();

        //
        // ****             RequestStop()       ****
        //
        /// <summary>
        /// Initiates the shutdown process.
        /// </summary>
        void RequestStop();




        // *****************************************
        // ****             events              ****
        // *****************************************
        // 
        // TODO: Need to create enum with various state flags, and EventArg for this.
        // Most important are the states: Connected/Disconnected


        //
        // ****         ServiceState Changed            ****        
        //
        /// <summary>
        /// Services should trigger this event whenever their state changes.
        /// And are expected to pass a ServiceStateEventArgs instance.
        /// </summary>
        event EventHandler ServiceStateChanged;


        //
        // ****             Stopping                    ****        
        //
        /// <summary>
        /// Services should trigger this event whenever their state changes.
        /// And are expected to pass a ServiceStateEventArgs instance.
        /// </summary>
        event EventHandler Stopping;                        // Finally triggered after disposing complete.



    }
}
