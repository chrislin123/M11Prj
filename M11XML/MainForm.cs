﻿using System;
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
using System.Xml;
using FluentFTP;
using System.Net;
using M11System;
using M11System.Model.Procal;
using M11System.Model.M11;

namespace M11XML
{
    public partial class MainForm : BaseForm
    {
        public MainForm()
        {
            InitializeComponent();
            base.InitForm();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Directory.CreateDirectory(M11Const.Path_Original);
            Directory.CreateDirectory(M11Const.Path_XmlResult);
            Directory.CreateDirectory(M11Const.Path_FTPQueueTxtOriginal);
            Directory.CreateDirectory(M11Const.Path_FTPQueueXmlResult);
            Directory.CreateDirectory(M11Const.Path_FTPQueueTxtOriginalBak);
            Directory.CreateDirectory(M11Const.Path_FTPQueueXmlResultBak);
            Directory.CreateDirectory(M11Const.Path_DBSimulation);
            Directory.CreateDirectory(M11Const.Path_XmlResultWeb);
            Directory.CreateDirectory(M11Const.Path_XmlResultWeb7Day);
            Directory.CreateDirectory(M11Const.Path_PrecipitationWeb7Day);
            Directory.CreateDirectory(M11Const.Path_FTPQueueXmlResult7Day);


            //DateTime dtCheck = new DateTime(2021, 3, 13, 17, 30, 8);
            //ReadOutDataToDB(dtCheck);

            //ProcPreGetNextDataFromPreData();

            //return;
            timer1.Enabled = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                timer1.Enabled = false;

                ShowMessageToFront("轉檔啟動");

                DateTime dtCheck = DateTime.Now;

                //產生Station設定檔中的XML
                InitStationXML();

                //讀取CGI原始資料轉檔到XML資料庫與DB資料庫
                ReadDataToXMLDB();

                //讀取委外資料轉檔到XML資料庫
                //ReadOutDataToXMLDB();

                //讀取委外資料轉檔到DB資料庫
                ReadOutDataToDB(dtCheck);

                //產生結果XML(每十分鐘)
                ProcGenResultXML(dtCheck);

                //雨量站回傳氣象局-產生結果XML(每十分鐘)
                ProcGenRainfallXML(dtCheck);

                //移除CGI資料超過四天的資料(每十分鐘)
                ProcRemoveCgiDb(dtCheck);


                ShowMessageToFront("轉檔完畢");

            }
            catch (Exception ex)
            {
                logger.Error(ex, "M11XML 轉檔錯誤:");                
            }
            finally
            {
                System.Threading.Thread.Sleep(2000);
                this.Close();
            }
        }


        private void ShowMessageToFront(string pMsg)
        {
            richTextBox1.AppendText(pMsg + "\r\n");
            this.Update();
            Application.DoEvents();
        }

        DataTable dtStationData = null;
        DataTable DtStationData
        {
            get
            {
                if (dtStationData == null)
                {
                    dtStationData = GetStationSetData();
                }

                return dtStationData;
            }
        }

        DataTable dtSensorData = null;
        DataTable DtSensorData
        {
            get
            {
                if (dtSensorData == null)
                {
                    dtSensorData = GetSensorSetData();
                }

                return dtSensorData;
            }
        }

     
        /// <summary>
        /// 
        /// </summary>
        /// <param name="SensorRow">感應器資料</param>
        /// <param name="dtCheck">XML產生時間</param>
        /// <param name="dtGetDataTime">取得資料時間</param>
        /// <returns></returns>
        private string getSensorDataResult(BasStationSensor SensorRow ,DateTime dtCheck, ref DateTime dtGetDataTime, ref string sCgiData) 
        {
            string sResult = "";
            string SensorName = SensorRow.Sensor; // SensorRow["Sensor"].ToString();
            switch (SensorName)
            {
                case "RG":
                    //sResult = getRainGauge(SensorRow, dtCheck, ref dtGetDataTime, ref sCgiData);
                    sResult = getRainGaugeFromDB(SensorRow, dtCheck, ref dtGetDataTime, ref sCgiData);
                    break;
                case "TM":
                case "TM1":
                    //sResult = getBiTiltMeter(SensorRow, dtCheck, ref dtGetDataTime, ref sCgiData);
                    sResult = getBiTiltMeterFromDB(SensorRow, dtCheck, ref dtGetDataTime, ref sCgiData);
                    break;
                case "TM2":
                    //sResult = getBiTiltMeter2(SensorRow, dtCheck, ref dtGetDataTime, ref sCgiData);
                    sResult = getBiTiltMeter2FromDB(SensorRow, dtCheck, ref dtGetDataTime, ref sCgiData);
                    break;
                case "GW":
                    //sResult = getObservationWell(SensorRow, dtCheck, ref dtGetDataTime, ref sCgiData);
                    sResult = getObservationWellFromDB(SensorRow, dtCheck, ref dtGetDataTime, ref sCgiData);
                    break;
                case "PM":
                    //sResult = getPiezoMeter(SensorRow, dtCheck, ref dtGetDataTime, ref sCgiData);
                    sResult = getPiezoMeterFromDB(SensorRow, dtCheck, ref dtGetDataTime, ref sCgiData);
                    
                    break;
                default:
                    sResult = "";
                    break;
            }

            return sResult;
        }


        /// <summary>
        /// 取得雨量資料結果(RG)
        /// </summary>
        /// <param name="StationName">站名</param>
        /// <param name="SensorName">儀器名稱</param>
        /// <param name="dtCheck">比對時間點</param>
        /// <returns></returns>
        private string getRainGauge(BasStationSensor SensorRow, DateTime dtCheck, ref DateTime dtGetDataTime, ref string sCgiData)
        {
            string sResult = "";
            string StationName = SensorRow.Station;
            string SensorName  = SensorRow.Sensor;

            DataTable dtStationData = GetStationData(StationName);

            //取得目前時段CGI的資料
            DateTime dtCgiStart = dtCheck;
            DateTime dtCgiPre10m = dtCheck.AddMinutes(-10);

            //string sRain = "";
            DataRow[] drCgi10m = dtStationData.Select(
                string.Format(" datetime >= '{0}' and datetime <= '{1}' ", dtCgiPre10m.ToString("yyyyMMddHHmmss"), dtCgiStart.ToString("yyyyMMddHHmmss"))
                , " datetime desc ");
            if (drCgi10m.Count() == 0)
            {
                //如果沒資料，則取前一個時段的資料
                DateTime dtPre = dtCheck.AddMinutes(-10);
                ssql = @"
                            select * from Result10MinData
                            where StationID = '{0}' and SensorID = '{1}' and DatetimeString = '{2}'
                        ";
                ssql = string.Format(ssql, StationName, SensorName, dtPre.ToString("yyyy-MM-dd HH:mm:00"));

                Result10MinData qRd = dbDapper.QuerySingleOrDefault<Result10MinData>(ssql);
                
                if (qRd != null && qRd.CgiData != "")
                {
                    sCgiData = qRd.CgiData;
                }                
            }
            else
            {
                sCgiData = drCgi10m[0][CgiConst.RAIN].ToString();
            }


            //將資料全部先彙整再處理(72h*6 每十分鐘一筆),因為需要與前一個時段比較，所以多加一筆
            List<string> lstRain = new List<string>();
            for (int i = 0; i < 72 * 6 + 1; i++)
            {
                DateTime dtStart = dtCheck.AddMinutes(-10 * i);
                DateTime dt10m = dtCheck.AddMinutes(-10 * (i + 1));

                string sRain = "";
                DataRow[] dr10m = dtStationData.Select(
                    string.Format(" datetime >= '{0}' and datetime <= '{1}' ", dt10m.ToString("yyyyMMddHHmmss"), dtStart.ToString("yyyyMMddHHmmss"))
                    , " datetime desc ");
                if (dr10m.Count() == 0)
                {
                    sRain = "0";
                }
                else
                {
                    sRain = dr10m[0][CgiConst.RAIN].ToString();

                    //第一筆資料紀錄資料時間
                    if (i == 0)
                    {
                        dtGetDataTime = Utils.getStringToDateTime(dr10m[0][CgiConst.DATETIME].ToString());
                    }
                }

                lstRain.Add(sRain);
            }

            double dtemp = 0;
            string sRain10m = "0";
            string sRain1h = "0";
            string sRain3h = "0";
            string sRain6h = "0";
            string sRain12h = "0";
            string sRain24h = "0";
            string sRain48h = "0";
            string sRain72h = "0";
            double dRain10m = 0;
            double dRain1h = 0;
            double dRain3h = 0;
            double dRain6h = 0;
            double dRain12h = 0;
            double dRain24h = 0;
            double dRain48h = 0;
            double dRain72h = 0;

            //原本以為是雨量的累積，但是因為儀器是用累計制的方式記錄雨量資料，所以調整計算方式
            ////10m
            //dtemp = 0;
            //double.TryParse(lstRain[0], out dtemp);
            //dRain10m = dtemp;

            ////1h
            //dtemp = 0;
            //for (int i = 0; i < 6 * 1; i++)
            //{
            //    double dLoop = 0;
            //    double.TryParse(lstRain[i], out dLoop);
            //    dtemp = dtemp + dLoop;
            //}
            //dRain1h = dtemp;

            ////3h
            //dtemp = 0;
            //for (int i = 0; i < 6 * 3; i++)
            //{
            //    double dLoop = 0;
            //    double.TryParse(lstRain[i], out dLoop);
            //    dtemp = dtemp + dLoop;
            //}
            //dRain3h = dtemp;

            ////6h
            //dtemp = 0;
            //for (int i = 0; i < 6 * 6; i++)
            //{
            //    double dLoop = 0;
            //    double.TryParse(lstRain[i], out dLoop);
            //    dtemp = dtemp + dLoop;
            //}
            //dRain6h = dtemp;

            ////12h
            //dtemp = 0;
            //for (int i = 0; i < 6 * 12; i++)
            //{
            //    double dLoop = 0;
            //    double.TryParse(lstRain[i], out dLoop);
            //    dtemp = dtemp + dLoop;
            //}
            //dRain12h = dtemp;

            ////24h
            //dtemp = 0;
            //for (int i = 0; i < 6 * 24; i++)
            //{
            //    double dLoop = 0;
            //    double.TryParse(lstRain[i], out dLoop);
            //    dtemp = dtemp + dLoop;
            //}
            //dRain24h = dtemp;

            ////48h
            //dtemp = 0;
            //for (int i = 0; i < 6 * 48; i++)
            //{
            //    double dLoop = 0;
            //    double.TryParse(lstRain[i], out dLoop);
            //    dtemp = dtemp + dLoop;
            //}
            //dRain48h = dtemp;

            ////72h
            //dtemp = 0;
            //for (int i = 0; i < 6 * 72; i++)
            //{
            //    double dLoop = 0;
            //    double.TryParse(lstRain[i], out dLoop);
            //    dtemp = dtemp + dLoop;
            //}
            //dRain72h = dtemp;


            //計數值的單位(累積)
            //double dValueUnit = 5;
            // 20210420 計數單位5=>0.5
            double dValueUnit = 0.5;

            //新計算方式，要跟前面的資料比對，才知道有沒有累積
            //10m
            dtemp = 0;

            double dtemp10m = 0;
            double dtemp10mPre = 0;
            double.TryParse(lstRain[0] , out dtemp10m);
            double.TryParse(lstRain[1], out dtemp10mPre);
            //本次雨量資料-前10分鐘雨量資料 > 0 ，才累積
            if (dtemp10m - dtemp10mPre > 0)
            {
                dRain10m = (dtemp10m - dtemp10mPre) * dValueUnit;
            }

            //1h
            dtemp = 0;
            for (int i = 0; i < 6 * 1; i++)
            {
                double dLoop = 0;
                double dLoopPre = 0; //前十分鐘
                double.TryParse(lstRain[i], out dLoop);
                double.TryParse(lstRain[i + 1], out dLoopPre);

                //本次雨量資料-前10分鐘雨量資料 > 0 ，才累積
                double dLoopResult = dLoop - dLoopPre;
                if (dLoopResult > 0)
                {
                    dtemp = dtemp + dLoopResult;
                }                
            }
            dRain1h = dtemp * dValueUnit;

            //3h
            dtemp = 0;
            for (int i = 0; i < 6 * 3; i++)
            {
                double dLoop = 0;
                double dLoopPre = 0; //前十分鐘
                double.TryParse(lstRain[i], out dLoop);
                double.TryParse(lstRain[i + 1], out dLoopPre);

                //本次雨量資料-前10分鐘雨量資料 > 0 ，才累積
                double dLoopResult = dLoop - dLoopPre;
                if (dLoopResult > 0)
                {
                    dtemp = dtemp + dLoopResult;
                }
            }
            dRain3h = dtemp * dValueUnit;


            //6h
            dtemp = 0;
            for (int i = 0; i < 6 * 6; i++)
            {
                double dLoop = 0;
                double dLoopPre = 0; //前十分鐘
                double.TryParse(lstRain[i], out dLoop);
                double.TryParse(lstRain[i + 1], out dLoopPre);

                //本次雨量資料-前10分鐘雨量資料 > 0 ，才累積
                double dLoopResult = dLoop - dLoopPre;
                if (dLoopResult > 0)
                {
                    dtemp = dtemp + dLoopResult;
                }
            }
            dRain6h = dtemp * dValueUnit;

            //12h
            dtemp = 0;
            for (int i = 0; i < 6 * 12; i++)
            {
                double dLoop = 0;
                double dLoopPre = 0; //前十分鐘
                double.TryParse(lstRain[i], out dLoop);
                double.TryParse(lstRain[i + 1], out dLoopPre);

                //本次雨量資料-前10分鐘雨量資料 > 0 ，才累積
                double dLoopResult = dLoop - dLoopPre;
                if (dLoopResult > 0)
                {
                    dtemp = dtemp + dLoopResult;
                }
            }
            dRain12h = dtemp * dValueUnit;

            //24h
            dtemp = 0;
            for (int i = 0; i < 6 * 24; i++)
            {
                double dLoop = 0;
                double dLoopPre = 0; //前十分鐘
                double.TryParse(lstRain[i], out dLoop);
                double.TryParse(lstRain[i + 1], out dLoopPre);

                //本次雨量資料-前10分鐘雨量資料 > 0 ，才累積
                double dLoopResult = dLoop - dLoopPre;
                if (dLoopResult > 0)
                {
                    dtemp = dtemp + dLoopResult;
                }
            }
            dRain24h = dtemp * dValueUnit;

            //48h
            dtemp = 0;
            for (int i = 0; i < 6 * 48; i++)
            {
                double dLoop = 0;
                double dLoopPre = 0; //前十分鐘
                double.TryParse(lstRain[i], out dLoop);
                double.TryParse(lstRain[i + 1], out dLoopPre);

                //本次雨量資料-前10分鐘雨量資料 > 0 ，才累積
                double dLoopResult = dLoop - dLoopPre;
                if (dLoopResult > 0)
                {
                    dtemp = dtemp + dLoopResult;
                }
            }
            dRain48h = dtemp * dValueUnit;

            //72h
            dtemp = 0;
            for (int i = 0; i < 6 * 72; i++)
            {
                double dLoop = 0;
                double dLoopPre = 0; //前十分鐘
                double.TryParse(lstRain[i], out dLoop);
                double.TryParse(lstRain[i + 1], out dLoopPre);

                //本次雨量資料-前10分鐘雨量資料 > 0 ，才累積
                double dLoopResult = dLoop - dLoopPre;
                if (dLoopResult > 0)
                {
                    dtemp = dtemp + dLoopResult;
                }
            }
            dRain72h = dtemp * dValueUnit;


            //四捨五入小數點1位
            dRain10m = Math.Round(dRain10m, 1);
            dRain1h = Math.Round(dRain1h, 1);
            dRain3h = Math.Round(dRain3h, 1);
            dRain6h = Math.Round(dRain6h, 1);
            dRain12h = Math.Round(dRain12h, 1);
            dRain24h = Math.Round(dRain24h, 1);
            dRain48h = Math.Round(dRain48h, 1);
            dRain72h = Math.Round(dRain72h, 1);

            sRain10m = dRain10m.ToString();
            sRain1h = dRain1h.ToString();
            sRain3h = dRain3h.ToString();
            sRain6h = dRain6h.ToString();
            sRain12h = dRain12h.ToString();
            sRain24h = dRain24h.ToString();
            sRain48h = dRain48h.ToString();
            sRain72h = dRain72h.ToString();

            sResult = string.Format("{0} {1} {2} {3} {4} {5} {6} {7}"
                , sRain10m, sRain1h, sRain3h, sRain6h, sRain12h, sRain24h, sRain48h, sRain72h);

            return sResult;
        }

