using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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
            timer.Interval = 300000; // 3600000;
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start();
        }
        public void OnTimer(object sender, ElapsedEventArgs args)
        {
            //GET BUSINES DATE
            DateTime buseinesdate = DateTime.Now;
            var querry = "SELECT DateValue FROM SKD.Parameter WHERE Code = 'BusinessDate'";
            String connectionString = "Data Source=KBIDC-SKD-DBMS.ptkbi.com;Initial Catalog=SKD;Persist Security Info=True;User ID=saskd;Password=P@ssw0rd123";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(querry, connection);
                connection.Open();
                command.CommandTimeout = 1800;
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    buseinesdate = Convert.ToDateTime((reader["DateValue"]));
                }
                reader.Close();
                connection.Dispose();
                connection.Close();
            }
            //END
            try
            {
                var dr = new DataSetProdTableAdapters.TradefeedExceptionTableAdapter();
                var dr_ap = new DataSetProdTableAdapters.uspTradeFeedExceptionApproveAllTableAdapter();
                var dt = dr.GetDataBydate(buseinesdate);
                List<DataSetProd.TradefeedExceptionRow> data = new List<DataSetProd.TradefeedExceptionRow>();
                List<DataSetProd.TradefeedExceptionRow> data_di_proses = new List<DataSetProd.TradefeedExceptionRow>();
                if (dt.Count != 0)
                {
                    foreach (var item in dt)
                    {
                        data.Add(item);

                        data_di_proses.Add(item);
                        dr_ap.GetData(Convert.ToInt32(item.ExchangeID), item.BusinessDate, "A", "ok", "Robot KBI");
                    }
                }

                if (data_di_proses.Count != 0)
                {
                    //SendTelegram("-1001671146559", "Tanggal " + DateTime.Now.ToString("dd-MMM-yyyy") + " sampai dengan " + DateTime.Now.ToString("HH:mm:ss") + " ada " + data.Count + " data tradefeed exceptions success auto approve");
                }
                data.Clear();
                data_di_proses.Clear();
            }
            catch (Exception ex)
            {
                SendTelegram("-1001671146559", "Proses approve gagal :" + ex.Message +"\n"+ DateTime.Now.ToString("dd-MMM-yyyy") );

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
        private static string SendTelegram(string chatId, string message)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.DefaultConnectionLimit = 9999;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12;

            var client = new RestClient("https://api.telegram.org/bot2144239635:AAFjcfn_GdHP4OkzzZomaZt4XbwpHDGyR-U/sendMessage?chat_id=" + chatId + "&text=" + message);
            client.Timeout = -1;
            var requestWa = new RestRequest(Method.GET);
            IRestResponse responseWa = client.Execute(requestWa);
            return responseWa.Content;
        }
    }
}
