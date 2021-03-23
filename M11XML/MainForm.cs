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



namespace M11XML
{
    public partial class MainForm : BaseForm
    {
        //string Path_Original = @"D:\M11\Data\Original";
        //string Path_XmlResult = @"D:\M11\Data\XmlResult";
        //string Path_FTPQueueTxtOriginal = @"D:\M11\Data\FTPQueueTxtOriginal";
        //string Path_FTPQueueXmlResult = @"D:\M11\Data\FTPQueueXmlResult";
        //string Path_FTPQueueTxtOriginalBak = @"D:\M11\Data\FTPQueueTxtOriginalBak";
        //string Path_FTPQueueXmlResultBak = @"D:\M11\Data\FTPQueueXmlResultBak";
        //string Path_DBSimulation = @"D:\M11\DBSimulation";
        //string Path_XmlResultWeb = @"D:\M11\M11Web\M11Service\R01";

        //string FilePath_SchemaCGIData = Path.Combine(@"D:\M11\DBSimulation", "Schema_CGIData.xml");
        //string FilePath_SchemaSetSatation = Path.Combine(@"D:\M11\DBSimulation", "Schema_SetSatation.xml");
        //string FilePath_SetSatation = Path.Combine(@"D:\M11\DBSimulation", "SetSatation.xml");
        //string FilePath_SchemaSetSensor = Path.Combine(@"D:\M11\DBSimulation", "Schema_SetSensor.xml");
        //string FilePath_SetSensor = Path.Combine(@"D:\M11\DBSimulation", "SetSensor.xml");

        ////1040806 新的ftp主機
        //string sIP = "140.116.38.196";
        ////string sUser = "FCU2015";
        ////string sPassword = "FCU2015";
        ////之後新增一個M11專屬使用者
        //string sUser = "admin";
        //string sPassword = "@@hydjan222!!";



        public MainForm()
        {
            InitializeComponent();
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


            

            try
            {
                //上傳FTP
                //ProcUploadFTP();

                //return;

                //產生Station設定檔中的XML
                InitStationXML();

                //讀取原始資料轉檔到XML資料庫
                ReadDataToXMLDB();

                //產生結果XML(每十分鐘)
                ProcGenResultXML();

                //上傳FTP
                ProcUploadFTP();

            }
            catch (Exception)
            {

                throw;
            }
            //DateTime dtCheck = new DateTime(2021, 3, 17, 14, 10, 8);
            //getRainGauge("XINZH_01", "RG", dtCheck);



            

            System.Threading.Thread.Sleep(5000);

            this.Close();
        }


        

        DataTable dtStationData = null;
        DataTable DtStationData
        {
            get
            {
                if (dtStationData == null)
                {
                    dtStationData = GetStationSetData();
                }

                return dtStationData;
            }
        }

        DataTable dtSensorData = null;
        DataTable DtSensorData
        {
            get
            {
                if (dtSensorData == null)
                {
                    dtSensorData = GetSensorSetData();
                }

                return dtSensorData;
            }
        }

     
        /// <summary>
        /// 
        /// </summary>
        /// <param name="SensorRow">感應器資料</param>
        /// <param name="dtCheck">XML產生時間</param>
        /// <param name="dtGetDataTime">取得資料時間</param>
        /// <returns></returns>
        private string getSensorDataResult(DataRow SensorRow ,DateTime dtCheck, ref DateTime dtGetDataTime) 
        {
            string sResult = "";
            string SensorName = SensorRow["Sensor"].ToString();
            switch (SensorName)
            {
                case "RG":
                    sResult = getRainGauge(SensorRow, dtCheck, ref dtGetDataTime);
                    break;
                case "TM":
                case "TM1":
                case "TM2":
                    sResult = getBiTiltMeter(SensorRow, dtCheck, ref dtGetDataTime); 
                    break;
                case "GW":
                    sResult = getObservationWell(SensorRow, dtCheck, ref dtGetDataTime);
                    break;
                case "PM":
                    sResult = getPiezoMeter(SensorRow, dtCheck, ref dtGetDataTime);
                    break;
                //case "RG":
                //    sResult = "RainGauge";
                //    break;
                //case "RG":
                //    sResult = "RainGauge";
                //    break;
                default:
                    sResult = "";
                    break;
            }

            return sResult;
        }


