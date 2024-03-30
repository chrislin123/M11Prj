using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using M11MVC.Class;
using M11System.Model.M11;
using Newtonsoft.Json;
using M11System;
using M10.lib;
using System.IO;
using System.IO.Compression;


namespace M11MVC.Controllers
{
    public class QueryController : BaseController
    {
        // GET: Query
        public ActionResult Index()
        {
            return View();
        }

        // GET: Query
        public ActionResult QueryXmlResult()
        {
            //string ssql = @" select * from  BasRainallStation order by SensorName ";
            //List<BasRainallStation> lstBasRain = new List<BasRainallStation>();
            //lstBasRain = dbDapper.Query<BasRainallStation>(ssql);


            //ViewData["lstBasRain"] = lstBasRain;


            return View();
        }

        // GET: Query/QueryCcdResult
        public ActionResult QueryCcdResult()
        {
            //string ssql = @" select * from  BasRainallStation order by SensorName ";
            //List<BasRainallStation> lstBasRain = new List<BasRainallStation>();
            //lstBasRain = dbDapper.Query<BasRainallStation>(ssql);


            //ViewData["lstBasRain"] = lstBasRain;


            return View();
        }

        public JsonResult QueryXmlResultGetStationDDL()
        {
            List<SelectListItem> items = new List<SelectListItem>();

            ssql = @"
                select distinct B.datavalue,a.station from  BasStationSensor A 
                left join BasM11Setting B on a.site = b.dataitem and datatype = 'SiteCName'
                order by a.station
            ";

            var Stations = dbDapper.Query(ssql);

            foreach (var item in Stations)
            {
                items.Add(new SelectListItem()
                {
                    Text = string.Format("[{0}]{1}", item.datavalue, item.station),
                    Value = item.station
                });
            }

            //if (Countrys.Count != 0)
            //{
            //    items.Insert(0, new SelectListItem() { Text = "全部", Value = "全部" });
            //}


            return this.Json(items, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public JsonResult getQueryXmlResult(string Params)
        {
            MVCControlClass.ApiResult aResult = new MVCControlClass.ApiResult();
            dynamic dParams = JsonConvert.DeserializeObject<dynamic>(Params);

            string station = dParams["station"];
            string sdtstart = dParams["dtstart"];
            string sdtend = dParams["dtend"];
            DateTime dtstart = Utils.getStringToDateTime(sdtstart);
            DateTime dtend = Utils.getStringToDateTime(sdtend);
            string RG = dParams["RG"];
            string TM = dParams["TM"];
            string PM = dParams["PM"];
            string GW = dParams["GW"];
            string GPS = dParams["GPS"];
            string stimerange = dParams["timerange"];

            List<string> lstSensor = new List<string>();
            if (RG == "Y") lstSensor.Add("RG");
            if (TM == "Y")
            {
                lstSensor.Add("TM");
                //DS011_01該站特殊TM有兩個
                lstSensor.Add("TM1");
                lstSensor.Add("TM2");
            }

            if (PM == "Y") lstSensor.Add("PM");
            if (GW == "Y") lstSensor.Add("GW");
            if (GPS == "Y") lstSensor.Add("GPS");

            string sInSensor = string.Join("','", lstSensor.ToArray());

            //轉換當天最大值
            dtend = Utils.getStringToDateTime(dtend.ToString("yyyy-MM-dd 23:59:59"));

            // 20200429 只顯示小時數據
            ssql = @"
                select a.*,b.datavalue from Result10MinData a 
			    left join BasM11Setting b on a.siteid = b.dataitem and b.datatype = 'SiteCName'
                where StationID = '{0}' 
                and SensorID in ( '{1}') 
                and DatetimeString between '{2}' and '{3}'                 
            ";
            if (stimerange == "hr")
            {
                ssql = ssql + " and DatetimeString like '%:00:00' ";
            }
            ssql = string.Format(ssql, station, sInSensor, M11Utils.M11DatetimeToString(dtstart), M11Utils.M11DatetimeToString(dtend));
            ssql = ssql + " order by StationID,SensorID,DatetimeString desc ";
            List<Result10MinData> Stations = dbDapper.Query<Result10MinData>(ssql);

            aResult.ApiResultStauts = "Y";
            aResult.Data = Stations;

            return this.Json(aResult, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public ActionResult DownRainStatistics(string Params)
        {
            MVCControlClass.ApiResult aResult = new MVCControlClass.ApiResult();

            dynamic dParams = JsonConvert.DeserializeObject<dynamic>(Params);

            string station = dParams["station"];
            string sdtstart = dParams["dtstart"];
            string sdtend = dParams["dtend"];
            DateTime dtstart = Utils.getStringToDateTime(sdtstart);
            DateTime dtend = Utils.getStringToDateTime(sdtend);
            string RG = dParams["RG"];
            string TM = dParams["TM"];
            string PM = dParams["PM"];
            string GW = dParams["GW"];
            string GPS = dParams["GPS"];
            string stimerange = dParams["timerange"];

            List<string> lstSensor = new List<string>();
            if (RG == "Y") lstSensor.Add("RG");
            if (TM == "Y")
            {
                lstSensor.Add("TM");
                //DS011_01該站特殊TM有兩個
                lstSensor.Add("TM1");
                lstSensor.Add("TM2");
            }

            if (PM == "Y") lstSensor.Add("PM");
            if (GW == "Y") lstSensor.Add("GW");
            if (GPS == "Y") lstSensor.Add("GPS");

            string sInSensor = string.Join("','", lstSensor.ToArray());

            //轉換當天最大值
            dtend = Utils.getStringToDateTime(dtend.ToString("yyyy-MM-dd 23:59:59"));

            // 20200429 只顯示小時數據
            ssql = @"
                select distinct a.SiteID, a.StationID,a.DatetimeString,b.datavalue 
                ,RG.value RGvalue,TM.value TMvalue,PM.value PMvalue,GW.value GWvalue,GPS.value GPSvalue
                ,TM1.value TM1value,TM2.value TM2value  
                from Result10MinData a 
			    left join BasM11Setting b on a.siteid = b.dataitem and b.datatype = 'SiteCName'
                left join Result10MinData RG on a.StationID = RG.StationID and RG.SensorID = 'RG' and a.DatetimeString = RG.DatetimeString
                left join Result10MinData TM on a.StationID = TM.StationID and TM.SensorID = 'TM' and a.DatetimeString = TM.DatetimeString
                left join Result10MinData PM on a.StationID = PM.StationID and PM.SensorID = 'PM' and a.DatetimeString = PM.DatetimeString
                left join Result10MinData GW on a.StationID = GW.StationID and GW.SensorID = 'GW' and a.DatetimeString = GW.DatetimeString
                left join Result10MinData GPS on a.StationID = GPS.StationID and GPS.SensorID = 'GPS' and a.DatetimeString = GPS.DatetimeString
                left join Result10MinData TM1 on a.StationID = TM1.StationID and TM1.SensorID = 'TM1' and a.DatetimeString = TM1.DatetimeString
                left join Result10MinData TM2 on a.StationID = TM2.StationID and TM2.SensorID = 'TM2' and a.DatetimeString = TM2.DatetimeString
                where a.StationID = '{0}'                 
                and a.DatetimeString between '{2}' and '{3}'                 
            ";
            if (stimerange == "hr")
            {
                ssql = ssql + " and a.DatetimeString like '%:00:00' ";
            }
            ssql = string.Format(ssql, station, sInSensor, M11Utils.M11DatetimeToString(dtstart), M11Utils.M11DatetimeToString(dtend));
            ssql = ssql + " order by a.StationID,a.DatetimeString desc ";
            //List<Result10MinData> Result10MinDatas = dbDapper.Query<Result10MinData>(ssql);
            List<dynamic> Result10MinDatas = dbDapper.Query<dynamic>(ssql);

            //建立Excel欄位
            List<string> head = new List<string>();
            head.Add("潛勢區");
            head.Add("潛勢區名稱");
            head.Add("測站");
            head.Add("時間");
            //head.Add("數據個數");
            //[RG]
            head.Add("[RG]10分鐘累積雨量(mm)");
            head.Add("[RG]1小時累積雨量(mm)");
            head.Add("[RG]3小時累積雨量(mm)");
            head.Add("[RG]6小時累積雨量(mm)");
            head.Add("[RG]12小時累積雨量(mm)");
            head.Add("[RG]24小時累積雨量(mm)");
            head.Add("[RG]48小時累積雨量(mm)");
            head.Add("[RG]72小時累積雨量(mm)");
            head.Add("[RG]7日累積雨量(mm)");
            head.Add("[RG]30日累積雨量(mm)");
            //TM
            head.Add("[TM]方位一觀測值(秒)");
            head.Add("[TM]方位二觀測值(秒)");
            head.Add("[TM]方位一累積變位量(秒)");
            head.Add("[TM]方位二累積變位量(秒)");
            head.Add("[TM]方位一速率(秒/天)");
            head.Add("[TM]方位二速率(秒/天)");
            //PM
            head.Add("[PM]水位高(m)");
            head.Add("[PM]相對水位高(m)");
            //GW
            head.Add("[GW]水位高(m)");
            head.Add("[GW]相對水位高(m)");
            //GPS
            head.Add("[GPS]E(m)");
            head.Add("[GPS]N(m)");
            head.Add("[GPS]H(m)");
            head.Add("[GPS]方位角(度)");
            head.Add("[GPS]三軸變位速率(mm/天)");
            head.Add("[GPS]平面變位速率(mm/天)");
            head.Add("[GPS]累積變位量(mm)");
            head.Add("[GPS]每日解算後E值(M)");
            head.Add("[GPS]每日解算後N值(M)");
            head.Add("[GPS]每日解算後H值(M)");
            //TM1
            head.Add("[TM1]方位一觀測值(秒)");
            head.Add("[TM1]方位二觀測值(秒)");
            head.Add("[TM1]方位一累積變位量(秒)");
            head.Add("[TM1]方位二累積變位量(秒)");
            head.Add("[TM1]方位一速率(秒/天)");
            head.Add("[TM1]方位二速率(秒/天)");
            //TM2
            head.Add("[TM2]方位一觀測值(秒)");
            head.Add("[TM2]方位二觀測值(秒)");
            head.Add("[TM2]方位一累積變位量(秒)");
            head.Add("[TM2]方位二累積變位量(秒)");
            head.Add("[TM2]方位一速率(秒/天)");
            head.Add("[TM2]方位二速率(秒/天)");

            List<string[]> datas = new List<string[]>();
            foreach (dynamic item in Result10MinDatas)
            {
                List<string> cols = new List<string>();
                cols.Add(item.SiteID);
                cols.Add(item.datavalue);
                cols.Add(item.StationID);
                //cols.Add(item.SensorID);
                cols.Add(item.DatetimeString);
                //cols.Add(item.observation_num);

                //[RG]數據拆開各自一個欄位
                List<string> lstTmp = new List<string>();
                for (int i = 0; i < 10; i++)
                {
                    lstTmp.Add("");
                }
                if (item.RGvalue != null)
                {
                    string[] aValue = item.RGvalue.Split(' ');
                    for (int i = 0; i < aValue.Length; i++)
                    {
                        lstTmp[i] = aValue[i];
                    }
                }

                foreach (string value in lstTmp)
                {
                    cols.Add(value);
                }

                //[TM]數據拆開各自一個欄位
                lstTmp.Clear();
                for (int i = 0; i < 6; i++)
                {
                    lstTmp.Add("");
                }
                if (item.TMvalue != null)
                {
                    string[] aValue = item.TMvalue.Split(' ');
                    for (int i = 0; i < aValue.Length; i++)
                    {
                        lstTmp[i] = aValue[i];
                    }
                }
                foreach (string value in lstTmp)
                {
                    cols.Add(value);
                }

                //[PM]數據拆開各自一個欄位
                lstTmp.Clear();
                for (int i = 0; i < 2; i++)
                {
                    lstTmp.Add("");
                }
                if (item.PMvalue != null)
                {
                    string[] aValue = item.PMvalue.Split(' ');
                    for (int i = 0; i < aValue.Length; i++)
                    {
                        lstTmp[i] = aValue[i];
                    }
                }
                foreach (string value in lstTmp)
                {
                    cols.Add(value);
                }

                //[GW]數據拆開各自一個欄位
                lstTmp.Clear();
                for (int i = 0; i < 2; i++)
                {
                    lstTmp.Add("");
                }
                if (item.GWvalue != null)
                {
                    string[] aValue = item.GWvalue.Split(' ');
                    for (int i = 0; i < aValue.Length; i++)
                    {
                        lstTmp[i] = aValue[i];
                    }
                }
                foreach (string value in lstTmp)
                {
                    cols.Add(value);
                }

                //[GPS]數據拆開各自一個欄位
                lstTmp.Clear();
                for (int i = 0; i < 10; i++)
                {
                    lstTmp.Add("");
                }
                if (item.GPSvalue != null)
                {
                    string[] aValue = item.GPSvalue.Split(' ');
                    for (int i = 0; i < aValue.Length; i++)
                    {
                        lstTmp[i] = aValue[i];
                    }
                }
                foreach (string value in lstTmp)
                {
                    cols.Add(value);
                }

                //[TM1]數據拆開各自一個欄位
                lstTmp.Clear();
                for (int i = 0; i < 6; i++)
                {
                    lstTmp.Add("");
                }
                if (item.TM1value != null)
                {
                    string[] aValue = item.TM1value.Split(' ');
                    for (int i = 0; i < aValue.Length; i++)
                    {
                        lstTmp[i] = aValue[i];
                    }
                }
                foreach (string value in lstTmp)
                {
                    cols.Add(value);
                }

                //[TM2]數據拆開各自一個欄位
                lstTmp.Clear();
                for (int i = 0; i < 6; i++)
                {
                    lstTmp.Add("");
                }
                if (item.TM2value != null)
                {
                    string[] aValue = item.TM2value.Split(' ');
                    for (int i = 0; i < aValue.Length; i++)
                    {
                        lstTmp[i] = aValue[i];
                    }
                }
                foreach (string value in lstTmp)
                {
                    cols.Add(value);
                }

                datas.Add(cols.ToArray());
            }

            //產生檔案路徑
            string sGUID = Guid.NewGuid().ToString();
            string sTempPath = Path.Combine(Server.MapPath("~/temp/"), "QueryXmlResult", sGUID);
            //建立資料夾
            Directory.CreateDirectory(sTempPath);

            string sFileName = string.Format("QueryXmlResult_{0}_{1}.xlsx", dtstart.ToString("yyyyMMdd"), dtend.ToString("yyyyMMdd"));
            string sSaveFilePath = Path.Combine(sTempPath, sFileName);

            //DataTable dt = Utils.ConvertToDataTable<RainStation>(wrs);
            //DataTable dt = new DataTable();


            DataExport de = new DataExport();
            //Boolean bSuccess = de.ExportBigDataToCsv(sSaveFilePath, dt);
            Boolean bSuccess = de.ExportListToExcel(sSaveFilePath, head, datas);


            //產生提供下載的路徑，到前端後，提供下載
            string sFileDownUrl = Request.Url.Scheme + "://" + Request.Url.Authority + Url.Content("~/temp/QueryXmlResult/" + sGUID + "/") + sFileName;



            //if (bSuccess)
            //{
            //    string filename = string.Format("WeaRainStatistics_{0}_{1}.xlsx", "", "");

            //    //ASP.NET 回應大型檔案的注意事項
            //    //http://blog.miniasp.com/post/2008/03/11/Caution-about-ASPNET-Response-a-Large-File.aspx


            //    //***** 下載檔案過大，使用特殊方法 *****
            //    HttpContext context = System.Web.HttpContext.Current;
            //    context.Response.TransmitFile(sSaveFilePath);
            //    context.Response.ContentType = "application/vnd.ms-excel";
            //    context.Response.AddHeader("Content-Disposition", string.Format("attachment;filename={0}", filename));
            //    Response.End();
            //}

            //dynamic dd = new  { test1:sSaveFilePath};



            aResult.ApiResultStauts = "Y";
            aResult.Data = sFileDownUrl;

            return this.Json(aResult, JsonRequestBehavior.AllowGet);

        }

        [HttpGet]
        public ActionResult DownCcdData(string Params)
        {

            string sFileDownUrl = "";

            MVCControlClass.ApiResult aResult = new MVCControlClass.ApiResult();

            dynamic dParams = JsonConvert.DeserializeObject<dynamic>(Params);

            try
            {
                //string station = dParams["station"];
                string sdtstart = dParams["dtstart"];
                string sdtend = dParams["dtend"];
                //DateTime dtstart = Utils.getStringToDateTime(sdtstart);
                //DateTime dtend = Utils.getStringToDateTime(sdtend);
                //string stimerange = dParams["timerange"];


                //站台
                //string sStation = "DS002_2";
                string sStation = dParams["station"];
                sStation = sStation.Split('_')[0] + "_" + sStation.Split('_')[1].Replace("0", "");

                //起始時間
                //DateTime dtStart = new DateTime(2024, 3, 25);
                //DateTime dtEnd = new DateTime(2024, 3, 27);
                DateTime dtStart = Utils.getStringToDateTime(sdtstart);
                DateTime dtEnd = Utils.getStringToDateTime(sdtend);


                //搜尋整點時刻
                //string sConTime = "12";
                string sConTime = dParams["timerange"];

                //雲端路徑由設定檔設定                
                string sCcdHistoryPath = @"G:\我的雲端硬碟\Project\M11\Data\ProjectData\Ccd\CcdHistory";
                
                //產生檔案路徑                
                string sCcdSearchTempPath = Path.Combine(Server.MapPath("~/temp/"), "QueryCcdResult");
                                
                ////建立資料夾
                Directory.CreateDirectory(sCcdSearchTempPath);


                //刪除1天以前的資料               
                foreach (string fname in Directory.GetFiles(sCcdSearchTempPath))
                {
                    try
                    {
                        FileInfo fi = new FileInfo(fname);                       

                        if (fi.LastWriteTime < DateTime.Now.AddDays(-3))
                        {
                            fi.Delete();
                        }

                    }
                    catch (Exception ex)
                    {
                        //有錯誤持續執行
                        continue;
                    }
                }

                //複製到本機端
                DateTime dt = DateTime.Now;
                string sZipFileName = Utils.getDatatimeString(dt, M10Const.DatetimeStringType.ADDT1);
                string sCcdCurrentPath = Path.Combine(sCcdSearchTempPath, sZipFileName);
                Directory.CreateDirectory(sCcdCurrentPath);

                for (DateTime dtTrans = dtStart; dtTrans <= dtEnd; dtTrans = dtTrans.AddDays(1))
                {

                    string sCcdHistoryPathByCond = Path.Combine(sCcdHistoryPath, sStation
                        , dtTrans.Year.ToString(), dtTrans.Month.ToString().PadLeft(2, '0'), dtTrans.Day.ToString().PadLeft(2, '0'));

                    //檔案格式可能有JPG與JPEG
                    //處理JPG
                    string sFileNameByCond = string.Format("{0}-{1}-{2}.jpg", sStation, dtTrans.ToString("yyyyMMdd"), sConTime + "0000");

                    FileInfo fi = new FileInfo(Path.Combine(sCcdHistoryPathByCond, sFileNameByCond));

                    //20240330 **非常重要的設定**
                    //因為Google Drive資料讀取，使用IIS Apppool/M11MVC的角色權限，無法讀取檔案
                    //但是在IIS環境設定IIS Apppool，有預設的角色M11MVC
                    //為了要讀取Google Drive資料，所以需要再IIS Apppool(應用程式集區)，M11MVC修改[進階設定][識別]，改成本機帳號登入(eswcrc2021)
                    //就可以讀取檔案且複製到目的地
                    //搞了兩天，終於結案

                    //檔案存在，才執行複製的程序
                    if (fi.Exists == true)
                    {
                        string sRemoteFullPathTest = Path.Combine(sCcdCurrentPath, fi.Name);
                        ////先刪除
                        //System.IO.File.Delete(sRemoteFullPathTest);
                        //在複製
                        fi.CopyTo(sRemoteFullPathTest, true);
                    }

                    //處理JPEG
                    string sFileNameByCondJPEG = string.Format("{0}-{1}-{2}.jpeg", sStation, dtTrans.ToString("yyyyMMdd"), sConTime + "0000");

                    FileInfo fiJPEG = new FileInfo(Path.Combine(sCcdHistoryPathByCond, sFileNameByCondJPEG));

                    //檔案存在，才執行複製的程序
                    if (fiJPEG.Exists == true)
                    {
                        string sRemoteFullPathTestJPEG = Path.Combine(sCcdCurrentPath, fiJPEG.Name);
                        ////先刪除
                        //System.IO.File.Delete(sRemoteFullPathTest);
                        //在複製
                        fiJPEG.CopyTo(sRemoteFullPathTestJPEG, true);
                    }
                }


                //--壓縮檔案

                ////產生ZIP檔
                string PathSource = sCcdCurrentPath;
                string sFileName1 = string.Format("CCDData-{0}-{1}.zip", sStation, sZipFileName);
                string PathDest = Path.Combine(sCcdSearchTempPath, sFileName1);

                //檔案存在則先刪除
                if (System.IO.File.Exists(PathDest) == true)
                {
                    new FileInfo(PathDest).Delete();
                    System.Threading.Thread.Sleep(1000);
                }

                // 壓縮目錄中檔案
                ZipFile.CreateFromDirectory(PathSource, PathDest);



                //產生提供下載的路徑，到前端後，提供下載
                sFileDownUrl = Request.Url.Scheme + "://" + Request.Url.Authority + Url.Content("~/temp/QueryCcdResult/") + sFileName1;
            }
            catch (Exception ex)
            {

                throw ex;
            }


            aResult.ApiResultStauts = "Y";
            aResult.Data += sFileDownUrl;
            


            return this.Json(aResult, JsonRequestBehavior.AllowGet);

        }

    }
}