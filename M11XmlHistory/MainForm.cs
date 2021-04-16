﻿using System;
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

namespace M11XmlHistory
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            //預計設定排程，每天00:00產生

            try
            {
                //建立必要資料夾
                Directory.CreateDirectory(M11Const.Path_FTPQueueXmlResult7Day);
                Directory.CreateDirectory(M11Const.Path_XmlResultWeb7Day);

                //移除超過七天資料
                //DateTime dtNow = DateTime.Now;
                DateTime dtNow7D = DateTime.Now.AddDays(-7);
                //
                DateTime dt7Day = new DateTime(dtNow7D.Year, dtNow7D.Month, dtNow7D.Day, 0, 0, 0);

                // 取得資料夾內所有檔案
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
                        }

                    }
                    catch (Exception ex)
                    {
                        //有錯誤持續執行
                        continue;
                        //ShowMessageToFront(ex.ToString());
                    }
                }

                // 刪除網站歷史7天以上的資料
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
                        }

                    }
                    catch (Exception ex)
                    {
                        //有錯誤持續執行
                        continue;
                        //ShowMessageToFront(ex.ToString());
                    }
                }

                //產生ZIP檔到Web路徑
                string PathSource = M11Const.Path_FTPQueueXmlResult7Day;
                string PathDest = Path.Combine(M11Const.Path_XmlResultWeb, "7Day_10min_a_ds_data.zip");

                if (File.Exists(PathDest) == true)
                {
                    new FileInfo(PathDest).Delete();
                    System.Threading.Thread.Sleep(2000);
                }

                // 壓縮目錄中檔案
                ZipFile.CreateFromDirectory(PathSource, PathDest);

                // 解壓縮
                //ZipFile.ExtractToDirectory(zipPath, extractPath);
            }
            catch (Exception)
            {


            }
            finally
            {
                System.Threading.Thread.Sleep(2000);

                this.Close();
            }
        }
    }
}
