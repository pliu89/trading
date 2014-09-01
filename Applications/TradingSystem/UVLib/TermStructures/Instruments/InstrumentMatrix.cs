using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UV.Lib.TermStructures.Instruments
{
    using UV.Lib.Products;
    using UV.Lib.Hubs;

    using UV.Lib.DatabaseReaderWriters;
    using UV.Lib.DatabaseReaderWriters.Queries;

    /// <summary>
    /// This class accepts a list of term structure related instruments upon instantiation
    /// and will communicate with the database to create hedge options and ratio's for each instrument
    /// and the resulting instrument it will create.
    /// </summary>
    public class InstrumentMatrix
    {
        #region  Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        // External hubs and objects
        private DatabaseReaderWriter m_DatabaseReaderWriter;
        private LogHub m_Log;                                       // this is the log from the database reader writer

        // Collections
        private List<InstrumentName> m_Instruments;


        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// Caller would like to create a new Instrument matrix for a given list of 
        /// instruments.  This class will use the provided DatabaseReaderWriter to submit
        /// queries and create the need matrix of quote and hedge legs as well as there hedge 
        /// ratios.
        /// </summary>
        /// <param name="instruments"></param>
        /// <param name="dbReaderWriter"></param>
        public InstrumentMatrix(List<InstrumentName> instruments, DatabaseReaderWriter dbReaderWriter)
        {
            m_Instruments = instruments;
            m_DatabaseReaderWriter = dbReaderWriter;
            m_Log = dbReaderWriter.Log;

            //m_DatabaseReaderWriter.QueryResponse += new EventHandler();
            // Need to think about threading issues here...who is going to own 
            // this object and how the collections will be accessed 
        }
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
        /// <summary>
        /// Caller would like to setup all queries to the database and submit
        /// them asyncronously to the database hub.
        /// </summary>
        private void CreateAndSubmitInstrumentQueries()
        {
            for (int i = 0; i < m_Instruments.Count; i++)
            {
                InstrumentInfoQuery instrumentInfoQuery = new InstrumentInfoQuery();
                instrumentInfoQuery.InstrumentName = m_Instruments[i];
                instrumentInfoQuery.IsRead  = true;                                     // we are reading fromt he db, not writing
                m_DatabaseReaderWriter.SubmitAsync(instrumentInfoQuery);
            }
        }
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
