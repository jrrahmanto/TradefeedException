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

namespace TradefeedExceptionClusterService
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
            timer.Interval = 60000;
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start();
        }
        public void OnTimer(object sender, ElapsedEventArgs args)
        {
            List<string> pesan = new List<string>();
            List<string> pesan2 = new List<string>();
            List<data_mentah> data_mentah_list = new List<data_mentah>();

            try
            {
                //GET BUSINES DATE
                DateTime buseinesdate = DateTime.Now;
                var querry = "SELECT DateValue FROM SKD.Parameter WHERE Code = 'BusinessDate'";
                String connectionString = "Data Source=KBIDC-SKD-DBMS.ptkbi.com;Initial Catalog=SKD;Persist Security Info=True;User ID=skd_service;Password=P@ssw0rd2022";
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


                var dr_sp = new DataSetProdTableAdapters.Get_Tradefeed_Exception_ClusterTableAdapter();
                var dr_cm = new DataSetProdTableAdapters.ClearingMemberNewTableAdapter();
                var dr_cl = new DataSetProdTableAdapters.ClusterNewTableAdapter();
                var dr_cl_insert = new DataSetProdTableAdapters.ClusterTableAdapter();
                var dr_cdt = new DataSetProdTableAdapters.CommodityNewTableAdapter();
                var dr_tr = new DataSetProdTableAdapters.TradefeedExceptionNewTableAdapter();
                var dr_approve = new DataSetProdTableAdapters.uspTradeFeedExceptionApproveAllTableAdapter();

                var dt_sp = dr_sp.GetData(buseinesdate);

                if (dt_sp.Count != 0)
                {
                    WriteToFile("ditemukan " + dt_sp.Count + " data cluster");
                    foreach (var item in dt_sp)
                    {
                        WriteToFile(item.BrokerCode+" "+ item.TraderCode+ " " + item.CommodityCode);
                        var code_broker = dr_cm.GetData(item.BrokerCode);
                        var code_trader = dr_cm.GetData(item.TraderCode);
                        var commodityId = dr_cdt.GetData(item.CommodityCode);

                        var dt_cl = dr_cl.GetData(code_broker[0].CMID, code_trader[0].CMID, item.BusinessDate, commodityId[0].CommodityID);
                        WriteToFile("CMID broker : " + code_broker[0].CMID + ", CMID Trader : " + code_trader[0].CMID + " Business Date : " + item.BusinessDate);
                        if (dt_cl.Count == 0)
                        {
                            var data = new data_mentah
                            {
                                CMID_Broker = code_broker[0].CMID,
                                CMID_Trader = code_trader[0].CMID,
                                businessDate = item.BusinessDate,
                                Exchange = item.ExchangeID,
                                CommodityId = commodityId[0].CommodityID
                            };
                            data_mentah_list.Add(data);
                        }
                    }
                }
                if (data_mentah_list.Count != 0)
                {
                    int i = 1;
                    var dt_cm = dr_tr.GetData(data_mentah_list[0].businessDate);
                    if (dt_cm.Count != 0)
                    {

                        foreach (var item in dt_cm)
                        {
                            pesan.Add(i + ". Tradefeed Id : " + item.TradeFeedID + ", Business Date : " + item.BusinessDate.ToString("dd/MMM/yyyy") + ", Messages : " + item.Message);
                            i++;
                        }
                    }

                    SendTelegram("-1001671146559", "Hari ini di temukan " + pesan.Count + " Exceptions cluster :\n\n" + String.Join("\n\n", pesan));
                    i = 1;
                    foreach (var item in data_mentah_list)
                    {
                        WriteToFile(i + ". Broker CMID : " + item.CMID_Broker + ", Trader CMID : " + item.CMID_Trader + ", Business Date : " + item.businessDate.ToString("dd/MMM/yyyy") + ", Commodity Id : " + item.CommodityId);
                        pesan2.Add(i + ". Broker CMID : " + item.CMID_Broker + ", Trader CMID : " + item.CMID_Trader + ", Business Date : " + item.businessDate.ToString("dd/MMM/yyyy") + ", Commodity Id : " + item.CommodityId);
                        dr_cl_insert.Insert(item.CMID_Trader, item.CMID_Broker, item.CommodityId, item.businessDate, "A", "Robot KBI", DateTime.Now, "Robot KBI", DateTime.Now, null, "oke", null, null);
                        dr_approve.GetData(Convert.ToInt32(item.Exchange), item.businessDate, "A", "ok", "Robot KBI");
                        i++;
                    }
                    SendTelegram("-1001671146559", data_mentah_list.Count + " data berhasil di prosess :\n\n" + String.Join("\n\n", pesan2));
                    SendTelegram("-1001671146559", "Auto Approval Tradefeed Exception Success ☺️☺️☺️");
                }
                pesan.Clear();
                pesan2.Clear();
                data_mentah_list.Clear();
            }
            catch (Exception ex)
            {
                pesan.Clear();
                pesan2.Clear();
                data_mentah_list.Clear();
                SendTelegram("-1001671146559", data_mentah_list.Count + " data exception cluster gagal di prosess : " + ex.Message);
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
        public class data_mentah
        {
            public decimal CMID_Broker { get; set; }
            public decimal CMID_Trader { get; set; }
            public DateTime businessDate { get; set; }
            public decimal Exchange { get; set; }
            public decimal CommodityId { get; set; }
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
