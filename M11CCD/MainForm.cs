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
using FluentFTP;
using System.Net;
using M11System;
using M11System.Model.Procal;
using M11System.Model.M11;

namespace M11CCD
{
    public partial class MainForm : BaseForm
    {
        public MainForm()
        {
            InitializeComponent();
            base.InitForm();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Directory.CreateDirectory(M11Const.Path_FTPQueueCcdResult);
            Directory.CreateDirectory(M11Const.Path_CcdResultHist);
            Directory.CreateDirectory(M11Const.Path_CcdResultWeb);

            //DateTime dtCheck = new DateTime(2021, 4, 26, 15, 20, 8);

            //int test = dtCheck.Minute % 5;

            timer1.Enabled = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                timer1.Enabled = false;

                ShowMessageToFront("轉檔啟動");

                DateTime dtCheck = DateTime.Now;
                //DateTime dtCheck = new DateTime(2021, 4, 26, 15, 20, 8);

                //產生結果CCD(每五分鐘)
                ProcGenResultCcd(dtCheck);

                //移除CCD資料超過5天的資料(每一小時)
                ProcRemoveCCD(dtCheck);

                ShowMessageToFront("轉檔完畢");

            }
            catch (Exception ex)
            {
                logger.Error(ex, "M11CCD 轉檔錯誤:");                
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
        /// 產生結果CCD(每五分鐘)
        /// </summary>
        private void ProcGenResultCcd(DateTime dtCheck) 
        {
            try
            {
                //預計排程每分鐘執行一次，每5分鐘的執行
                if (dtCheck.Minute % 5 != 0) return;

                dtCheck = Utils.getStringToDateTime(dtCheck.ToString("yyyy-MM-dd HH:mm:00"));
                DateTime dtStart = Utils.getStringToDateTime(dtCheck.AddMinutes(-5).ToString("yyyy-MM-dd HH:mm:00"));
                DateTime dtEnd = Utils.getStringToDateTime(dtCheck.ToString("yyyy-MM-dd HH:mm:00"));

                //取得有設定的資料清單
                List<string> lstStation = new List<string>();
                ssql = @"
                    select * from BasM11Setting where DataType = 'StMapCcdProcal'
                ";
                List<BasM11Setting> lstBasM11Setting = dbDapper.Query<BasM11Setting>(ssql);
                //lstStation = lstBasM11Setting.Select(x => x.DataValue).Distinct().ToList();

                Dictionary<string, CcdProcClass> diCcdData = new Dictionary<string, CcdProcClass>();
                foreach (BasM11Setting item in lstBasM11Setting)
                {
                    CcdProcClass cpc = new CcdProcClass();
                    cpc.ProcalName = item.DataValue;
                    cpc.M11Name = item.DataItem;
                    cpc.M11FolderName = item.DataRemark;
                    cpc.dtGet = new DateTime();

                    diCcdData.Add(item.DataValue, cpc);
                }

                    
                //取得資料夾所有檔案
                int iIndex = 1;
                string[] FileLists = Directory.GetFiles(M11Const.Path_CcdSource);
                foreach (string fname in FileLists)
                {
                    FileInfo fi = new FileInfo(fname);

                    
                    //解析檔名及檔案時間
                    string[] sTempNames = fi.Name.Replace(fi.Extension, "").Split('_');
                    if (sTempNames.Length != 3) continue; //檔名格式不符合

                    string sStationNm = sTempNames[0];
                    string sDataTime = sTempNames[1] + sTempNames[2];
                    if (sTempNames[1].Length != 8) continue; //日期格式不符合
                    if (sTempNames[2].Length != 6) continue; //時間格式不符合

                    DateTime dtFileName = Utils.getStringToDateTime(sDataTime);


                    //判斷是否為5分鐘內產生
                    if (dtFileName < dtStart || dtFileName > dtEnd) continue; //超過該時段區間

                    //判斷是否存在
                    if (diCcdData.ContainsKey(sStationNm) == true)
                    {
                        //比較紀錄最新日期
                        if (dtFileName > diCcdData[sStationNm].dtGet)
                        {
                            diCcdData[sStationNm].dtGet = dtFileName;
                            diCcdData[sStationNm].FileFullName = fname;
                        }
                    }
                }

                //處理檔案搬移到發布區及上傳區
                foreach (string item in diCcdData.Keys)
                {
                    FileInfo fi = new FileInfo(diCcdData[item].FileFullName);
                    if (fi.Exists == true)
                    {
                        //網頁發布路徑測試目標資料夾並新增
                        string sSaveFolderFullName = Path.Combine(M11Const.Path_CcdResultWeb, diCcdData[item].M11FolderName);
                        Directory.CreateDirectory(sSaveFolderFullName);

                        //儲存到網頁發布路徑
                        string sM11FileName = diCcdData[item].M11Name + fi.Extension;
                        string sSaveFileFullName = Path.Combine(sSaveFolderFullName, sM11FileName);
                        fi.CopyTo(sSaveFileFullName, true);

                        //儲存到準備FTP上傳路徑
                        string sM11FtpFileName = string.Format("{0}-{1}-{2}.jpeg", diCcdData[item].M11Name
                            , dtCheck.ToString("yyyyMMdd"), dtCheck.ToString("HHmmss"));
                        string sFTPQueueSaveFileFullName = Path.Combine(M11Const.Path_FTPQueueCcdResult, sM11FtpFileName);
                        fi.CopyTo(sFTPQueueSaveFileFullName, true);

                        //儲存到5日歷史區                       
                        string sCcdResultHistSaveFileFullName = Path.Combine(M11Const.Path_CcdResultHist, sM11FtpFileName);
                        fi.CopyTo(sCcdResultHistSaveFileFullName, true);
                    }
                }

                System.Threading.Thread.Sleep(500);


                //處理檔案刪除
                foreach (string fname in FileLists)
                {
                    try
                    {
                        new FileInfo(fname).Delete();
                    }
                    catch 
                    {
                        continue;
                    }
                }
                
            }
            catch (Exception ex)
            {
                logger.Error(ex, "M11CCD_XMLResult 轉檔錯誤:");

            }
        }


        public class CcdProcClass
        {
            public string ProcalName;
            public string M11Name;
            public string M11FolderName;
            public DateTime dtGet;
            public string FileFullName;
        }

        /// <summary>
        /// 移除CCD資料超過5天的資料(每一小時)
        /// </summary>
        private void ProcRemoveCCD(DateTime dtCheck)
        {
            try
            {
                //預計排程每分鐘執行一次，每一小時執行一次
                if (dtCheck.Minute.ToString() != "00") return;

                //移除超過5天的資料
                dtCheck = dtCheck.AddDays(-5);

                string[] FileLists = Directory.GetFiles(M11Const.Path_CcdResultHist);
                foreach (string fname in FileLists)
                {
                    try
                    {
                        FileInfo fi = new FileInfo(fname);

                        //檢核檔案資料是否超過5天
                        //解析檔名及檔案時間
                        string[] sTempNames = fi.Name.Replace(fi.Extension, "").Split('-');
                        if (sTempNames.Length != 3) continue; //檔名格式不符合

                        string sStationNm = sTempNames[0];
                        string sDataTime = sTempNames[1] + sTempNames[2];
                        if (sTempNames[1].Length != 8) continue; //日期格式不符合
                        if (sTempNames[2].Length != 6) continue; //時間格式不符合

                        DateTime dtFileName = Utils.getStringToDateTime(sDataTime);

                        if (dtFileName > dtCheck) continue;

                        //刪除檔案
                        fi.Delete();
                    }
                    catch
                    {
                        continue;
                        
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ProcRemoveCCD 轉檔錯誤:");
            }

        }

    }
}
