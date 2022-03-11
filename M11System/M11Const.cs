using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace M11System
{
    public static class M11Const
    {
        /// <summary>
        /// @"D:\M11\Data\Original"
        /// </summary>
        public static string Path_Original = @"D:\M11\Data\Original";
        /// <summary>
        /// @"D:\M11\Data\XmlResult"
        /// </summary>
        public static string Path_XmlResult = @"D:\M11\Data\XmlResult";
        /// <summary>
        /// @"D:\M11\Data\FTPQueueTxtOriginal"
        /// </summary>
        public static string Path_FTPQueueTxtOriginal = @"D:\M11\Data\FTPQueueTxtOriginal";
        /// <summary>
        /// @"D:\M11\Data\FTPQueueTxtOriginalBak"
        /// </summary>
        public static string Path_FTPQueueTxtOriginalBak = @"D:\M11\Data\FTPQueueTxtOriginalBak";
        /// <summary>
        /// @"D:\M11\Data\FTPQueueXmlResult"
        /// </summary>
        public static string Path_FTPQueueXmlResult = @"D:\M11\Data\FTPQueueXmlResult";
        /// <summary>
        /// @"D:\M11\Data\FTPQueueXmlResultBak"
        /// </summary>
        public static string Path_FTPQueueXmlResultBak = @"D:\M11\Data\FTPQueueXmlResultBak";
        /// <summary>
        /// @"D:\M11\Data\FTPQueueXmlResult7Day"
        /// </summary>
        public static string Path_FTPQueueXmlResult7Day = @"D:\M11\Data\FTPQueueXmlResult7Day";
        /// <summary>
        /// @"D:\M11\Data\FTPQueueCcdResult"
        /// </summary>
        public static string Path_FTPQueueCcdResult = @"D:\M11\Data\FTPQueueCcdResult";
        /// <summary>
        /// @"D:\M11\Data\FTPQueueGPSData"
        /// </summary>
        public static string Path_FTPQueueGPSData = @"D:\M11\Data\FTPQueueGPSData";
        /// <summary>
        /// @"D:\M11\Data\CcdResultHist"
        /// </summary>
        public static string Path_CcdResultHist = @"D:\M11\Data\CcdResultHist";
        /// <summary>
        /// @"D:\M11\DBSimulation"
        /// </summary>
        public static string Path_DBSimulation = @"D:\M11\DBSimulation";

        //備份相關
        /// <summary>
        /// @"D:\M11\Data\20-BackupData\CGIData"
        /// </summary>
        public static string Path_BackupCGIData = @"D:\M11\Data\20-BackupData\CGIData";
        /// <summary>
        /// @"D:\M11\Data\20-BackupData\CGIDataZip"
        /// </summary>
        public static string Path_BackupCGIDataZip = @"D:\M11\Data\20-BackupData\CGIDataZip";

        //GPS相關
        /// <summary>
        /// @"D:\FTP\Data\chiiso"
        /// </summary>
        public static string Path_ChiisoSource = @"D:\FTP\Data\chiiso";

        //發布網站相關
        /// <summary>
        /// @"D:\M11\M11Web\dsmon\am"
        /// </summary>
        public static string Path_XmlResultWeb = @"D:\M11\M11Web\dsmon\am";
        /// <summary>
        /// @"D:\M11\M11Web\dsmon\amhist"
        /// </summary>
        public static string Path_XmlResultWeb7Day = @"D:\M11\M11Web\dsmon\amhist";
        /// <summary>
        /// @"D:\M11\M11Web\dsmon\pmhist"
        /// </summary>
        public static string Path_PrecipitationWeb7Day = @"D:\M11\M11Web\dsmon\pmhist";
        /// <summary>
        /// @"D:\M11\M11Web\dsmon\vm"
        /// </summary>
        public static string Path_CcdResultWeb = @"D:\M11\M11Web\dsmon\vm";

        //CCTV相關
        /// <summary>
        ///  @"D:\FTP\Data\cctv"
        /// </summary>
        public static string Path_CcdSource = @"D:\FTP\Data\cctv";

        public static string FilePath_SchemaCGIData = Path.Combine(@"D:\M11\DBSimulation", "Schema_CGIData.xml");
        public static string FilePath_SchemaSetSatation = Path.Combine(@"D:\M11\DBSimulation", "Schema_SetSatation.xml");
        public static string FilePath_SetSatation = Path.Combine(@"D:\M11\DBSimulation", "SetSatation.xml");
        public static string FilePath_SchemaSetSensor = Path.Combine(@"D:\M11\DBSimulation", "Schema_SetSensor.xml");
        public static string FilePath_SetSensor = Path.Combine(@"D:\M11\DBSimulation", "SetSensor.xml");

        //1040806 新的ftp主機
        public static string FTP_IP = "140.116.38.196";
        //string sUser = "FCU2015";
        //string sPassword = "FCU2015";
        //之後新增一個M11專屬使用者
        public static string FTP_User = "admin";
        public static string FTP_Password = "@@hydjan222!!";

        //public static string API_Status
        
        public static string SensorDataType_RG = "RG";
        public static string SensorDataType_TM = "TM";
        public static string SensorDataType_GW = "GW";
        public static string SensorDataType_PM = "PM";
        public static string SensorDataType_GPS = "GPS";

        //資料表代碼==BasM11Setting.DataType
        public static string BasM11SettingDataType_SensorObs_num = "SensorObs_num";




    }
}
