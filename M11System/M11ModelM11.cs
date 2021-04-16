using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;

namespace M11System.Model.M11
{
    //設定key
    //[Key] //自動增加的key
    //[ExplicitKey] //非自動增加的key


    /// <summary>
    /// M11_XMLResult結果檔紀錄資料表
    /// </summary>
    [Table("Result10MinData")]
    public class Result10MinData
    {
        [Key]
        public long no { get; set; }

        public string SiteID { get; set; }

        public string StationID { get; set; }

        public string SensorID { get; set; }

        public string DataType { get; set; }

        public string DataName { get; set; }

        public DateTime? Datetime { get; set; }

        public string DatetimeString { get; set; }

        public string GetTime { get; set; }

        public string observation_num { get; set; }

        public string sensor_status { get; set; }

        public string value { get; set; }

        public string remark { get; set; }

        public string CgiData { get; set; }
    }

    /// <summary>
    /// 基本資料-Station與Sensor
    /// </summary>
    [Table("BasStationSensor")]
    public class BasStationSensor
    {
        [Key]
        public long no { get; set; }

        public string Site { get; set; }

        public string Station { get; set; }

        public string Sensor { get; set; }

        public string DefaultWater { get; set; }

        public string RenderXML_YN { get; set; }

    }

    /// <summary>
    /// 基本資料-雨量站回傳氣象局
    /// </summary>
    [Table("BasRainallStation")]
    public class BasRainallStation
    {
        [Key]
        public long no { get; set; }

        public string ID { get; set; }

        public string Name { get; set; }

        public string Lon_67 { get; set; }

        public string Lat_67 { get; set; }

        public string Distrct { get; set; }

        public string SensorName { get; set; }

    }

    /// <summary>
    /// Station中的Cgi資料檔
    /// </summary>
    [Table("CgiStationData")]
    public class CgiStationData
    {
        [Key]
        public long no { get; set; }

        public string DatetimeString { get; set; }

        public string Cgi { get; set; }

        public string ID { get; set; }

        public string Station { get; set; }

        public string DataType { get; set; }

        public string Value { get; set; }

    }




}
