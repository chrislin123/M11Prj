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



            timer1.Enabled = true;



            //

            return;


            UserCredential credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets { ClientId = "52848081395-idb16mbka1jli6qjir2elmdks2b0bkpi.apps.googleusercontent.com", ClientSecret = "9LDYZ8yAdPqYNPPB2v06U9M9" },
                new[] { DriveService.Scope.Drive, DriveService.Scope.DriveFile },
                "MProject",
                CancellationToken.None,
                new FileDataStore("Drive.Auth.Store")).Result;

            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "QuantaShopFloor",
            });


            //string parentid = "1j8IsCfC04ZqpYE2N8KCFn40M3ReWCnEj";
            string parentid = "1yLhPU9W9L91DzP0S67k35O7vV5wB-fJ3";
            
            //建立資料夾
            //GData.File body = CreateFolder(service, parentid, "MyCreateFolder");

            //上傳檔案
            //body = CreateFile(service, @"C:\Users\chrislin\Downloads\ICP DAS WISE User Manual_v1.5.1tc_523x_224x.pdf", "_fileId");

            //更新檔案
            //body = updateFile(service, @"C:\Users\chrislin\Downloads\ICP DAS WISE User Manual_v1.5.1tc_523x_224x.pdf", "_fileId");

            //取得資料夾中的資料夾清單
            List<GData.File> ttt = GetFolderList(service, parentid);

            //取得資料夾中的檔案清單
            List<GData.File> ttt1 = GetFileList(service, parentid);

            //
        }


        /// <summary>
        /// 建立一個 File 物件
        /// </summary>
        /// <param name="parentid">父層目錄ID</param>
        /// <param name="title">File's Name</param>
        /// <param name="description">File's Description</param>
        /// <param name="mimeType">File's MimeType</param>
        /// <returns></returns>
        private GData.File GenFileObject(string parentid, string title, string description = "", string mimeType = "")
        {
            try
            {
                GData.File body = new GData.File();
                
                body.Name = title;
                body.Description = description;
                body.MimeType = mimeType;
                if (parentid != "")
                    //body.Parents = new List<GData.ParentReference>() { new GData.ParentReference() { Id = parentid } };
                    body.Parents = new List<string> { parentid };

                return body;
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        /// <summary>
        /// 建立一個資料夾
        /// </summary>
        /// <param name="_service"></param>
        /// <param name="parentid"></param>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public GData.File CreateFolder(DriveService _service, string parentid, string title, string description = "")
        {
            try
            {
                // setting file
                string mimeType = "application/vnd.google-apps.folder";
                GData.File body = GenFileObject(parentid, title, description, mimeType);
                FilesResource.CreateRequest request = _service.Files.Create(body);
                GData.File folder = request.Execute();
                return folder;
            }
            catch
            {
                throw;
            }
        }


        /// <summary>
        /// 取得資料夾中的資料夾
        /// </summary>
        /// <param name="_service"></param>
        /// <param name="parentid"></param>
        /// <returns></returns>
        public List<GData.File> GetFolderList(DriveService _service, string parentid = "")
        {
            string searchPattern = "trashed=false";
            searchPattern += " and mimeType='application/vnd.google-apps.folder'";
            //searchPattern += string.Format(" and '{0}' in owners", owner);

            if (parentid != "")
                searchPattern += string.Format(" and '{0}' in parents", parentid);
            List<GData.File> result = List(_service, searchPattern);
            return result;
        }

        /// <summary>
        /// 取得資料夾中的檔案
        /// </summary>
        /// <param name="_service"></param>
        /// <param name="parentid"></param>
        /// <returns></returns>
        public List<GData.File> GetFileList(DriveService _service, string parentid = "")
        {
            string searchPattern = "trashed=false";
            searchPattern += " and mimeType!='application/vnd.google-apps.folder'";
            //searchPattern += string.Format(" and '{0}' in owners", owner);

            if (parentid != "")
                searchPattern += string.Format(" and '{0}' in parents", parentid);
            List<GData.File> result = List(_service, searchPattern);
            return result;
        }

        /// <summary>
        /// 取得檔案清單
        /// Documentation List: https://developers.google.com/drive/v2/reference/files/list 
        /// Documentation Search: https://developers.google.com/drive/web/search-parameters 
        /// </summary>
        /// <param name="searchPattern">搜尋條件</param>
        /// <returns></returns>
        public List<GData.File> List(DriveService _service, string searchPattern = "*")
        {
            List<GData.File> result = new List<GData.File>();
            try
            {
                FilesResource.ListRequest request = _service.Files.List();
                request.PageSize = 1000;
                //request.MaxResults = 1000;
                if (searchPattern != "*")
                {
                    request.Q = searchPattern;
                }
                GData.FileList filesFeed = request.Execute();

                
                // 判斷資料是否回傳結束
                while (filesFeed.Files != null)
                {
                    // add to the list
                    result.AddRange(filesFeed.Files);

                    if (filesFeed.NextPageToken != null)
                    {
                        // 若有下一頁，繼續
                        request.PageToken = filesFeed.NextPageToken;

                        // 執行 NextPage 的 request
                        filesFeed = request.Execute();
                    }
                    else
                    {
                        // 若沒有下一頁，結束
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return result;
        }

        /// <summary>
        /// 更新已經存在的檔案
        /// </summary>
        /// <param name="_service"></param>
        /// <param name="_uploadFile"></param>
        /// <param name="_fileId"></param>
        /// <returns></returns>
        public static GData.File updateFile(DriveService _service, string _uploadFile, string _fileId)
        {
            if (System.IO.File.Exists(_uploadFile))
            {
                GData.File body = new GData.File();
                body.Name = System.IO.Path.GetFileName(_uploadFile);
                body.Description = "File updated by Diamto Drive Sample";
                body.MimeType = GetMimeType(_uploadFile);

                byte[] byteArray = System.IO.File.ReadAllBytes(_uploadFile);
                System.IO.MemoryStream stream = new System.IO.MemoryStream(byteArray);
                try
                {
                    FilesResource.UpdateMediaUpload request = _service.Files.Update(body, _fileId, stream, GetMimeType(_uploadFile));                    
                    request.Upload();
                    return request.ResponseBody;
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                    return null;
                }
            }
            else
            {
                Console.WriteLine("File does not exist: " + _uploadFile);
                return null;
            }

        }

        /// <summary>
        /// 在資料夾中建立新的檔案
        /// </summary>
        /// <param name="_service"></param>
        /// <param name="parentid"></param>
        /// <param name="_uploadFile"></param>
        /// <returns></returns>
        public static GData.File CreateFile(DriveService _service, string parentid, string _uploadFile)
        {
            if (System.IO.File.Exists(_uploadFile))
            {
                GData.File body = new GData.File();
                body.Name = System.IO.Path.GetFileName(_uploadFile);
                body.Description = "File updated by Diamto Drive Sample";
                body.MimeType = GetMimeType(_uploadFile);
                body.Parents = new List<string> { parentid };

                byte[] byteArray = System.IO.File.ReadAllBytes(_uploadFile);
                System.IO.MemoryStream stream = new System.IO.MemoryStream(byteArray);
                try
                {
                    FilesResource.CreateMediaUpload request = _service.Files.Create(body, stream, GetMimeType(_uploadFile));
                    request.Upload();
                    return request.ResponseBody;
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                    return null;
                }
            }
            else
            {
                Console.WriteLine("File does not exist: " + _uploadFile);
                return null;
            }

        }


        /// <summary>
        /// 自動判斷取得mimeType
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static string GetMimeType(string fileName)
        {
            string mimeType = "application/unknown";
            string ext = System.IO.Path.GetExtension(fileName).ToLower();
            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
            if (regKey != null && regKey.GetValue("Content Type") != null)
                mimeType = regKey.GetValue("Content Type").ToString();
            return mimeType;
        }




        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                timer1.Enabled = false;

                ShowMessageToFront("轉檔啟動");



                //先做資料夾分類
                List<string> lstFile = new List<string>();

                ShowMessageToFront("[]FTP-上傳CGI檔案==啟動");
                try
                {

                    // 取得資料夾內所有檔案
                    int iIndex = 1;
                    string[] FileLoops = Directory.GetFiles(M11Const.Path_FTPQueueTxtOriginalBak);
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

                            //從檔案取得資料時間
                            DateTime dt = DateTime.ParseExact(CgiNameSplit[2] + CgiNameSplit[3] + CgiNameSplit[4] + CgiNameSplit[5] + CgiNameSplit[6] + CgiNameSplit[7], "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture);

                            //建立資料夾
                            string sFolderYear = dt.ToString("yyyy");
                            string sFolderMonth = dt.ToString("yyyyMM");
                            string sFolderDay = dt.ToString("yyyyMMdd");

                            string sBackupFolder = Path.Combine(M11Const.Path_BackupCGIData, sFolderYear, sFolderMonth, sFolderDay);
                            Directory.CreateDirectory(sBackupFolder);


                            //存至備份資料夾
                            fi.CopyTo(Path.Combine(sBackupFolder, fi.Name), true);

                            ////刪除已處理資料
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

        private void ShowMessageToFront(string pMsg)
        {
            richTextBox1.AppendText(pMsg + "\r\n");
            this.Update();
            Application.DoEvents();
        }
            
        
    }
}
