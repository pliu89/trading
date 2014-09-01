using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;

namespace UVTests.DatabaseReaderWriters
{
    using UV.Lib.Products;
    using UV.Lib.DatabaseReaderWriters;
    using Queries = UV.Lib.DatabaseReaderWriters.Queries;

    public partial class Test1 : Form
    {
        #region no Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        private DatabaseReaderWriter m_DBReaderWriter = null;

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public Test1()
        {
            InitializeComponent();

            // Create the database connection.
            DatabaseInfo dbInfo = DatabaseInfo.Create(DatabaseInfo.DatabaseLocation.apastor);
            m_DBReaderWriter = new DatabaseReaderWriter(dbInfo);
            m_DBReaderWriter.QueryResponse += new EventHandler(DatabaseReaderWriter_QueryResponse);
            

            // Request info
            //Queries.Instruments instrQ = new Queries.Instruments();
            //instrQ.InstrumentName = new InstrumentName(new Product("CME", "ZT", ProductTypes.Future), "H4");
            //m_DBReaderWriter.SubmitAsync(instrQ);

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


        #region External Event Handlers
        // *****************************************************************
        // ****              External Event Handlers                    ****
        // *****************************************************************
        //
        //
        //
        // *************************************************************
        // ****         DatabaseReaderWriter_QueryResponse()        ****
        // *************************************************************
        private void DatabaseReaderWriter_QueryResponse(object sender, EventArgs eventArgs)
        {
            if (this.InvokeRequired)
                Invoke(new EventHandler(DatabaseReaderWriter_QueryResponse), sender, eventArgs);
            else
            {
                listBox1.Items.Add(string.Format("{0}",eventArgs.ToString()) );

            }

        }// DatabaseReaderWriter_QueryResponse
        //
        //
        //
        #endregion//External Event Handlers

        
        #region Form Event Handlers
        // *****************************************************************
        // ****                Form Event Handlers                     ****
        // *****************************************************************
        //
        //
        //
        private void Button_Click(object sender, EventArgs e)
        {
            if (sender is Button)
            {
                Button button = (Button)sender;
                // Process clicks of buttons.
                if (button == this.button1)
                {
                    Queries.MarketDataQuery instrQ = new Queries.MarketDataQuery();
                    instrQ.InstrumentName = new InstrumentName(new Product("CME", "ZT", ProductTypes.Future), "Mar14");
                    instrQ.MaxRows = 3000;
                    instrQ.StartDate = new DateTime(2013, 11, 20, 01, 00, 00);
                    //instrQ.EndDate = new DateTime(2013, 11, 20, 02, 00, 00);
                    m_DBReaderWriter.SubmitSync(instrQ);
                    listBox1.Items.Add(string.Format("{0}", instrQ.ToString()));                    
                }
            }
        }
        //
        //
        //
        //
        private void Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (m_DBReaderWriter != null)
            {
                m_DBReaderWriter.RequestStop();
            }

        }
        //
        //
        //


        #endregion//Form Event Handlers

    }//end class
}
