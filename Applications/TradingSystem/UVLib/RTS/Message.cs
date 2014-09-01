using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UV.Lib.RTS
{
    // *********************************************************
    // ****                 Message Class                   ****
    // *********************************************************
    public class Message : EventArgs
    {
        public int rid = -1;
        public List<int> fid = new List<int>();
        public List<string> field = new List<string>();
        public Dictionary<int, string> pairs = new Dictionary<int, string>();   // duplicate info for searches.

        private Message(){}


        // ****             GetContractID               ****
        //
        public int ContractID
        {
            get { return GetIntField(IDinfo.FID.contract_id); }
        }
        public int GetIntField(IDinfo.FID fid)
        {
            int key = (int)fid;
            string s;
            if (this.pairs.TryGetValue(key, out s))
                return Convert.ToInt32(s);
            else
                return -1;
        }
        //
        //
        //
        // ****                 Create()                    ****
        //
        //
        //
        static public Message Create(string rtsRecord)
        {
            string[] fPairs = rtsRecord.Split(RTS.IDinfo.fsChar);
            // TODO: Validate fPairs?
            Message newMsg = new Message();
            newMsg.rid = Convert.ToInt32(fPairs[0]);
            int i = 1;
            while (i + 1 < fPairs.Length)
            {
                int fid = Int32.Parse(fPairs[i]);
                //int fid = Convert.ToInt32(fPairs[i]);
                // update serial pairs
                newMsg.fid.Add(fid);
                newMsg.field.Add(fPairs[i + 1]);
                // update dictionary table
                newMsg.pairs[fid] = fPairs[i+1];

                i += 2;
            }
            return newMsg;
        }//end Create().
        //
        //
        //
        //
        //
        // ****                 ToString()              ****
        //
        public override string ToString()
        {
            StringBuilder outString = new StringBuilder();
            outString.Append(Enum.GetName(typeof(IDinfo.RID), this.rid));
            outString.Append("(");
            //foreach (int key in pairs.Keys)
            //{
            //    string fidStr = Enum.GetName(typeof(IDinfo.FID), key);
            //    //outString.AppendFormat("[{0} {1}]", key, pairs[key]);
            //    outString.AppendFormat("[{0} {1}]", fidStr, pairs[key]);
            //}
            for (int i = 0; i < this.fid.Count; ++i)
            {
                string fidStr = Enum.GetName(typeof(IDinfo.FID), this.fid[i] );
                outString.AppendFormat("[{0} {1}]", fidStr, this.field[i]);
            }
            outString.Append(")");
            return outString.ToString();
        }//end ToString().






    }//end Message class
}
