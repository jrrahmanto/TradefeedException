using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace CekExceptionService
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
            timer.Interval = 3600000;  
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start();
        }
        public void OnTimer(object sender, ElapsedEventArgs args)
        {
            var hours = DateTime.Now.TimeOfDay.Hours;
            //if (hours > 6 && hours < 22)
            //{
                var dr_contract = new DataSetProdTableAdapters.ContractTableAdapter();
                var drc = new DataSetProdTableAdapters.CommodityTableAdapter();
                var drs = new DataSetProdTableAdapters.SubCategoryTableAdapter();

                List<int> messageNumber = new List<int>();
                var wa = new DataSetProdTableAdapters.Whatsapp_parameterTableAdapter();
                var dt_wa = wa.GetDataById(1);
                chat_id = dt_wa[0].parameter;
                var dr = new DataSetProdTableAdapters.Get_Tradefeed_ExceptionTableAdapter();
                var dt = dr.GetData(DateTime.Now.Date);
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
                            msg.Add("/e Commodity Code : " + item.CommodityCode + " Contract Month : " + item.Contract_Month + " Contract Year : " + item.Contract_Year +" Exchange : "+item.ExchangeID);
                        }
                    }
                    if (msg.Count != 0)
                    {
                        SendMessage(chat_id, "Di temukan " + msg.Count + " exception contract tanggal " + DateTime.Now.ToString("dd-MMM-yyyy") + " :", "");
                        foreach (var item in msg)
                        {
                            SendMessage(chat_id, item, "");
                        }
                        SendMessage(chat_id, "Reply pesan di atas dengan format dd/MM/yyyy \n Effective_Start_Date \n Start_Date \n Start_Spot \n End_Spot \n Effective_End_Date", "");

                    }
                }

            try
            {
                string fileCLSJ = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\flag" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";

                string[] textFlag = new string[20];
                if (File.Exists(fileCLSJ))
                {
                    textFlag = File.ReadAllLines(fileCLSJ);
                }

                var dt_contract = dr_contract.GetDataByCLSJ(1227, DateTime.Now.Date);
                if (dt_contract.Count != 0 && textFlag == null)
                {
                    foreach (var item in dt_contract)
                    {
                        SendMessage(chat_id, "/clsj Contract CLSJ_BBJ Month : " + item.ContractMonth + ", Year : " + item.ContractYear + " berakhir tanggal : " + item.EndSpot.ToString("dd MMM yyyy") + " , untuk perpanjang balas chat ini dengan format : \nContract Month : MM\nContract Year : YYYY\nStart Date : dd/MM/yyyy\nStart Spot : dd/MM/yyyy\nEnd Spot : dd/MM/yyyy", "");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteToFile("fail clsj " + ex.Message);
            }
            //}
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
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\flag" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
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
    }
}
