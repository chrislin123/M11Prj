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
    public partial class MainFormCCD : BaseForm
    {
        public MainFormCCD()
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

            //DateTime dtCheck = DateTime.Now.AddMinutes(25);

            //for (int i = 0; i < 60; i++)
            //{
            //    dtCheck = dtCheck.AddMinutes(1);
            //    ProcRemoveCCD(dtCheck);
            //}

            
            




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

                //10s
                //需要調整，只要偵測到檔案就處理資料，所以勝邦提供的檔案檔名要確定
                //ProcGenResultCcd10S(dtCheck);

                //移除CCD資料超過5天的資料(每一小時)
                //20230329 停止存放到5日歷史區
                //ProcRemoveCCD(dtCheck);

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
                ////預計排程每分鐘執行一次，每5分鐘的執行
                //if (dtCheck.Minute % 5 != 0) return;
                //20230418 新增由MVC網站設定CCD產生結果間隔(一分鐘、五分鐘)
                ssql = @"
                    select * from BasM11Setting where DataType = 'CCD' and DataRemark = 'PreventPeriodOneMin'
                ";

                BasM11Setting oBasM11Setting = dbDapper.QuerySingleOrDefault<BasM11Setting>(ssql);
                if (oBasM11Setting != null && oBasM11Setting.DataValue == "Y") //啟動一分鐘上傳一次
                {
                    //預計排程每分鐘執行一次，每1分鐘的執行
                    //if (dtCheck.Minute % 5 != 0) return;
                }
                else
                {
                    //預計排程每分鐘執行一次，每5分鐘的執行
                    if (dtCheck.Minute % 5 != 0) return;
                }
                

                dtCheck = Utils.getStringToDateTime(dtCheck.ToString("yyyy-MM-dd HH:mm:00"));
                //20210715 因為盛邦儀器時間不準，所以拉大時間範圍抓影像資料
                DateTime dtStart = Utils.getStringToDateTime(dtCheck.AddMinutes(-5).ToString("yyyy-MM-dd HH:mm:00"));
                DateTime dtEnd = Utils.getStringToDateTime(dtCheck.AddMinutes(3).ToString("yyyy-MM-dd HH:mm:00"));

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
                string[] FileLists = Directory.GetFiles(M11Const.Path_CcdSource, "*.*", SearchOption.AllDirectories);
                foreach (string fname in FileLists)
                {
                    FileInfo fi = new FileInfo(fname);

                    //虛擬站名(盛邦對應的名稱)
                    //string sVirtualStationNm = "";
                    CcdProcClass VirtualCcd = new CcdProcClass();

                    //使用完整的檔案路徑判斷屬於哪一個CCD儀器
                    foreach (KeyValuePair<string,CcdProcClass> item in diCcdData)
                    {
                        if (fname.Contains(item.Key) == true)
                        {
                            VirtualCcd = item.Value;
                            break;
                        }
                    }

                    string sDataTime = "";
                    //解析檔名及檔案時間
                    if (VirtualCcd.ProcalName == "Wanshan")
                    {
                        string[] sTempNames = fi.Name.Replace(fi.Extension, "").Split('_');
                        if (sTempNames.Length != 3) continue; //檔名格式不符合

                        //sStationNm = sTempNames[0];
                        sDataTime = sTempNames[1] + sTempNames[2];
                        if (sTempNames[1].Length != 8) continue; //日期格式不符合
                        if (sTempNames[2].Length != 6) continue; //時間格式不符合
                    }
                    else //ProcalName = "CCD1","CCD2","CCD3","CCD4","CCD5","CCD6"
                    {
                        string sFileName = fi.Name.Replace(fi.Extension, "");
                        //U00C0T20210714150008382
                        //檔名格式不符合
                        if (sFileName.Length != 23) continue; 

                        sDataTime = sFileName.Substring(6,14);
                    }

                    DateTime dtFileName = Utils.getStringToDateTime(sDataTime);

                    //判斷是否為5分鐘內產生
                    //20210715 東陽說，不用限制時間，盛邦傳上來的就顯示
                    //if (dtFileName < dtStart || dtFileName > dtEnd) continue; //超過該時段區間

                    //比較紀錄最新日期
                    if (dtFileName > VirtualCcd.dtGet)
                    {
                        VirtualCcd.dtGet = dtFileName;
                        VirtualCcd.FileFullName = fname;
                    }
                    

                    //處理檔案搬移到發布區及上傳區
                    //FileInfo fi = new FileInfo(diCcdData[item].FileFullName);
                    //if (fi.Exists == true)
                    //{ }
                    //網頁發布路徑測試目標資料夾並新增
                    string sSaveFolderFullName = Path.Combine(M11Const.Path_CcdResultWeb, VirtualCcd.M11FolderName);
                    Directory.CreateDirectory(sSaveFolderFullName);

                    //儲存到網頁發布路徑
                    string sM11FileName = VirtualCcd.M11Name + fi.Extension;
                    string sSaveFileFullName = Path.Combine(sSaveFolderFullName, sM11FileName);
                    fi.CopyTo(sSaveFileFullName, true);

                    //儲存到準備FTP上傳路徑
                    string sM11FtpFileName = string.Format("{0}-{1}-{2}.{3}", VirtualCcd.M11Name
                        , dtCheck.ToString("yyyyMMdd"), dtCheck.ToString("HHmmss"), fi.Extension.Replace(".",""));
                    string sFTPQueueSaveFileFullName = Path.Combine(M11Const.Path_FTPQueueCcdResult, sM11FtpFileName);
                    fi.CopyTo(sFTPQueueSaveFileFullName, true);

                    //儲存到5日歷史區
                    //20230329 停止存放到5日歷史區
                    //string sCcdResultHistSaveFileFullName = Path.Combine(M11Const.Path_CcdResultHist, sM11FtpFileName);
                    //fi.CopyTo(sCcdResultHistSaveFileFullName, true);
                   

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

        /// <summary>
        /// 產生結果CCD(每10秒)
        /// </summary>
        private void ProcGenResultCcd10S(DateTime dtCheck)
        {
            try
            {
                //預計排程每分鐘執行一次，每5分鐘的執行
                if (dtCheck.Minute % 5 != 0) return;

                dtCheck = Utils.getStringToDateTime(dtCheck.ToString("yyyy-MM-dd HH:mm:00"));
                //20210715 因為盛邦儀器時間不準，所以拉大時間範圍抓影像資料
                DateTime dtStart = Utils.getStringToDateTime(dtCheck.AddMinutes(-5).ToString("yyyy-MM-dd HH:mm:00"));
                DateTime dtEnd = Utils.getStringToDateTime(dtCheck.AddMinutes(3).ToString("yyyy-MM-dd HH:mm:00"));

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
                string[] FileLists = Directory.GetFiles(M11Const.Path_CcdSource, "*.*", SearchOption.AllDirectories);
                foreach (string fname in FileLists)
                {
                    try
                    {
                        FileInfo fi = new FileInfo(fname);

                        //虛擬站名(盛邦對應的名稱)
                        //string sVirtualStationNm = "";
                        CcdProcClass VirtualCcd = new CcdProcClass();

                        //使用完整的檔案路徑判斷屬於哪一個CCD儀器
                        foreach (KeyValuePair<string, CcdProcClass> item in diCcdData)
                        {
                            if (fname.Contains(item.Key) == true)
                            {
                                VirtualCcd = item.Value;
                                break;
                            }
                        }

                        string sDataTime = "";
                        //解析檔名及檔案時間
                        if (VirtualCcd.ProcalName == "Wanshan")
                        {
                            string[] sTempNames = fi.Name.Replace(fi.Extension, "").Split('_');
                            if (sTempNames.Length != 3) continue; //檔名格式不符合

                            //sStationNm = sTempNames[0];
                            sDataTime = sTempNames[1] + sTempNames[2];
                            if (sTempNames[1].Length != 8) continue; //日期格式不符合
                            if (sTempNames[2].Length != 6) continue; //時間格式不符合
                        }
                        else //ProcalName = "CCD1","CCD2","CCD3","CCD4","CCD5","CCD6"
                        {
                            string sFileName = fi.Name.Replace(fi.Extension, "");
                            //U00C0T20210714150008382
                            //檔名格式不符合
                            if (sFileName.Length != 23) continue;

                            sDataTime = sFileName.Substring(6, 14);
                        }

                        DateTime dtFileName = Utils.getStringToDateTime(sDataTime);

                        //判斷是否為5分鐘內產生
                        //20210715 東陽說，不用限制時間，盛邦傳上來的就顯示
                        //if (dtFileName < dtStart || dtFileName > dtEnd) continue; //超過該時段區間

                        //比較紀錄最新日期
                        if (dtFileName > VirtualCcd.dtGet)
                        {
                            VirtualCcd.dtGet = dtFileName;
                            VirtualCcd.FileFullName = fname;
                        }


                        //處理檔案搬移到發布區及上傳區
                        //FileInfo fi = new FileInfo(diCcdData[item].FileFullName);
                        //if (fi.Exists == true)
                        //{ }
                        //網頁發布路徑測試目標資料夾並新增
                        string sSaveFolderFullName = Path.Combine(M11Const.Path_CcdResultWeb, VirtualCcd.M11FolderName);
                        Directory.CreateDirectory(sSaveFolderFullName);

                        //儲存到網頁發布路徑
                        string sM11FileName = VirtualCcd.M11Name + fi.Extension;
                        string sSaveFileFullName = Path.Combine(sSaveFolderFullName, sM11FileName);
                        fi.CopyTo(sSaveFileFullName, true);

                        //儲存到準備FTP上傳路徑
                        string sM11FtpFileName = string.Format("{0}-{1}-{2}.{3}", VirtualCcd.M11Name
                            , dtCheck.ToString("yyyyMMdd"), dtCheck.ToString("HHmmss"), fi.Extension.Replace(".", ""));
                        string sFTPQueueSaveFileFullName = Path.Combine(M11Const.Path_FTPQueueCcdResult, sM11FtpFileName);
                        fi.CopyTo(sFTPQueueSaveFileFullName, true);

                        //儲存到5日歷史區                       
                        //2022011 改十秒版本之後，雲端主機則不保留五日歷史區
                        //string sCcdResultHistSaveFileFullName = Path.Combine(M11Const.Path_CcdResultHist, sM11FtpFileName);
                        //fi.CopyTo(sCcdResultHistSaveFileFullName, true);

                        fi.Delete();
                    }
                    catch (Exception ex)
                    {
                        //有錯誤持續執行
                        continue;
                    }

                }

                //System.Threading.Thread.Sleep(500);


                ////處理檔案刪除
                //foreach (string fname in FileLists)
                //{
                //    try
                //    {
                //        new FileInfo(fname).Delete();
                //    }
                //    catch
                //    {
                //        continue;
                //    }
                //}

            }
            catch (Exception ex)
            {
                logger.Error(ex, "M11CCD_ 轉檔錯誤:");

            }
        }


        public class CcdProcClass
        {
            public string ProcalName ="";
            public string M11Name = "";
            public string M11FolderName = "";
            public DateTime dtGet;
            public string FileFullName = "";
        }

        /// <summary>
        /// 移除CCD資料超過5天的資料(每一小時)
        /// </summary>
        private void ProcRemoveCCD(DateTime dtCheck)
        {
            try
            {
                //預計排程每分鐘執行一次，每一小時執行一次
                if (dtCheck.Minute != 0) return;

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