        /// <summary>
        /// 取得雨量資料結果(RG)
        /// </summary>
        /// <param name="StationName">站名</param>
        /// <param name="SensorName">儀器名稱</param>
        /// <param name="dtCheck">比對時間點</param>
        /// <returns></returns>
        private string getRainGauge(DataRow SensorRow, DateTime dtCheck, ref DateTime dtGetDataTime)
        {
            string sResult = "";
            string StationName = SensorRow["Station"].ToString();
            string SensorName = SensorRow["Sensor"].ToString();


            DataTable dtStationData = GetStationData(StationName);


            //將資料全部先彙整再處理(72h*6 每十分鐘一筆)
            List<string> lstRain = new List<string>();
            for (int i = 0; i < 432; i++)
            {
                DateTime dtStart = dtCheck.AddMinutes(-10 * i);
                DateTime dt10m = dtCheck.AddMinutes(-10 * (i + 1));

                string sRain = "";
                DataRow[] dr10m = dtStationData.Select(
                    string.Format(" datetime >= '{0}' and datetime <= '{1}' ", dt10m.ToString("yyyyMMddHHmmss"), dtStart.ToString("yyyyMMddHHmmss"))
                    , " datetime desc ");
                if (dr10m.Count() == 0)
                {
                    sRain = "0";
                }
                else
                {
                    sRain = dr10m[0][CgiConst.RAIN].ToString();

                    //第一筆資料紀錄資料時間
                    if (i == 0)
                    {
                        dtGetDataTime = Utils.getStringToDateTime(dr10m[0][CgiConst.DATETIME].ToString());
                    }
                }

                lstRain.Add(sRain);
            }

            double dtemp = 0;
            string sRain10m = "0";
            string sRain1h = "0";
            string sRain3h = "0";
            string sRain6h = "0";
            string sRain12h = "0";
            string sRain24h = "0";
            string sRain48h = "0";
            string sRain72h = "0";
            double dRain10m = 0;
            double dRain1h = 0;
            double dRain3h = 0;
            double dRain6h = 0;
            double dRain12h = 0;
            double dRain24h = 0;
            double dRain48h = 0;
            double dRain72h = 0;

            //10m
            dtemp = 0;
            double.TryParse(lstRain[0], out dtemp);
            dRain10m = dtemp;

            //1h
            dtemp = 0;
            for (int i = 0; i < 6 * 1; i++)
            {
                double dLoop = 0;
                double.TryParse(lstRain[i], out dLoop);
                dtemp = dtemp + dLoop;
            }
            dRain1h = dtemp;

            //3h
            dtemp = 0;
            for (int i = 0; i < 6 * 3; i++)
            {
                double dLoop = 0;
                double.TryParse(lstRain[i], out dLoop);
                dtemp = dtemp + dLoop;
            }
            dRain3h = dtemp;

            //6h
            dtemp = 0;
            for (int i = 0; i < 6 * 6; i++)
            {
                double dLoop = 0;
                double.TryParse(lstRain[i], out dLoop);
                dtemp = dtemp + dLoop;
            }
            dRain6h = dtemp;

            //12h
            dtemp = 0;
            for (int i = 0; i < 6 * 12; i++)
            {
                double dLoop = 0;
                double.TryParse(lstRain[i], out dLoop);
                dtemp = dtemp + dLoop;
            }
            dRain12h = dtemp;

            //24h
            dtemp = 0;
            for (int i = 0; i < 6 * 24; i++)
            {
                double dLoop = 0;
                double.TryParse(lstRain[i], out dLoop);
                dtemp = dtemp + dLoop;
            }
            dRain24h = dtemp;

            //48h
            dtemp = 0;
            for (int i = 0; i < 6 * 48; i++)
            {
                double dLoop = 0;
                double.TryParse(lstRain[i], out dLoop);
                dtemp = dtemp + dLoop;
            }
            dRain48h = dtemp;

            //72h
            dtemp = 0;
            for (int i = 0; i < 6 * 72; i++)
            {
                double dLoop = 0;
                double.TryParse(lstRain[i], out dLoop);
                dtemp = dtemp + dLoop;
            }
            dRain72h = dtemp;


            //四捨五入小數點1位
            dRain10m = Math.Round(dRain10m, 1);
            dRain1h = Math.Round(dRain1h, 1);
            dRain3h = Math.Round(dRain3h, 1);
            dRain6h = Math.Round(dRain6h, 1);
            dRain12h = Math.Round(dRain12h, 1);
            dRain24h = Math.Round(dRain24h, 1);
            dRain48h = Math.Round(dRain48h, 1);
            dRain72h = Math.Round(dRain72h, 1);

            sRain10m = dRain10m.ToString();
            sRain1h = dRain1h.ToString();
            sRain3h = dRain3h.ToString();
            sRain6h = dRain6h.ToString();
            sRain12h = dRain12h.ToString();
            sRain24h = dRain24h.ToString();
            sRain48h = dRain48h.ToString();
            sRain72h = dRain72h.ToString();

            sResult = string.Format("{0} {1} {2} {3} {4} {5} {6} {7}"
                , sRain10m, sRain1h, sRain3h, sRain6h, sRain12h, sRain24h, sRain48h, sRain72h);

            return sResult;
        }

