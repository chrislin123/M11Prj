using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using M10.lib;
using System.Xml;
using System.Net;
using M11System;
using System.Configuration;
using System.Net.Mail;
using M11System.Model.M11;

namespace M11Mail
{
    public partial class MainFormMail : BaseForm
    {
        public MainFormMail()
        {
            InitializeComponent();
            base.InitForm();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            timer1.Enabled = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                timer1.Enabled = false;

                ShowMessageToFront("轉檔啟動");

                DateTime dtCheck = DateTime.Now;

                //發送Mail(儀器警示)每天(00:00)警示前一天資料異常
                ProcMailInstrumentAlert();
                                
                ShowMessageToFront("轉檔完畢");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "MainFormFTP轉檔錯誤:");                
            }
            finally
            {
                System.Threading.Thread.Sleep(2000);
                this.Close();
            }
        }

        private void ShowMessageToFront(string pMsg)
        {
            richTextBox1.AppendText(pMsg + "\r\n");
            this.Update();
            Application.DoEvents();
        }


        /// <summary>
        /// 發送Mail(儀器警示)
        /// </summary>
        private void ProcMailInstrumentAlert()
        {
            //功能
            //判斷系統設定是否執行警示
            ssql = @"
                    select * from BasM11Setting where DataType = 'Sensor' and DataRemark = 'DataErrorAlertMail'
                ";

            BasM11Setting oBasM11Setting = dbDapper.QuerySingleOrDefault<BasM11Setting>(ssql);
            if (oBasM11Setting != null && oBasM11Setting.DataValue != "Y") //啟動一分鐘上傳一次
            {
                //設定不啟動，則離開程序
                return;                
            }

            //判斷時間是否正確
            //改為server設定啟動時間，目前不判斷
            DateTime dtCheck = DateTime.Now;

            //處理資料是否異常判斷(TM、GPS)



            //取得TM有多少儀器(比對4個數值)            
            ssql = @"
                    select distinct StationID from Result10MinData where SensorID = 'TM' and DatetimeString >= '{0}'  order by StationID
                ";

            ssql = string.Format(ssql, dtCheck.AddDays(-1).ToString("yyyy-MM-dd 00:00:00"));
            List<string> ListTM = dbDapper.Query<string>(ssql);

            List<Result10MinData> TMErrorList = new List<Result10MinData>();
            //每個儀器判斷資料是否有36筆異常
            foreach (string StationID in ListTM)
            {
                ssql = @"
                    select * from Result10MinData where SensorID = 'TM' and StationID = '{0}' and DatetimeString >= '{1}'  order by StationID
                ";
                ssql = string.Format(ssql, StationID, dtCheck.AddDays(-1).ToString("yyyy-MM-dd 00:00:00"));
                List<Result10MinData> ListResult10Min = dbDapper.Query<Result10MinData>(ssql);

                foreach (Result10MinData item in ListResult10Min)
                {
                    DateTime dtStart = (DateTime)item.Datetime;
                    DateTime dtEnd = dtStart.AddMinutes(360);

                    string sDate1 = "";
                    string sDate2 = "";
                    string sDate3 = "";
                    string sDate4 = "";

                    int i = 0;
                    bool bTheSame = true;
                    for (DateTime dtTrans = dtStart; dtTrans < dtEnd; dtTrans = dtTrans.AddMinutes(10))
                    {
                        Result10MinData temp = ListResult10Min.Where(s => s.Datetime == dtTrans).FirstOrDefault();
                        //確認有取得資料
                        if (temp == null) continue;

                        i++; //紀錄筆數確認，最後判斷是否為36筆

                        string[] aDate = temp.value.Split(' ');

                        //確認資料內容
                        if (aDate.Length != 6) continue;
                        if (dtTrans == dtStart) //第一筆
                        {
                            sDate1 = aDate[0];
                            sDate2 = aDate[1];
                            sDate3 = aDate[2];
                            sDate4 = aDate[3];
                            continue;
                        }

                        if (sDate1 != aDate[0]
                            || sDate2 != aDate[1]
                            || sDate3 != aDate[2]
                            || sDate4 != aDate[3])
                        {
                            bTheSame = false;
                        }
                    }

                    if (i == 36 && bTheSame == true)
                    {
                        //判斷該日是否已經有資料，沒有資料才加入
                        if (TMErrorList.Where(s => s.StationID == item.StationID).FirstOrDefault() == null)
                        {
                            TMErrorList.Add(item);
                        }
                    }
                }
            }

            //取得GPS有多少儀器(比對3個數值)            
            ssql = @"
                    select distinct StationID from Result10MinData where SensorID = 'GPS' and DatetimeString >= '{0}'  order by StationID
                ";

            ssql = string.Format(ssql, dtCheck.AddDays(-1).ToString("yyyy-MM-dd 00:00:00"));
            List<string> ListGPS = dbDapper.Query<string>(ssql);

            List<Result10MinData> GPSErrorList = new List<Result10MinData>();
            //每個儀器判斷資料是否有36筆異常
            foreach (string StationID in ListGPS)
            {
                ssql = @"
                    select * from Result10MinData where SensorID = 'GPS' and StationID = '{0}' and DatetimeString >= '{1}'  order by StationID
                ";
                ssql = string.Format(ssql, StationID, dtCheck.AddDays(-1).ToString("yyyy-MM-dd 00:00:00"));
                List<Result10MinData> ListResult10Min = dbDapper.Query<Result10MinData>(ssql);

                foreach (Result10MinData item in ListResult10Min)
                {
                    DateTime dtStart = (DateTime)item.Datetime;
                    DateTime dtEnd = dtStart.AddMinutes(360);

                    string sDate1 = "";
                    string sDate2 = "";
                    string sDate3 = "";

                    int i = 0;
                    bool bTheSame = true;
                    for (DateTime dtTrans = dtStart; dtTrans < dtEnd; dtTrans = dtTrans.AddMinutes(10))
                    {
                        Result10MinData temp = ListResult10Min.Where(s => s.Datetime == dtTrans).FirstOrDefault();
                        //確認有取得資料
                        if (temp == null) continue;

                        i++; //紀錄筆數確認，最後判斷是否為36筆

                        string[] aDate = temp.value.Split(' ');

                        //確認資料內容
                        if (aDate.Length != 10) continue;
                        if (dtTrans == dtStart) //第一筆
                        {
                            sDate1 = aDate[0];
                            sDate2 = aDate[1];
                            sDate3 = aDate[2];
                            continue;
                        }

                        if (sDate1 != aDate[0]
                            || sDate2 != aDate[1]
                            || sDate3 != aDate[2])
                        {
                            bTheSame = false;
                        }
                    }

                    if (i == 36 && bTheSame == true)
                    {
                        //判斷該日是否已經有資料，沒有資料才加入
                        if (GPSErrorList.Where(s => s.StationID == item.StationID).FirstOrDefault() == null)
                        {
                            GPSErrorList.Add(item);
                        }
                    }
                }
            }
         

            //發送mail
            string sSubject = "〔水土保持生態工程研究中心 大規模崩塌平台〕儀器異常通知 - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string SenderMail = string.Empty;
            string SenderPass = string.Empty;
            List<string> AddressList = new List<string>();
            List<Attachment> AttachmentList = new List<Attachment>();

            //傳送附件
            //AttachmentList.Add(new Attachment(sAttachFileName)); 

            //取得設定檔寄送mail的帳號、密碼、收件者清單
            BasM11Setting TempData = dbDapper.QuerySingleOrDefault<BasM11Setting>("select * from BasM11Setting where DataType = 'Mail' and DataRemark = 'Account' ");
            if (TempData != null) SenderMail = TempData.DataValue;
            TempData = dbDapper.QuerySingleOrDefault<BasM11Setting>("select * from BasM11Setting where DataType = 'Mail' and DataRemark = 'Password' ");
            if (TempData != null) SenderPass = TempData.DataValue;

            var TempDataList = dbDapper.Query("select * from BasM11Setting where DataType = 'Mail' and DataRemark = 'AddressList'");
            foreach (var item in TempDataList)
            {
                AddressList.Add(item.DataValue as string);
            }

            //編排寄送Mail格式
            List<string> HtmlContentList = new List<string>();
            HtmlContentList.Add("<table style='border: 1px #cccccc solid;' cellpadding='10' border='1'>");
            HtmlContentList.Add("<thead>");
            HtmlContentList.Add("<tr>");
            HtmlContentList.Add("<th style='min-width: 80px; '>測站</ th>");
            HtmlContentList.Add("<th style='min-width: 80px; '>儀器</ th>");
            HtmlContentList.Add("<th style='min-width: 80px; '>警訊說明</ th>");
            HtmlContentList.Add("<th style='min-width: 80px; '>分析時間</ th>");

            HtmlContentList.Add("</tr>");
            HtmlContentList.Add("</thead>");
            HtmlContentList.Add("<tbody>");
            //TM異常
            foreach (Result10MinData item in TMErrorList)
            {
                HtmlContentList.Add("<tr>");

                string sStationName = GetStationNameForMail(item.StationID);
                HtmlContentList.Add(string.Format("<td>{0}</td>", sStationName));
                HtmlContentList.Add(string.Format("<td>{0}</td>", "TM(雙軸傾斜儀)"));
                HtmlContentList.Add(string.Format("<td>{0}</td>", "連續6小時回傳值皆無變化"));
                HtmlContentList.Add(string.Format("<td>{0}</td>", ((DateTime)item.Datetime).ToString("yyyy-MM-dd HH:mm")));
                HtmlContentList.Add("</tr>");
                //
            }
            //GPS異常
            foreach (Result10MinData item in GPSErrorList)
            {
                HtmlContentList.Add("<tr>");

                string sStationName = GetStationNameForMail(item.StationID);
                HtmlContentList.Add(string.Format("<td>{0}</td>", sStationName));
                HtmlContentList.Add(string.Format("<td>{0}</td>", "GPS(雙頻GPS地表變位)"));
                HtmlContentList.Add(string.Format("<td>{0}</td>", "連續6小時回傳值皆無變化"));
                HtmlContentList.Add(string.Format("<td>{0}</td>", ((DateTime)item.Datetime).ToString("yyyy-MM-dd HH:mm")));
                HtmlContentList.Add("</tr>");
                //
            }
            HtmlContentList.Add("</tbody>");
            HtmlContentList.Add("</table>");

            //1060519 寄送Gmail
            Gmail.SendMailByGmail(SenderMail, SenderPass, HtmlContentList, sSubject, AddressList, AttachmentList);

        }

        private string GetStationNameForMail(string sStationID) 
        {
            string result = "";
                        
            ssql = @"
                select distinct B.datavalue,a.station from  BasStationSensor A 
                left join BasM11Setting B on a.site = b.dataitem and datatype = 'SiteCName'
                where A.Station = '{0}'
                order by a.station
                ";


            ssql = string.Format(ssql, sStationID);
            var Stations = dbDapper.Query(ssql);
            foreach (var itemStationName in Stations)
            {
                result = string.Format("[{0}]{1}", itemStationName.datavalue, itemStationName.station);
            }

            return result;
        }

    }
}
