using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace UV.Lib.RTS
{
    public class RTSContract
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        private static IDinfo.FID[] GuaranteedFIDList = null;        
        private Dictionary<int, string> m_Fields = new Dictionary<int, string>();


        private int m_MaxMarketDepthEventThreshold = 0;

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public RTSContract(Message rtsMessage)
        {            
            InitializeFIDList();
            // Validate the message.
            if ((IDinfo.RID)rtsMessage.rid == IDinfo.RID.contract)		// make sure this is a contract msg.
                Initialize(rtsMessage.fid, rtsMessage.field);	
        }
        //
        public RTSContract()
        {
            InitializeFIDList();
            
        }
        //
        //
        //
        private void InitializeFIDList()
        {
            if (GuaranteedFIDList == null)
            {
                GuaranteedFIDList = new IDinfo.FID[]{IDinfo.FID.symbol,IDinfo.FID.isin,
                    IDinfo.FID.exchange_contract_id,IDinfo.FID.contract_group,
                    IDinfo.FID.tick_size,IDinfo.FID.value,
                    IDinfo.FID.expiration_date,IDinfo.FID.expiration_month,
                    IDinfo.FID.contract_id};
            }
        }
        //
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
		//
		// These elements are guarenteed to exist.
		//
        public string Symbol
        {
            get{ return m_Fields[(int) IDinfo.FID.symbol]; }
        }
        public string ISIN
        {
            get { return m_Fields[(int) IDinfo.FID.isin]; }
        }
        public string Name
        {
            get { return m_Fields[(int) IDinfo.FID.exchange_contract_id]; }
        }
        public string Group
        {
            get { return m_Fields[(int) IDinfo.FID.contract_group]; }
        }
		//
		// These elements are not guarenteed.
		//
        public double TickSize
        {
            get
            {
                string s = (m_Fields[(int) IDinfo.FID.tick_size]);
                if (string.IsNullOrEmpty(s))
                    return 0;
                else
                    return Convert.ToDouble(s);
            }
        }
        public double TickValue
        {
            get
            {
                string s = (m_Fields[(int)IDinfo.FID.contract_size]);
                if (string.IsNullOrEmpty(s))
                    return 0;
                else
                {
                    double x = Convert.ToDouble(s);     // dollar per unit value change  [$/dp]
                    double y = this.TickSize;           // value change per tick         [dp/dtick]
                    return x * y;                       // dollar per tick change = [$/dtick]
                }
                    
            }
        }
        public int RTS_ID
        {
            get 
            {
                string s = (m_Fields[(int) IDinfo.FID.contract_id]);
                if ( string.IsNullOrEmpty(s) )
                    return -1;
                else
                    return Convert.ToInt32(s); 
            }
        }
		//
		//
		public DateTime ExpirationDateTime
		{
			get
			{
				string s;
				if (m_Fields.TryGetValue((int)IDinfo.FID.expiration_date, out s) && ! string.IsNullOrEmpty(s))
				{
					try
					{
						int yr = Convert.ToInt32(s.Substring(0, 4));
						int mo = Convert.ToInt32(s.Substring(4, 2));
						int day = Convert.ToInt32(s.Substring(6, 2));
						DateTime expiryDate = new DateTime(yr, mo, day);
						if (m_Fields.TryGetValue((int)IDinfo.FID.expiration_time, out s))
						{
							int hr = Convert.ToInt32(s.Substring(0, 2));
							int min = Convert.ToInt32(s.Substring(2, 2));
							int sec = Convert.ToInt32(s.Substring(4, 2));
							expiryDate = expiryDate.Add(new TimeSpan(hr, min, sec));

						}
						else
							expiryDate = expiryDate.AddDays(1.0);		// push to midnight of expiration date, if time unknown.
						return expiryDate;						
					}
					catch (Exception)
					{
						return DateTime.MaxValue.AddDays(-1000);
					}
				}
				else
					return DateTime.MaxValue.AddDays(-1000);	// this means never expire. Offset useful for later analysis.
			}
		}
		public string ExpirationDateTimeString
		{
			get
			{	DateTime dt = ExpirationDateTime;
				return string.Format("{0} {1}",dt.ToShortDateString(),dt.ToShortTimeString());
			}
		}
		//
		//
        public int MaxMarketDepthEventThreshold
        {
            get { return m_MaxMarketDepthEventThreshold; }
            set { m_MaxMarketDepthEventThreshold = value; }
        }
        //
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
        // ****             CopyTo()                ****
        //
        public virtual void CopyTo(RTSContract aContract)
        {
            aContract.m_Fields.Clear();
            aContract.Initialize(this.m_Fields.Keys.ToList(), this.m_Fields.Values.ToList());

        }//end CopyTo()
        //
        //
		public bool TryGetValue(IDinfo.FID fieldID, out string value)
		{
			int id = (int) fieldID;
			return (m_Fields.TryGetValue(id, out value));
		}
		//
		//
        //
        // ****             ToString()              ****
        //
        public override string ToString()
        {
			if (string.IsNullOrEmpty(this.Name)) { return this.Symbol; }
            return this.Name;
        }
        //
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
//        private void Initialize(Dictionary<int,string> fields)
        private void Initialize(List<int> fidList, List<string> fields)
        {
            //
            // Extract the parameters.
            //
            for (int i = 0; i < fidList.Count; ++i)
            {
                int fid = fidList[i];
                if (this.m_Fields.Keys.Contains(fid))
                    this.m_Fields[fid] = fields[i];
                else
                    this.m_Fields.Add(fid, fields[i]);
            }
            //foreach (int fid in fields.Keys)
            //{
            //    if (this.m_Fields.Keys.Contains(fid))
            //        this.m_Fields[fid] = fields[fid];
            //    else
            //        m_Fields.Add(fid, fields[fid]);
            //}
            //
            // Finalize
            //
            // make sure that the important FID's exist.
            foreach (IDinfo.FID fid in RTSContract.GuaranteedFIDList)
            {
                int i = (int)fid;
                if (!m_Fields.Keys.Contains(i))  m_Fields.Add(i, string.Empty); 
            }

        }
        //
        #endregion//Private Methods

        #region no Static Functions
        // *****************************************************************
        // ****                     Static Methods                      ****
        // *****************************************************************
        //
        //
        //
        #endregion//end static functions

    }
}
