using CekTradefeedExceptionPriceFall;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CekTradefeedExceptionPriceFall
{
    class Program
    {
        static void Main(string[] args)
        {
            var hours = DateTime.Now.TimeOfDay.Hours;
            var chat_id = "";
            if (hours > 6 && hours < 20)
            {
                try
                {
                    var wa = new DataSetProdTableAdapters.Whatsapp_parameterTableAdapter();
                    var dt_wa = wa.GetDataById(2);
                    chat_id = dt_wa[0].parameter;
                    var dr = new DataSetProdTableAdapters.TradefeedExceptionTableAdapter();
                    var dr_ap = new DataSetProdTableAdapters.uspTradeFeedExceptionApproveAllTableAdapter();
                    var dt = dr.GetDataBydate(DateTime.Now.Date);
                    List<DataSetProd.TradefeedExceptionRow> data = new List<DataSetProd.TradefeedExceptionRow>();
                    if (dt.Count != 0)
                    {
                        foreach (var item in dt)
                        {
                            data.Add(item);
                            if (item.ApprovalStatus == "P")
                            {
                                dr_ap.GetData(Convert.ToInt32(item.ExchangeID), item.BusinessDate, "A", "ok", "Robot KBI");
                            }
                        }
                    }
                    SendMessage(chat_id, "Sampai dengan " + DateTime.Now.ToString("dd-MMM-yyyy") + " " + DateTime.Now.ToString("HH:mm:ss") + " ada " + data.Count + " data tradefeed exceptions price fall/rise success auto approve", "");
                    data.Clear();
                }
                catch (Exception ex)
                {
                    SendMessage(chat_id, "Proses gagal :" + ex.Message, "");
                }
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
    }
}
