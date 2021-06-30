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

namespace M11FTP
{
    public partial class MainFormFTP : BaseForm
    {
        public MainFormFTP()
        {
            InitializeComponent();
            base.InitForm();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            //Directory.CreateDirectory(Path_Original);
            Directory.CreateDirectory(M11Const.Path_Original);
            Directory.CreateDirectory(M11Const.Path_XmlResult);
            Directory.CreateDirectory(M11Const.Path_FTPQueueTxtOriginal);
            Directory.CreateDirectory(M11Const.Path_FTPQueueXmlResult);
            Directory.CreateDirectory(M11Const.Path_FTPQueueTxtOriginalBak);
            Directory.CreateDirectory(M11Const.Path_FTPQueueXmlResultBak);
            Directory.CreateDirectory(M11Const.Path_DBSimulation);
            Directory.CreateDirectory(M11Const.Path_XmlResultWeb);
            Directory.CreateDirectory(M11Const.Path_XmlResultWeb7Day);
            Directory.CreateDirectory(M11Const.Path_FTPQueueXmlResult7Day);
            Directory.CreateDirectory(M11Const.Path_FTPQueueGPSData);
            

            timer1.Enabled = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                timer1.Enabled = false;

                ShowMessageToFront("轉檔啟動");
                
                //上傳FTP(10分鐘)
                ProcUploadFTP();
                                
                ShowMessageToFront("轉檔完畢");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "M10Winform轉檔錯誤:");                
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
        /// CGI資料上傳到FTP
        /// </summary>
        private void UploadFTPCgi() 
        {
            List<string> lstFile = new List<string>();

            ShowMessageToFront("[]FTP-上傳CGI檔案==啟動");
            //上傳CgiData
            FtpClient client = new FtpClient();
            try
            {
                client.Host = M11Const.FTP_IP;
                client.SocketKeepAlive = true;
                client.Credentials = new NetworkCredential(M11Const.FTP_User, M11Const.FTP_Password);
                client.Connect();

                // 取得資料夾內所有檔案
                int iIndex = 1;
                foreach (string fname in Directory.GetFiles(M11Const.Path_FTPQueueTxtOriginal))
                {
                    try
                    {
                        lstFile.Add(fname);
                        //FTP上傳路徑規劃
                        ///M11_System/Data/CgiData/2021/03/21

                        FileInfo fi = new FileInfo(fname);
                        string[] CgiNameSplit = fi.Name.Replace(fi.Extension, "").Split('-');

                        //避免舊檔案格式問題，排除沒有分析完整的檔案名稱
                        if (CgiNameSplit.Length != 8) continue;


                        //從檔案取得資料時間
                        DateTime dt = DateTime.ParseExact(CgiNameSplit[2] + CgiNameSplit[3] + CgiNameSplit[4] + CgiNameSplit[5] + CgiNameSplit[6] + CgiNameSplit[7], "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture);

                        //FluentFTP 起始路徑都是跟目錄開始，目錄結尾都是/
                        string sLocalPath = fi.FullName;
                        string sRemotePath = "/M11_System/Data/CgiData/";
                        sRemotePath = string.Format(@"{0}{1}/{2}/{3}/{4}"
                                , sRemotePath, dt.ToString("yyyy"), dt.ToString("MM"), dt.ToString("dd"), fi.Name);

                        //設定嘗試次數
                        client.RetryAttempts = 3;
                        //上傳檔案
                        client.UploadFile(sLocalPath, sRemotePath, FtpRemoteExists.Overwrite, true, FtpVerify.Retry);
                        ShowMessageToFront(string.Format("[{0}/{1}]上傳CGI檔案 成功=={2}", iIndex.ToString(), Directory.GetFiles(M11Const.Path_FTPQueueTxtOriginal).Length, fname));
                        iIndex++;

                        //存至備份資料夾
                        //fi.CopyTo(Path.Combine(M11Const.Path_FTPQueueTxtOriginalBak, fi.Name), true);
                        // 20210521 直接移動到備份資料夾進行處理
                        M11Helper.M11BackupCopyToCGIData(fi);

                        //刪除已處理資料
                        fi.Delete();
                    }
                    catch (Exception ex)
                    {
                        //有錯誤持續執行
                        continue;
                    }                    
                }
            }                
            finally
            {
                client.Disconnect();
                client.Dispose();
            }

            //全部處理完畢再一次刪除
            //foreach (string fname in lstFile)
            //{
            //    FileInfo fi = new FileInfo(fname);

            //    //存至備份資料夾
            //    fi.CopyTo(Path.Combine(M11Const.Path_FTPQueueTxtOriginalBak, fi.Name), true);
                
            //    //刪除已處理資料
            //    fi.Delete();
            //}
        }

        /// <summary>
        /// XML結果資料上傳到FTP
        /// </summary>
        private void UploadFTPXmlResult()
        {
            List<string> lstFile = new List<string>();

            ShowMessageToFront("[]FTP-上傳XML檔案==啟動");
            //上傳XmlResult
            FtpClient client = new FtpClient();
            try
            {
                client.Host = M11Const.FTP_IP;
                client.SocketKeepAlive = true;
                client.Credentials = new NetworkCredential(M11Const.FTP_User, M11Const.FTP_Password);
                client.Connect();

                // 取得資料夾內所有檔案
                int iIndex = 1;
                foreach (string fname in Directory.GetFiles(M11Const.Path_FTPQueueXmlResult))
                {
                    try
                    {
                        lstFile.Add(fname);
                        //FTP上傳路徑規劃
                        ///M11_System/Data/CgiData/2021/03/21

                        //XmlResult檔名範例(202103171410_10min_a_ds_data.xml)
                        FileInfo fi = new FileInfo(fname);
                        string[] XmlResultSplit = fi.Name.Replace(fi.Extension, "").Split('_');

                        //避免舊檔案格式問題，排除沒有分析完整的檔案名稱
                        //if (XmlResultSplit.Length > 0) continue;

                        //從檔案取得資料時間
                        DateTime dt = DateTime.ParseExact(XmlResultSplit[0], "yyyyMMddHHmm", System.Globalization.CultureInfo.CurrentCulture);

                        //FluentFTP 起始路徑都是跟目錄開始，目錄結尾都是/
                        string sLocalPath = fi.FullName;
                        string sRemotePath = "/M11_System/Data/XmlResult/";
                        sRemotePath = string.Format(@"{0}{1}/{2}/{3}/{4}"
                                , sRemotePath, dt.ToString("yyyy"), dt.ToString("MM"), dt.ToString("dd"), fi.Name);

                        //設定嘗試次數
                        client.RetryAttempts = 3;
                        //上傳檔案
                        client.UploadFile(sLocalPath, sRemotePath, FtpRemoteExists.Overwrite, true, FtpVerify.Retry);

                        ShowMessageToFront(string.Format("[{0}/{1}]上傳XML檔案 成功=={2}", iIndex.ToString(), Directory.GetFiles(M11Const.Path_FTPQueueXmlResult).Length, fname));
                        iIndex++;

                        //存至備份資料夾
                        fi.CopyTo(Path.Combine(M11Const.Path_FTPQueueXmlResultBak, fi.Name), true);
                        //刪除已處理資料
                        fi.Delete();
                    }
                    catch (Exception ex)
                    {
                        //有錯誤持續執行
                        continue;
                        //ShowMessageToFront(ex.ToString());
                    }
                }
            }
            finally
            {
                client.Disconnect();
                client.Dispose();
            }
            

            ////全部處理完畢再一次刪除
            //foreach (string fname in lstFile)
            //{
            //    FileInfo fi = new FileInfo(fname);
                
            //    //存至備份資料夾
            //    fi.CopyTo(Path.Combine(M11Const.Path_FTPQueueXmlResultBak, fi.Name), true);
            //    //刪除已處理資料
            //    fi.Delete();
            //}
        }

        /// <summary>
        /// CCD資料上傳到FTP
        /// </summary>
        private void UploadCcdToFTP()
        {
            ShowMessageToFront("[]FTP-上傳CCD檔案==啟動");
            //上傳CgiData
            FtpClient client = new FtpClient();
            try
            {
                client.Host = M11Const.FTP_IP;
                client.SocketKeepAlive = true;
                client.Credentials = new NetworkCredential(M11Const.FTP_User, M11Const.FTP_Password);
                client.Connect();

                // 取得資料夾內所有檔案
                int iIndex = 1;
                foreach (string fname in Directory.GetFiles(M11Const.Path_FTPQueueCcdResult))
                {
                    try
                    {
                        //FTP上傳路徑規劃
                        ///M11_System/Data/CCD/2021/03/21

                        FileInfo fi = new FileInfo(fname);
                        string[] CcdNameSplit = fi.Name.Replace(fi.Extension, "").Split('-');

                        //避免舊檔案格式問題，排除沒有分析完整的檔案名稱
                        if (CcdNameSplit.Length != 3) continue;

                        string sDataTime = CcdNameSplit[1] + CcdNameSplit[2];
                        if (CcdNameSplit[1].Length != 8) continue; //日期格式不符合
                        if (CcdNameSplit[2].Length != 6) continue; //時間格式不符合

                        //從檔案取得資料時間
                        DateTime dt = Utils.getStringToDateTime(sDataTime);

                        //FluentFTP 起始路徑都是跟目錄開始，目錄結尾都是/
                        string sLocalPath = fi.FullName;
                        string sRemotePath = "/M11_System/Data/CCD/";
                        sRemotePath = string.Format(@"{0}{1}/{2}/{3}/{4}"
                                , sRemotePath, dt.ToString("yyyy"), dt.ToString("MM"), dt.ToString("dd"), fi.Name);

                        //設定嘗試次數
                        client.RetryAttempts = 3;
                        //上傳檔案
                        client.UploadFile(sLocalPath, sRemotePath, FtpRemoteExists.Overwrite, true, FtpVerify.Retry);
                        ShowMessageToFront(string.Format("[{0}/{1}]上傳CCD檔案 成功=={2}", iIndex.ToString(), Directory.GetFiles(M11Const.Path_FTPQueueTxtOriginal).Length, fname));
                        iIndex++;

                        //存至備份資料夾
                        //fi.CopyTo(Path.Combine(M11Const.Path_FTPQueueTxtOriginalBak, fi.Name), true);

                        //刪除已處理資料
                        fi.Delete();
                    }
                    catch (Exception ex)
                    {
                        //有錯誤持續執行
                        continue;
                    }
                }
            }
            finally
            {
                client.Disconnect();
                client.Dispose();
            }

            //全部處理完畢再一次刪除
            //foreach (string fname in lstFile)
            //{
            //    FileInfo fi = new FileInfo(fname);

            //    //存至備份資料夾
            //    fi.CopyTo(Path.Combine(M11Const.Path_FTPQueueTxtOriginalBak, fi.Name), true);

            //    //刪除已處理資料
            //    fi.Delete();
            //}
        }

        /// <summary>
        /// GPS資料上傳到FTP
        /// </summary>
        private void UploadGpsToFTP()
        {
            ShowMessageToFront("[]FTP-上傳GPS檔案==啟動");
            //上傳CgiData
            FtpClient client = new FtpClient();
            try
            {
                client.Host = M11Const.FTP_IP;
                client.SocketKeepAlive = true;
                client.Credentials = new NetworkCredential(M11Const.FTP_User, M11Const.FTP_Password);
                client.Connect();

                // 取得資料夾內所有檔案
                int iIndex = 1;

                string[] aFiles = Directory.GetFiles(M11Const.Path_FTPQueueGPSData);
                foreach (string fname in aFiles)
                {
                    try
                    {
                        //FTP上傳路徑規劃
                        ///M11_System/Data/GpsData/2021/03/21

                        FileInfo fi = new FileInfo(fname);
                        string[] CcdNameSplit = fi.Name.Replace(fi.Extension, "").Split('-');

                        //避免舊檔案格式問題，排除沒有分析完整的檔案名稱
                        if (CcdNameSplit.Length != 3) continue;

                        string sDataTime = CcdNameSplit[2];
                        
                        //從檔案取得資料時間
                        DateTime dt = Utils.getStringToDateTime(sDataTime);

                        //FluentFTP 起始路徑都是跟目錄開始，目錄結尾都是/
                        string sLocalPath = fi.FullName;
                        string sRemotePath = "/M11_System/Data/GpsData/";
                        sRemotePath = string.Format(@"{0}{1}/{2}/{3}/{4}"
                                , sRemotePath, dt.ToString("yyyy"), dt.ToString("MM"), dt.ToString("dd"), fi.Name);

                        //設定嘗試次數
                        client.RetryAttempts = 3;
                        //上傳檔案
                        client.UploadFile(sLocalPath, sRemotePath, FtpRemoteExists.Overwrite, true, FtpVerify.Retry);
                        ShowMessageToFront(string.Format("[{0}/{1}]上傳CCD檔案 成功=={2}", iIndex.ToString(), aFiles.Length, fname));
                        iIndex++;

                        //存至備份資料夾
                        //fi.CopyTo(Path.Combine(M11Const.Path_FTPQueueTxtOriginalBak, fi.Name), true);

                        //刪除已處理資料
                        fi.Delete();
                    }
                    catch (Exception ex)
                    {
                        //有錯誤持續執行
                        continue;
                    }
                }
            }
            finally
            {
                client.Disconnect();
                client.Dispose();
            }

            //全部處理完畢再一次刪除
            //foreach (string fname in lstFile)
            //{
            //    FileInfo fi = new FileInfo(fname);

            //    //存至備份資料夾
            //    fi.CopyTo(Path.Combine(M11Const.Path_FTPQueueTxtOriginalBak, fi.Name), true);

            //    //刪除已處理資料
            //    fi.Delete();
            //}
        }


        /// <summary>
        /// 上傳FTP序列中的檔案
        /// </summary>
        private void ProcUploadFTP()
        {
            // CGI資料上傳到FTP
            UploadFTPCgi();

            //CGI資料上傳到FTP
            UploadFTPXmlResult();

            //CCD資料上傳到FTP
            UploadCcdToFTP();

            //GPS資料上傳到FTP
            UploadGpsToFTP();
        }
        
    }
}
