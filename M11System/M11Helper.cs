using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using M10.lib;
using System.Configuration;
using System.Xml;
using M11System.Model.M11;

namespace M11System
{
    public class M11Helper
    {

        private static string _ConnectionString;
        private string _ConnectionStringProcal;
        private static DALDapper _dbDapper;
        private DALDapper _dbDapperProcal;
        public static string ssql = string.Empty;
        //public Logger logger;
        private StockHelper _stockhelper;

        public static DALDapper dbDapper
        {
            get
            {
                if (_dbDapper == null)
                {
                    _dbDapper = new DALDapper(ConnectionString);
                }

                return _dbDapper;
            }
        }

        public static string ConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(_ConnectionString))
                {
                    _ConnectionString = ConfigurationManager.ConnectionStrings[ConfigurationManager.AppSettings["DBDefault"]].ConnectionString;
                }

                return _ConnectionString;
            }
        }

        public DALDapper dbDapperProcal
        {
            get
            {
                if (_dbDapperProcal == null)
                {
                    _dbDapperProcal = new DALDapper(ConnectionStringProcal);
                }

                return _dbDapperProcal;
            }
        }

        public string ConnectionStringProcal
        {
            get
            {
                if (string.IsNullOrEmpty(_ConnectionStringProcal))
                {
                    _ConnectionStringProcal = ConfigurationManager.ConnectionStrings[ConfigurationManager.AppSettings["DBProcal"]].ConnectionString;
                }

                return _ConnectionStringProcal;
            }
        }

        /// <summary>
        /// CGI資料移動到備份資料夾(傳入的FileInfo檔案不刪除)
        /// </summary>
        /// <param name="fi"></param>
        public static void M11BackupCopyToCGIData(FileInfo fi)
        {
            string[] CgiNameSplit = fi.Name.Replace(fi.Extension, "").Split('-');

            //避免舊檔案格式問題，排除沒有分析完整的檔案名稱
            if (CgiNameSplit.Length != 8)
            {
                return;
            }

            //從檔案取得資料時間
            DateTime dt = DateTime.ParseExact(CgiNameSplit[2] + CgiNameSplit[3] + CgiNameSplit[4] + CgiNameSplit[5] + CgiNameSplit[6] + CgiNameSplit[7], "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture);

            //建立資料夾
            //string sFolderYear = dt.ToString("yyyy");
            //string sFolderMonth = dt.ToString("yyyyMM");
            string sFolderDay = dt.ToString("yyyyMMdd");

            //string sBackupFolder = Path.Combine(M11Const.Path_BackupCGIData, sFolderYear, sFolderMonth, sFolderDay);
            string sBackupFolder = Path.Combine(M11Const.Path_BackupCGIData, sFolderDay);
            Directory.CreateDirectory(sBackupFolder);

            //存至備份資料夾
            fi.CopyTo(Path.Combine(sBackupFolder, fi.Name), true);

            
        }


        /// <summary>
        /// 產生Result XML 資料來源改為讀取資料庫產生XML
        /// </summary>
        /// <param name="dtCheck"></param>
        public static XmlDocument ProcGenResultXMLFromDB(DateTime dtCheck)
        {
            XmlDocument sFilePath = null;
            //string ssql = "";
            //DALDapper dbDapper = new DALDapper(ConnectionString);
            try
            {
                //讀取站點設定檔
                List<string> lstSite = new List<string>();
                ssql = @"
                    select * from  BasStationSensor where RenderXML_YN = 'Y'  order by Site,Station
                ";
                List<BasStationSensor> lstBasStationSensor = dbDapper.Query<BasStationSensor>(ssql);
                lstSite = lstBasStationSensor.Select(x => x.Site).Distinct().ToList();

                //調整時間為整點格式 如(2021-04-12 23:30:00)-"yyyy-MM-dd HH:mm:ss"
                dtCheck = Utils.getStringToDateTime(dtCheck.ToString("yyyy-MM-dd HH:mm:00"));
                
                
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
                        file_attribute.SetAttribute("time", M11Utils.M11DatetimeToString(dtCheck));
                        doc.AppendChild(file_attribute);
                        var tenmin_a_ds_data = doc.CreateElement("tenmin_a_ds_data");
                        file_attribute.AppendChild(tenmin_a_ds_data);

                        int iIndex = 1;
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

                            //station-取得站點清單
                            List<string> lstStation =
                                lstBasStationSensor.Where(x => x.Site == SiteName).Select(x => x.Station).Distinct().ToList();
                            foreach (string StationName in lstStation)
                            {
                                var station = doc.CreateElement("station");
                                station.SetAttribute("stationId", StationName);

                                //Sensor-取得感應器清單
                                List<BasStationSensor> lstSensor
                                    = lstBasStationSensor.Where(x => x.Station == StationName).ToList();
                                foreach (BasStationSensor SensorRow in lstSensor)
                                {
                                    DateTime dtGetDataTime = new DateTime();
                                    string sCgiData = "";
                                    string SensorName = SensorRow.Sensor; //SensorRow["Sensor"].ToString();
                                    string sobservation_num = getObservation_num(SensorName); //取得感應器對應的參數數量
                                    string ssensor_status = "0";
                                    string svalue = getSensorDataValueFromResult10MinData(SensorRow, dtCheck, ref dtGetDataTime, ref sCgiData);
                                    string sGetTime = dtCheck.ToString("yyyy-MM-dd HH:mm:00");

                                    var sensor = doc.CreateElement("sensor");
                                    sensor.SetAttribute("sensorId", string.Format("{0}-{1}", StationName, SensorName));
                                    sensor.SetAttribute("sensor_type", getSensor_Type(SensorName));
                                    sensor.SetAttribute("observation_num", sobservation_num);
                                    sensor.SetAttribute("sensor_status", ssensor_status);
                                    sensor.SetAttribute("time", sGetTime);
                                    sensor.InnerText = svalue;
                                    station.AppendChild(sensor);
                                }

                                site_data.AppendChild(station);
                            }


                            tenmin_a_ds_data.AppendChild(site_data);
                        }

                        doc.WriteTo(xmlWriter);
                        xmlWriter.Flush();

                        sFilePath = doc;

                        ////一般產出
                        //if (GenType.ToUpper() == "Normal")
                        //{
                        //    //儲存到網頁發布路徑
                        //    doc.Save(Path.Combine(M11Const.Path_XmlResultWeb, "10min_a_ds_data.xml"));
                        //}

                        ////儲存到網頁發布路徑-7天歷史資料區
                        ////doc.Save(Path.Combine(M11Const.Path_XmlResultWeb7Day, string.Format("{0}_{1}", dtCheck.ToString("yyyyMMddHHmm"), "10min_a_ds_data.xml")));
                        //// 20220210 調整網站歷史資料區存放路徑
                        ///*
                        // 按照目錄規範存放：/yyyy/mmdd/，需經/符號區隔
                        // */
                        ////1.建立路徑
                        //string Web7DaySavePath = Path.Combine(M11Const.Path_XmlResultWeb7Day, dtCheck.ToString("yyyy"), dtCheck.ToString("MMdd"));
                        //Directory.CreateDirectory(Web7DaySavePath);
                        ////2.存放資料
                        //doc.Save(Path.Combine(Web7DaySavePath, string.Format("{0}_{1}", dtCheck.ToString("HHmm"), "10min_a_ds_data.xml")));

                        ////儲存到準備FTP上傳路徑
                        //doc.Save(Path.Combine(M11Const.Path_FTPQueueXmlResult, string.Format("{0}_{1}", dtCheck.ToString("yyyyMMddHHmm"), "10min_a_ds_data.xml")));

                        ////儲存到歷史路徑
                        //doc.Save(Path.Combine(M11Const.Path_FTPQueueXmlResult7Day, string.Format("{0}_{1}", dtCheck.ToString("yyyyMMddHHmm"), "10min_a_ds_data.xml")));
                    }
                }


            }
            catch (Exception ex)
            {
                //logger.Error(ex, "M11XML_XMLResult 轉檔錯誤:");
            }

            return sFilePath;
        }

        /// <summary>
        /// 直接從資料表(Result10MinData)取得XML每個站點偵測器的資料
        /// </summary>
        /// <param name="StationName">站名</param>
        /// <param name="SensorName">儀器名稱</param>
        /// <param name="dtCheck">比對時間點</param>
        /// <returns></returns>
        private static string getSensorDataValueFromResult10MinData(BasStationSensor SensorRow, DateTime dtCheck, ref DateTime dtGetDataTime, ref string sCgiData)
        {
            string sResult = "";
            //string ssql = "";
            string StationName = SensorRow.Station;
            string SensorName = SensorRow.Sensor;
            string sDatetimeString = M11Utils.M11DatetimeToString(dtCheck);
            
            ssql = @"
                    select * from Result10MinData where stationid = '{0}' and sensorid = '{1}' and datetimestring = '{2}'
                    ";
            ssql = string.Format(ssql, StationName, SensorName, sDatetimeString);
            List<Result10MinData> lstData = dbDapper.Query<Result10MinData>(ssql);

            //此時刻資料庫有資料
            if (lstData.Count > 0)
            {
                Result10MinData oResult10MinData = lstData[0];
                sResult = oResult10MinData.value;
                sCgiData = "";
            }
            else //此時刻資料庫沒資料，取得最接近的資料
            {
                ssql = @"
                    select top 1 * from Result10MinData where stationid = '{0}' and sensorid = '{1}' and datetimestring < '{2}'
                    order by datetimestring desc
                    ";
                ssql = string.Format(ssql, StationName, SensorName, sDatetimeString);
                lstData = dbDapper.Query<Result10MinData>(ssql);

                if (lstData.Count > 0)
                {
                    Result10MinData oResult10MinData = lstData[0];
                    sResult = oResult10MinData.value;
                    sCgiData = "";
                }
            }

            //如果資料庫都沒資料時，提供預設值
            if (sResult == "")
            {
                sResult = GetDefaultValueBySensor(SensorRow);
            }

            return sResult;
        }

        /// <summary>
        /// XML結果資料的預設值
        /// </summary>
        /// <param name="SensorRow"></param>
        /// <returns></returns>
        private static string GetDefaultValueBySensor(BasStationSensor SensorRow)
        {
            string sResult = "";

            if (SensorRow.Sensor == "RG")
            {
                sResult = string.Format("{0} {1} {2} {3} {4} {5} {6} {7}"
                , 0, 0, 0, 0, 0, 0, 0, 0);
                return sResult;
            }

            if (SensorRow.Sensor == "TM" || SensorRow.Sensor == "TM1" || SensorRow.Sensor == "TM2")
            {
                string sSX = "0";
                string sSY = "0";
                string sD_before = "0";
                string sE_before = "0";

                //D方位一觀測值(秒)	E方位二觀測值(秒)	F方位一累積變位量(秒)	G方位二累積變位量(秒)	H方位一速率(秒/天)	I方位二速率(秒/天)
                //D=SX*3600	        E=SY*3600	    F=D-初始值(0)	        G=E-初始值(0)	        H=今天D-昨天D	    I=今天E-昨天E

                double dSX = 0;
                double dSY = 0;
                double dD = 0;
                double dE = 0;
                double dF = 0;
                double dG = 0;
                double dH = 0;
                double dI = 0;

                //一天前的資料
                double dD_before = 0;
                double dE_before = 0;

                double.TryParse(sSX, out dSX);
                double.TryParse(sSY, out dSY);
                double.TryParse(sD_before, out dD_before);
                double.TryParse(sE_before, out dE_before);
                dD = dSX * 3600;
                dE = dSY * 3600;
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

            if (SensorRow.Sensor == "GW")
            {
                string sWater = "0";
                //水位高(m)	相對水位高(m)
                //相對水位高 = 水位高 - 常時水位
                double dWater = 0;
                double dDefaultWater = 0;
                double dRelativeWater = 0;
                double.TryParse(sWater, out dWater);
                double.TryParse(SensorRow.DefaultWater, out dDefaultWater);
                dRelativeWater = dWater - dDefaultWater;

                //四捨五入小數點2位
                dWater = Math.Round(dWater, 2);
                dRelativeWater = Math.Round(dRelativeWater, 2);

                //儀器異常，則不顯示數值
                if (SensorRow.DefaultWater == "-999")
                {
                    dWater = 0;
                    dRelativeWater = 0;
                }

                sResult = string.Format("{0} {1}", dWater.ToString(), dRelativeWater.ToString());

                return sResult;
            }

            if (SensorRow.Sensor == "PM")
            {
                //預設為0
                string sWaterCgi = "0";

                //水位高(m)	相對水位高(m)
                // 202106121 新增水位高公式
                //水位高(m) = -(10-0.1*儀器回傳值)
                //相對水位高 = 水位高 - 常時水位
                double dWaterCgi = 0;       //儀器回傳值
                double dWater = 0;          //水位高
                double dDefaultWater = 0;   //常時水位
                double dRelativeWater = 0;  //相對水位高
                double.TryParse(sWaterCgi, out dWaterCgi);
                double.TryParse(SensorRow.DefaultWater, out dDefaultWater);
                dWater = -1 * (10 - (0.1 * dWaterCgi));
                dRelativeWater = dWater - dDefaultWater;

                //四捨五入小數點2位            
                dWater = Math.Round(dWater, 2);
                dRelativeWater = Math.Round(dRelativeWater, 2);

                //儀器異常，則不顯示數值
                if (SensorRow.DefaultWater == "-999")
                {
                    dWater = 0;
                    dRelativeWater = 0;
                }

                sResult = string.Format("{0} {1}", dWater.ToString(), dRelativeWater.ToString());

                return sResult;
            }

            if (SensorRow.Sensor == "GPS")
            {
                //預設為0 0 0 0 0 0
                string sGpsData = "0 0 0 0 0 0";
                sResult = sGpsData;

                //儀器異常，則不顯示數值
                if (SensorRow.DefaultWater == "-999")
                {
                    sResult = sGpsData;
                }

                return sResult;
            }


            return sResult;
        }

        /// <summary>
        /// 取得偵測器在XML上面的名稱
        /// </summary>
        /// <param name="SensorName"></param>
        /// <returns></returns>
        public static string getSensor_Type(string SensorName)
        {
            string sResult = "";

            List<BasM11Setting> oList = GetBasM11Setting(M11Const.BasM11SettingDataType_SensorObs_num);
            sResult = oList.Where(x => x.DataItem == SensorName).Select(x => x.DataRemark).DefaultIfEmpty<string>("").First();

            return sResult;


            //switch (SensorName)
            //{
            //    case "RG":
            //        sResult = "RainGauge";
            //        break;
            //    case "TM":
            //    case "TM1":
            //    case "TM2":
            //        sResult = "BiTiltMeter";
            //        break;
            //    case "GW":
            //        sResult = "ObservationWell";
            //        break;
            //    case "PM":
            //        sResult = "PiezoMeter";
            //        break;
            //    case "GPS":
            //        sResult = "GPSForecast3db";
            //        break;
            //    //case "RG":
            //    //    sResult = "RainGauge";
            //    //    break;
            //    //case "RG":
            //    //    sResult = "RainGauge";
            //    //    break;
            //    default:
            //        sResult = "";
            //        break;
            //}

            //return sResult;
        }

        /// <summary>
        /// 取得感應器對應的參數數量
        /// </summary>
        /// <param name="SensorName"></param>
        /// <returns></returns>
        public static string getObservation_num(string SensorName)
        {
            string sResult = "";

            List<BasM11Setting> oList = GetBasM11Setting(M11Const.BasM11SettingDataType_SensorObs_num);
            sResult = oList.Where(x => x.DataItem == SensorName).Select(x => x.DataValue).DefaultIfEmpty<string>("").First();
           
            return sResult;
        }


        public static List<BasM11Setting> GetBasM11Setting(string DataType)
        {
            List<BasM11Setting> oResult = new List<BasM11Setting>();

            ssql = @"
                    select * from BasM11Setting where DataType = '{0}'
                    ";
            ssql = string.Format(ssql, DataType);
            oResult  = dbDapper.Query<BasM11Setting>(ssql);

            return oResult;
        }


    }
}
