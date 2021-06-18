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
using System.IO.Compression;
using M10.lib;
using M11System;
using M11System.Model.M11;

namespace M11XmlHistory
{
    public partial class MainFormXmlHistory : BaseForm
    {
        public MainFormXmlHistory()
        {
            InitializeComponent();
            base.InitForm();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            //預計設定排程，每天00:00產生

            //建立必要資料夾
            Directory.CreateDirectory(M11Const.Path_FTPQueueXmlResult7Day);
            Directory.CreateDirectory(M11Const.Path_XmlResultWeb7Day);
            Directory.CreateDirectory(M11Const.Path_PrecipitationWeb7Day);




            timer1.Enabled = true;         
            
            
        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                timer1.Enabled = false;

                ShowMessageToFront("轉檔啟動");

                //M11資料補遺(XML、資料庫)
                M11AddendumXML();

                //M11的XML歷史資料維持30天
                M11HisXMLMaintain();


                ShowMessageToFront("轉檔完畢");

            }
            catch (Exception ex)
            {
                logger.Error(ex, "M11XML 轉檔錯誤:");
            }
            finally
            {
                System.Threading.Thread.Sleep(2000);
                this.Close();
            }
        }


        /// <summary>
        /// M11資料補遺(XML、資料庫)
        /// </summary>
        private void M11AddendumXML() 
        {
            try
            {
                //Utils.getStringToDateTime("2020-06-06 08:50:00");

                //DateTime ddd = Utils.getStringToDateTime("2020-06-09 08:50:00");

                //從前一天的最後一個時刻開始
                DateTime dtCheck = Utils.getStringToDateTime(DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd 23:50:00"));
                //DateTime dtCheck = Utils.getStringToDateTime(ddd.AddDays(-1).ToString("yyyy-MM-dd 23:50:00"));
                DateTime dtStart = dtCheck.AddDays(-10);
                //DateTime dtStart = dtCheck.AddDays(-2);

                ShowMessageToFront("M11資料補遺(XML、資料庫)==開始");
                //預設補遺十天以前的資料
                for (DateTime i = dtStart; i <= dtCheck; i = i.AddMinutes(10))
                {
                    //dtCheck = Utils.getStringToDateTime(dtCheck.ToString("2020-06-06 08:50:00"));

                    //測試XML補遺
                    M11AddendumXMLByTime(i);
                    
                }
                ShowMessageToFront("M11資料補遺(XML、資料庫)==結束");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "[M11AddendumXML]M11資料補遺(XML、資料庫) 轉檔錯誤:");
            }
        }

