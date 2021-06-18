using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace M11System
{
    public class M11Helper
    {
        

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
    }
}
