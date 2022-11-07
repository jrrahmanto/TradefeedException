using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTradefeedException
{
    class Program
    {
        public static string chat_id = "6289630870658@c.us";
        static void Main(string[] args)
        {
            var dr_cm = new DataSetProdTableAdapters.ClearingMemberTableAdapter();
            var dr_sp = new DataSetProdTableAdapters.Get_Tradefeed_ExceptionTableAdapter();
            var dt_sp = dr_sp.GetData(Convert.ToDateTime("2022-06-30"));
            foreach (var item in dt_sp)
            {
                var x = item.BrokerCode;
                var y = item.TraderCode;
                var code_broker = dr_cm.GetDataByCode(item.BrokerCode);
                var code_trader = dr_cm.GetDataByCode(item.TraderCode);
                //var commodityId = dr_cdt.GetDataByCode(item.CommodityCode);
                var xy = (code_broker[0].CMID + " " + code_trader[0].CMID + " " + item.BusinessDate);
            }


            var dr_contract = new DataSetDevTableAdapters.ContractTableAdapter();
            var drc = new DataSetProdTableAdapters.CommodityTableAdapter();
            var drs = new DataSetProdTableAdapters.SubCategoryTableAdapter();

            List<int> messageNumber = new List<int>();
            var wa = new DataSetProdTableAdapters.Whatsapp_parameterTableAdapter();
            var dt_wa = wa.GetDataById(2);
            //chat_id = dt_wa[0].parameter;

            int lognumber = 0;
            string file = AppDomain.CurrentDomain.BaseDirectory + "\\Setting\\logLastMessage.txt";
            string[] text = File.ReadAllLines(file);
            messageNumber.Add(Convert.ToInt32(text[0]));

            CultureInfo culture = new CultureInfo("es-ES");

            WebClient client = new WebClient();
            string message = GetMessageList(chat_id, messageNumber[messageNumber.Count - 1]);
            ResponseChat rc = JsonConvert.DeserializeObject<ResponseChat>(message);
            var dr = new DataSetDevTableAdapters.Get_Tradefeed_ExceptionTableAdapter();
            var dt = dr.GetData(DateTime.Parse("2020-10-27"));
            List<string> msg = new List<string>();

            if (dt.Count != 0)
            {
                foreach (var item in dt)
                {
                    var dtc = drc.GetDataByCommodityCode(item.CommodityCode);
                    var CommodityID = dtc[0].CommodityID;

                    var dt_contract = dr_contract.GetDataByFilter(CommodityID, Convert.ToInt32(item.Contract_Year), Convert.ToInt32(item.Contract_Month));
                    if (dt_contract.Count == 0)
                    {
                        msg.Add("/e Commodity Code : " + item.CommodityCode + " Contract Month : " + item.Contract_Month + " Contract Year : " + item.Contract_Year);
                    }
                }

                SendMessage(chat_id, "Di temukan " + msg.Count + " exception tanggal " + DateTime.Now.ToString("dd-MMM-yyyy") + " :", "");
                foreach (var item in msg)
                {
                    SendMessage(chat_id, item, "");
                }
                SendMessage(chat_id, "Reply pesan di atas dengan format dd/MM/yyyy \n Effective_Start_Date \n Start_Date \n Start_Spot \n End_Spot \n Effective_End_Date", "");
            }
            //batas

            foreach (MessageChat mc in rc.messages)
            {
                lognumber = mc.messageNumber;
                if (mc.author != "6285772388368@c.us")
                {
                    try
                    {
                        string[] arrmcbody = new string[50];
                        if (mc.quotedMsgBody != null)
                        {
                            arrmcbody = mc.quotedMsgBody.Split(' ');
                        }
                        
                        if (arrmcbody[0] == "/e")
                        {
                            string[] arrbody = mc.body.Split('\n');

                            var dtc = drc.GetDataByCommodityCode(arrmcbody[4]);
                            var CommodityID = dtc[0].CommodityID;

                            Decimal SubCategoryId = dr_contract.GetData().OrderByDescending(x => x.ContractID).Where(x=>x.CommodityID == CommodityID).Select(x=>x.SubCategoryID).FirstOrDefault();


                            DateTime efective_start_date = Convert.ToDateTime(arrbody[0], culture);
                            DateTime start_date = Convert.ToDateTime(arrbody[1], culture);
                            DateTime end_spot = Convert.ToDateTime(arrbody[3], culture);
                            DateTime start_spot = Convert.ToDateTime(arrbody[2], culture);
                            DateTime efective_end_date = Convert.ToDateTime(arrbody[4], culture);

                            dr_contract.InsertQuery(CommodityID,
                                               Convert.ToInt32(arrmcbody[8]),
                                               Convert.ToInt32(arrmcbody[12]),
                                               "A",
                                               efective_start_date,
                                               dtc[0].ContractSize,
                                               dtc[0].SettlementType,
                                               "",
                                               dtc[0].Unit,
                                               start_date,
                                               end_spot,
                                               start_spot,
                                               dtc[0].PEG,
                                               dtc[0].VMIRCACalType,
                                               dtc[0].SettlementFactor,
                                               dtc[0].DayRef,
                                               dtc[0].Divisor,
                                               dtc[0].MarginTender,
                                               dtc[0].MarginSpot,
                                               dtc[0].MarginRemote,
                                               dtc[0].CalSpreadRemoteMargin,
                                               dtc[0].IsKIE,
                                               "Robot System",
                                               DateTime.Now,
                                               "Robot System",
                                               DateTime.Now,
                                               efective_end_date,
                                               dtc[0].ApprovalDesc,
                                               dtc[0].HomeCurrencyID,
                                               dtc[0].CrossCurr,
                                               dtc[0].CrossCurrProduct,
                                               null,
                                               dtc[0].ActionFlag,
                                               dtc[0].TenderReqType,
                                               SubCategoryId,
                                               dtc[0].ModeK1,
                                               dtc[0].ValueK1,
                                               dtc[0].ContractRefK1,
                                               dtc[0].ModeK2,
                                               dtc[0].ValueK2,
                                               dtc[0].ContractRefK2,
                                               dtc[0].ModeD,
                                               dtc[0].ValueD,
                                               dtc[0].ContractRefD,
                                               dtc[0].ModeIM,
                                               dtc[0].PercentageSpotIM,
                                               dtc[0].PercentageRemoteIM,
                                              dtc[0].ModeFee);


                            SendMessage(chat_id, "Process Success "+ DateTime.Now.ToString("HH:mm:ss") + " \nCommodity ID: " + CommodityID +
                                                                       "\nMonth : " + Convert.ToInt32(arrmcbody[8]) +
                                                                       "\nYear : " + Convert.ToInt32(arrmcbody[12]) +
                                                                       "\nEffective Start Date : " + arrbody[0] +
                                                                       "\nContract Size : " + dtc[0].ContractSize +
                                                                       "\nSettlementType : " + dtc[0].SettlementType +
                                                                       "\nUnit : " + dtc[0].Unit +
                                                                       "\nStart Date :" + arrbody[1] +
                                                                       "\nEnd Spot : " + arrbody[3] +
                                                                       "\nStart Spot : " + arrbody[2] +
                                                                       "\nEffective End Date : " + efective_end_date.ToString("dd/MMM/yyyy")+
                                                                       "\nPEG : " + dtc[0].PEG +
                                                                       "\nVM IRCA Cal Type : " + dtc[0].VMIRCACalType +
                                                                       "\nSettlement Factor :" + dtc[0].SettlementFactor +
                                                                       "\nDay Ref : " + dtc[0].DayRef +
                                                                       "\nDivisor : " + dtc[0].Divisor +
                                                                       "\nMargin Tender : " + dtc[0].MarginTender +
                                                                       "\nMargin Spot : " + dtc[0].MarginSpot +
                                                                       "\nMargin Remote : " + dtc[0].MarginRemote +
                                                                      "\nMode Fee : " + dtc[0].ModeFee, mc.id);

                        }
                    }
                    catch (Exception ex)
                    {
                        SendMessage(chat_id, "Process Fail : " + ex.Message, mc.id);
                        //masukin last message numbernya
                        string text1 = System.IO.File.ReadAllText(file);
                        text1 = lognumber.ToString();
                        System.IO.File.WriteAllText(file, text1);
                    }

                }
                
                //masukin last message numbernya
                string text2 = System.IO.File.ReadAllText(file);
                text2 = lognumber.ToString();
                System.IO.File.WriteAllText(file, text2);
            }
        }

        public class ResponseChat
        {
            public IEnumerable<MessageChat> messages { get; set; }
            public int lastMessageNumber { get; set; }
        }
        public class MessageChat
        {
            public string id { get; set; }
            public string body { get; set; }
            public string fromMe { get; set; }
            public string self { get; set; }
            public string isForwarded { get; set; }
            public string author { get; set; }
            public double time { get; set; }
            public string chatId { get; set; }
            public int messageNumber { get; set; }
            public string type { get; set; }
            public string senderName { get; set; }
            public string caption { get; set; }
            public string quotedMsgBody { get; set; }
            public string quotedMsgId { get; set; }
            public string quotedMsgType { get; set; }
            public string chatName { get; set; }
        }
        private static void SendFile(string chatId, string data)
        {
            var client = new RestClient("https://api.chat-api.com/instance127354/sendFile?token=jkdjtwjkwq2gfkac");
            client.Timeout = -1;
            var requestWa = new RestRequest(Method.POST);
            requestWa.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            requestWa.AddParameter("chatId", chatId);
            requestWa.AddParameter("filename", "IRCA.csv");
            requestWa.AddParameter("body", data);
            IRestResponse responseWa = client.Execute(requestWa);
            Console.WriteLine(responseWa.Content);
        }
        private static void SendMessage(string chatId, string body, string quotesMsgId)
        {
            var client = new RestClient("https://api.chat-api.com/instance127354/sendMessage?token=jkdjtwjkwq2gfkac");
            client.Timeout = -1;
            var requestWa = new RestRequest(Method.POST);
            requestWa.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            requestWa.AddParameter("chatId", chatId);
            requestWa.AddParameter("body", body);
            requestWa.AddParameter("quotedMsgId", quotesMsgId);
            IRestResponse responseWa = client.Execute(requestWa);
            Console.WriteLine(responseWa.Content);
        }
        private static string GetMessageList(string chatId, int lastMessageNumber)
        {
            var client = new RestClient("https://api.chat-api.com/instance127354/messages?token=jkdjtwjkwq2gfkac&lastMessageNumber=" + lastMessageNumber + "&chatId=" + chatId);
            client.Timeout = -1;
            var requestWa = new RestRequest(Method.GET);
            IRestResponse responseWa = client.Execute(requestWa);
            return responseWa.Content;
        }
    }
}