        /// <summary>
        /// 取得水位資料結果(GW)
        /// </summary>
        /// <param name="StationName">站名</param>
        /// <param name="SensorName">儀器名稱</param>
        /// <param name="dtCheck">比對時間點</param>
        /// <returns></returns>
        private string getObservationWell(DataRow SensorRow, DateTime dtCheck, ref DateTime dtGetDataTime)
        {
            string sResult = "";
            string StationName = SensorRow["Station"].ToString();
            string SensorName = SensorRow["Sensor"].ToString();

            DataTable dtStationData = GetStationData(StationName);


            DateTime dtStart = dtCheck;
            DateTime dt10m = dtCheck.AddMinutes(-10);

            string sWater = "";
            DataRow[] dr10m = dtStationData.Select(
                string.Format(" datetime >= '{0}' and datetime <= '{1}' ", dt10m.ToString("yyyyMMddHHmmss"), dtStart.ToString("yyyyMMddHHmmss"))
                , " datetime desc ");
            if (dr10m.Count() == 0)
            {
                sWater = "0";
            }
            else
            {
                sWater = dr10m[0][CgiConst.WATER].ToString();
                //紀錄資料時間
                dtGetDataTime = Utils.getStringToDateTime(dr10m[0][CgiConst.DATETIME].ToString());
            }

            //水位高(m)	相對水位高(m)
            //相對水位高 = 水位高 - 常時水位
            double dWater = 0;
            double dDefaultWater = 0;
            double dRelativeWater = 0;
            double.TryParse(sWater, out dWater);
            double.TryParse(SensorRow["DefaultWater"].ToString(), out dDefaultWater);
            dRelativeWater = dWater - dDefaultWater;

            //四捨五入小數點2位
            dWater = Math.Round(dWater, 2);
            dRelativeWater = Math.Round(dRelativeWater, 2);

            //儀器異常，則不顯示數值
            if (SensorRow["DefaultWater"].ToString() == "-999")
            {
                dWater = 0;
                dRelativeWater = 0;
            }

            sResult = string.Format("{0} {1}", dWater.ToString(), dRelativeWater.ToString());

            return sResult;
        }