        /// <summary>
        /// 取得雨量資料結果(RG)
        /// </summary>
        /// <param name="StationName">站名</param>
        /// <param name="SensorName">儀器名稱</param>
        /// <param name="dtCheck">比對時間點</param>
        /// <returns></returns>
        private string getRainGaugeFromDB(BasStationSensor SensorRow, DateTime dtCheck, ref DateTime dtGetDataTime, ref string sCgiData)
        {
            string sResult = "";
            string StationName = SensorRow.Station;
            string SensorName = SensorRow.Sensor;

            //[RAIN]
            ssql = @"
                    select * from CgiStationData where Station = '{0}' and datatype = 'RAIN' order by DatetimeString asc                                                       
                    ";
            ssql = string.Format(ssql, StationName);
            List<CgiStationData> lstData = dbDapper.Query<CgiStationData>(ssql);

            if (lstData.Count == 0 )
            {
                sResult = string.Format("{0} {1} {2} {3} {4} {5} {6} {7}"
                , 0, 0, 0, 0, 0, 0, 0, 0);
                sCgiData = "0";
                return sResult;
            }

            ////取得目前時段CGI的資料
            //DateTime dtCgiStart = dtCheck;
            //DateTime dtCgiPre10m = dtCheck.AddMinutes(-10);

            //取得 目前的CGIDATA 
            List<CgiStationData> lstTmp = lstData.Where(c => c.DatetimeString == M11DatetimeToString(dtCheck)).ToList<CgiStationData>();
            if (lstTmp.Count > 0)
            {
                sCgiData = lstTmp[0].Value;
                dtGetDataTime = Utils.getStringToDateTime(lstTmp[0].DatetimeString);
            }
                

            //資料處理
            //補齊沒有資料的時段
            //計算累積量(與前一個時段相比)
            //GwRainData
            List<GwRainData> lstGwRainData = new List<GwRainData>();
            int idex = 0;
            //由第一筆開始計算(SQL ASC)
            DateTime dtStart1 = new DateTime();
            if (lstData.Count > 0)
            {
                dtStart1 = Utils.getStringToDateTime(lstData[0].DatetimeString);
            }

            
            GwRainData PreGrd = new GwRainData();
            for (DateTime i = dtStart1; i <= dtCheck; i = i.AddMinutes(10))
            {
                double dTemp = 0;
                GwRainData grd = new GwRainData();
                grd.dAccumulation = 0;

                List <CgiStationData> tmpCgiStationDatas = lstData.Where(c => c.DatetimeString == M11DatetimeToString(i)).ToList<CgiStationData>();
                if (tmpCgiStationDatas.Count == 0)
                {
                    //如果沒資料，則抓上次的雨量資料，但是，不計算累積量
                    grd.DatetimeString = M11DatetimeToString(i);
                    grd.dtDatetime = i;                    
                    grd.dRAIN = PreGrd.dRAIN;
                    grd.dAccumulation = 0;
                }
                else
                {
                    if (idex == 0) //第一筆特殊處理
                    {
                        grd.dAccumulation = 0;
                        grd.DatetimeString = M11DatetimeToString(i);
                        grd.dtDatetime = i;
                        double.TryParse(tmpCgiStationDatas[0].Value, out dTemp);                        
                        grd.dRAIN = dTemp;
                        lstGwRainData.Add(grd);

                        PreGrd = grd;
                        idex++;
                        continue;
                    }

                    grd.DatetimeString = M11DatetimeToString(i);
                    grd.dtDatetime = i;
                    double.TryParse(tmpCgiStationDatas[0].Value, out dTemp);
                    grd.dRAIN = dTemp;
                    //與前一筆比較，計算累積量(跨天計數會歸0，可能造成負數)
                    if ((grd.dRAIN - PreGrd.dRAIN) > 0)
                    {
                        grd.dAccumulation = grd.dRAIN - PreGrd.dRAIN;
                    }
                    
                }

                lstGwRainData.Add(grd);
                PreGrd = grd;

                idex++;
            }

            
            string sRain10m = "0";
            string sRain1h = "0";
            string sRain3h = "0";
            string sRain6h = "0";
            string sRain12h = "0";
            string sRain24h = "0";
            string sRain48h = "0";
            string sRain72h = "0";
            double dRain10m = 0;
            double dRain1h = 0;
            double dRain3h = 0;
            double dRain6h = 0;
            double dRain12h = 0;
            double dRain24h = 0;
            double dRain48h = 0;
            double dRain72h = 0;

            //計數值的單位(累積)
            //double dValueUnit = 5;
            // 20210420 計數單位5改為0.5
            double dValueUnit = 0.5;

            //新計算方式，要跟前面的資料比對，才知道有沒有累積

            //10m - 直接取得累積量直接帶入
            GwRainData tmpGwRainData = lstGwRainData.Where(c => c.DatetimeString == M11DatetimeToString(dtCheck)).FirstOrDefault();
            if (tmpGwRainData != null)
            {
                dRain10m = lstGwRainData.Where(c => c.DatetimeString == M11DatetimeToString(dtCheck)).FirstOrDefault().dAccumulation;
                dRain10m = dRain10m * dValueUnit;
            }
            

            //1h
            DateTime dtPreTemp = dtCheck.AddHours(-1);
            dRain1h = lstGwRainData.Where(c => c.dtDatetime >= dtPreTemp && c.dtDatetime <= dtCheck).Sum(x => x.dAccumulation);
            dRain1h = dRain1h * dValueUnit;

            //3h
            dtPreTemp = dtCheck.AddHours(-3);
            dRain3h = lstGwRainData.Where(c => c.dtDatetime >= dtPreTemp && c.dtDatetime <= dtCheck).Sum(x => x.dAccumulation);
            dRain3h = dRain3h * dValueUnit;

            //6h
            dtPreTemp = dtCheck.AddHours(-6);
            dRain6h = lstGwRainData.Where(c => c.dtDatetime >= dtPreTemp && c.dtDatetime <= dtCheck).Sum(x => x.dAccumulation);
            dRain6h = dRain6h * dValueUnit;

            //12h
            dtPreTemp = dtCheck.AddHours(-12);
            dRain12h = lstGwRainData.Where(c => c.dtDatetime >= dtPreTemp && c.dtDatetime <= dtCheck).Sum(x => x.dAccumulation);
            dRain12h = dRain12h * dValueUnit;

            //24h
            dtPreTemp = dtCheck.AddHours(-24);
            dRain24h = lstGwRainData.Where(c => c.dtDatetime >= dtPreTemp && c.dtDatetime <= dtCheck).Sum(x => x.dAccumulation);
            dRain24h = dRain24h * dValueUnit;

            //48h
            dtPreTemp = dtCheck.AddHours(-48);
            dRain48h = lstGwRainData.Where(c => c.dtDatetime >= dtPreTemp && c.dtDatetime <= dtCheck).Sum(x => x.dAccumulation);
            dRain48h = dRain48h * dValueUnit;

            //72h
            dtPreTemp = dtCheck.AddHours(-72);
            dRain72h = lstGwRainData.Where(c => c.dtDatetime >= dtPreTemp && c.dtDatetime <= dtCheck).Sum(x => x.dAccumulation);
            dRain72h = dRain72h * dValueUnit;

            //四捨五入小數點1位
            dRain10m = Math.Round(dRain10m, 1);
            dRain1h = Math.Round(dRain1h, 1);
            dRain3h = Math.Round(dRain3h, 1);
            dRain6h = Math.Round(dRain6h, 1);
            dRain12h = Math.Round(dRain12h, 1);
            dRain24h = Math.Round(dRain24h, 1);
            dRain48h = Math.Round(dRain48h, 1);
            dRain72h = Math.Round(dRain72h, 1);

            sRain10m = dRain10m.ToString();
            sRain1h = dRain1h.ToString();
            sRain3h = dRain3h.ToString();
            sRain6h = dRain6h.ToString();
            sRain12h = dRain12h.ToString();
            sRain24h = dRain24h.ToString();
            sRain48h = dRain48h.ToString();
            sRain72h = dRain72h.ToString();

            sResult = string.Format("{0} {1} {2} {3} {4} {5} {6} {7}"
                , sRain10m, sRain1h, sRain3h, sRain6h, sRain12h, sRain24h, sRain48h, sRain72h);

            return sResult;
        }

