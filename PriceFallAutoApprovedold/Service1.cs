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

namespace PriceFallAutoApproved
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
            timer.Interval = 1200000; // 3600000;
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start();
        }
        public void OnTimer(object sender, ElapsedEventArgs args)
        {
            var hours = DateTime.Now.TimeOfDay.Hours;
            var chat_id = "";
            //if (hours > 6 && hours < 22)
            //{
            try
            {
                var wa = new DataSetProdTableAdapters.Whatsapp_parameterTableAdapter();
                var dt_wa = wa.GetDataById(1);
                chat_id = dt_wa[0].parameter;
                var dr = new DataSetProdTableAdapters.TradefeedExceptionTableAdapter();
                var dr_ap = new DataSetProdTableAdapters.uspTradeFeedExceptionApproveAllTableAdapter();
                var dt = dr.GetDataBydate(Convert.ToDateTime(DateTime.Now.Date));
                List<DataSetProd.TradefeedExceptionRow> data = new List<DataSetProd.TradefeedExceptionRow>();
                List<DataSetProd.TradefeedExceptionRow> data_di_proses = new List<DataSetProd.TradefeedExceptionRow>();
                if (dt.Count != 0)
                {
                    foreach (var item in dt)
                    {
                        data.Add(item);
                        if (item.ApprovalStatus == "P")
                        {
                            data_di_proses.Add(item);
                            dr_ap.GetData(Convert.ToInt32(item.ExchangeID), item.BusinessDate, "A", "ok", "Robot KBI");
                        }
                    }
                }

                if (data_di_proses.Count != 0)
                {
                    SendMessage(chat_id, "Tanggal " + DateTime.Now.ToString("dd-MMM-yyyy") + " sampai dengan " + DateTime.Now.ToString("HH:mm:ss") + " ada " + data.Count + " data tradefeed exceptions price fall/rise success auto approve", "");
                }
                data.Clear();
                data_di_proses.Clear();
            }
            catch (Exception ex)
            {
                SendMessage(chat_id, "Proses gagal :" + ex.Message, "");
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
