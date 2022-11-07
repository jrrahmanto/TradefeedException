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
            timer.Interval = 300000;
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
            var dr_contract = new DataSetProdTableAdapters.ContractTableAdapter();
            var drc = new DataSetProdTableAdapters.CommodityTableAdapter();
            var drs = new DataSetProdTableAdapters.SubCategoryTableAdapter();
            try
            {
                List<int> messageNumber = new List<int>();
                chat_id = "6289630870658-1628583102@g.us";
                var dr = new DataSetProdTableAdapters.Get_Tradefeed_ExceptionTableAdapter();
                var dt = dr.GetData(buseinesdate);
                List<string> msg = new List<string>();
                WriteToFile(buseinesdate.ToString() + " " + dt.Count.ToString());
                if (dt.Count != 0)
                {
                    foreach (var item in dt)
                    {
                        var dtc = drc.GetDataByCommodityCode(item.CommodityCode);
                        var CommodityID = dtc[0].CommodityID;

                        var dt_contract = dr_contract.GetDataByFilter(CommodityID, Convert.ToInt32(item.Contract_Year), Convert.ToInt32(item.Contract_Month));
                        if (dt_contract.Count == 0)
                        {
                            msg.Add("/e Commodity Code : " + item.CommodityCode + " Contract Month : " + item.Contract_Month + " Contract Year : " + item.Contract_Year + " Exchange : " + item.ExchangeID + " \nPlease answer with format dd/MM/yyyy\nStart date\nStart spot\nEnd spot");
                        }
                    }
                    if (msg.Count != 0)
                    {
                        SendTelegram("-1001671146559", "Di temukan " + msg.Count + " exception contract tanggal " + DateTime.Now.ToString("dd-MMM-yyyy"));
                        foreach (var item in msg)
                        {
                            SendTelegram("-1001671146559", item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SendTelegram("-1001671146559", "Notif contract error "+ex.Message);
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
                WriteToFile(dt_contract.Count + " data clsj akan habis kontrak");
                if (dt_contract.Count != 0 && textFlag == null)
                {
                    foreach (var item in dt_contract)
                    {
                        SendTelegram("-1001671146559", "/clsj Contract CLSJ_BBJ Month : " + item.ContractMonth + ", Year : " + item.ContractYear + " berakhir tanggal : " + item.EndSpot.ToString("dd MMM yyyy") + " , untuk perpanjang balas chat ini dengan format : \nContract Month : MM\nContract Year : YYYY\nStart Date : dd/MM/yyyy\nStart Spot : dd/MM/yyyy\nEnd Spot : dd/MM/yyyy");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteToFile("cek contract clsj fail : " + ex.Message);
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