        /// <summary>
        /// 取得水位資料結果(GW)
        /// </summary>
        /// <param name="StationName">站名</param>
        /// <param name="SensorName">儀器名稱</param>
        /// <param name="dtCheck">比對時間點</param>
        /// <returns></returns>
        private string getObservationWell(BasStationSensor SensorRow, DateTime dtCheck, ref DateTime dtGetDataTime, ref string sCgiData)
        {
            string sResult = "";
            string StationName = SensorRow.Station;
            string SensorName = SensorRow.Sensor;

            DataTable dtStationData = GetStationData(StationName);


            DateTime dtStart = dtCheck;
            DateTime dt10m = dtCheck.AddMinutes(-10);

            string sWater = "";
            DataRow[] dr10m = dtStationData.Select(
                string.Format(" datetime >= '{0}' and datetime <= '{1}' ", dt10m.ToString("yyyyMMddHHmmss"), dtStart.ToString("yyyyMMddHHmmss"))
                , " datetime desc ");
            if (dr10m.Count() == 0)
            {
                sWater = "0";
            }
            else
            {
                sWater = dr10m[0][CgiConst.WATER].ToString();
                //紀錄資料時間
                dtGetDataTime = Utils.getStringToDateTime(dr10m[0][CgiConst.DATETIME].ToString());
            }

            //水位高(m)	相對水位高(m)
            //相對水位高 = 水位高 - 常時水位
            double dWater = 0;
            double dDefaultWater = 0;
            double dRelativeWater = 0;
            double.TryParse(sWater, out dWater);
            double.TryParse(SensorRow.DefaultWater, out dDefaultWater);
            dRelativeWater = dWater - dDefaultWater;

            //四捨五入小數點2位
            dWater = Math.Round(dWater, 2);
            dRelativeWater = Math.Round(dRelativeWater, 2);

            //儀器異常，則不顯示數值
            if (SensorRow.DefaultWater == "-999")
            {
                dWater = 0;
                dRelativeWater = 0;
            }

            sResult = string.Format("{0} {1}", dWater.ToString(), dRelativeWater.ToString());

            return sResult;
        }

        /// <summary>
        /// 取得水位資料結果(GW)
        /// </summary>
        /// <param name="StationName">站名</param>
        /// <param name="SensorName">儀器名稱</param>
        /// <param name="dtCheck">比對時間點</param>
        /// <returns></returns>
        private string getObservationWellFromDB(BasStationSensor SensorRow, DateTime dtCheck, ref DateTime dtGetDataTime, ref string sCgiData)
        {
            string sResult = "";
            string StationName = SensorRow.Station;
            string SensorName = SensorRow.Sensor;
            
            //預設為0
            string sWater = "0";
            //[WATER]
            ssql = @"
                    select * from CgiStationData where Station = '{0}' and datatype = 'WATER' and DatetimeString = '{1}'
                    ";
            ssql = string.Format(ssql, StationName , M11DatetimeToString(dtCheck));
            List<CgiStationData> lstData = dbDapper.Query<CgiStationData>(ssql);

            if (lstData.Count() > 0)
            {
                sWater = lstData[0].Value;
                sCgiData = lstData[0].Value;
                //紀錄資料時間
                dtGetDataTime = Utils.getStringToDateTime(lstData[0].DatetimeString);
            }

            //水位高(m)	相對水位高(m)
            //相對水位高 = 水位高 - 常時水位
            double dWater = 0;
            double dDefaultWater = 0;
            double dRelativeWater = 0;
            double.TryParse(sWater, out dWater);
            double.TryParse(SensorRow.DefaultWater, out dDefaultWater);
            dRelativeWater = dWater - dDefaultWater;

            //四捨五入小數點2位
            dWater = Math.Round(dWater, 2);
            dRelativeWater = Math.Round(dRelativeWater, 2);

            //儀器異常，則不顯示數值
            if (SensorRow.DefaultWater == "-999")
            {
                dWater = 0;
                dRelativeWater = 0;
            }

            sResult = string.Format("{0} {1}", dWater.ToString(), dRelativeWater.ToString());

            return sResult;
        }


        /// <summary>
        /// 取得水位資料結果(PM)
        /// </summary>
        /// <param name="StationName">站名</param>
        /// <param name="SensorName">儀器名稱</param>
        /// <param name="dtCheck">比對時間點</param>
        /// <returns></returns>
        private string getPiezoMeter(BasStationSensor SensorRow, DateTime dtCheck, ref DateTime dtGetDataTime, ref string sCgiData)
        {
            string sResult = "";
            string StationName = SensorRow.Station;
            string SensorName = SensorRow.Sensor;

            DataTable dtStationData = GetStationData(StationName);


            DateTime dtStart = dtCheck;
            DateTime dt10m = dtCheck.AddMinutes(-10);

            string sWater = "";
            DataRow[] dr10m = dtStationData.Select(
                string.Format(" datetime >= '{0}' and datetime <= '{1}' ", dt10m.ToString("yyyyMMddHHmmss"), dtStart.ToString("yyyyMMddHHmmss"))
                , " datetime desc ");
            if (dr10m.Count() == 0)
            {
                sWater = "0";
            }
            else
            {
                sWater = dr10m[0][CgiConst.WATER].ToString();

                //紀錄資料時間
                dtGetDataTime = Utils.getStringToDateTime(dr10m[0][CgiConst.DATETIME].ToString());
            }

            //水位高(m)	相對水位高(m)
            //相對水位高 = 水位高 - 常時水位
            double dWater = 0;
            double dDefaultWater = 0;
            double dRelativeWater = 0;
            double.TryParse(sWater, out dWater);
            double.TryParse(SensorRow.DefaultWater, out dDefaultWater);
            dRelativeWater = dWater - dDefaultWater;

            //四捨五入小數點2位            
            dWater = Math.Round(dWater, 2);
            dRelativeWater = Math.Round(dRelativeWater, 2);

            //儀器異常，則不顯示數值
            if (SensorRow.DefaultWater == "-999")
            {
                dWater = 0;
                dRelativeWater = 0;
            }

            sResult = string.Format("{0} {1}", dWater.ToString(), dRelativeWater.ToString());

            return sResult;
        }

        /// <summary>
        /// 取得水位資料結果(PM)
        /// </summary>
        /// <param name="StationName">站名</param>
        /// <param name="SensorName">儀器名稱</param>
        /// <param name="dtCheck">比對時間點</param>
        /// <returns></returns>
        private string getPiezoMeterFromDB(BasStationSensor SensorRow, DateTime dtCheck, ref DateTime dtGetDataTime, ref string sCgiData)
        {
            string sResult = "";
            string StationName = SensorRow.Station;
            string SensorName = SensorRow.Sensor;

            //預設為0
            string sWater = "0";
            //[WATER]
            ssql = @"
                    select * from CgiStationData where Station = '{0}' and datatype = 'WATER' and DatetimeString = '{1}'
                    ";
            ssql = string.Format(ssql, StationName, M11DatetimeToString(dtCheck));
            List<CgiStationData> lstData = dbDapper.Query<CgiStationData>(ssql);

            if (lstData.Count() > 0)
            {
                sWater = lstData[0].Value;
                sCgiData = lstData[0].Value;
                //紀錄資料時間
                dtGetDataTime = Utils.getStringToDateTime(lstData[0].DatetimeString);
            }

            //水位高(m)	相對水位高(m)
            //相對水位高 = 水位高 - 常時水位
            double dWater = 0;
            double dDefaultWater = 0;
            double dRelativeWater = 0;
            double.TryParse(sWater, out dWater);
            double.TryParse(SensorRow.DefaultWater, out dDefaultWater);
            dRelativeWater = dWater - dDefaultWater;

            //四捨五入小數點2位            
            dWater = Math.Round(dWater, 2);
            dRelativeWater = Math.Round(dRelativeWater, 2);

            //儀器異常，則不顯示數值
            if (SensorRow.DefaultWater == "-999")
            {
                dWater = 0;
                dRelativeWater = 0;
            }

            sResult = string.Format("{0} {1}", dWater.ToString(), dRelativeWater.ToString());

            return sResult;
        }

