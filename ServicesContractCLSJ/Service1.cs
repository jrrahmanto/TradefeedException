using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ServicesContractCLSJ
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            WriteToFile("Service is started at " + DateTime.Now);
            Timer timer = new Timer();
            timer.Interval = 15000;
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start();
        }
        public void OnTimer(object sender, ElapsedEventArgs args)
        {
            var wa = new DataSet1TableAdapters.Whatsapp_parameterTableAdapter();
            var dt_wa = wa.GetData(1);
            var chat_id = dt_wa[0].parameter;
            try
            {
                var dr_contract = new DataSet1TableAdapters.ContractTableAdapter();
                var dr_sp = new DataSet1TableAdapters.CommodityTableAdapter();
                var dtc = dr_sp.GetDataByCode("CLSJ_BBJ");
                var CommodityID = dtc[0].CommodityID;
                int lognumber = 0;
                List<int> messageNumber = new List<int>();
                string file = AppDomain.CurrentDomain.BaseDirectory + "\\Setting\\logLastMessage.txt";
                string[] text = File.ReadAllLines(file);
                messageNumber.Add(Convert.ToInt32(text[0]));

                CultureInfo culture = new CultureInfo("es-ES");

                WebClient client = new WebClient();
                string message = GetMessageList(chat_id, messageNumber[0]);
                ResponseChat rc = JsonConvert.DeserializeObject<ResponseChat>(message);
                foreach (MessageChat mc in rc.messages)
                {
                    lognumber = mc.messageNumber;
                    //masukin last message numbernya
                    string text2 = System.IO.File.ReadAllText(file);
                    text2 = lognumber.ToString();
                    System.IO.File.WriteAllText(file, text2);
                    if (mc.author != "6285772388368@c.us")
                    {
                        string[] arrmcbody = new string[50];
                        if (mc.quotedMsgBody != null)
                        {
                            arrmcbody = mc.quotedMsgBody.Split(' ');
                        }

                        if (arrmcbody[0] == "/clsj")
                        {
                            string[] arrbody = mc.body.Split('\n');
                            DateTime efective_start_date = Convert.ToDateTime(arrbody[2], culture);
                            DateTime start_date = efective_start_date;
                            DateTime end_spot = Convert.ToDateTime(arrbody[4], culture);
                            DateTime start_spot = Convert.ToDateTime(arrbody[3], culture);
                            DateTime efective_end_date = end_spot;

                            Decimal SubCategoryId = dr_contract.GetData().OrderByDescending(x => x.ContractID).Where(x => x.CommodityID == CommodityID).Select(x => x.SubCategoryID).FirstOrDefault();

                            dr_contract.Insert(CommodityID,
                       Convert.ToInt32(arrbody[0]),
                       Convert.ToInt32(arrbody[1]),
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
                       "Robot KBI",
                       DateTime.Now,
                       "Robot KBI",
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


                            SendMessage(chat_id, "Process Success " + DateTime.Now.ToString("HH:mm:ss") + " \nCommodity ID: " + CommodityID +
                                                                       "\nMonth : " + Convert.ToInt32(arrbody[0]) +
                                                                       "\nYear : " + Convert.ToInt32(arrbody[1]) +
                                                                       "\nEffective Start Date : " + efective_start_date.ToString("dd MMM yyyy") +
                                                                       "\nContract Size : " + dtc[0].ContractSize +
                                                                       "\nSettlementType : " + dtc[0].SettlementType +
                                                                       "\nUnit : " + dtc[0].Unit +
                                                                       "\nStart Date :" + start_date.ToString("dd MMM yyyy") +
                                                                       "\nEnd Spot : " + end_spot.ToString("dd MMM yyyy") +
                                                                       "\nStart Spot : " + start_spot.ToString("dd MMM yyyy") +
                                                                       "\nEffective End Date : " + efective_end_date.ToString("dd MMM yyyy") +
                                                                       "\nPEG : " + dtc[0].PEG +
                                                                       "\nVM IRCA Cal Type : " + dtc[0].VMIRCACalType +
                                                                       "\nSettlement Factor :" + dtc[0].SettlementFactor +
                                                                       "\nDay Ref : " + dtc[0].DayRef +
                                                                       "\nDivisor : " + dtc[0].Divisor +
                                                                       "\nMargin Tender : " + dtc[0].MarginTender +
                                                                       "\nMargin Spot : " + dtc[0].MarginSpot +
                                                                       "\nMargin Remote : " + dtc[0].MarginRemote +
                                                                      "\nMode Fee : " + dtc[0].ModeFee, mc.id);
                            WriteToFlag("Success insert CLSJ");
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                SendMessage(chat_id, "Insert contract CLSJ Fail : " + ex.Message,"");
            }
        }
        public static void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.   
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }
        public static void WriteToFlag(string Message)
        {
            string path = "D:\\TradefeedExceptionServices\\CekExceptionService\\bin\\Debug\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = "D:\\TradefeedExceptionServices\\CekExceptionService\\bin\\Debug\\Logs\\flag" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.   
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }
        protected override void OnStop()
        {
            WriteToFile("Service is started at " + DateTime.Now);
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
