using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace UV.Lib.Socket
{
    public class SocketManager2 : SocketManager
    {


        protected override void ProcessMessage(ref string newMessage, ref List<string> completeMessages, ref string unfinishedMessage)
        {
            int startIdx = 0;
            int endIdx = 9;
            int lengOfS = newMessage.Length - 1;
            int LengtMsg = 0;
            string msgPresent = string.Empty;


            while (endIdx <= lengOfS)
            {

                LengtMsg = Convert.ToInt16(newMessage.Substring(startIdx, 10));
                startIdx = endIdx + 1;
                endIdx = endIdx + LengtMsg;

                if (endIdx <= lengOfS)
                {
                    msgPresent = newMessage.Substring(startIdx, LengtMsg);
                    if (msgPresent.ToLower().Contains("ping"))
                    {
                        msgPresent = msgPresent.Replace('{', '|');
                        msgPresent = msgPresent.Replace('}', '|');
                        msgPresent = msgPresent.Trim('|');
                        string[] part = msgPresent.Split('|');
                        Dictionary<string, string> dictForImpKeys = new Dictionary<string, string>();
                        string[] subparts2 = null;
                        int lengDictImp = 0;
                        for (int partIdx = 0; partIdx < part.Length; partIdx++)
                        {
                            if (!string.IsNullOrEmpty(part[partIdx]))
                            {
                                subparts2 = part[partIdx].Split('=');
                                dictForImpKeys.Add(subparts2[0].ToLower(), subparts2[1]);
                                lengDictImp = lengDictImp + 1;
                            }
                        }
                            string blahblah = "{message_info={message_type=event_send}|event_path=apollo_ge.tramp|value={msgID=" + dictForImpKeys["msgid"] + "|status=confirmed|msgType=ping|contractID=apollo}}";
                            string appendableStr = Convert.ToString(blahblah.Length);
                            int appendableStrLength = 10 - appendableStr.Length;
                            StringBuilder msgBuilder = new StringBuilder();  
                            for (int iii = 0; iii < appendableStrLength; iii++)
                            { msgBuilder.Append("0"); }
                            msgBuilder.Append(appendableStr);
                            msgBuilder.Append(blahblah);

                            Send(msgBuilder.ToString());
                    }
                    else
                    {
                    //OnMessageReceived(msgPresent);     // Report the actual message
                    //OnInternalMessage(String.Format("Recv: {0}", msgPresent)); // report to log  
                    completeMessages.Add(msgPresent);
                    }
                    startIdx = endIdx + 1;
                    endIdx = endIdx + 10;
                }
                else
                {
                    unfinishedMessage = newMessage.Substring(startIdx - 10, lengOfS - startIdx + 11);
                }


            }

            if (String.IsNullOrEmpty(unfinishedMessage))
            {
                if (startIdx <= lengOfS)
                {
                    unfinishedMessage = newMessage.Substring(startIdx, lengOfS - startIdx + 1);
                }
            }
        }//ProcessMessage().

        public override void SendLoginMessage()
        {
           

                string blahblah = "{message_info={message_type=login}|login_id=hunai01|password=}";
                //string blahblah = "{message_info={message_type=login}|login_id=hunai02|password=}";
                //string blahblah = "{liquidator_version=7.1.25|message_info={message_type=login}|login_id=hunai01|password=}";
                //string blahblah = "{liquidator_version=7.1.25|message_info={message_type=login}|login_id=hunai02|password=}";
                string appendableStr = Convert.ToString(blahblah.Length);
                int appendableStrLength = 10 - appendableStr.Length;
                StringBuilder newMsg11 = new StringBuilder();
                for (int i = 0; i < appendableStrLength; i++)
                { newMsg11.Append("0"); }
                newMsg11.Append(appendableStr);
                newMsg11.Append(blahblah);
                Send(newMsg11.ToString());
                blahblah = "{message_info={message_type=event_subscribe}|event_path=apollo_ge}";
                appendableStr = Convert.ToString(blahblah.Length);
                appendableStrLength = 10 - appendableStr.Length;
                StringBuilder newMsg12 = new StringBuilder();
                for (int i = 0; i < appendableStrLength; i++)
                { newMsg12.Append("0"); }
                newMsg12.Append(appendableStr);
                newMsg12.Append(blahblah);
                Send(newMsg12.ToString());
                //SendTrimMessage(  "0000000066{message_info={message_type=event_subscribe}|event_path=apollo_ge}");
                //blahblah = "0000000130{message_info={message_type=event_send}|event_path=apollo_assistant_ge.tramp|value={contract_id3=CMEGEH1-GEM1|volume=10|cs=0.225}}";
                //SendTrimMessage(blahblah);
                //blahblah = "0000000130{message_info={message_type=event_send}|event_path=apollo_assistant_ge.tramp|value={contract_id3=CMEGEH1-GEM1|volume=10|cs=0.220}}";
                //SendTrimMessage(blahblah);
                /*blahblah = "{message_info={message_type=event_send}|event_path=apollo_ge.tramp|value={msgID=10000|status=request|msgType=trading|contractID=CMEGEH1-GEM1|params={bVolQT=10|bCSij=0.225|aVolQT=10|aCSij=0.235}}}";
                appendableStr = Convert.ToString(blahblah.Length);
                appendableStrLength = 10 - appendableStr.Length;
                StringBuilder newMsg13 = new StringBuilder(); 
                for (int i = 0; i < appendableStrLength; i++)
                { newMsg13.Append("0"); }
                newMsg13.Append(appendableStr);
                newMsg13.Append(blahblah);
                SendTrimMessage(newMsg13.ToString());*/
                //blahblah  = "0000000036{message_info={message_type=logout}}";
                //SendTrimMessage(blahblah);

        }//end Initialize()


    }
}