        /// <summary>
        /// 取得水位資料結果(TM)
        /// </summary>
        /// <param name="StationName">站名</param>
        /// <param name="SensorName">儀器名稱</param>
        /// <param name="dtCheck">比對時間點</param>
        /// <returns></returns>
        private string getBiTiltMeter(BasStationSensor SensorRow, DateTime dtCheck, ref DateTime dtGetDataTime, ref string sCgiData)
        {
            string sResult = "";
            //string StationName = SensorRow["Station"].ToString();
            //string SensorName = SensorRow["Sensor"].ToString();
            string StationName = SensorRow.Station;
            string SensorName = SensorRow.Sensor;

            DataTable dtStationData = GetStationData(StationName);


            DateTime dtStart = dtCheck;
            DateTime dt10m = dtCheck.AddMinutes(-10);

            string sSX = "0";
            string sSY = "0";
            DataRow[] dr10m = dtStationData.Select(
                string.Format(" datetime >= '{0}' and datetime <= '{1}' ", dt10m.ToString("yyyyMMddHHmmss"), dtStart.ToString("yyyyMMddHHmmss"))
                , " datetime desc ");
            if (dr10m.Count() == 0)
            {
                sSX = "0";
                sSY = "0";
            }
            else
            {
                sSX = dr10m[0][CgiConst.SX].ToString();
                sSY = dr10m[0][CgiConst.SY].ToString();

                //紀錄資料時間
                dtGetDataTime = Utils.getStringToDateTime(dr10m[0][CgiConst.DATETIME].ToString());
                
            }

            //一天前的資料
            //DateTime dtStart_before = dtCheck.AddMinutes(-10 * 144);
            //DateTime dt10m_before = dtCheck.AddMinutes(-10 * (144 + 1));
            //string sSX_before = "0";
            //string sSY_before = "0";
            //DataRow[] dr10m_before = dtStationData.Select(
            //    string.Format(" datetime >= '{0}' and datetime <= '{1}' ", dt10m_before.ToString("yyyyMMddHHmmss"), dtStart_before.ToString("yyyyMMddHHmmss"))
            //    , " datetime desc ");
            //if (dr10m_before.Count() == 0)
            //{
            //    sSX_before = "0";
            //    sSY_before = "0";
            //}
            //else
            //{
            //    sSX_before = dr10m_before[0][CgiConst.SX].ToString();
            //    sSY_before = dr10m_before[0][CgiConst.SY].ToString();
            //}

            
            //調整時間為整點格式 如(2021-04-12 23:30:00)-"yyyy-MM-dd HH:mm:ss"
            //dtCheck = Utils.getStringToDateTime(dtCheck.ToString("yyyy-MM-dd HH:mm:00"));
            //20210413 改抓已經產的XML資料，避免沒有資料產生時，資料會有異常
            DateTime dtBefore1D = dtCheck.AddDays(-1);
            string sD_before = "0";
            string sE_before = "0";

            ssql = @"
                        select * from Result10MinData
                        where StationID = '{0}' and SensorID = '{1}' and DatetimeString = '{2}'
                    ";
            ssql = string.Format(ssql, StationName, SensorName, dtBefore1D.ToString("yyyy-MM-dd HH:mm:00"));

            Result10MinData qRd = dbDapper.QuerySingleOrDefault<Result10MinData>(ssql);

            if (qRd != null)
            {
                string[] values = qRd.value.Split(' ');
                if (values.Length == 6)
                {
                    sD_before = values[0];
                    sE_before = values[1];
                }
            }




            //D方位一觀測值(秒)	E方位二觀測值(秒)	F方位一累積變位量(秒)	G方位二累積變位量(秒)	H方位一速率(秒/天)	I方位二速率(秒/天)
            //D=SX*3600	        E=SY*3600	    F=D-初始值(0)	        G=E-初始值(0)	        H=今天D-昨天D	    I=今天E-昨天E

            double dSX = 0;
            double dSY = 0;
            double dD = 0;
            double dE = 0;
            double dF = 0;
            double dG = 0;
            double dH = 0;
            double dI = 0;

            //一天前的資料
            //double dSX_before = 0;
            //double dSY_before = 0;
            double dD_before = 0;
            double dE_before = 0;

            double.TryParse(sSX, out dSX);
            double.TryParse(sSY, out dSY);
            double.TryParse(sD_before, out dD_before);
            double.TryParse(sE_before, out dE_before);
            dD = dSX * 3600;
            dE = dSY * 3600;
            //dD_before = dSX_before * 3600;
            //dE_before = dSY_before * 3600;
            dF = dD - 0;
            dG = dE - 0;
            dH = dD - dD_before;
            dI = dE - dE_before;

            //四捨五入整數位
            dD = Math.Round(dD, 0);
            dE = Math.Round(dE, 0);
            dF = Math.Round(dF, 0);
            dG = Math.Round(dG, 0);
            dH = Math.Round(dH, 0);
            dI = Math.Round(dI, 0);


            sResult = string.Format("{0} {1} {2} {3} {4} {5}", dD.ToString(), dE.ToString()
                , dF.ToString(), dG.ToString(), dH.ToString(), dI.ToString());

            return sResult;
        }

        /// <summary>
        /// 取得水位資料結果(TM)
        /// </summary>
        /// <param name="StationName">站名</param>
        /// <param name="SensorName">儀器名稱</param>
        /// <param name="dtCheck">比對時間點</param>
        /// <returns></returns>
        private string getBiTiltMeterFromDB(BasStationSensor SensorRow, DateTime dtCheck, ref DateTime dtGetDataTime, ref string sCgiData)
        {
            string sResult = "";
            string StationName = SensorRow.Station;
            string SensorName = SensorRow.Sensor;

            string sSX = "0";
            string sSY = "0";

            //[SX SY]
            ssql = @"
                    select * from CgiStationData where Station = '{0}' and DatetimeString = '{1}' and datatype in ('{2}','{3}') 
                    ";
            ssql = string.Format(ssql, StationName, M11DatetimeToString(dtCheck), CgiConst.SX, CgiConst.SY);
            List<CgiStationData> lstData = dbDapper.Query<CgiStationData>(ssql);

            if (lstData.Count() > 0)
            {
                if (lstData.Where(c => c.DataType == CgiConst.SX).Count() > 0)
                {
                    CgiStationData dataSX = lstData.Where(c => c.DataType == CgiConst.SX).ToList()[0];
                    sSX = dataSX.Value;
                }
                
                if (lstData.Where(c => c.DataType == CgiConst.SY).Count() > 0)
                {
                    CgiStationData dataSY = lstData.Where(c => c.DataType == CgiConst.SY).ToList()[0];
                    sSY= dataSY.Value;
                }
                
                sCgiData = string.Format("{0} {1}", sSX, sSY);
                //紀錄資料時間
                dtGetDataTime = Utils.getStringToDateTime(lstData[0].DatetimeString);
            }
           
            //20210413 改抓已經產的XML資料，避免沒有資料產生時，資料會有異常
            //抓昨天的資料
            DateTime dtBefore1D = dtCheck.AddDays(-1);
            string sD_before = "0";
            string sE_before = "0";

            ssql = @"
                        select * from Result10MinData
                        where StationID = '{0}' and SensorID = '{1}' and DatetimeString = '{2}'
                    ";
            ssql = string.Format(ssql, StationName, SensorName, M11DatetimeToString(dtBefore1D));

            Result10MinData qRd = dbDapper.QuerySingleOrDefault<Result10MinData>(ssql);

            if (qRd != null)
            {
                string[] values = qRd.value.Split(' ');
                if (values.Length == 6)
                {
                    sD_before = values[0];
                    sE_before = values[1];
                }
            }

            //D方位一觀測值(秒)	E方位二觀測值(秒)	F方位一累積變位量(秒)	G方位二累積變位量(秒)	H方位一速率(秒/天)	I方位二速率(秒/天)
            //D=SX*3600	        E=SY*3600	    F=D-初始值(0)	        G=E-初始值(0)	        H=今天D-昨天D	    I=今天E-昨天E

            double dSX = 0;
            double dSY = 0;
            double dD = 0;
            double dE = 0;
            double dF = 0;
            double dG = 0;
            double dH = 0;
            double dI = 0;

            //一天前的資料
            double dD_before = 0;
            double dE_before = 0;

            double.TryParse(sSX, out dSX);
            double.TryParse(sSY, out dSY);
            double.TryParse(sD_before, out dD_before);
            double.TryParse(sE_before, out dE_before);
            dD = dSX * 3600;
            dE = dSY * 3600;
            dF = dD - 0;
            dG = dE - 0;
            dH = dD - dD_before;
            dI = dE - dE_before;

            //四捨五入整數位
            dD = Math.Round(dD, 0);
            dE = Math.Round(dE, 0);
            dF = Math.Round(dF, 0);
            dG = Math.Round(dG, 0);
            dH = Math.Round(dH, 0);
            dI = Math.Round(dI, 0);

            sResult = string.Format("{0} {1} {2} {3} {4} {5}", dD.ToString(), dE.ToString()
                , dF.ToString(), dG.ToString(), dH.ToString(), dI.ToString());

            return sResult;
        }

        /// <summary>
        /// 取得水位資料結果(TM2)
        /// </summary>
        /// <param name="StationName">站名</param>
        /// <param name="SensorName">儀器名稱</param>
        /// <param name="dtCheck">比對時間點</param>
        /// <returns></returns>
        private string getBiTiltMeter2(BasStationSensor SensorRow, DateTime dtCheck, ref DateTime dtGetDataTime, ref string sCgiData)
        {
            string sResult = "";
            string StationName = SensorRow.Station;
            string SensorName = SensorRow.Sensor;

            DataTable dtStationData = GetStationData(StationName);


            DateTime dtStart = dtCheck;
            DateTime dt10m = dtCheck.AddMinutes(-10);

            string sSX = "0";
            string sSY = "0";
            DataRow[] dr10m = dtStationData.Select(
                string.Format(" datetime >= '{0}' and datetime <= '{1}' ", dt10m.ToString("yyyyMMddHHmmss"), dtStart.ToString("yyyyMMddHHmmss"))
                , " datetime desc ");
            if (dr10m.Count() == 0)
            {
                sSX = "0";
                sSY = "0";
            }
            else
            {
                sSX = dr10m[0][CgiConst.S2X].ToString();
                sSY = dr10m[0][CgiConst.S2Y].ToString();

                //紀錄資料時間
                dtGetDataTime = Utils.getStringToDateTime(dr10m[0][CgiConst.DATETIME].ToString());

            }

            //一天前的資料
            DateTime dtStart_before = dtCheck.AddMinutes(-10 * 144);
            DateTime dt10m_before = dtCheck.AddMinutes(-10 * (144 + 1));
            string sSX_before = "0";
            string sSY_before = "0";
            DataRow[] dr10m_before = dtStationData.Select(
                string.Format(" datetime >= '{0}' and datetime <= '{1}' ", dt10m_before.ToString("yyyyMMddHHmmss"), dtStart_before.ToString("yyyyMMddHHmmss"))
                , " datetime desc ");
            if (dr10m_before.Count() == 0)
            {
                sSX_before = "0";
                sSY_before = "0";
            }
            else
            {
                sSX_before = dr10m_before[0][CgiConst.S2X].ToString();
                sSY_before = dr10m_before[0][CgiConst.S2Y].ToString();
            }

            //D方位一觀測值(秒)	E方位二觀測值(秒)	F方位一累積變位量(秒)	G方位二累積變位量(秒)	H方位一速率(秒/天)	I方位二速率(秒/天)
            //D=SX*3600	        E=SY*3600	    F=D-初始值(0)	    G=E-初始值(0)	    H=今天D-昨天D	    I=今天E-昨天E

            double dSX = 0;
            double dSY = 0;
            double dD = 0;
            double dE = 0;
            double dF = 0;
            double dG = 0;
            double dH = 0;
            double dI = 0;

            //一天前的資料
            double dSX_before = 0;
            double dSY_before = 0;
            double dD_before = 0;
            double dE_before = 0;

            double.TryParse(sSX, out dSX);
            double.TryParse(sSY, out dSY);
            double.TryParse(sSX_before, out dSX_before);
            double.TryParse(sSY_before, out dSY_before);
            dD = dSX * 3600;
            dE = dSY * 3600;
            dD_before = dSX_before * 3600;
            dE_before = dSY_before * 3600;
            dF = dD - 0;
            dG = dE - 0;
            dH = dD - dD_before;
            dI = dE - dE_before;

            //四捨五入整數位
            dD = Math.Round(dD, 0);
            dE = Math.Round(dE, 0);
            dF = Math.Round(dF, 0);
            dG = Math.Round(dG, 0);
            dH = Math.Round(dH, 0);
            dI = Math.Round(dI, 0);


            sResult = string.Format("{0} {1} {2} {3} {4} {5}", dD.ToString(), dE.ToString()
                , dF.ToString(), dG.ToString(), dH.ToString(), dI.ToString());

            return sResult;
        }

