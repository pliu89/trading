using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace UVTests.Filters
{
    ////using UV.Math.Filters;
    //using UV.Lib.Products;
    //using UV.Lib.DatabaseReaderWriters;
    //using Queries = UV.Lib.DatabaseReaderWriters.Queries;
    ///// <summary>
    ///// Simple way to take a small sample of data and write a csv to test a filter implementation.
    ///// </summary>
    //public partial class FilterTest : Form
    //{

    //    #region Members
    //    // *****************************************************************
    //    // ****                     Members                             ****
    //    // *****************************************************************
    //    //
    //    private DatabaseReaderWriter m_DBReaderWriter = null;
    //    //
    //    // // some of the filters we are testing
    //    ////
    //    //private Ema m_EMA = new Ema(300, 1);
    //    //private EmaHull m_EmaHulll = new EmaHull(300, 1);
    //    //private Sma m_VolatilityZSqSMA = new Sma(300, 1);
    //    //private Sma m_VolatilityZSma = new Sma(300, 1);
    //    //
    //    //
    //    // files to write the output from the filters
    //    //
    //    private string m_EMAfileName = @"C:\Users\pelsasser\Desktop\emaTest.csv";
    //    #endregion// members


    //    #region Constructors
    //    // *****************************************************************
    //    // ****                     Constructors                        ****
    //    // *****************************************************************
    //    //
    //    //
    //    public FilterTest()
    //    {
    //        InitializeComponent();

    //        // Create the database connection to read in data
    //        DatabaseInfo dbInfo = DatabaseInfo.Create(DatabaseInfo.DatabaseLocation.uv1);
    //        m_DBReaderWriter = new DatabaseReaderWriter(dbInfo);
    //        m_DBReaderWriter.QueryResponse += new EventHandler(DatabaseReaderWriter_QueryResponse);


    //    }
    //    //       
    //    #endregion//Constructors


    //    #region no Properties
    //    // *****************************************************************
    //    // ****                     Properties                          ****
    //    // *****************************************************************
    //    //
    //    //
    //    #endregion//Properties


    //    #region no Public Methods
    //    // *****************************************************************
    //    // ****                     Public Methods                      ****
    //    // *****************************************************************
    //    //
    //    //
    //    //
    //    //
    //    //
    //    //
    //    #endregion//Public Methods


    //    #region no Private Methods
    //    // *****************************************************************
    //    // ****                     Private Methods                     ****
    //    // *****************************************************************
    //    //
    //    //
    //    #endregion//Private Methods


    //    #region External Event Handlers
    //    // *****************************************************************
    //    // ****              External Event Handlers                    ****
    //    // *****************************************************************
    //    //
    //    //
    //    //
    //    // *************************************************************
    //    // ****         DatabaseReaderWriter_QueryResponse()        ****
    //    // *************************************************************
    //    private void DatabaseReaderWriter_QueryResponse(object sender, EventArgs eventArgs)
    //    {
    //        if (this.InvokeRequired)
    //            Invoke(new EventHandler(DatabaseReaderWriter_QueryResponse), sender, eventArgs);
    //        else
    //        {
    //            if (eventArgs is Queries.MarketDataQuery)
    //            {
    //                Queries.MarketDataQuery marketDataQuery = (Queries.MarketDataQuery)eventArgs;

    //                //
    //                // Create new filters...so this can be ran multiple times.
    //                //
    //                m_EMA = new Ema(300, 1);
    //                m_EmaHulll = new EmaHull(300, 1);
    //                m_VolatilityZSqSMA = new Sma(300, 1);
    //                m_VolatilityZSma = new Sma(300, 1);
                    
    //                using (var emaFile = System.IO.File.CreateText(m_EMAfileName))
    //                {
    //                    StringBuilder stringBuilder = new StringBuilder();
    //                    stringBuilder.AppendFormat("DataSeries,EMA-Standard,EMA-Hull,m_VolatilityZSmaFromHull,m_VolatilityZSqSMAFromHull,VolatilityFromHull,");
    //                    emaFile.WriteLine(stringBuilder);

    //                    // fake warm up
    //                    m_EMA.CurrentValue = marketDataQuery.Result[0].Price[0];
    //                    m_EmaHulll.EMAHull.CurrentValue = m_EMA.CurrentValue = marketDataQuery.Result[0].Price[0];
    //                    m_EmaHulll.EMAOrig.CurrentValue = m_EMA.CurrentValue = marketDataQuery.Result[0].Price[0];
    //                    m_EmaHulll.EMAShort.CurrentValue = m_EMA.CurrentValue = marketDataQuery.Result[0].Price[0];
                        
    //                    for (int i = 1; i < marketDataQuery.Result.Count; i++)
    //                    {
    //                        double bidPrice = marketDataQuery.Result[i].Price[0];

    //                        //
    //                        // Compute EMA
    //                        //
    //                        m_EMA.Update(bidPrice);
    //                        m_EmaHulll.Update(bidPrice);


    //                        //
    //                        // compute volatility
    //                        //
    //                        double z = bidPrice - m_EmaHulll.CurrentValue;
    //                        m_VolatilityZSma.Update(z);
    //                        m_VolatilityZSqSMA.Update(z * z);
    //                        double volatility = System.Math.Sqrt(m_VolatilityZSqSMA.CurrentValue - m_VolatilityZSma.CurrentValue * m_VolatilityZSma.CurrentValue);

    //                        //
    //                        // Write all values to a .csv
    //                        //
    //                        stringBuilder.Clear();
    //                        stringBuilder.AppendFormat("{0},{1},{2},{3},{4},{5}", bidPrice, m_EMA.CurrentValue, m_EmaHulll.CurrentValue, m_VolatilityZSma.CurrentValue, m_VolatilityZSqSMA.CurrentValue, volatility);
    //                        emaFile.WriteLine(stringBuilder);
    //                    }
    //                }
    //            }

    //        }

    //    }// DatabaseReaderWriter_QueryResponse
    //    //
    //    //
    //    //
    //    #endregion//External Event Handlers


    //    #region Form Event Handlers
    //    // *****************************************************************
    //    // ****                Form Event Handlers                     ****
    //    // *****************************************************************
    //    //
    //    //
    //    //
    //    private void Button_Click(object sender, EventArgs e)
    //    {
    //        if (sender is Button)
    //        {
    //            Button button = (Button)sender;
    //            // Process clicks of buttons.
    //            if (button == this.button1)
    //            {
    //                Queries.MarketDataQuery instrQ = new Queries.MarketDataQuery();
    //                instrQ.InstrumentName = new InstrumentName(new Product("CME", "ES", ProductTypes.Future), "Mar14");
    //                instrQ.MaxRows = 1000;
    //                instrQ.StartDate = new DateTime(2013, 11, 20, 01, 00, 00);
    //                //instrQ.EndDate = new DateTime(2013, 11, 20, 02, 00, 00);
    //                m_DBReaderWriter.SubmitAsync(instrQ);
    //                //listBox1.Items.Add(string.Format("{0}", instrQ.ToString()));                    
    //            }
    //        }
    //    }
    //    //
    //    //
    //    //
    //    //
    //    private void Form_FormClosing(object sender, FormClosingEventArgs e)
    //    {
    //        if (m_DBReaderWriter != null)
    //        {
    //            m_DBReaderWriter.RequestStop();
    //        }

    //    }
    //    //
    //    //
    //    //


    //    #endregion//Form Event Handlers
    //}//end class
}
