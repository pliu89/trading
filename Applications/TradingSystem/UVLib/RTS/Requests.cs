using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;



namespace UV.Lib.RTS
{
    public class Requests
    {

        // *****************************************************************************
        // ****                         Static Functions                            ****
        // *****************************************************************************
        //
        //
        // ****             BuildRequest              ****
        //
        /// <summary>
        /// Acceptable sets of arguments:
        /// 1.  RID.ctr_req_load_ctr_symbol, FID.symbol, "ED"   gets all contracts with this symbol
        /// 2.  RID.ctr_req_load_underlying_ctr_id, FID.underlying_contract_id, "116" gets all contracts/options
        ///     based on that underlying contract.
        /// </summary>
        /// <param name="requestID"></param>
        /// <param name="fieldID1"></param>
        /// <param name="fieldValue1"></param>
        /// <param name="contextID"></param>
        /// <returns></returns>
        public static string RequestBuilder(IDinfo.RID requestID, IDinfo.FID fieldID1, string fieldValue1, int contextID)
        {
            StringBuilder requestString = new StringBuilder();
            // int rid = (int)IDinfo.RID.ctr_req_load_ctr_symbol;
            //int rid = (int)IDinfo.RID.ctr_req_load_underlying_ctr_id;
            int rid = (int)requestID;
            int fid = (int)fieldID1;
            requestString.Append(rid);
            requestString.Append(IDinfo.fs);

            requestString.Append((int)IDinfo.FID.context); requestString.Append(IDinfo.fs);
            requestString.Append(contextID); requestString.Append(IDinfo.fs);

            requestString.Append((int)IDinfo.FID.update);
            requestString.Append(IDinfo.fs);
            requestString.Append("1");
            requestString.Append(IDinfo.fs);

            requestString.Append(fid);
            requestString.Append(IDinfo.fs);
            requestString.Append(fieldValue1);
            requestString.Append(IDinfo.rs);

            return requestString.ToString();

        }
        //
        //
        //
        //
        
        
        
        
        
        
        // exchange request
        public static string ExchangeRequest()
        {
            StringBuilder request = new StringBuilder();
            int rid = (int) IDinfo.RID.exchange_req_load_all;
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.context);
            request.Append(IDinfo.fs);
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.update);
            request.Append(IDinfo.fs);
            request.Append("1");
            request.Append(IDinfo.rs);
            return request.ToString();
        }
        // Login request
        public static string LoginReq(string name, string psw)
        {
            StringBuilder request = new StringBuilder();
            int rid = (int)IDinfo.RID.login_req;
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.context);
            request.Append(IDinfo.fs);
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.usr_name);
            request.Append(IDinfo.fs);
            request.Append(name);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.password);
            request.Append(IDinfo.fs);
            request.Append(psw);
            request.Append(IDinfo.rs);
            return request.ToString();
        }
        // acc request
        public static string AcctReq()
        {
            StringBuilder request = new StringBuilder();
            int rid = (int) IDinfo.RID.acc_req_load_all;
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.context);
            request.Append(IDinfo.fs);
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.update);
            request.Append(IDinfo.fs);
            request.Append("1");
            request.Append(IDinfo.rs);

            return request.ToString();
        }
        // request for whole contract info with the user input symbol
        public static string CtrSymReq(string symbol)
        {
            StringBuilder request = new StringBuilder();
            int rid = (int) IDinfo.RID.ctr_req_load_ctr_symbol;
            int fid = (int)IDinfo.FID.symbol;
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.context);
            request.Append(IDinfo.fs);
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.update);
            request.Append(IDinfo.fs);
            request.Append("1");
            request.Append(IDinfo.fs);
            request.Append(fid);
            request.Append(IDinfo.fs);
            request.Append(symbol);
            request.Append(IDinfo.rs);
            return request.ToString();

        }
        //
        //
       
        //
        //
        //

        // request for whole contract info with the user input symbol
        public static string CtrTypeReq(int type)
        {
            StringBuilder request = new StringBuilder();
            int rid = (int) IDinfo.RID.ctr_req_load_contract_type;
            int fid = (int)IDinfo.FID.contract_type;
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.context);
            request.Append(IDinfo.fs);
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.update);
            request.Append(IDinfo.fs);
            request.Append("1");
            request.Append(IDinfo.fs);
            request.Append(fid);
            request.Append(IDinfo.fs);
            request.Append(type);
            request.Append(IDinfo.rs);
            return request.ToString();

        }

        public static string UserReq()
        {
            StringBuilder request = new StringBuilder();
            int rid = (int) IDinfo.RID.usr_req_load_all;
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.context);
            request.Append(IDinfo.fs);
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.update);
            request.Append(IDinfo.fs);
            request.Append("1");
            request.Append(IDinfo.rs);

            return request.ToString();
        }

        //reqeuest for topbook info upon the contract id
        // for update 0:Request without affecting subscriptions
        //            1:Request and subscribe for updates
        //            2:Unsubscribe from updates
        public static string TopBookReq(int contractid, int update, int contextID)
        {
            StringBuilder request = new StringBuilder();
            request.Append( (int) IDinfo.RID.price_req_load_ctr_id );	
			request.Append(IDinfo.fs);	
            request.Append((int) IDinfo.FID.context);		
			request.Append(IDinfo.fs);
            request.Append(contextID);						
			request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.update);		
			request.Append(IDinfo.fs);
            request.Append(update);							
			request.Append(IDinfo.fs);
			request.Append((int)IDinfo.FID.contract_id);	
			request.Append(IDinfo.fs);
            request.Append(contractid);						
			request.Append(IDinfo.rs);
            return request.ToString();
        }
        //request for compelete book
        public static string CompleteBookReq(int contractid, int update)
        {
            StringBuilder request = new StringBuilder();
            int rid = (int) IDinfo.RID.market_load_ctr_ids;
            int fid = (int)IDinfo.FID.contract_id;
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.context);
            request.Append(IDinfo.fs);
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.update);
            request.Append(IDinfo.fs);
            request.Append(update);
            request.Append(IDinfo.fs);
            request.Append(fid);
            request.Append(IDinfo.fs);
            request.Append(contractid);
            request.Append(IDinfo.rs);
            return request.ToString();

        }

        //request for historical date using the contract id
        public static string HistoryDataReq(int contractid, int update)
        {
            StringBuilder request = new StringBuilder();
            int rid = (int) IDinfo.RID.hist_req_load_last_ctr;
            int fid = (int)IDinfo.FID.contract_id;
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.context);
            request.Append(IDinfo.fs);
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.update);
            request.Append(IDinfo.fs);
            request.Append(update);
            request.Append(IDinfo.fs);
            request.Append(fid);
            request.Append(IDinfo.fs);
            request.Append(contractid);
            request.Append(IDinfo.rs);
            return request.ToString();

        }

        // request for a list of all existing orders.
        public static string OrderAllReg(int update)
        {
            StringBuilder request = new StringBuilder();
            int rid = (int) IDinfo.RID.order_req_load_all;

            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.context);
            request.Append(IDinfo.fs);
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.update);
            request.Append(IDinfo.fs);
            request.Append(update);
            request.Append(IDinfo.rs);
            return request.ToString();

        }

        //request for history order
        public static string OrderHistAllReq(int update)
        {
            int rid = (int) IDinfo.RID.ord_hist_req_load_all;
            StringBuilder request = new StringBuilder();
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.context);
            request.Append(IDinfo.fs);
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.update);
            request.Append(IDinfo.fs);
            request.Append(update);
            request.Append(IDinfo.rs);
            return request.ToString();
        }

        //request for positon
        public static string PositionAllReq(int update)
        {
            StringBuilder request = new StringBuilder();
            int rid = (int) IDinfo.RID.pos_req_load_all;
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.context);
            request.Append(IDinfo.fs);
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.update);
            request.Append(IDinfo.fs);
            request.Append(update);
            request.Append(IDinfo.rs);
            return request.ToString();
        }
        //request for positon for particular account
        public static string PositionAccReq(int accoutid, int update)
        {
            StringBuilder request = new StringBuilder();
            int rid = (int) IDinfo.RID.pos_req_load_account;
            int fid = (int)IDinfo.FID.account_id;
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.context);
            request.Append(IDinfo.fs);
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.update);
            request.Append(IDinfo.fs);
            request.Append(update);
            request.Append(IDinfo.fs);
            request.Append(fid);
            request.Append(IDinfo.fs);
            request.Append(accoutid);
            request.Append(IDinfo.rs);
            return request.ToString();
        }

        //request for limit_all
        public static string LimitAllReq()
        {
            StringBuilder request = new StringBuilder();
            int rid = (int) IDinfo.RID.limit_req_load_all;
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.context);
            request.Append(IDinfo.fs);
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.update);
            request.Append(IDinfo.fs);
            request.Append("0");
            request.Append(IDinfo.rs);
            return request.ToString();
        }

        public static string TradeAccReq(int accountid, int update)
        {
            StringBuilder request = new StringBuilder();
            int rid = (int) IDinfo.RID.trade_req_load_trade_account_date;
            int fid = (int)IDinfo.FID.account_id;
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.context);
            request.Append(IDinfo.fs);
            request.Append(rid);
            request.Append(IDinfo.fs);
            request.Append((int) IDinfo.FID.update);
            request.Append(IDinfo.fs);
            request.Append(update);
            request.Append(IDinfo.fs);
            request.Append(fid);
            request.Append(IDinfo.fs);
            request.Append(accountid);
            request.Append(IDinfo.rs);
            return request.ToString();

        }





    }
}