        /// <summary>
        /// 取得水位資料結果(TM2)
        /// </summary>
        /// <param name="StationName">站名</param>
        /// <param name="SensorName">儀器名稱</param>
        /// <param name="dtCheck">比對時間點</param>
        /// <returns></returns>
        private string getBiTiltMeter2FromDB(BasStationSensor SensorRow, DateTime dtCheck, ref DateTime dtGetDataTime, ref string sCgiData)
        {
            string sResult = "";
            string StationName = SensorRow.Station;
            string SensorName = SensorRow.Sensor;

            string sSX = "0";
            string sSY = "0";

            //[SX SY]
            ssql = @"
                    select * from CgiStationData where Station = '{0}' and DatetimeString = '{1}' and datatype in ('{2}','{3}') 
                    ";
            ssql = string.Format(ssql, StationName, M11DatetimeToString(dtCheck), CgiConst.S2X, CgiConst.S2Y);
            List<CgiStationData> lstData = dbDapper.Query<CgiStationData>(ssql);

            if (lstData.Count() > 0)
            {
                if (lstData.Where(c => c.DataType == CgiConst.S2X).Count() > 0)
                {
                    CgiStationData dataS2X = lstData.Where(c => c.DataType == CgiConst.S2X).ToList()[0];
                    sSX = dataS2X.Value;
                }

                if (lstData.Where(c => c.DataType == CgiConst.S2Y).Count() > 0)
                {
                    CgiStationData dataS2Y = lstData.Where(c => c.DataType == CgiConst.S2Y).ToList()[0];
                    sSY = dataS2Y.Value;
                }

                sCgiData = string.Format("{0} {1}", sSX, sSY);
                //紀錄資料時間
                dtGetDataTime = Utils.getStringToDateTime(lstData[0].DatetimeString);
            }

            //20210413 改抓已經產的XML資料，避免沒有資料產生時，資料會有異常
            //抓昨天的資料
            DateTime dtBefore1D = dtCheck.AddDays(-1);
            string sD_before = "0";
            string sE_before = "0";

            ssql = @"
                        select * from Result10MinData
                        where StationID = '{0}' and SensorID = '{1}' and DatetimeString = '{2}'
                    ";
            ssql = string.Format(ssql, StationName, SensorName, M11DatetimeToString(dtBefore1D));

            Result10MinData qRd = dbDapper.QuerySingleOrDefault<Result10MinData>(ssql);

            if (qRd != null)
            {
                string[] values = qRd.value.Split(' ');
                if (values.Length == 6)
                {
                    sD_before = values[0];
                    sE_before = values[1];
                }
            }

            //D方位一觀測值(秒)	E方位二觀測值(秒)	F方位一累積變位量(秒)	G方位二累積變位量(秒)	H方位一速率(秒/天)	I方位二速率(秒/天)
            //D=SX*3600	        E=SY*3600	    F=D-初始值(0)	    G=E-初始值(0)	    H=今天D-昨天D	    I=今天E-昨天E

            double dSX = 0;
            double dSY = 0;
            double dD = 0;
            double dE = 0;
            double dF = 0;
            double dG = 0;
            double dH = 0;
            double dI = 0;

            //一天前的資料
            //double dSX_before = 0;
            //double dSY_before = 0;
            double dD_before = 0;
            double dE_before = 0;

            double.TryParse(sSX, out dSX);
            double.TryParse(sSY, out dSY);
            double.TryParse(sD_before, out dD_before);
            double.TryParse(sE_before, out dE_before);
            dD = dSX * 3600;
            dE = dSY * 3600;
            dF = dD - 0;
            dG = dE - 0;
            dH = dD - dD_before;
            dI = dE - dE_before;

            //四捨五入整數位
            dD = Math.Round(dD, 0);
            dE = Math.Round(dE, 0);
            dF = Math.Round(dF, 0);
            dG = Math.Round(dG, 0);
            dH = Math.Round(dH, 0);
            dI = Math.Round(dI, 0);

            sResult = string.Format("{0} {1} {2} {3} {4} {5}", dD.ToString(), dE.ToString()
                , dF.ToString(), dG.ToString(), dH.ToString(), dI.ToString());

            return sResult;
        }

