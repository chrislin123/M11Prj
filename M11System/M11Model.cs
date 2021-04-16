using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;

namespace M11System.Model.Procal
{
    //設定key
    //[Key] //自動增加的key
    //[ExplicitKey] //非自動增加的key

    [Table("StationData")]
    public class StationData
    {
        [ExplicitKey]
        public string StationID { get; set; }

        [ExplicitKey]
        public DateTime DataTime { get; set; }

        public string FileName { get; set; }

        public decimal? CH1 { get; set; }

        public decimal? CH2 { get; set; }

        public decimal? CH3 { get; set; }

        public decimal? CH4 { get; set; }

        public decimal? CH5 { get; set; }

        public decimal? CH6 { get; set; }

        public decimal? CH7 { get; set; }

        public decimal? CH8 { get; set; }

        public decimal? CH9 { get; set; }

        public decimal? CH10 { get; set; }

        public decimal? CH11 { get; set; }

        public decimal? CH12 { get; set; }

        public decimal? CH13 { get; set; }

        public decimal? CH14 { get; set; }

        public decimal? CH15 { get; set; }

        public decimal? CH16 { get; set; }

        public decimal? CH17 { get; set; }

        public decimal? CH18 { get; set; }

        public decimal? CH19 { get; set; }

        public decimal? CH20 { get; set; }

        public decimal? CH21 { get; set; }

        public decimal? CH22 { get; set; }

        public decimal? CH23 { get; set; }

        public decimal? CH24 { get; set; }

    }

    [Table("StationReal")]
    public class StationReal
    {
        [ExplicitKey]
        public string StationID { get; set; }

        [ExplicitKey]
        public byte MapCH { get; set; }

        public string Title { get; set; }

        public string Unit { get; set; }

        public DateTime? DataTime { get; set; }

        public decimal? RealVale { get; set; }

        public byte StationCH { get; set; }

        public string StationIDReal { get; set; }

        public decimal? ParameterBP { get; set; }

        public decimal? ParameterR { get; set; }

        public decimal? ParameterC { get; set; }

        public decimal? ParameterD { get; set; }

        public decimal? ParameterL { get; set; }

        public decimal? Alarm1 { get; set; }

        public decimal? Alarm2 { get; set; }

        public decimal? Alarm3 { get; set; }

        public int? RealShow { get; set; }

        public decimal? MaxValue { get; set; }

        public decimal? MinValue { get; set; }

        public decimal? Coefficient { get; set; }

    }

    [Table("WaterLevelStation")]
    public class WaterLevelStation
    {
        //設定key
        [ExplicitKey]
        public string StationID { get; set; }

        public string StationID_UP { get; set; }

        public string ChineseName { get; set; }

        public string DeptID { get; set; }

        public string Address { get; set; }

        public string RiverName { get; set; }

        public string BasinID { get; set; }

        public string Basin { get; set; }

        public string SubBasin { get; set; }

        public string Location_TWD97_X { get; set; }

        public string Location_TWD97_Y { get; set; }

        public string Latitude { get; set; }

        public string Longitude { get; set; }

        public decimal? WaterlevelElevation { get; set; }

        public decimal? LeftTopLine { get; set; }

        public decimal? RightTopLine { get; set; }

        public int? StationStatus { get; set; }

        public DateTime? ModifyTime { get; set; }

        public string ModifyName { get; set; }

        public DateTime? SetDate { get; set; }

        public DateTime? WarrantyTime { get; set; }

        public DateTime? MaintainTime { get; set; }

        public string ReMarks { get; set; }

        public string SectionID { get; set; }

        public string UpperStationID { get; set; }

    }

}
