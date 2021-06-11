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
using System.Xml;
using FluentFTP;
using System.Net;
using M11System;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using GData = Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Threading;

namespace M11Google
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

            Directory.CreateDirectory(M11Const.Path_FTPQueueTxtOriginalBak);
            Directory.CreateDirectory(M11Const.Path_BackupCGIData);
            Directory.CreateDirectory(M11Const.Path_BackupCGIDataZip);

            

            timer1.Enabled = true;            

            //DriveService service = GoogleDrive.GenDriveService();

            //string parentid = "1j8IsCfC04ZqpYE2N8KCFn40M3ReWCnEj";
            string ParentIDRoot = "1yLhPU9W9L91DzP0S67k35O7vV5wB-fJ3";



            
            //建立資料夾
            //GData.File body = CreateFolder(service, parentid, "MyCreateFolder");

            //上傳檔案
            //body = CreateFile(service, @"C:\Users\chrislin\Downloads\ICP DAS WISE User Manual_v1.5.1tc_523x_224x.pdf", "_fileId");

            //更新檔案
            //body = updateFile(service, @"C:\Users\chrislin\Downloads\ICP DAS WISE User Manual_v1.5.1tc_523x_224x.pdf", "_fileId");

            
            //取得資料夾中的資料夾清單
            //List<GData.File> ttt = GoogleDrive.GetFolderList(service, ParentIDRoot);

            //取得資料夾中的檔案清單
            //List<GData.File> ttt1 = GoogleDrive.GetFileList(service, ParentIDRoot);

            //
        }






        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                timer1.Enabled = false;

                ShowMessageToFront("轉檔啟動");

                //壓縮CGI檔案
                //DoGGIDataToZip();

                //上傳到Google Drive
                //DoGGIDataToGoogleDrive();



                //先做資料夾分類
                List<string> lstFile = new List<string>();

                ShowMessageToFront("[]FTP-上傳CGI檔案==啟動");
                try
                {

                    // 取得資料夾內所有檔案
                    int iIndex = 1;
                    string[] FileLoops = Directory.GetFiles(@"D:\M11\Data\20-BackupData\2021","*", SearchOption.AllDirectories);
                    foreach (string fname in FileLoops)
                    {
                        try
                        {
                            lstFile.Add(fname);

                            FileInfo fi = new FileInfo(fname);

                            string[] CgiNameSplit = fi.Name.Replace(fi.Extension, "").Split('-');
                            //避免舊檔案格式問題，排除沒有分析完整的檔案名稱
                            if (CgiNameSplit.Length != 8)
                            {
                                //刪除已處理資料
                                fi.Delete();
                                continue;
                            }

                            M11Helper.M11BackupCopyToCGIData(fi);

                            //刪除已處理資料
                            fi.Delete();

                            ShowMessageToFront(string.Format("[{0}/{1}]移動CGI檔案 成功=={2}", iIndex.ToString(), FileLoops.Length, fname));

                            iIndex++;
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
                    //client.Disconnect();
                    //client.Dispose();
                }

                //上傳FTP(10分鐘)
                //ProcUploadFTP();

                //處理本機端檔案壓縮

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

        private void DoGGIDataToGoogleDrive()
        {
            throw new NotImplementedException();
        }

        private void DoGGIDataToZip()
        {
            ShowMessageToFront("[]壓縮CGI檔案-壓縮CGI檔案==啟動");
            try
            {
                // 取得資料夾內所有資料夾
                int iIndex = 1;
                string[] DirectoryLoops = Directory.GetDirectories(M11Const.Path_BackupCGIData,"*", SearchOption.TopDirectoryOnly);
                foreach (string fname in DirectoryLoops)
                {
                    try
                    {
                        DirectoryInfo fi = new DirectoryInfo(fname);

                        //排除當天的資料夾
                        if (fi.Name == DateTime.Now.ToString("yyyyMMdd"))
                        {
                            continue;
                        }


                        ////產生ZIP檔
                        string PathSource = fname;
                        string sFileName = string.Format("CGIData_{0}.zip", fi.Name);
                        string PathDest = Path.Combine(M11Const.Path_BackupCGIDataZip, sFileName);
                        
                        //檔案存在則先刪除
                        if (File.Exists(PathDest) == true)
                        {
                            new FileInfo(PathDest).Delete();
                            System.Threading.Thread.Sleep(1000);
                        }

                        // 壓縮目錄中檔案
                        ZipFile.CreateFromDirectory(PathSource, PathDest);

                        //解壓縮
                        //ZipFile.ExtractToDirectory(zipPath, extractPath);
                        
                        //刪除已處理資料
                        fi.Delete(true);

                        ShowMessageToFront(string.Format("[{0}/{1}]壓縮CGI檔案 成功=={2}", iIndex.ToString(), DirectoryLoops.Length, fname));

                        iIndex++;
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

            }
        }

        private void ShowMessageToFront(string pMsg)
        {
            richTextBox1.AppendText(pMsg + "\r\n");
            this.Update();
            Application.DoEvents();
        }
            
        
    }
}