        /// <summary>
        /// 取得水位資料結果(PM)
        /// </summary>
        /// <param name="StationName">站名</param>
        /// <param name="SensorName">儀器名稱</param>
        /// <param name="dtCheck">比對時間點</param>
        /// <returns></returns>
        private string getPiezoMeter(DataRow SensorRow, DateTime dtCheck, ref DateTime dtGetDataTime)
        {
            string sResult = "";
            string StationName = SensorRow["Station"].ToString();
            string SensorName = SensorRow["Sensor"].ToString();

            DataTable dtStationData = GetStationData(StationName);


            DateTime dtStart = dtCheck;
            DateTime dt10m = dtCheck.AddMinutes(-10);

            string sWater = "";
            DataRow[] dr10m = dtStationData.Select(
                string.Format(" datetime >= '{0}' and datetime <= '{1}' ", dt10m.ToString("yyyyMMddHHmmss"), dtStart.ToString("yyyyMMddHHmmss"))
                , " datetime desc ");
            if (dr10m.Count() == 0)
            {
                sWater = "0";
            }
            else
            {
                sWater = dr10m[0][CgiConst.WATER].ToString();

                //紀錄資料時間
                dtGetDataTime = Utils.getStringToDateTime(dr10m[0][CgiConst.DATETIME].ToString());
            }

            //水位高(m)	相對水位高(m)
            //相對水位高 = 水位高 - 常時水位
            double dWater = 0;
            double dDefaultWater = 0;
            double dRelativeWater = 0;
            double.TryParse(sWater, out dWater);
            double.TryParse(SensorRow["DefaultWater"].ToString(), out dDefaultWater);
            dRelativeWater = dWater - dDefaultWater;

            //四捨五入小數點2位            
            dWater = Math.Round(dWater, 2);
            dRelativeWater = Math.Round(dRelativeWater, 2);

            //儀器異常，則不顯示數值
            if (SensorRow["DefaultWater"].ToString() == "-999")
            {
                dWater = 0;
                dRelativeWater = 0;
            }

            sResult = string.Format("{0} {1}", dWater.ToString(), dRelativeWater.ToString());

            return sResult;
        }

