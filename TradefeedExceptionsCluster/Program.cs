using RestSharp;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradefeedExceptionsCluster
{
    class Program
    {
        public static string chat_id = "";

        static void Main(string[] args)
        {
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
                    DateTime PatientID = Convert.ToDateTime((reader["DateValue"]));
                }
                reader.Close();
                connection.Dispose();
                connection.Close();
            }
            List<string> pesan = new List<string>();
            List<string> pesan2 = new List<string>();
            List<data_mentah> data_mentah_list = new List<data_mentah>();
            var dr_sp = new DataSetProdTableAdapters.Get_Tradefeed_Exception_ClusterTableAdapter();
            var dr_cm = new DataSetProdTableAdapters.ClearingMemberTableAdapter();
            var dr_cl = new DataSetProdTableAdapters.ClusterTableAdapter();
            var dr_cdt = new DataSetProdTableAdapters.CommodityTableAdapter();
            //var dr_tr = new DataSetDevTableAdapters.tra();
            var dr_approve = new DataSetProdTableAdapters.uspTradeFeedExceptionApproveAllTableAdapter();

            var wa = new DataSetProdTableAdapters.Whatsapp_parameterTableAdapter();


            var dt_wa = wa.GetDataById(2);
            chat_id = dt_wa[0].parameter;

            var dt_sp = dr_sp.GetData(Convert.ToDateTime("2022-04-26"));

            try
            {
                if (dt_sp.Count != 0)
                {
                    foreach (var item in dt_sp)
                    {
                        var code_broker = dr_cm.GetDataByCode(item.BrokerCode);
                        var code_trader = dr_cm.GetDataByCode(item.TraderCode);
                        var commodityId = dr_cdt.GetDataByCode(item.CommodityCode);
                        var dt_cl = dr_cl.GetDataByFilter(code_broker[0].CMID, code_trader[0].CMID, item.BusinessDate);

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
                    //var dt_cm = dr_tr.GetDataByDate(data_mentah_list[0].businessDate);
                    //if (dt_cm.Count != 0)
                    //{

                    //    foreach (var item in dt_cm)
                    //    {
                    //        pesan.Add(i + ". Tradefeed Id : " + item.TradeFeedID + ", Business Date : " + item.BusinessDate.ToString("dd/MMM/yyyy") + ", Messages : " + item.Message);
                    //        i++;
                    //    }
                    //}

                    SendMessage(chat_id, "Hari ini di temukan " + pesan.Count + " Exceptions cluster :\n\n" + String.Join("\n\n", pesan), "");
                    i = 1;
                    foreach (var item in data_mentah_list)
                    {
                        pesan2.Add(i + ". Broker CMID : " + item.CMID_Broker + ", Trader CMID : " + item.CMID_Trader + ", Business Date : " + item.businessDate.ToString("dd/MMM/yyyy") + ", Commodity Id : " + item.CommodityId);
                        dr_cl.Insert(item.CMID_Trader, item.CMID_Broker, item.CommodityId, item.businessDate, "A", "Robot KBI", DateTime.Now, "Robot KBI", DateTime.Now, null, "oke", null, null);
                        dr_approve.GetData(Convert.ToInt32(item.Exchange), item.businessDate, "A", "oke", "Robot KBI");
                        i++;
                    }
                    SendMessage(chat_id, data_mentah_list.Count + " data berhasil di prosess :\n\n" + String.Join("\n\n", pesan2), "");
                    SendMessage(chat_id, "Auto Approval Tradefeed Exception Success ☺️☺️☺️", "");
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
                SendMessage(chat_id, data_mentah_list.Count + " data gagal di prosess : " + ex.Message, "");
            }

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
    }
}
