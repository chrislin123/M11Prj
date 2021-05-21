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

        public static string Path_Original = @"D:\M11\Data\Original";
        public static string Path_XmlResult = @"D:\M11\Data\XmlResult";
        public static string Path_FTPQueueTxtOriginal = @"D:\M11\Data\FTPQueueTxtOriginal";
        public static string Path_FTPQueueTxtOriginalBak = @"D:\M11\Data\FTPQueueTxtOriginalBak";
        public static string Path_FTPQueueXmlResult = @"D:\M11\Data\FTPQueueXmlResult";
        public static string Path_FTPQueueXmlResultBak = @"D:\M11\Data\FTPQueueXmlResultBak";
        public static string Path_FTPQueueXmlResult7Day = @"D:\M11\Data\FTPQueueXmlResult7Day";
        public static string Path_FTPQueueCcdResult = @"D:\M11\Data\FTPQueueCcdResult";
        public static string Path_CcdResultHist = @"D:\M11\Data\CcdResultHist";        
        public static string Path_DBSimulation = @"D:\M11\DBSimulation";

        //備份相關
        public static string Path_BackupCGIData = @"D:\M11\Data\20-BackupData\CGIData";

        public static string Path_XmlResultWeb = @"D:\M11\M11Web\dsmon\am";
        public static string Path_XmlResultWeb7Day = @"D:\M11\M11Web\dsmon\amhist";
        public static string Path_PrecipitationWeb7Day = @"D:\M11\M11Web\dsmon\pmhist";
        public static string Path_CcdResultWeb = @"D:\M11\M11Web\dsmon\vm";

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



    }
}