        /// <summary>
        /// 取得水位資料結果(TM)
        /// </summary>
        /// <param name="StationName">站名</param>
        /// <param name="SensorName">儀器名稱</param>
        /// <param name="dtCheck">比對時間點</param>
        /// <returns></returns>
        private string getBiTiltMeter(DataRow SensorRow, DateTime dtCheck, ref DateTime dtGetDataTime)
        {
            string sResult = "";
            string StationName = SensorRow["Station"].ToString();
            string SensorName = SensorRow["Sensor"].ToString();

            DataTable dtStationData = GetStationData(StationName);


            DateTime dtStart = dtCheck;
            DateTime dt10m = dtCheck.AddMinutes(-10);

            string sSX = "0";
            string sSY = "0";
            DataRow[] dr10m = dtStationData.Select(
                string.Format(" datetime >= '{0}' and datetime <= '{1}' ", dt10m.ToString("yyyyMMddHHmmss"), dtStart.ToString("yyyyMMddHHmmss"))
                , " datetime desc ");
            if (dr10m.Count() == 0)
            {
                sSX = "0";
                sSY = "0";
            }
            else
            {
                sSX = dr10m[0][CgiConst.SX].ToString();
                sSY = dr10m[0][CgiConst.SY].ToString();

                //紀錄資料時間
                dtGetDataTime = Utils.getStringToDateTime(dr10m[0][CgiConst.DATETIME].ToString());
                
            }

            //一天前的資料
            DateTime dtStart_before = dtCheck.AddMinutes(-10 * 144);
            DateTime dt10m_before = dtCheck.AddMinutes(-10 * (144 + 1));
            string sSX_before = "0";
            string sSY_before = "0";
            DataRow[] dr10m_before = dtStationData.Select(
                string.Format(" datetime >= '{0}' and datetime <= '{1}' ", dt10m_before.ToString("yyyyMMddHHmmss"), dtStart_before.ToString("yyyyMMddHHmmss"))
                , " datetime desc ");
            if (dr10m_before.Count() == 0)
            {
                sSX_before = "0";
                sSY_before = "0";
            }
            else
            {
                sSX_before = dr10m_before[0][CgiConst.SX].ToString();
                sSY_before = dr10m_before[0][CgiConst.SY].ToString();
            }

            //D方位一觀測值(秒)	E方位二觀測值(秒)	F方位一累積變位量(秒)	G方位二累積變位量(秒)	H方位一速率(秒/天)	I方位二速率(秒/天)
            //D=SX*3600	        E=SY*3600	    F=D-初始值(0)	    G=E-初始值(0)	    H=今天D-昨天D	    I=今天E-昨天E

            double dSX = 0;
            double dSY = 0;
            double dD = 0;
            double dE = 0;
            double dF = 0;
            double dG = 0;
            double dH = 0;
            double dI = 0;

            //一天前的資料
            double dSX_before = 0;
            double dSY_before = 0;
            double dD_before = 0;
            double dE_before = 0;

            double.TryParse(sSX, out dSX);
            double.TryParse(sSY, out dSY);
            double.TryParse(sSX_before, out dSX_before);
            double.TryParse(sSY_before, out dSY_before);
            dD = dSX * 3600;
            dE = dSY * 3600;
            dD_before = dSX_before * 3600;
            dE_before = dSY_before * 3600;
            dF = dD - 0;
            dG = dE - 0;
            dH = dD - dD_before;
            dI = dE - dE_before;

            //四捨五入整數位
            dD = Math.Round(dD, 0);
            dE = Math.Round(dE, 0);
            dF = Math.Round(dF, 0);
            dG = Math.Round(dG, 0);
            dH = Math.Round(dH, 0);
            dI = Math.Round(dI, 0);


            sResult = string.Format("{0} {1} {2} {3} {4} {5}", dD.ToString(), dE.ToString()
                , dF.ToString(), dG.ToString(), dH.ToString(), dI.ToString());

            return sResult;
        }


