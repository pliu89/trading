using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;

namespace MrData
{
    using Misty.Lib.Application;
    using Misty.Lib.Hubs;
    using Misty.Lib.IO.Xml;
    using Misty.Lib.MarketHubs;
    using Misty.Lib.BookHubs;

    /// <summary>
    /// Procedure:
    ///     1) A MrData instance is created and added as a IService.
    ///     2) It is loaded with InstrumentTickets.
    ///     3) Call MrData.Start():
    ///         a) Searches for needed IServices.
    /// </summary>
    public class MrData : Hub , IService , IStringifiable
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // 
        // Global variables and Services
        private static int m_LastMrDataId = -1;                 // Ensures that each MrData instance has unique name.
        private MarketHub m_MarketHub = null;

        // Internal controls
        private List<InstrumentTicket> m_InstrumentTickets = new List<InstrumentTicket>();


        //        
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public MrData()
             : base( string.Format("MrData{0}", Interlocked.Increment(ref MrData.m_LastMrDataId)) , AppInfo.GetInstance().LogPath, false, LogLevel.ShowAllMessages)
        {

        }
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


        #region no Public Methods
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


        #region Event Handler and Processing
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        protected override void HubEventHandler(EventArgs[] e)
        {
            
        }
        //
        #endregion//Event Handlers


        #region IStringifiable
        // *****************************************************************
        // ****                    IStringifiable                       ****
        // *****************************************************************
        string IStringifiable.GetAttributes()
        {
            return string.Empty;
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            List<IStringifiable> elements = new List<IStringifiable>();
            foreach (InstrumentTicket ticket in m_InstrumentTickets)
                elements.Add(ticket);

            return elements;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
            if (subElement is InstrumentTicket)
                m_InstrumentTickets.Add((InstrumentTicket)subElement);
        }        
        //
        #endregion//IStringifiable


        #region IService
        // *****************************************************************
        // ****                    IService                             ****
        // *****************************************************************
        string IService.ServiceName
        {
            get { return base.m_HubName; }
        }
        void IService.Start()
        {
            // Search for necessary services.
            AppServices app = AppServices.GetInstance();
            foreach (IService iservice in app.GetServices())
                if (iservice is MarketHub)
                {
                    m_MarketHub = (MarketHub)iservice;
                    break;
                }            


            base.Start();       // Start this Hub.
            
        }
        void IService.Connect()
        {
            throw new NotImplementedException();
        }
        public override void RequestStop()
        {
            
        }
        event EventHandler IService.ServiceStateChanged
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }
        //
        //
        #endregion//IService








    }
}