        /// <summary>
        /// M11的XML歷史資料維護(30天)
        /// </summary>
        private void M11HisXMLMaintain()
        {
            try
            {
                //移除超過七天資料                
                //DateTime dtNow7D = DateTime.Now.AddDays(-7);
                //20210429 改維持30天
                DateTime dtNow7D = DateTime.Now.AddDays(-30);
                //
                DateTime dt7Day = new DateTime(dtNow7D.Year, dtNow7D.Month, dtNow7D.Day, 0, 0, 0);

                // 刪除XML資料夾[監測資料]歷史30天以上的資料
                ShowMessageToFront("刪除[XML資料夾]歷史30天以上的資料==開始");
                foreach (string fname in Directory.GetFiles(M11Const.Path_FTPQueueXmlResult7Day))
                {
                    try
                    {
                        FileInfo fi = new FileInfo(fname);
                        string[] XmlResultSplit = fi.Name.Replace(fi.Extension, "").Split('_');

                        //避免舊檔案格式問題，排除沒有分析完整的檔案名稱
                        if (XmlResultSplit.Length != 5) continue;

                        //從檔案取得資料時間
                        DateTime dt = DateTime.ParseExact(XmlResultSplit[0], "yyyyMMddHHmm", System.Globalization.CultureInfo.CurrentCulture);

                        if (dt < dt7Day)
                        {
                            fi.Delete();

                            ShowMessageToFront(string.Format("刪除[XML資料夾]歷史30天以上的資料=={0}", fi.Name));
                        }

                    }
                    catch (Exception ex)
                    {
                        //有錯誤持續執行
                        continue;
                        //ShowMessageToFront(ex.ToString());
                    }
                }
                ShowMessageToFront("刪除[XML資料夾]歷史30天以上的資料==結束");

                // 刪除網站[監測資料]歷史30天以上的資料
                ShowMessageToFront("刪除網站[監測資料]歷史30天以上的資料==開始");
                foreach (string fname in Directory.GetFiles(M11Const.Path_XmlResultWeb7Day))
                {
                    try
                    {
                        FileInfo fi = new FileInfo(fname);
                        string[] XmlResultSplit = fi.Name.Replace(fi.Extension, "").Split('_');

                        //避免舊檔案格式問題，排除沒有分析完整的檔案名稱
                        if (XmlResultSplit.Length != 5) continue;

                        //從檔案取得資料時間
                        DateTime dt = DateTime.ParseExact(XmlResultSplit[0], "yyyyMMddHHmm", System.Globalization.CultureInfo.CurrentCulture);

                        if (dt < dt7Day)
                        {
                            fi.Delete();

                            ShowMessageToFront(string.Format("刪除網站[監測資料]歷史30天以上的資料=={0}", fi.Name));
                        }

                    }
                    catch (Exception ex)
                    {
                        //有錯誤持續執行
                        continue;
                        //ShowMessageToFront(ex.ToString());
                    }
                }
                ShowMessageToFront("刪除網站[監測資料]歷史30天以上的資料==結束");

                // 刪除網站[氣象局資料]歷史30天以上的資料
                ShowMessageToFront("刪除網站[氣象局資料]歷史30天以上的資料==開始");
                foreach (string fname in Directory.GetFiles(M11Const.Path_PrecipitationWeb7Day))
                {
                    try
                    {
                        FileInfo fi = new FileInfo(fname);
                        string[] XmlResultSplit = fi.Name.Replace(fi.Extension, "").Split('_');

                        //避免舊檔案格式問題，排除沒有分析完整的檔案名稱
                        if (XmlResultSplit.Length != 2) continue;

                        //從檔案取得資料時間
                        DateTime dt = DateTime.ParseExact(XmlResultSplit[0], "yyyyMMddHHmm", System.Globalization.CultureInfo.CurrentCulture);

                        if (dt < dt7Day)
                        {
                            fi.Delete();
                            ShowMessageToFront(string.Format("刪除網站[氣象局資料]歷史30天以上的資料=={0}", fi.Name));
                        }

                    }
                    catch (Exception ex)
                    {
                        //有錯誤持續執行
                        continue;
                        //ShowMessageToFront(ex.ToString());
                    }
                }
                ShowMessageToFront("刪除網站[氣象局資料]歷史30天以上的資料==結束");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "[M11HisXMLMaintain]M11的XML歷史資料維護(30天) 轉檔錯誤:");
            }
        }



        /// <summary>
        /// CGI資料移動到備份資料夾(傳入的FileInfo檔案不刪除)
        /// </summary>
        /// <param name="fi"></param>
        public void M11AddendumXMLByTime(DateTime dtCheck)
        {

            bool hasDbData = false;
            bool hasXMLData = false;


            ShowMessageToFront("M11資料補遺(XML、資料庫)==[檢核資料]" + M11Utils.M11DatetimeToString(dtCheck));
            //判斷資料庫該時刻是否有資料
            ssql = @"
                        select * from Result10MinData
                        where DatetimeString = '{0}'
                    ";
            ssql = string.Format(ssql, M11Utils.M11DatetimeToString(dtCheck));


            List<Result10MinData> lstTemp = dbDapper.Query<Result10MinData>(ssql);

            //該時刻有資料不用補
            if (lstTemp.Count > 0)
            {
                hasDbData = true;
            }

            //取得資料庫最接近時刻的資料
            if (hasDbData == false)
            {
                ShowMessageToFront("M11資料補遺(XML、資料庫)==[補遺]" + M11Utils.M11DatetimeToString(dtCheck));

                //取得前一個時段的資料
                //DateTime dtPre = dtCheck.AddMinutes(-10);
                DateTime dtStart = dtCheck.AddDays(-10);

                //取得十天中最接近現在時刻的資料
                ssql = @"
                    select * from Result10MinData
                    where DatetimeString = (
	                    select top 1 DatetimeString from Result10MinData
	                    where DatetimeString between '{0}' and '{1}'    
	                    order by DatetimeString desc
                    )
                ";
                
                ssql = string.Format(ssql, M11Utils.M11DatetimeToString(dtStart), M11Utils.M11DatetimeToString(dtCheck));

                List<Result10MinData> lstPreTemp = dbDapper.Query<Result10MinData>(ssql);
                foreach (Result10MinData item in lstPreTemp)
                {
                    item.Datetime = dtCheck;
                    item.DatetimeString = M11Utils.M11DatetimeToString(dtCheck);
                    item.GetTime = M11Utils.M11DatetimeToString(dtCheck);

                    //寫入前一個時刻的資料
                    dbDapper.Insert<Result10MinData>(item);
                }
            }


            //判斷檔案時刻是否有資料



            //取得檔案前一時刻的資料









            //string[] CgiNameSplit = fi.Name.Replace(fi.Extension, "").Split('-');

            ////避免舊檔案格式問題，排除沒有分析完整的檔案名稱
            //if (CgiNameSplit.Length != 8)
            //{
            //    return;
            //}

            ////從檔案取得資料時間
            //DateTime dt = DateTime.ParseExact(CgiNameSplit[2] + CgiNameSplit[3] + CgiNameSplit[4] + CgiNameSplit[5] + CgiNameSplit[6] + CgiNameSplit[7], "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture);

            ////建立資料夾
            ////string sFolderYear = dt.ToString("yyyy");
            ////string sFolderMonth = dt.ToString("yyyyMM");
            //string sFolderDay = dt.ToString("yyyyMMdd");

            ////string sBackupFolder = Path.Combine(M11Const.Path_BackupCGIData, sFolderYear, sFolderMonth, sFolderDay);
            //string sBackupFolder = Path.Combine(M11Const.Path_BackupCGIData, sFolderDay);
            //Directory.CreateDirectory(sBackupFolder);

            ////存至備份資料夾
            //fi.CopyTo(Path.Combine(sBackupFolder, fi.Name), true);


        }


        private void ShowMessageToFront(string pMsg,bool bShowText)
        {
            if (bShowText == true) ShowMessageToFront(pMsg);

            this.Update();
            Application.DoEvents();
        }

        private void ShowMessageToFront(string pMsg)
        {
            richTextBox1.AppendText(pMsg + "\r\n");
            this.Update();
            Application.DoEvents();
        }

    }
}