        /// <summary>
        /// 產生結果XML(每十分鐘)
        /// </summary>
        private void ProcGenResultXML() 
        {
            try
            {
                //預計排程每分鐘執行一次，排除非剛好10分鐘的執行(00,10,20,30,40,50)
                DateTime dtCheck = DateTime.Now;
#if DEBUG
                //dtCheck = new DateTime(2021, 3, 17, 14, 10, 8);
#endif

                if (dtCheck.Minute.ToString().PadLeft(2, '0').Substring(1, 1) != "0") return;

                List<string> lstSite = new List<string>();
                foreach (DataRow TmpRow in DtStationData.Rows)
                {
                    if (lstSite.Contains(TmpRow["site"].ToString()) == false)
                    {
                        lstSite.Add(TmpRow["site"].ToString());
                    }
                }

                using (StringWriter stringWriter = new StringWriter())
                {
                    using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter))
                    {
                        XmlDocument doc = new XmlDocument();
                        XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
                        doc.AppendChild(dec);
                        var file_attribute = doc.CreateElement("file_attribute");
                        file_attribute.SetAttribute("file_name", "10min_a_ds_data.xml");
                        file_attribute.SetAttribute("mteam_id", "ESWCRC");
                        file_attribute.SetAttribute("time", M11DatetimeToString(dtCheck));
                        doc.AppendChild(file_attribute);
                        var tenmin_a_ds_data = doc.CreateElement("tenmin_a_ds_data");
                        file_attribute.AppendChild(tenmin_a_ds_data);

                        foreach (string SiteName in lstSite)
                        {
                            //site_data
                            var site_data = doc.CreateElement("site_data");
                            site_data.SetAttribute("siteid", SiteName);
                            site_data.SetAttribute("monitoring_light", "Green");

                            //factorInfo
                            var factorInfo = doc.CreateElement("factorInfo");
                            factorInfo.SetAttribute("factors_num", "0");
                            site_data.AppendChild(factorInfo);

                            //station
                            DataRow[] tmpRows = DtStationData.Select(string.Format(" Site = '{0}' ", SiteName));
                            foreach (DataRow StationRow in tmpRows)
                            {
                                string StationName = StationRow["Station"].ToString();
                                var station = doc.CreateElement("station");
                                station.SetAttribute("stationId", StationName);

                                //Sensor
                                tmpRows = DtSensorData.Select(string.Format(" Station = '{0}' ", StationName));
                                foreach (DataRow SensorRow in tmpRows)
                                {
                                    string SensorName = SensorRow["Sensor"].ToString();
                                    var sensor = doc.CreateElement("sensor");
                                    sensor.SetAttribute("sensorId", string.Format("{0}-{1}", StationName, SensorName));
                                    sensor.SetAttribute("sensor_type", getSensor_Type(SensorName));
                                    sensor.SetAttribute("observation_num", getObservation_num(SensorName));
                                    sensor.SetAttribute("sensor_status", "0");
                                    DateTime dtGetDataTime = new DateTime();
                                    sensor.InnerText = getSensorDataResult(SensorRow, dtCheck, ref dtGetDataTime);
                                    if (dtGetDataTime.Year == 1) //沒有取得資料，所以回傳預設時間
                                    {
                                        sensor.SetAttribute("time", "0000-00-00 00:00:00");
                                    }
                                    else
                                    {
                                        sensor.SetAttribute("time", M11DatetimeToString(dtGetDataTime));
                                    }
                                    
                                    
                                    station.AppendChild(sensor);
                                }

                                site_data.AppendChild(station);
                            }


                            tenmin_a_ds_data.AppendChild(site_data);
                        }

                        var body = doc.CreateElement("body");
                        file_attribute.AppendChild(body);

                        doc.WriteTo(xmlWriter);
                        xmlWriter.Flush();

                        //儲存到網頁發布路徑
                        doc.Save(Path.Combine(M11Const.Path_XmlResultWeb, "10min_a_ds_data.xml"));

                        //儲存到準備FTP上傳路徑
                        doc.Save(Path.Combine(M11Const.Path_FTPQueueXmlResult, string.Format("{0}_{1}", dtCheck.ToString("yyyyMMddHHmm"), "10min_a_ds_data.xml")));
                    }
                }
            }
            catch (Exception)
            {

                
            }
        }


        private void UploadFTPCgi() 
        {
            List<string> lstFile = new List<string>();

            //上傳CgiData
            FtpClient client = new FtpClient();
            try
            {
                
                client.Host = M11Const.FTP_IP;
                client.SocketKeepAlive = true;
                client.Credentials = new NetworkCredential(M11Const.FTP_User, M11Const.FTP_Password);
                client.Connect();

                // 取得資料夾內所有檔案
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
            foreach (string fname in lstFile)
            {
                FileInfo fi = new FileInfo(fname);

                //存至備份資料夾
                fi.CopyTo(Path.Combine(M11Const.Path_FTPQueueTxtOriginalBak, fi.Name), true);
                
                //刪除已處理資料
                fi.Delete();
            }
        }

        private void UploadFTPXmlResult()
        {
            List<string> lstFile = new List<string>();
            
            //上傳XmlResult
            FtpClient client = new FtpClient();
            try
            {
                client.Host = M11Const.FTP_IP;
                client.SocketKeepAlive = true;
                client.Credentials = new NetworkCredential(M11Const.FTP_User, M11Const.FTP_Password);
                client.Connect();

                // 取得資料夾內所有檔案
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
            

            //全部處理完畢再一次刪除
            foreach (string fname in lstFile)
            {
                FileInfo fi = new FileInfo(fname);
                
                //存至備份資料夾
                fi.CopyTo(Path.Combine(M11Const.Path_FTPQueueXmlResultBak, fi.Name), true);
                //刪除已處理資料
                fi.Delete();
            }
        }

        /// <summary>
        /// 上傳FTP序列中的檔案
        /// </summary>
        private void ProcUploadFTP()
        {
            UploadFTPCgi();

            UploadFTPXmlResult();
        }


        private string getSensor_Type(string SensorName) 
        {
            string sResult = "";

            switch (SensorName)
            {
                case "RG":
                    sResult = "RainGauge";
                    break;
                case "TM":
                case "TM1":
                case "TM2":
                    sResult = "BiTiltMeter";
                    break;
                case "GW":
                    sResult = "ObservationWell";
                    break;
                case "PM":
                    sResult = "PiezoMeter";
                    break;
                //case "RG":
                //    sResult = "RainGauge";
                //    break;
                //case "RG":
                //    sResult = "RainGauge";
                //    break;
                default:
                    sResult = "";
                    break;
            }

            return sResult;
        }

        private string getObservation_num(string SensorName)
        {
            string sResult = "";

            switch (SensorName)
            {
                case "RG":
                    sResult = "8";
                    break;
                case "TM":
                case "TM1":
                case "TM2":
                    sResult = "6";
                    break;
                case "GW":
                    sResult = "2";
                    break;
                case "PM":
                    sResult = "2";
                    break;
                //case "RG":
                //    sResult = "RainGauge";
                //    break;
                //case "RG":
                //    sResult = "RainGauge";
                //    break;
                default:
                    sResult = "";
                    break;
            }

            return sResult;
        }

        /// <summary>
        /// 取得Station設定檔資料
        /// </summary>
        /// <returns></returns>
        private DataTable GetStationSetData() 
        {
            DataTable dt = new DataTable();
            dt.ReadXmlSchema(M11Const.FilePath_SchemaSetSatation);
            dt.ReadXml(M11Const.FilePath_SetSatation);

            return dt;
        }

        /// <summary>
        /// 取得Sensor設定檔資料
        /// </summary>
        /// <returns></returns>
        private DataTable GetSensorSetData()
        {
            DataTable dt = new DataTable();
            dt.ReadXmlSchema(M11Const.FilePath_SchemaSetSensor);
            dt.ReadXml(M11Const.FilePath_SetSensor);

            return dt;
        }

        /// <summary>
        /// 取得Station資料
        /// </summary>
        /// <returns></returns>
        private DataTable GetStationData(string StationName)
        {
            //判斷Station資料如果不存在，則返回null
            if (File.Exists(Path.Combine(M11Const.Path_DBSimulation, string.Format("{0}.xml", StationName))) == false) 
                return null;

            DataTable dt = new DataTable();
            
            dt.ReadXmlSchema(M11Const.FilePath_SchemaCGIData);
            dt.ReadXml(Path.Combine(M11Const.Path_DBSimulation, string.Format("{0}.xml", StationName)));

            return dt;
        }


        /// <summary>
        /// 初始化StationXML資料
        /// </summary>
        /// <param name="StationName"></param>
        private void InitStationXML() 
        {
            try
            {
                foreach (DataRow dr in DtStationData.Rows)
                {
                    string StationFileName = string.Format("{0}.xml", dr["Station"].ToString());
                    //判斷檔案是否已經存在，不存在才產生
                    if (System.IO.File.Exists(Path.Combine(M11Const.Path_DBSimulation, StationFileName)) == true) continue;

                    DataTable dt = new DataTable();
                    dt.ReadXmlSchema(Path.Combine(M11Const.Path_DBSimulation, "Schema_CGIData.xml"));
                    dt.WriteXml(Path.Combine(M11Const.Path_DBSimulation, StationFileName));
                }
            }
            catch (Exception ex)
            {

                
            }
        }


        /// <summary>
        /// 讀取原始資料轉檔到XML資料庫
        /// </summary>
        private void ReadDataToXMLDB()
        {
            try
            {
                // 取得資料夾內所有檔案
                foreach (string fname in Directory.GetFiles(M11Const.Path_Original))
                {
                    //轉檔到XML
                    TransToXML(fname);

                    //FileInfo fi = new FileInfo(fname);

                    //記錄轉檔資料
                    //FileTransLog(fi.Name);

                    //自動歸檔到NAS(FTP)
                    //ArrangeTransFile(fi);

                    //存至備份資料夾
                    //fi.CopyTo(folderBack + fi.Name, true);

                    //刪除已處理資料
                    //fi.Delete();
                }

                //全部處理完畢再一次刪除
                foreach (string fname in Directory.GetFiles(M11Const.Path_Original))
                {
                    FileInfo fi = new FileInfo(fname);

                    //刪除已處理資料
                    fi.Delete();
                }
            }
            catch (Exception)
            {

                
            }
        }

        private Dictionary<string, string> TransCGIData(string FileName) 
        {
            Dictionary<string, string> di = new Dictionary<string, string>();

            string line;

            using (StreamReader file = new StreamReader(FileName)) 
            {
                while ((line = file.ReadLine()) != null)
                {
                    string[] sLine = line.Split('=');
                    if (sLine.Count() == 2)
                    {
                        di.Add(sLine[0], sLine[1]);
                    }
                }
            }            

            return di;
        }

        /// <summary>
        /// 資料轉檔到XML資料庫
        /// </summary>
        /// <param name="fname"></param>
        private void TransToXML(string fname)
        {
            //分析時間
            FileInfo fi = new FileInfo(fname);            
            string[] CgiNameSplit = fi.Name.Replace(fi.Extension, "").Split('-');

            //避免舊檔案格式問題，排除沒有分析完整的檔案名稱
            if (CgiNameSplit.Length != 8) return;

            string StationName = CgiNameSplit[1];
            DateTime dt = DateTime.ParseExact(CgiNameSplit[2] + CgiNameSplit[3] + CgiNameSplit[4] + CgiNameSplit[5] + CgiNameSplit[6] + CgiNameSplit[7], "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture);

            //解析檔案中的資料
            Dictionary<string, string> di = TransCGIData(fi.FullName);

            //取得文字檔資料
            DataTable dtStationData = GetStationData(StationName);

            //判斷站名是否已經存在設定檔，如果資料不存在則返回
            if (dtStationData == null) return;

            //todo 判斷重複資料
            if (dtStationData.Select(" datetime = '" + dt.ToString("yyyyMMddHHmmss") + "' ").Length > 0) return;

            DataRow dr = dtStationData.NewRow();
            dr["datetime"] = dt.ToString("yyyyMMddHHmmss");

            //文字檔資料塞入資料表
            foreach (string key in di.Keys)
            {
                if (dr.Table.Columns.Contains(key) == true)
                {
                    dr[key] = di[key] == "None" ? "" : di[key];
                }
            }

            dtStationData.Rows.Add(dr);

            DateTime dtTemp = DateTime.Now.AddDays(-4);

            //移除超過四天資料
            DataRow[] rows = dtStationData.Select("datetime < '" + dtTemp.ToString("yyyyMMddHHmmss") + "' ");
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i].Delete();
            }


            //寫入
            dtStationData.WriteXml(Path.Combine(M11Const.Path_DBSimulation, string.Format("{0}.xml", StationName)));

        }

        /// <summary>
        /// M11專案Datetime轉文字時間格式(yyyy-MM-dd HH:mm:ss)
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        private string M11DatetimeToString(DateTime dt)
        {
            string sResult = "";
            sResult = dt.ToString("yyyy-MM-dd HH:mm:ss");
            return sResult;
        }
    }


    public class CgiConst 
    {
        public const string DATETIME = "datetime";
        public const string CGI = "cgi";
        public const string ID = "ID";
        public const string NAME = "name";
        public const string RAIN = "RAIN";
        public const string WATER = "water";
        public const string SX = "SX";
        public const string SY = "SY";
        public const string TEST22 = "test22";
    }
}
