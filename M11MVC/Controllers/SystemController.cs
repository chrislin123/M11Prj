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

        // GET: System/SetBureauActive
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


        // GET: System/SetPreventPeriodOneMinActive - CCD防災期間一分鐘傳送啟動設定
        public ActionResult SetPreventPeriodOneMinActive()
        {   
            string ssql = @" select * from BasM11Setting where DataType = '{0}' and DataRemark = '{1}' ";
            ssql = string.Format(ssql, M11System.M11Const.BasM11SettingDataType_CCD, M11System.M11Const.BasM11SettingDataRemark_PreventPeriodOneMin);
            BasM11Setting oBasM11Setting = new BasM11Setting();         
            oBasM11Setting = dbDapper.QuerySingleOrDefault<BasM11Setting>(ssql);

            ViewData["PreventPeriodOneMin"] = oBasM11Setting.DataValue;

            return View();
        }

        [HttpPost]
        public JsonResult SetPreventPeriodOneMinActiveSave(BasM11Setting JsonInput)
        {
            
            JsonInput.DataType = M11System.M11Const.BasM11SettingDataType_CCD;
            JsonInput.DataRemark = M11System.M11Const.BasM11SettingDataRemark_PreventPeriodOneMin;
            
            ssql = @" select * from  BasM11Setting where DataType = '{0}' and DataRemark = '{1}' ";
            ssql = string.Format(ssql, JsonInput.DataType, JsonInput.DataRemark);
            BasM11Setting BRS = dbDapper.QuerySingleOrDefault<BasM11Setting>(ssql);
            BRS.DataValue = JsonInput.DataValue;

            dbDapper.Update<BasM11Setting>(BRS);


            var FailResult = new { Success = "False", Message = "Error" };
            var SuccessResult = new { Success = "True", Message = "儲存完成" };

            return Json(SuccessResult, JsonRequestBehavior.AllowGet);

        }

        

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


    }
}