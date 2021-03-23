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

            //建立必要資料夾
            Directory.CreateDirectory(M11Const.Path_FTPQueueXmlResult7Day);

            //移除超過七天資料
            //DateTime dtNow = DateTime.Now;
            //DateTime dtNow7D = dtNow.AddDays(-7);

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

                    if (dt < DateTime.Now.AddDays(-7))
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

            //產生ZIP檔
            

            //移動ZIP檔到Web路徑

        }
    }
}
