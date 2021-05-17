using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M11System
{
    public class M11Utils
    {


        /// <summary>
        /// M11專案Datetime轉文字時間格式(yyyy-MM-dd HH:mm:ss)
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string M11DatetimeToString(DateTime dt)
        {
            string sResult = "";
            sResult = dt.ToString("yyyy-MM-dd HH:mm:ss");
            return sResult;
        }
    }
}
