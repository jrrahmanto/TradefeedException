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

namespace TradefeedExceptionServices
{
    public partial class Service1 : ServiceBase
    {
        public static string chat_id = "";
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
            try
            {
                var dr_contract = new DataSetProdTableAdapters.ContractTableAdapter();
                var drc = new DataSetProdTableAdapters.CommodityTableAdapter();
                var drs = new DataSetProdTableAdapters.SubCategoryTableAdapter();
                var drt = new DataSetProdTableAdapters.uspTradeFeedExceptionApproveAllTableAdapter();

                List<int> messageNumber = new List<int>();
                var wa = new DataSetProdTableAdapters.Whatsapp_parameterTableAdapter();
                var dt_wa = wa.GetDataById(1);
                chat_id = dt_wa[0].parameter;

                int lognumber = 0;
                string file = AppDomain.CurrentDomain.BaseDirectory + "\\Setting\\logLastMessage.txt";
                string[] text = File.ReadAllLines(file);
                messageNumber.Add(Convert.ToInt32(text[0]));

                CultureInfo culture = new CultureInfo("es-ES");

                WebClient client = new WebClient();
                string message = GetMessageList(chat_id, messageNumber[messageNumber.Count - 1]);
                ResponseChat rc = JsonConvert.DeserializeObject<ResponseChat>(message);

                foreach (MessageChat mc in rc.messages)
                {
                    lognumber = mc.messageNumber;
                    //masukin last message numbernya
                    string text2 = System.IO.File.ReadAllText(file);
                    text2 = lognumber.ToString();
                    System.IO.File.WriteAllText(file, text2);

                    string[] arrmcbody = new string[50];
                    if (mc.quotedMsgBody != null)
                    {
                        if (mc.quotedMsgBody.Contains(" ")) ;
                        {
                            arrmcbody = mc.quotedMsgBody.Split(' ');
                            if (arrmcbody[0] == "/e")
                            {
                                try
                                {
                                    string[] arrbody = mc.body.Split('\n');

                                    var dtc = drc.GetDataByCommodityCode(arrmcbody[4]);
                                    var CommodityID = dtc[0].CommodityID;

                                    Decimal SubCategoryId = dr_contract.GetData().OrderByDescending(x => x.ContractID).Where(x => x.CommodityID == CommodityID).Select(x => x.SubCategoryID).FirstOrDefault();


                                    DateTime efective_start_date = Convert.ToDateTime(arrbody[0], culture);
                                    DateTime start_date = Convert.ToDateTime(arrbody[0], culture);
                                    DateTime end_spot = Convert.ToDateTime(arrbody[2], culture);
                                    DateTime start_spot = Convert.ToDateTime(arrbody[1], culture);
                                    DateTime efective_end_date = Convert.ToDateTime(arrbody[2], culture);

                                    dr_contract.Insert(CommodityID,
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

                                    drt.GetData(Convert.ToInt32(arrmcbody[15]), DateTime.Now.Date, "A", "ok", "Robot KBI");

                                    SendMessage(chat_id, "Success insert contract \nStart date : " + arrbody[0] + "\nStart spot" + arrbody[1] + "\nEnd spot" + arrbody[2], mc.id);

                                }
                                catch (Exception ex)
                                {
                                    SendMessage(chat_id, "Process insert contrct Fail : " + ex.Message, "");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception x)
            {
                SendMessage("6289630870658@c.us", "insert contract fail " + x.Message, "");
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
