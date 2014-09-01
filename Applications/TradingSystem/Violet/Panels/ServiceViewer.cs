using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UV.Violet.Panels
{
    using UV.Lib.Application;
    using UV.Lib.Application.Managers;
    using UV.Lib.Hubs;
    using UV.Lib.Engines;
    using UV.Lib.FrontEnds;

    public partial class ServiceViewer : UserControl
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        private AppServices m_AppServices = null;
        private FrontEndServer m_FrontEndServer = null;
        public bool IsThisControlNeedsUpdating = false;

        //
        private List<ServiceListEntry> m_ServicesNew = new List<ServiceListEntry>();
        



        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public ServiceViewer()
        {
            InitializeComponent();
          
            //this.listBoxServices.DrawMode = DrawMode.OwnerDrawFixed;
            //this.listBoxServices.DrawItem += new DrawItemEventHandler(this.listBoxServices_DrawItem);

            m_AppServices = AppServices.GetInstance();
            m_AppServices.ServiceAdded += new EventHandler(this.AppServices_ServiceAdded);
            m_AppServices.ServiceStopped += new EventHandler(this.AppServices_ServiceStopped);

            // Create new service entries
            List<string> names =  m_AppServices.GetServiceNames();
            if (names.Count > 0)
            {
                ServiceListEntry entry;
                lock (m_ServicesNew)
                {
                    foreach (string name in names)
                        if (TryCreateServiceListEntry(name, out entry))
                            m_ServicesNew.Add(entry);
                }
                UpdateThisControl(this, EventArgs.Empty);
            }

        }//ServiceViewer()
        //
        //
        //
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


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        private void UpdateThisControl(object sender, EventArgs eventArgs)
        {
            if (this.InvokeRequired)
                BeginInvoke(new EventHandler(UpdateThisControl), sender, eventArgs);
            else
            {
                // Check for new services
                lock (m_ServicesNew)
                {
                    if (m_ServicesNew.Count > 0)
                    {
                        this.listBoxServices.SuspendLayout();
                        while (m_ServicesNew.Count > 0)
                        {
                            ServiceListEntry entry = m_ServicesNew[0];
                            entry.Index = this.listBoxServices.Items.Add(entry);
                            m_ServicesNew.RemoveAt(0);
                        }
                        if (this.listBoxServices.SelectedIndex < 0)
                            this.listBoxServices.SelectedIndex = 0;
                        this.listBoxServices.ResumeLayout(false);
                    }
                }
                IsThisControlNeedsUpdating = false;
            }
        }//UpdateThisControl();
        //
        //
        //
        // *****************************************************
        // ****         Create ServiceListEntry()           ****
        // *****************************************************
        /// <summary>
        /// Called each time new service is added to application. Here, it is examined
        /// and added to our list.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="entry"></param>
        /// <returns></returns>        
        private bool TryCreateServiceListEntry( string serviceName, out ServiceListEntry entry)
        {            
            IService iService;
            if (m_AppServices.TryGetService(serviceName, out iService))
            {
                // Discover a frontend server to which we can send requests.
                if (m_FrontEndServer == null && iService is FrontEndServer)
                    m_FrontEndServer = (FrontEndServer)iService;

                // Create a entry element for this service.
                entry = new ServiceListEntry();
                entry.ServiceName = serviceName;
                entry.ServiceClassName = iService.GetType().Name;

                entry.Service = iService;
                if (iService is ForeignService)
                {
                    ForeignService foreignService = (ForeignService)iService;
                    entry.IsForeign = true;
                    string[] s =  foreignService.ClassName.Split('.');
                    entry.ServiceClassName = s[s.Length - 1];
                }
                return true;
            }
            else
            {
                entry = null;
                return false;
            }
        }// TryCreateServiceListEntry()
        //
        //
        //
        // *********************************************
        // ****     UpdateServiceSelection()        ****
        // *********************************************
        private void UpdateServiceSelection()
        {
            int index = listBoxServices.SelectedIndex;
            if (index >= 0 && index < listBoxServices.Items.Count)
            {
                ServiceListEntry entry = (ServiceListEntry) listBoxServices.Items[index];
                textServiceName.Text = entry.ServiceName;
                textServiceType.Text = entry.ServiceClassName;
                if (entry.IsForeign)
                {
                    ForeignService foreignService = (ForeignService) entry.Service;
                    textServiceLocation.Text = "Foreign";//foreignService.Parent.
                }
                else
                    textServiceLocation.Text = "Local";
                
                // Update state of buttons
                if (entry.Service is IEngineHub)
                    buttonLaunchDisplay.Enabled = true;
                else
                    buttonLaunchDisplay.Enabled = false;
                if (entry.Service is Hub)
                    buttonLaunchLogViewer.Enabled = true;
                else
                    buttonLaunchLogViewer.Enabled = false;

            }
        }
        //
        #endregion//Private Methods


        #region Foreign Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        //
        // *****************************************************
        // ****         AppServices_ServiceAdded()          ****
        // *****************************************************
        private void AppServices_ServiceAdded(object sender, EventArgs eventArgs)
        {
            if (eventArgs is AppServiceEventArg)
            {   // A New service has appeared.
                // Create an entry for it, add it to list.
                AppServiceEventArg e = (AppServiceEventArg)eventArgs;
                ServiceListEntry entry;
                if (e.EventType == AppServiceEventType.ServiceAdded && TryCreateServiceListEntry(e.ServiceName,out entry))
                {
                    lock(m_ServicesNew)
                    {
                        m_ServicesNew.Add(entry);
                    }
                    IsThisControlNeedsUpdating = true;
                }
            }
            //
            if (IsThisControlNeedsUpdating)
                UpdateThisControl(this, EventArgs.Empty);


        }// AppServices_ServiceAdded()
        
        //
        //
        // *****************************************************
        // ****         AppServices_ServiceStopped()        ****
        // *****************************************************
        private void AppServices_ServiceStopped(object sender, EventArgs eventArgs)
        {
            if (eventArgs is AppServiceEventArg)
            {

            }
        }// AppServices_ServiceStopped()
        //
        //
        //
        #endregion//Event Handlers




        #region Class ServiceListEntry
        // *****************************************************************
        // ****                     ServiceListEntry                    ****
        // *****************************************************************
        //
        //
        //
        private class ServiceListEntry
        {
            public string ServiceName;
            public string ServiceClassName = string.Empty;
            public int Index = -1;
            public bool IsForeign = false;
            
            public IService Service = null;


            public override string ToString()
            {
                return ServiceName;
            }
        }
        #endregion

        #region Control Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        private void listBoxServices_DrawItem(object sender, DrawItemEventArgs e)
        {
            Color myColor = Color.Red;
            Font myFont = new Font(e.Font, FontStyle.Italic);
            ServiceListEntry service = (ServiceListEntry)listBoxServices.Items[e.Index];
            if (service.IsForeign)
            {
                //using(Brush brush = new SolidBrush(myColor))
                //{                
                //    e.Graphics.DrawString( service.ServiceName , myFont, brush, e.Bounds);
                //}
                e.Graphics.DrawString(service.ServiceName, myFont, new SolidBrush(e.ForeColor), e.Bounds);
            }
            else
            {
                e.Graphics.DrawString(service.ServiceName, e.Font, new SolidBrush(e.ForeColor), e.Bounds);
            }
        }//listBoxServices_DrawItem()
        //
        //
        // *************************************************************
        // ****         listBoxServices_SelectionChanged()          ****
        // *************************************************************
        /// <summary>
        /// User has selected a new service to examine.
        /// </summary>
        private void listBoxServices_SelectionChanged(object sender, EventArgs e)
        {
            UpdateServiceSelection();
        }
        //
        //
        //
        //
        //
        // *****************************************
        // ****         Button_Click()          ****
        // *****************************************
        /// <summary>
        /// This manages all button clicks on this control.
        /// </summary>
        private void Button_Click(object sender, EventArgs e)
        {
            if (m_FrontEndServer != null)
                m_FrontEndServer.Log.NewEntry(LogLevel.Major, "Violet.Button_Click: {0} {1}", ((Control)sender).Name, e);
            
            // Determine which service is selected.
            ServiceListEntry selectedServiceEntry = null;
            if (listBoxServices.SelectedItem != null)
                selectedServiceEntry = (ServiceListEntry)listBoxServices.SelectedItem;
            
            // Determine which button was clicked.
            if (sender == buttonLaunchDisplay)
            {
                // If service is an engine hub, request its display.
                if (selectedServiceEntry!=null && selectedServiceEntry.Service is IEngineHub)
                    m_FrontEndServer.TryRequestDisplay(selectedServiceEntry.ServiceName);
            }
            else if (sender == buttonLaunchLogViewer)
            {
                // If service is a local hub, open its log viewer.
                if (selectedServiceEntry != null && selectedServiceEntry.Service is Hub)
                    ((Hub)selectedServiceEntry.Service).Log.IsViewActive = true;
            }
            
        }//
        //
        //
        //
        #endregion//Control Event Handlers


    }//end class
}