        /// <summary>
        /// 產生結果XML(每十分鐘)
        /// </summary>
        private void ProcGenResultXML(DateTime dtCheck) 
        {
            try
            {

                #if DEBUG
                //dtCheck = new DateTime(2021, 4, 13, 17, 30, 8);
                #endif

                //預計排程每分鐘執行一次，排除非剛好10分鐘的執行(00,10,20,30,40,50)
                if (dtCheck.Minute.ToString().PadLeft(2, '0').Substring(1, 1) != "0") return;

                List<string> lstSite = new List<string>();
                ssql = @"
                    select * from  BasStationSensor where RenderXML_YN = 'Y'  order by Site,Station
                ";
                List<BasStationSensor> lstBasStationSensor = dbDapper.Query<BasStationSensor>(ssql);
                lstSite = lstBasStationSensor.Select(x => x.Site).Distinct().ToList();
                

                //調整時間為整點格式 如(2021-04-12 23:30:00)-"yyyy-MM-dd HH:mm:ss"
                dtCheck = Utils.getStringToDateTime(dtCheck.ToString("yyyy-MM-dd HH:mm:00"));

                ShowMessageToFront("產生結果XML(每十分鐘)==啟動");
                using (StringWriter stringWriter = new StringWriter())
                {
                    using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter))
                    {
                        XmlDocument doc = new XmlDocument();
                        XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
                        doc.AppendChild(dec);
                        var file_attribute = doc.CreateElement("file_attribute");
                        file_attribute.SetAttribute("file_name", "10min_a_ds_data.xml");
                        file_attribute.SetAttribute("mteam_id", "ESWCRC");
                        file_attribute.SetAttribute("time", M11DatetimeToString(dtCheck));
                        doc.AppendChild(file_attribute);
                        var tenmin_a_ds_data = doc.CreateElement("tenmin_a_ds_data");
                        file_attribute.AppendChild(tenmin_a_ds_data);

                        int iIndex = 1;
                        foreach (string SiteName in lstSite)
                        {
                            ShowMessageToFront(string.Format("[{0}/{1}]產生結果XML(每十分鐘)=={2}", iIndex.ToString(), lstSite.Count(), SiteName));
                            //site_data
                            var site_data = doc.CreateElement("site_data");
                            site_data.SetAttribute("siteid", SiteName);
                            site_data.SetAttribute("monitoring_light", "Green");

                            //factorInfo
                            var factorInfo = doc.CreateElement("factorInfo");
                            factorInfo.SetAttribute("factors_num", "0");
                            site_data.AppendChild(factorInfo);

                            //station
                            List<string> lstStation = 
                                lstBasStationSensor.Where(x => x.Site == SiteName).Select(x => x.Station).Distinct().ToList();
                            foreach (string StationName in lstStation)
                            {   
                                var station = doc.CreateElement("station");
                                station.SetAttribute("stationId", StationName);

                                //Sensor
                                List<BasStationSensor> lstSensor
                                    = lstBasStationSensor.Where(x => x.Station == StationName).ToList();
                                foreach (BasStationSensor SensorRow in lstSensor)
                                {
                                    DateTime dtGetDataTime = new DateTime();
                                    string sCgiData = "";
                                    string SensorName = SensorRow.Sensor; //SensorRow["Sensor"].ToString();
                                    string sobservation_num = getObservation_num(SensorName);
                                    string ssensor_status = "0";
                                    string svalue = getSensorDataResult(SensorRow, dtCheck, ref dtGetDataTime, ref sCgiData);
                                    // 20210414 調整為XML產生時間，不需要資料取得時間
                                    //string sGetTime = M11DatetimeToString(dtGetDataTime);
                                    string sGetTime = dtCheck.ToString("yyyy-MM-dd HH:mm:00");
                                    string sRemark = "";
                                    if (dtGetDataTime.Year == 1) //沒有取得資料，所以回傳預設時間
                                    {
                                        //sGetTime = "0000-00-00 00:00:00";

                                        //如果沒有取得資料，則使用上個時段的資料顯示
                                        DateTime dtPre = dtCheck.AddMinutes(-10);
                                        ssql = @"
                                            select * from Result10MinData
                                            where StationID = '{0}' and SensorID = '{1}' and DatetimeString = '{2}'
                                        ";
                                        ssql = string.Format(ssql, StationName, SensorName, dtPre.ToString("yyyy-MM-dd HH:mm:00"));

                                        Result10MinData qRd = dbDapper.QuerySingleOrDefault<Result10MinData>(ssql);

                                        if (qRd != null)
                                        {
                                            svalue = qRd.value;
                                            sRemark = "取前一時刻資料";
                                        }

                                    }

                                    var sensor = doc.CreateElement("sensor");
                                    sensor.SetAttribute("sensorId", string.Format("{0}-{1}", StationName, SensorName));
                                    sensor.SetAttribute("sensor_type", getSensor_Type(SensorName));
                                    sensor.SetAttribute("observation_num", sobservation_num);
                                    sensor.SetAttribute("sensor_status", ssensor_status);
                                    sensor.SetAttribute("time", sGetTime);
                                    sensor.InnerText = svalue;
                                    station.AppendChild(sensor);


                                    //判斷是否有資料存在，沒資料新增，有資料更新
                                    ssql = @"
                                            select * from Result10MinData
                                            where StationID = '{0}' and SensorID = '{1}' and DatetimeString = '{2}'
                                        ";
                                    ssql = string.Format(ssql, StationName, SensorName, dtCheck.ToString("yyyy-MM-dd HH:mm:00"));
                                    List<Result10MinData> lstcheck = dbDapper.Query<Result10MinData>(ssql);

                                    //如果出現多筆，則先刪除後再新增
                                    if (lstcheck.Count > 1)
                                    {
                                        foreach (Result10MinData item in lstcheck)
                                        {
                                            dbDapper.Delete<Result10MinData>(item);
                                        }
                                    }
                                    
                                    Result10MinData RD = dbDapper.QuerySingleOrDefault<Result10MinData>(ssql);
                                    if (RD == null)
                                    {
                                        //將每次結果寫入資料庫
                                        RD = new Result10MinData();
                                        RD.SiteID = SiteName;
                                        RD.StationID = StationName;
                                        RD.SensorID = SensorName;
                                        RD.DataType = getSensor_Type(SensorName);
                                        RD.DataName = getSensor_Type(SensorName);
                                        RD.Datetime = dtCheck;
                                        RD.DatetimeString = dtCheck.ToString("yyyy-MM-dd HH:mm:00");
                                        RD.GetTime = sGetTime;
                                        RD.observation_num = sobservation_num;
                                        RD.sensor_status = ssensor_status;
                                        RD.value = svalue;
                                        RD.remark = sRemark;
                                        RD.CgiData = sCgiData;

                                        dbDapper.Insert<Result10MinData>(RD);
                                    }
                                    else
                                    {
                                        RD.SiteID = SiteName;
                                        RD.StationID = StationName;
                                        RD.SensorID = SensorName;
                                        RD.DataType = getSensor_Type(SensorName);
                                        RD.DataName = getSensor_Type(SensorName);
                                        RD.Datetime = dtCheck;
                                        RD.DatetimeString = dtCheck.ToString("yyyy-MM-dd HH:mm:00");
                                        RD.GetTime = sGetTime;
                                        RD.observation_num = sobservation_num;
                                        RD.sensor_status = ssensor_status;
                                        RD.value = svalue;
                                        RD.remark = sRemark;
                                        RD.CgiData = sCgiData;
                                        dbDapper.Update<Result10MinData>(RD);
                                    }
                                }

                                site_data.AppendChild(station);
                            }


                            tenmin_a_ds_data.AppendChild(site_data);
                        }

                        //var body = doc.CreateElement("body");
                        //file_attribute.AppendChild(body);

                        doc.WriteTo(xmlWriter);
                        xmlWriter.Flush();

                        //儲存到網頁發布路徑
                        doc.Save(Path.Combine(M11Const.Path_XmlResultWeb, "10min_a_ds_data.xml"));

                        //儲存到網頁發布路徑-7天歷史資料區
                        doc.Save(Path.Combine(M11Const.Path_XmlResultWeb7Day, string.Format("{0}_{1}", dtCheck.ToString("yyyyMMddHHmm"), "10min_a_ds_data.xml")));

                        //儲存到準備FTP上傳路徑
                        doc.Save(Path.Combine(M11Const.Path_FTPQueueXmlResult, string.Format("{0}_{1}", dtCheck.ToString("yyyyMMddHHmm"), "10min_a_ds_data.xml")));

                        //儲存到歷史路徑
                        doc.Save(Path.Combine(M11Const.Path_FTPQueueXmlResult7Day, string.Format("{0}_{1}", dtCheck.ToString("yyyyMMddHHmm"), "10min_a_ds_data.xml")));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "M11XML_XMLResult 轉檔錯誤:");

            }
        }


        /// <summary>
        /// 雨量站回傳氣象局-產生結果XML(每十分鐘)
        /// </summary>
        private void ProcGenRainfallXML(DateTime dtCheck)
        {
            try
            {

#if DEBUG
                //dtCheck = new DateTime(2021, 4, 29, 10, 00, 8);
#endif

                //預計排程每分鐘執行一次，排除非剛好10分鐘的執行(00,10,20,30,40,50)
                if (dtCheck.Minute.ToString().PadLeft(2, '0').Substring(1, 1) != "0") return;

                List<string> lstSite = new List<string>();
                foreach (DataRow TmpRow in DtStationData.Rows)
                {
                    if (lstSite.Contains(TmpRow["site"].ToString()) == false)
                    {
                        lstSite.Add(TmpRow["site"].ToString());
                    }
                }

                ssql = @"
                    select * from  BasRainallStation order by SensorName
                ";
                List<BasRainallStation> lstTmp = dbDapper.Query<BasRainallStation>(ssql);

                //調整時間為整點格式 如(2021-04-12 23:30:00)-"yyyy-MM-dd HH:mm:ss"
                dtCheck = Utils.getStringToDateTime(dtCheck.ToString("yyyy-MM-dd HH:mm:00"));

                //ShowMessageToFront(string.Format("[{0}/{1}]讀取原始資料轉檔到XML資料庫=={2}", iIndex.ToString(), aFiles.Length, fname));
                ShowMessageToFront("產生結果XML(每十分鐘)==啟動");
                using (StringWriter stringWriter = new StringWriter())
                {
                    using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter))
                    {
                        XmlDocument doc = new XmlDocument();
                        XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
                        doc.AppendChild(dec);
                        var DocumentElement = doc.CreateElement("DocumentElement");
                        doc.AppendChild(DocumentElement);
                        
                        foreach (BasRainallStation item in lstTmp)
                        {
                            string sStationName = "";
                            string sSensorName = "";

                            string[] aTmp = item.SensorName.Split('-');
                            if (aTmp.Length == 2)
                            {
                                sStationName = aTmp[0];
                                sSensorName  = aTmp[1];
                            }

                            double dRainfall = 0.0;                            
                            ssql = @"
                                            select * from Result10MinData
                                            where StationID = '{0}' and SensorID = '{1}' and DatetimeString = '{2}'
                                        ";
                            ssql = string.Format(ssql, sStationName, sSensorName, dtCheck.ToString("yyyy-MM-dd HH:mm:00"));
                            Result10MinData rmd = dbDapper.QuerySingleOrDefault<Result10MinData>(ssql);
                            if (rmd != null)
                            {
                                double.TryParse(rmd.CgiData, out dRainfall);
                            }
                            dRainfall = dRainfall * 0.5;

                            // 20210426 雨量站維護期間，使用網頁設定不啟用。並將雨量值改變-98表示維護中。
                            if (item.Active_YN == "N")
                            {
                                dRainfall = -98;
                            }

                            //row
                            var row = doc.CreateElement("row");

                            var id = doc.CreateElement("id");
                            var name = doc.CreateElement("name");
                            var Lon_67 = doc.CreateElement("Lon_67");
                            var Lat_67 = doc.CreateElement("Lat_67");
                            var disrict = doc.CreateElement("disrict");
                            var SensorName = doc.CreateElement("SensorName");
                            var rainfall = doc.CreateElement("rainfall");
                            var rtime = doc.CreateElement("rtime");

                            id.InnerText = item.ID;
                            name.InnerText = item.Name;
                            Lon_67.InnerText = item.Lon_67;
                            Lat_67.InnerText = item.Lat_67;
                            disrict.InnerText = item.Distrct;
                            SensorName.InnerText = item.SensorName;
                            rainfall.InnerText = dRainfall.ToString("#0.0");
                            rtime.InnerText = dtCheck.ToString("yyyy-MM-dd HH:mm:00");

                            row.AppendChild(id);
                            row.AppendChild(name);
                            row.AppendChild(Lon_67);
                            row.AppendChild(Lat_67);
                            row.AppendChild(disrict);
                            row.AppendChild(SensorName);
                            row.AppendChild(rainfall);
                            row.AppendChild(rtime);

                            DocumentElement.AppendChild(row);
                        }

                        doc.WriteTo(xmlWriter);
                        xmlWriter.Flush();

                        //儲存到網頁發布路徑
                        doc.Save(Path.Combine(M11Const.Path_XmlResultWeb, "PrecipitationToday.xml"));

                        //儲存到網頁發布路徑-7天歷史資料區
                        doc.Save(Path.Combine(M11Const.Path_PrecipitationWeb7Day, string.Format("{0}_{1}", dtCheck.ToString("yyyyMMddHHmm"), "PrecipitationToday.xml")));

                        //儲存到準備FTP上傳路徑
                        doc.Save(Path.Combine(M11Const.Path_FTPQueueXmlResult, string.Format("{0}_{1}", dtCheck.ToString("yyyyMMddHHmm"), "PrecipitationToday.xml")));

                        //儲存到歷史路徑
                        doc.Save(Path.Combine(M11Const.Path_FTPQueueXmlResult7Day, string.Format("{0}_{1}", dtCheck.ToString("yyyyMMddHHmm"), "PrecipitationToday.xml")));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "M11XML_XMLResult 轉檔錯誤:");

            }
           
        }


        
        /// <summary>
        /// 移除CGI資料超過四天的資料(每十分鐘)
        /// </summary>
        private void ProcRemoveCgiDb(DateTime dtCheck)
        {
            try
            {

                #if DEBUG
                    //dtCheck = new DateTime(2021, 4, 13, 17, 30, 8);
                #endif

                //預計排程每分鐘執行一次，排除非剛好10分鐘的執行(00,10,20,30,40,50)
                if (dtCheck.Minute.ToString().PadLeft(2, '0').Substring(1, 1) != "0") return;

                //移除超過四天的資料
                dtCheck = dtCheck.AddDays(-4);
                string sDatetimeString = M11DatetimeToString(dtCheck);

                ssql = @"
                    delete CgiStationData where DatetimeString < '{0}'
                ";
                ssql = string.Format(ssql, sDatetimeString);
                dbDapper.Execute(ssql);

            }
            catch (Exception ex)
            {
                logger.Error(ex, "M11XML_XMLResult 轉檔錯誤:");

            }

        }

        private void UploadFTPCgi() 
        {
            List<string> lstFile = new List<string>();

            ShowMessageToFront("[]FTP-上傳CGI檔案==啟動");
            //上傳CgiData
            FtpClient client = new FtpClient();
            try
            {
                
                client.Host = M11Const.FTP_IP;
                client.SocketKeepAlive = true;
                client.Credentials = new NetworkCredential(M11Const.FTP_User, M11Const.FTP_Password);
                client.Connect();

                // 取得資料夾內所有檔案
                int iIndex = 1;
                foreach (string fname in Directory.GetFiles(M11Const.Path_FTPQueueTxtOriginal))
                {
                    try
                    {
                        lstFile.Add(fname);
                        //FTP上傳路徑規劃
                        ///M11_System/Data/CgiData/2021/03/21

                        FileInfo fi = new FileInfo(fname);
                        string[] CgiNameSplit = fi.Name.Replace(fi.Extension, "").Split('-');

                        //避免舊檔案格式問題，排除沒有分析完整的檔案名稱
                        if (CgiNameSplit.Length != 8) continue;


                        //從檔案取得資料時間
                        DateTime dt = DateTime.ParseExact(CgiNameSplit[2] + CgiNameSplit[3] + CgiNameSplit[4] + CgiNameSplit[5] + CgiNameSplit[6] + CgiNameSplit[7], "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture);

                        //FluentFTP 起始路徑都是跟目錄開始，目錄結尾都是/
                        string sLocalPath = fi.FullName;
                        string sRemotePath = "/M11_System/Data/CgiData/";
                        sRemotePath = string.Format(@"{0}{1}/{2}/{3}/{4}"
                                , sRemotePath, dt.ToString("yyyy"), dt.ToString("MM"), dt.ToString("dd"), fi.Name);

                        //設定嘗試次數
                        client.RetryAttempts = 3;
                        //上傳檔案
                        client.UploadFile(sLocalPath, sRemotePath, FtpRemoteExists.Overwrite, true, FtpVerify.Retry);
                        ShowMessageToFront(string.Format("[{0}/{1}]上傳CGI檔案 成功=={2}", iIndex.ToString(), Directory.GetFiles(M11Const.Path_FTPQueueTxtOriginal).Length, fname));
                        iIndex++;

                        //存至備份資料夾
                        fi.CopyTo(Path.Combine(M11Const.Path_FTPQueueTxtOriginalBak, fi.Name), true);

                        //刪除已處理資料
                        fi.Delete();
                    }
                    catch (Exception ex)
                    {
                        //有錯誤持續執行
                        continue;
                    }                    
                }
            }                
            finally
            {
                client.Disconnect();
                client.Dispose();
            }

            //全部處理完畢再一次刪除
            //foreach (string fname in lstFile)
            //{
            //    FileInfo fi = new FileInfo(fname);

            //    //存至備份資料夾
            //    fi.CopyTo(Path.Combine(M11Const.Path_FTPQueueTxtOriginalBak, fi.Name), true);
                
            //    //刪除已處理資料
            //    fi.Delete();
            //}
        }

        private void UploadFTPXmlResult()
        {
            List<string> lstFile = new List<string>();

            ShowMessageToFront("[]FTP-上傳XML檔案==啟動");
            //上傳XmlResult
            FtpClient client = new FtpClient();
            try
            {
                client.Host = M11Const.FTP_IP;
                client.SocketKeepAlive = true;
                client.Credentials = new NetworkCredential(M11Const.FTP_User, M11Const.FTP_Password);
                client.Connect();

                // 取得資料夾內所有檔案
                int iIndex = 1;
                foreach (string fname in Directory.GetFiles(M11Const.Path_FTPQueueXmlResult))
                {
                    try
                    {
                        lstFile.Add(fname);
                        //FTP上傳路徑規劃
                        ///M11_System/Data/CgiData/2021/03/21

                        //XmlResult檔名範例(202103171410_10min_a_ds_data.xml)
                        FileInfo fi = new FileInfo(fname);
                        string[] XmlResultSplit = fi.Name.Replace(fi.Extension, "").Split('_');

                        //避免舊檔案格式問題，排除沒有分析完整的檔案名稱
                        //if (XmlResultSplit.Length > 0) continue;

                        //從檔案取得資料時間
                        DateTime dt = DateTime.ParseExact(XmlResultSplit[0], "yyyyMMddHHmm", System.Globalization.CultureInfo.CurrentCulture);

                        //FluentFTP 起始路徑都是跟目錄開始，目錄結尾都是/
                        string sLocalPath = fi.FullName;
                        string sRemotePath = "/M11_System/Data/XmlResult/";
                        sRemotePath = string.Format(@"{0}{1}/{2}/{3}/{4}"
                                , sRemotePath, dt.ToString("yyyy"), dt.ToString("MM"), dt.ToString("dd"), fi.Name);

                        //設定嘗試次數
                        client.RetryAttempts = 3;
                        //上傳檔案
                        client.UploadFile(sLocalPath, sRemotePath, FtpRemoteExists.Overwrite, true, FtpVerify.Retry);

                        ShowMessageToFront(string.Format("[{0}/{1}]上傳XML檔案 成功=={2}", iIndex.ToString(), Directory.GetFiles(M11Const.Path_FTPQueueXmlResult).Length, fname));
                        iIndex++;

                        //存至備份資料夾
                        fi.CopyTo(Path.Combine(M11Const.Path_FTPQueueXmlResultBak, fi.Name), true);
                        //刪除已處理資料
                        fi.Delete();
                    }
                    catch (Exception ex)
                    {
                        //有錯誤持續執行
                        continue;
                        //ShowMessageToFront(ex.ToString());
                    }
                }
            }
            finally
            {
                client.Disconnect();
                client.Dispose();
            }
            

            ////全部處理完畢再一次刪除
            //foreach (string fname in lstFile)
            //{
            //    FileInfo fi = new FileInfo(fname);
                
            //    //存至備份資料夾
            //    fi.CopyTo(Path.Combine(M11Const.Path_FTPQueueXmlResultBak, fi.Name), true);
            //    //刪除已處理資料
            //    fi.Delete();
            //}
        }

        /// <summary>
        /// 上傳FTP序列中的檔案
        /// </summary>
        private void ProcUploadFTP()
        {
            UploadFTPCgi();

            UploadFTPXmlResult();
        }


        private string getSensor_Type(string SensorName) 
        {
            string sResult = "";

            switch (SensorName)
            {
                case "RG":
                    sResult = "RainGauge";
                    break;
                case "TM":
                case "TM1":
                case "TM2":
                    sResult = "BiTiltMeter";
                    break;
                case "GW":
                    sResult = "ObservationWell";
                    break;
                case "PM":
                    sResult = "PiezoMeter";
                    break;
                //case "RG":
                //    sResult = "RainGauge";
                //    break;
                //case "RG":
                //    sResult = "RainGauge";
                //    break;
                default:
                    sResult = "";
                    break;
            }

            return sResult;
        }


        private string getObservation_num(string SensorName)
        {
            string sResult = "";

            switch (SensorName)
            {
                case "RG":
                    sResult = "8";
                    break;
                case "TM":
                case "TM1":
                case "TM2":
                    sResult = "6";
                    break;
                case "GW":
                    sResult = "2";
                    break;
                case "PM":
                    sResult = "2";
                    break;
                //case "RG":
                //    sResult = "RainGauge";
                //    break;
                //case "RG":
                //    sResult = "RainGauge";
                //    break;
                default:
                    sResult = "";
                    break;
            }

            return sResult;
        }

        /// <summary>
        /// 取得Station設定檔資料
        /// </summary>
        /// <returns></returns>
        private DataTable GetStationSetData() 
        {
            DataTable dt = new DataTable();
            dt.ReadXmlSchema(M11Const.FilePath_SchemaSetSatation);
            dt.ReadXml(M11Const.FilePath_SetSatation);

            return dt;
        }

        /// <summary>
        /// 取得Sensor設定檔資料
        /// </summary>
        /// <returns></returns>
        private DataTable GetSensorSetData()
        {
            DataTable dt = new DataTable();
            dt.ReadXmlSchema(M11Const.FilePath_SchemaSetSensor);
            dt.ReadXml(M11Const.FilePath_SetSensor);

            return dt;
        }

        /// <summary>
        /// 取得Station資料
        /// </summary>
        /// <returns></returns>
        private DataTable GetStationData(string StationName)
        {
            //判斷Station資料如果不存在，則返回null
            if (File.Exists(Path.Combine(M11Const.Path_DBSimulation, string.Format("{0}.xml", StationName))) == false) 
                return null;

            DataTable dt = new DataTable();
            
            dt.ReadXmlSchema(M11Const.FilePath_SchemaCGIData);
            dt.ReadXml(Path.Combine(M11Const.Path_DBSimulation, string.Format("{0}.xml", StationName)));

            return dt;
        }


        /// <summary>
        /// 初始化StationXML資料
        /// </summary>
        /// <param name="StationName"></param>
        private void InitStationXML() 
        {
            try
            {
                foreach (DataRow dr in DtStationData.Rows)
                {
                    string StationFileName = string.Format("{0}.xml", dr["Station"].ToString());
                    //判斷檔案是否已經存在，不存在才產生
                    if (System.IO.File.Exists(Path.Combine(M11Const.Path_DBSimulation, StationFileName)) == true) continue;

                    DataTable dt = new DataTable();
                    dt.ReadXmlSchema(Path.Combine(M11Const.Path_DBSimulation, "Schema_CGIData.xml"));
                    dt.WriteXml(Path.Combine(M11Const.Path_DBSimulation, StationFileName));
                }

                //產生外部站號的XML檔案
                //string[] OutSourceStations = { "DS009_02", "DS009_03", "DS009_05", "DS011_02" };
                //foreach (string item in OutSourceStations)
                //{
                //    string StationFileName = item;
                //    //判斷檔案是否已經存在，不存在才產生
                //    if (System.IO.File.Exists(Path.Combine(M11Const.Path_DBSimulation, StationFileName)) == true) continue;

                //    DataTable dt = new DataTable();
                //    dt.ReadXmlSchema(Path.Combine(M11Const.Path_DBSimulation, "Schema_CGIData.xml"));
                //    dt.WriteXml(Path.Combine(M11Const.Path_DBSimulation, StationFileName));
                //}
            }
            catch (Exception ex)
            {

                
            }
        }


        /// <summary>
        /// 讀取原始資料轉檔到XML資料庫與DB資料庫
        /// </summary>
        private void ReadDataToXMLDB()
        {
            try
            {
                //現在的時間預先補上個時段的資料到下一個時段的資料
                ProcPreGetNextDataFromPreData();

                //==========================================================================================================================

                int iIndex = 1;

                string[] aFiles = Directory.GetFiles(M11Const.Path_Original);
                // 取得資料夾內所有檔案
                foreach (string fname in aFiles)
                {
                    ShowMessageToFront(string.Format("[{0}/{1}]讀取原始資料轉檔到XML資料庫=={2}", iIndex.ToString(), aFiles.Length, fname));
                    //轉檔到XML
                    //TransToXML(fname);

                    //轉檔Cgi資料到資料庫中
                    TransCgiDataToDB(fname);


                    //移除超過四天資料
                    //DateTime dtTemp = DateTime.Now.AddDays(-4);


                    iIndex++;
                }

                //全部處理完畢再一次刪除
                foreach (string fname in Directory.GetFiles(M11Const.Path_Original))
                {
                    FileInfo fi = new FileInfo(fname);

                    //刪除已處理資料
                    fi.Delete();
                }
            }
            catch (Exception)
            {

                
            }
        }

        /// <summary>
        /// 讀取委外資料轉檔到XML資料庫
        /// </summary>
        private void ReadOutDataToXMLDB()
        {
            try
            {
                int iIndex = 1;

                //對應資料
                string[] sDS009_02 = {"21040603", "21040604"};
                string[] sDS009_03 = {"21040605", "21040606"};
                string[] sDS009_05 = {"21040601", "21040602"};
                string[] sDS011_02 = {"21040607", "21040608"};
                Dictionary<string, string[]> OutDataMapping = new Dictionary<string, string[]>();
                OutDataMapping.Add("DS009_02", sDS009_02);
                OutDataMapping.Add("DS009_03", sDS009_03);
                OutDataMapping.Add("DS009_05", sDS009_05);
                OutDataMapping.Add("DS011_02", sDS011_02);
                
                foreach (var items in OutDataMapping)
                {

                    string StationName = items.Key;

                    //解析檔案中的資料
                    Dictionary<string, string> di = new Dictionary<string, string>();
                    foreach (string MapItem in items.Value)
                    {
                        //到資料庫Procal資料
                        ssql = @"
                            select * from StationReal where StationID = '{0}'
                        ";
                        ssql = string.Format(ssql, MapItem);
                        
                        List<StationReal> lstStationReal = dbDapperProcal.Query<StationReal>(ssql);

                        foreach (StationReal sr in lstStationReal)
                        {
                            if (sr.Title == "水位")
                            {
                                di.Add("water", sr.RealVale.ToString());
                            }

                            if (sr.Title == "傾斜X")
                            {
                                di.Add("SX", sr.RealVale.ToString());
                            }

                            if (sr.Title == "傾斜Y")
                            {
                                di.Add("SY", sr.RealVale.ToString());
                            }
                        }
                    }


                    //取得文字檔資料
                    DataTable dtStationData = GetStationData(StationName);

                    //判斷站名是否已經存在設定檔，如果資料不存在則返回
                    if (dtStationData == null) return;

                    //以現在的時間存入
                    DateTime dt = DateTime.Now;

                    //todo 判斷重複資料
                    if (dtStationData.Select(" datetime = '" + dt.ToString("yyyyMMddHHmmss") + "' ").Length > 0) return;

                    DataRow dr = dtStationData.NewRow();
                    dr["datetime"] = dt.ToString("yyyyMMddHHmmss");

                    //文字檔資料塞入資料表
                    foreach (string key in di.Keys)
                    {
                        if (dr.Table.Columns.Contains(key) == true)
                        {
                            dr[key] = di[key] == "None" ? "" : di[key];
                        }
                    }

                    dtStationData.Rows.Add(dr);

                    DateTime dtTemp = DateTime.Now.AddDays(-4);

                    //移除超過四天資料
                    DataRow[] rows = dtStationData.Select("datetime < '" + dtTemp.ToString("yyyyMMddHHmmss") + "' ");
                    for (int i = 0; i < rows.Length; i++)
                    {
                        rows[i].Delete();
                    }

                    //寫入
                    dtStationData.WriteXml(Path.Combine(M11Const.Path_DBSimulation, string.Format("{0}.xml", StationName)));
                }              
            }
            catch (Exception)
            {


            }
        }

        /// <summary>
        /// 讀取委外資料轉檔到資料庫
        /// </summary>
        private void ReadOutDataToDB(DateTime dtCheck)
        {
            try
            {
                //取得下一個時段的時間
                dtCheck = TransDatetimeToNextFullDatetime(dtCheck);
                string sDatetimeString = M11DatetimeToString(dtCheck);

                //取得下一個時段資料庫資料
                ssql = @"
                    select * from CgiStationData where DatetimeString = '{0}'                                                            
                    ";
                ssql = string.Format(ssql, sDatetimeString);
                List<CgiStationData> lstCgiData = dbDapper.Query<CgiStationData>(ssql);

                //取得Procal資料StationReal即時資料表(不判斷時間，如果盛邦未更新也繼續抓即時資料)
                ssql = @"
                            select * from StationReal 
                        ";
                List<StationReal> lstStationReal = dbDapperProcal.Query<StationReal>(ssql);


                //取得Station與Procal的StationID設定檔
                ssql = @"
                            select * from BasM11Setting where DataType = 'StMapProcal'
                        ";
                List<BasM11Setting> lstStationMap = dbDapper.Query<BasM11Setting>(ssql);
                
                foreach (BasM11Setting StationMap in lstStationMap)
                {
                    string sCgi = "procal";
                    string StationName = StationMap.DataItem;
                    //取盛邦的站號來存
                    string sID = StationMap.DataValue;

                    List<StationReal> lstStationRealTmps =
                            lstStationReal.Where(c => c.StationID == sID).ToList();

                    foreach (StationReal sr in lstStationRealTmps)
                    {
                        string sDataType = "";
                        if (sr.Title == "水位") sDataType = "WATER";
                        if (sr.Title == "傾斜X") sDataType = "SX";
                        if (sr.Title == "傾斜Y") sDataType = "SY";

                        //如果資料類別沒對應上，則繼續
                        if (sDataType == "") continue;

                        //判斷資料是否存在
                        List<CgiStationData> tmp
                            = lstCgiData.Where(c => c.DataType == sDataType.ToUpper()
                                && c.Station == StationName).ToList();

                        ////寫入資料庫
                        if (tmp.Count >= 1)
                        {
                            //更新資料
                            CgiStationData tmpData = tmp[0];
                            tmpData.Value = sr.RealVale.ToString();
                            tmpData.Status = "Y";
                            dbDapper.Update<CgiStationData>(tmpData);
                        }
                        else
                        {
                            //新增資料
                            CgiStationData tmpData = new CgiStationData();
                            tmpData.DatetimeString = sDatetimeString;
                            tmpData.ID = sID;
                            tmpData.Cgi = sCgi;
                            tmpData.Station = StationName;
                            tmpData.DataType = sDataType.ToUpper();
                            tmpData.Value = sr.RealVale.ToString();
                            tmpData.Status = "Y";
                            dbDapper.Insert<CgiStationData>(tmpData);
                        }
                    }


                }
            }
            catch (Exception)
            {


            }
        }

        private Dictionary<string, string> TransCGIData(string FileName) 
        {
            Dictionary<string, string> di = new Dictionary<string, string>();

            string line;

            using (StreamReader file = new StreamReader(FileName)) 
            {
                while ((line = file.ReadLine()) != null)
                {
                    string[] sLine = line.Split('=');
                    if (sLine.Count() == 2)
                    {
                        di.Add(sLine[0], sLine[1]);
                    }
                }
            }            

            return di;
        }

        /// <summary>
        /// 資料轉檔到XML資料庫
        /// </summary>
        /// <param name="fname"></param>
        private void TransToXML(string fname)
        {
            //分析時間
            FileInfo fi = new FileInfo(fname);            
            string[] CgiNameSplit = fi.Name.Replace(fi.Extension, "").Split('-');

            //避免舊檔案格式問題，排除沒有分析完整的檔案名稱
            if (CgiNameSplit.Length != 8) return;

            string StationName = CgiNameSplit[1];
            DateTime dt = DateTime.ParseExact(CgiNameSplit[2] + CgiNameSplit[3] + CgiNameSplit[4] + CgiNameSplit[5] + CgiNameSplit[6] + CgiNameSplit[7], "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture);

            //解析檔案中的資料
            Dictionary<string, string> di = TransCGIData(fi.FullName);

            //取得文字檔資料
            DataTable dtStationData = GetStationData(StationName);

            //判斷站名是否已經存在設定檔，如果資料不存在則返回
            if (dtStationData == null) return;

            //todo 判斷重複資料
            if (dtStationData.Select(" datetime = '" + dt.ToString("yyyyMMddHHmmss") + "' ").Length > 0) return;

            DataRow dr = dtStationData.NewRow();
            dr["datetime"] = dt.ToString("yyyyMMddHHmmss");

            //文字檔資料塞入資料表
            foreach (string key in di.Keys)
            {
                if (dr.Table.Columns.Contains(key) == true)
                {
                    dr[key] = di[key] == "None" ? "" : di[key];
                }
            }

            dtStationData.Rows.Add(dr);

            DateTime dtTemp = DateTime.Now.AddDays(-4);

            //移除超過四天資料
            DataRow[] rows = dtStationData.Select("datetime < '" + dtTemp.ToString("yyyyMMddHHmmss") + "' ");
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i].Delete();
            }


            //寫入
            dtStationData.WriteXml(Path.Combine(M11Const.Path_DBSimulation, string.Format("{0}.xml", StationName)));

        }

        /// <summary>
        /// 轉檔Cgi資料到資料庫中
        /// </summary>
        /// <param name="fname"></param>
        private void TransCgiDataToDB(string fname)
        {
            //分析時間
            FileInfo fi = new FileInfo(fname);
            string[] CgiNameSplit = fi.Name.Replace(fi.Extension, "").Split('-');

            //避免舊檔案格式問題，排除沒有分析完整的檔案名稱
            if (CgiNameSplit.Length != 8) return;

            string StationName = CgiNameSplit[1];
            DateTime dt = DateTime.ParseExact(CgiNameSplit[2] + CgiNameSplit[3] + CgiNameSplit[4] + CgiNameSplit[5] + CgiNameSplit[6] + CgiNameSplit[7], "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture);

            //取得下一個時段的時間
            dt = TransDatetimeToNextFullDatetime(dt);
            string sDatetimeString = M11DatetimeToString(dt);
            //取得下一個時段資料庫資料
            ssql = @"
                    select * from CgiStationData where Station = '{0}' and DatetimeString = '{1}'                                                            
                    ";
            ssql = string.Format(ssql, StationName, sDatetimeString);
            List<CgiStationData> lstData = dbDapper.Query<CgiStationData>(ssql);

            //解析CGI檔案中的資料
            Dictionary<string, string> di = TransCGIData(fi.FullName);

            //取得基本資料
            string sCgi = "";
            string sID = "";
            string sStation = "";
            foreach (string key in di.Keys)
            {
                if (key.ToUpper() == "CGI") sCgi = di[key];
                if (key.ToUpper() == "ID")  sID = di[key];
                if (key.ToUpper() == "NAME") sStation = di[key];
            }


            //文字檔資料塞入資料表
            foreach (string key in di.Keys)
            {
                //排除主要資料
                if (key.ToUpper() == "CGI" || key.ToUpper() == "ID" || key.ToUpper() == "NAME")
                {
                    continue;
                }

                string sValue = di[key] == "None" ? "" : di[key];

                //RAIN雨量特殊處理，跨天記數歸0
                //判斷00:00:00
                if (dt.Hour == 0 && dt.Minute == 0 && key.ToUpper() == "RAIN")
                {
                    sValue = "0";
                }

                //判斷資料是否存在
                List<CgiStationData> tmp = lstData.Where(c => c.DataType == key.ToUpper()).ToList();

                //寫入資料庫
                if (tmp.Count >= 1)
                {
                    //更新資料
                    CgiStationData tmpData = tmp[0];
                    tmpData.Value = sValue;
                    tmpData.Status = "Y";
                    dbDapper.Update<CgiStationData>(tmpData);
                }
                else
                {
                    //新增資料
                    CgiStationData tmpData = new CgiStationData();
                    tmpData.DatetimeString = sDatetimeString;
                    tmpData.ID = sID;
                    tmpData.Cgi = sCgi;
                    tmpData.Station = sStation;
                    tmpData.DataType = key.ToUpper();
                    tmpData.Value = sValue;
                    tmpData.Status = "Y";
                    dbDapper.Insert<CgiStationData>(tmpData);
                }
            }
        }

        /// <summary>
        /// 現在的時間預先補上個時段的資料到下一個時段的資料
        /// </summary>
        /// <param name="fname"></param>
        private void ProcPreGetNextDataFromPreData()
        {
            //取得現在時間
            DateTime dt = DateTime.Now;

            //取得下一個時段的時間
            dt = TransDatetimeToNextFullDatetime(dt);

            //下個時段時間字串
            string sNextDatetimeString = M11DatetimeToString(dt);
            //上個時段時間字串
            string sPreDatetimeString = M11DatetimeToString(dt.AddMinutes(-10));


            //取得上個時段資料庫資料
            ssql = @"
                    select * from CgiStationData where DatetimeString = '{0}'                                                            
                    ";
            ssql = string.Format(ssql, sPreDatetimeString);
            List<CgiStationData> lstPreData = dbDapper.Query<CgiStationData>(ssql);

            //取得下個時段資料庫資料
            ssql = @"
                    select * from CgiStationData where DatetimeString = '{0}'                                                            
                    ";
            ssql = string.Format(ssql, sNextDatetimeString);
            List<CgiStationData> lstNextData = dbDapper.Query<CgiStationData>(ssql);

            foreach (CgiStationData item in lstPreData)
            {
                //判斷下個時段資料是否存在
                if (lstNextData.Where(c => c.Station == item.Station && c.DataType == item.DataType).Count() == 0)
                {
                    //資料不存在，則新增一筆資料
                    item.DatetimeString = sNextDatetimeString;
                    item.Status = "N"; //預設先註記非即時資料

                    dbDapper.Insert<CgiStationData>(item);
                }
            }
        }

        /// <summary>
        /// M11專案Datetime轉文字時間格式(yyyy-MM-dd HH:mm:ss)
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        private string M11DatetimeToString(DateTime dt)
        {
            string sResult = "";
            sResult = dt.ToString("yyyy-MM-dd HH:mm:ss");
            return sResult;
        }
        
        /// <summary>
        /// 將時間格式轉換為下一個整點時段格式[DateTime(2021, 4, 13, 17, 30, 8) => DateTime(2021, 4, 13, 17, 40, 00)]
        /// </summary>
        /// <param name="dtCheck"></param>
        /// <returns></returns>
        private DateTime TransDatetimeToNextFullDatetime(DateTime dtCheck)
        {
            //DateTime dtCheck = new DateTime(2021, 4, 13, 17, 30, 8);
            string sDatetime = dtCheck.AddMinutes(10).ToString("yyyyMMddHHmmss");
            //只取分鐘的十分位
            string sMinFirst = sDatetime.Substring(10, 1);
            sDatetime = dtCheck.AddMinutes(10).ToString("yyyyMMddHH") + sMinFirst + "000";

            dtCheck = Utils.getStringToDateTime(sDatetime);

            return dtCheck;
        }

        
    }


    public class GwRainData 
    {
        public string DatetimeString = "";
        public DateTime dtDatetime ;
        public double dRAIN = 0;
        //累積量(與前一個時段相比)
        public double dAccumulation = 0;
    }


    public class CgiConst 
    {
        public const string DATETIME = "datetime";
        public const string CGI = "cgi";
        public const string ID = "ID";
        public const string NAME = "name";
        public const string RAIN = "RAIN";
        public const string WATER = "water";
        public const string SX = "SX";
        public const string SY = "SY";
        public const string S2X = "S2X";
        public const string S2Y = "S2Y";
        public const string TEST22 = "test22";
    }
}
