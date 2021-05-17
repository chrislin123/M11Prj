using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using M11MVC.Class;
using Dapper;
using Dapper.Contrib.Extensions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using M10.lib;
using System.Data;
using System.Dynamic;
using M10.lib.model;
using M11System.Model.M11;
//using System.Web.Http;

namespace M11MVC.Controllers
{
    public class SystemController : BaseController
    {
        // GET: System
        public ActionResult Index()
        {
            return View();
        }

        // GET: System
        public ActionResult SetBureauActive()
        {

            //ViewBag.forecastdate = AlertUpdateTm == null ? "" : AlertUpdateTm.ToString();

            //string ssql = @" select LRTIAlert.*, 
            //    LRTIAlertRefData.FlowWarning,
            //            LRTIAlertRefData.Rt_70,
            //            LRTIAlertRefData.R3_70,
            //            LRTIAlertRefData.Rt_50,
            //            LRTIAlertRefData.R3_50
            //    from LRTIAlert 
            //    left join LRTIAlertRefData on LRTIAlert.villageID = LRTIAlertRefData.villageID and LRTIAlertRefData.ver = 'now'

            //    where status = '{0}' order by country,town ";
            ////黃色
            //var dataA1 = dbDapper.Query(string.Format(ssql, "A1"));
            ////橙色
            //var dataA2 = dbDapper.Query(string.Format(ssql, "A2"));
            ////紅色
            //var dataA3 = dbDapper.Query(string.Format(ssql, "A3"));
            ////解除
            //var dataAD = dbDapper.Query(string.Format(ssql, "AD"));
            string ssql = @" select * from  BasRainallStation order by SensorName ";
            //var AlertUpdateTm = dbDapper.ExecuteScale(@" select * from  BasRainallStation order by SensorName ");
            List<BasRainallStation> lstBasRain = new List<BasRainallStation>();
            lstBasRain = dbDapper.Query<BasRainallStation>(ssql);
            //var data1 = dbDapper.Query(ssql);
            //List<dynamic> data = new List<dynamic>();
            //data.AddRange(dataA1);
            //data.AddRange(dataA2);
            //data.AddRange(dataA3);
            //data.AddRange(dataAD);

            ViewData["lstBasRain"] = lstBasRain;


            return View();
        }


        [HttpPost]
        public JsonResult SetBureauActiveSave(List<BasRainallStation> JsonInput)
        {

            foreach (BasRainallStation item in JsonInput)
            {
                ssql = @" select * from  BasRainallStation where ID = '{0}' ";
                ssql = string.Format(ssql, item.ID);
                BasRainallStation BRS = dbDapper.QuerySingleOrDefault<BasRainallStation>(ssql);
                BRS.Active_YN = item.Active_YN;

                dbDapper.Update<BasRainallStation>(BRS);
            }


            var FailResult = new { Success = "False", Message = "Error" };
            var SuccessResult = new { Success = "True", Message = "儲存完成" };

            return Json(SuccessResult, JsonRequestBehavior.AllowGet);

        }

        //[HttpPost]
        //public ActionResult ExportExcel(string StartDate, string EndDate)
        //{
        //    // 將資料寫入串流
        //    MemoryStream files = new MemoryStream();


        //    string sSaveFilePath = @"d:\temp\" + "AlertLRTI_" + Guid.NewGuid().ToString() + ".xlsx";

        //    using (FileStream fs = System.IO.File.OpenRead(@"c:\test.xls"))
        //    {
        //        fs.CopyTo(files);
        //    }

        //    //workSpase.Write(files);
        //    files.Close();



        //    return this.File(files.ToArray(), "application/vnd.ms-excel", "Download.xlsx"); ;
        //}

        [HttpPost]
        //[System.Web.Http.Route("System/SetBureauActiveSave/{JsonInput}")]
        //public JsonResult SetBureauActiveSave(BasRainallStation JsonInput)
        public JsonResult test1(string ddd)
        {

            ssql = @" select * from  BasRainallStation where ID = '{0}' ";
                ssql = string.Format(ssql, "935");
                BasRainallStation BRS = dbDapper.QuerySingleOrDefault<BasRainallStation>(ssql);
                //BRS.Active_YN = item.Active_YN;
            //JObject jo = JObject.Parse(JsonInput);

            //var tt = JsonInput;
            var FailResult = new { Success = "False", Message = "Error" };
            var SuccessResult = new { Success = "True", Message = "Pickups Scheduled Successfully." + BRS.Name };

            return Json(SuccessResult, JsonRequestBehavior.AllowGet);

        }

        public JsonResult test2(string ddd)
        {

            ssql = @" select * from  BasRainallStation where ID = '{0}' ";
            ssql = string.Format(ssql, "935");
            BasRainallStation BRS = dbDapper.QuerySingleOrDefault<BasRainallStation>(ssql);
            //BRS.Active_YN = item.Active_YN;
            //JObject jo = JObject.Parse(JsonInput);

            //var tt = JsonInput;
            var FailResult = new { Success = "False", Message = "Error" };
            var SuccessResult = new { Success = "True", Message = "Pickups Scheduled Successfully." + BRS.Name };

            return Json(SuccessResult, JsonRequestBehavior.AllowGet);

        }


        //[HttpPost]
        //public ActionResult SetBureauActive()
        //{

        //    //ViewBag.forecastdate = AlertUpdateTm == null ? "" : AlertUpdateTm.ToString();

        //    //string ssql = @" select LRTIAlert.*, 
        //    //    LRTIAlertRefData.FlowWarning,
        //    //            LRTIAlertRefData.Rt_70,
        //    //            LRTIAlertRefData.R3_70,
        //    //            LRTIAlertRefData.Rt_50,
        //    //            LRTIAlertRefData.R3_50
        //    //    from LRTIAlert 
        //    //    left join LRTIAlertRefData on LRTIAlert.villageID = LRTIAlertRefData.villageID and LRTIAlertRefData.ver = 'now'

        //    //    where status = '{0}' order by country,town ";
        //    ////黃色
        //    //var dataA1 = dbDapper.Query(string.Format(ssql, "A1"));
        //    ////橙色
        //    //var dataA2 = dbDapper.Query(string.Format(ssql, "A2"));
        //    ////紅色
        //    //var dataA3 = dbDapper.Query(string.Format(ssql, "A3"));
        //    ////解除
        //    //var dataAD = dbDapper.Query(string.Format(ssql, "AD"));
        //    string ssql = @" select * from  BasRainallStation order by SensorName ";
        //    //var AlertUpdateTm = dbDapper.ExecuteScale(@" select * from  BasRainallStation order by SensorName ");
        //    List<BasRainallStation> lstBasRain = new List<BasRainallStation>();
        //    lstBasRain = dbDapper.Query<BasRainallStation>(ssql);
        //    //var data1 = dbDapper.Query(ssql);
        //    //List<dynamic> data = new List<dynamic>();
        //    //data.AddRange(dataA1);
        //    //data.AddRange(dataA2);
        //    //data.AddRange(dataA3);
        //    //data.AddRange(dataAD);

        //    ViewData["lstBasRain"] = lstBasRain;


        //    return View();
        //}
    }
}