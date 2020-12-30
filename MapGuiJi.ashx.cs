using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace H5Video
{
    /// <summary>
    /// MapGuiJi 的摘要说明
    /// </summary>
    public class MapGuiJi : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "json";
            string PlateNumber = context.Request["PlateNumber"];
            string txb_starttime = context.Request["txb_starttime"];
            string tbx_endtime = context.Request["tbx_endtime"];
            string funID = context.Request["funID"];

            //连接字符串
            string connStr = "Server=localhost;Database=xdwtest;Uid=root;Pwd=123456;CharSet=utf8;";
            DataTable xdw;
            xdw = MYSQLSelect("SELECT longitude,latitude FROM 809vecdata WHERE platenumber='"+ PlateNumber+"' AND DataTime BETWEEN '"+ txb_starttime + "' and '"+ tbx_endtime + "'  ORDER BY DataTime", connStr);
            if (xdw.Rows.Count > 0)
            {
                for (int i = 0; i < xdw.Rows.Count; i++)
                {
                    double[] xdw11 = wgs84togcj02(Convert.ToDouble(xdw.Rows[i][0]), Convert.ToDouble(xdw.Rows[i][1]));
                    xdw.Rows[i][0] = xdw11[0].ToString("F6");
                    xdw.Rows[i][1] = xdw11[1].ToString("F6");                 
                }
            }
            
            if(funID=="2")
            {
                xdw = MYSQLSelect("SELECT DataTime,OverspeedAlarm,EmergencyAlarm,speed,mileage FROM 809vecdata WHERE OverspeedAlarm='1' AND platenumber='" + PlateNumber + "' AND DataTime BETWEEN '" + txb_starttime + "' and '" + tbx_endtime + "'  ORDER BY DataTime DESC", connStr);
                if (xdw.Rows.Count > 0)
                {
                    for (int i = 0; i < xdw.Rows.Count; i++)
                    {
                        if(xdw.Rows[i][1].ToString()=="1")
                        {
                            xdw.Rows[i][1] = "是";
                        }
                        else
                        {
                            xdw.Rows[i][1] = "否";
                        }

                        if (xdw.Rows[i][2].ToString() == "1")
                        {
                            xdw.Rows[i][2] = "是";
                        }
                        else
                        {
                            xdw.Rows[i][2] = "否";
                        }

                    }
                }
            }
            string jsonTable = "student";
            string strRe = TableToJson(xdw, jsonTable);
            context.Response.Write(strRe);
        }

        public string TableToJson(DataTable dt, string jsonName)
        {
            var jsonBuilder = new StringBuilder();
            jsonBuilder.Append("{\"");
            //jsonBuilder.Append(dt.TableName.ToString());
            jsonBuilder.Append(jsonName + "\":");
            jsonBuilder.Append("[");
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                jsonBuilder.Append("{");
                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    jsonBuilder.Append("\"");
                    jsonBuilder.Append(dt.Columns[j].ColumnName);
                    jsonBuilder.Append("\":\"");
                    jsonBuilder.Append(dt.Rows[i][j].ToString());
                    jsonBuilder.Append("\",");
                }
                jsonBuilder.Remove(jsonBuilder.Length - 1, 1);
                jsonBuilder.Append("},");
            }
            if (dt.Rows.Count > 0)//表示有数据,则需要删除最后的一个 逗号','
                jsonBuilder.Remove(jsonBuilder.Length - 1, 1);
            jsonBuilder.Append("]");
            jsonBuilder.Append("}");
            return jsonBuilder.ToString();
        }

        public DataTable SQLExeQuery(string sql, string conString)
        {
            DataTable dt = new DataTable();
            try
            {
                using (OdbcConnection con = new OdbcConnection(conString))
                {
                    con.Open();
                    using (OdbcCommand cmd = new OdbcCommand(sql, con))
                    {
                        using (OdbcDataAdapter da = new OdbcDataAdapter(cmd))
                        {
                            da.Fill(dt);
                            da.Dispose();
                            cmd.Dispose();
                            con.Dispose();
                            return dt;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                string llm = e.ToString();
                return dt;
            }

        }

        public DataTable MYSQLSelect(string sql, string conString)
        {
            DataTable dt = new DataTable();
            try
            {
                MySqlConnection con = new MySqlConnection(conString);
                con.Open();//打开连接
                MySqlDataAdapter mda = new MySqlDataAdapter(sql, con);
                mda.Fill(dt);
                mda.Dispose();
                con.Close();
                return dt;
            }
            catch (Exception e)
            {
                string llm = e.ToString();
                return dt;
            }

        }

        private string GetData(string url)
        {
            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);
            myRequest.Method = "GET";
            HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse();
            StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8);
            string content = reader.ReadToEnd().Trim();
            reader.Close();
            return content;
        }

        public void AnHuanJuDataPOST(string Datatext)
        {
            try
            {
                //连接字符串
                //string connStr = "Server=localhost;Database=xdwtest;Uid=root;Pwd=123456;CharSet=utf8;";
                //string wsx = PostData("http://58.210.128.10:8888/api/webappcorp/post", Datatext);
                string wsx = PostData("http://112.35.1.155:1992/sms/norsubmit", Datatext);
                JArray jArray = (JArray)JsonConvert.DeserializeObject(wsx);//jsonArrayText必须是带[]数组格式字符串
                string json = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "AnHuanJuData.json");
                dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                jsonObj["total"] = jArray.Count();
                jsonObj["rows"] = jArray;
                string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "AnHuanJuData.json", output);
            }
            catch (Exception e)
            {
                string llm = e.ToString();
            }

        }

        private string PostData(string url, string postData)
        {
            //ASCIIEncoding encoding = new ASCIIEncoding();
            //byte[] data = encoding.GetBytes(postData);
            byte[] data = Encoding.UTF8.GetBytes(postData);
            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);

            myRequest.Method = "POST";
            myRequest.ContentType = "application/x-www-form-urlencoded";
            myRequest.ContentLength = data.Length;
            Stream newStream = myRequest.GetRequestStream();

            newStream.Write(data, 0, data.Length);
            newStream.Close();

            HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse();
            //StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.Default);
            StreamReader reader = new StreamReader(myResponse.GetResponseStream());
            string content = reader.ReadToEnd().Trim();
            reader.Close();
            return content;
        }

        public String encryptToMD5(String password)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            //string t2 = BitConverter.ToString(md5.ComputeHash(Encoding.Default.GetBytes(password)));
            //t2 = t2.Replace("-", "");
            //return t2.ToLower();
            string pwd = "";
            byte[] s = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
            // 通过使用循环，将字节类型的数组转换为字符串，此字符串是常规字符格式化所得
            for (int i = 0; i < s.Length; i++)
            {
                // 将得到的字符串使用十六进制类型格式。格式后的字符是小写的字母，如果使用大写（X）则格式后的字符是大写字符 
                pwd = pwd + s[i].ToString("x");

            }
            return pwd;
        }

        public String encryptToMD522(String password)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] message;
            message = Encoding.Default.GetBytes(password);
            md5.Initialize();
            md5.TransformFinalBlock(message, 0, message.Length);
            //Console.WriteLine(Convert.ToBase64String(md5.Hash));
            return Convert.ToBase64String(md5.Hash);
        }

        static double x_pi = 3.14159265358979324 * 3000.0 / 180.0;
        // π
        static double pi = 3.1415926535897932384626;
        // 长半轴
        static double a = 6378245.0;
        // 扁率
        static double ee = 0.00669342162296594323;
        /**
	 * WGS84转GCJ02(火星坐标系)
	 * 
	 * @param lng
	 * WGS84坐标系的经度
	 * @param lat
	 * WGS84坐标系的纬度
	 * @return 火星坐标数组
	 */
        public static double[] wgs84togcj02(double lng, double lat)
        {
            if (out_of_china(lng, lat))
            {
                return new double[] { lng, lat };
            }
            double dlat = transformlat(lng - 105.0, lat - 35.0);
            double dlng = transformlng(lng - 105.0, lat - 35.0);
            double radlat = lat / 180.0 * pi;
            double magic = System.Math.Sin(radlat);
            magic = 1 - ee * magic * magic;
            double sqrtmagic = System.Math.Sqrt(magic);
            dlat = (dlat * 180.0) / ((a * (1 - ee)) / (magic * sqrtmagic) * pi);
            dlng = (dlng * 180.0) / (a / sqrtmagic * System.Math.Cos(radlat) * pi);
            double mglat = lat + dlat;
            double mglng = lng + dlng;
            return new double[] { mglng, mglat };
        }
        /**
	 * 纬度转换
	 * 
	 * @param lng
	 * @param lat
	 * @return
	 */
        public static double transformlat(double lng, double lat)
        {
            double ret = -100.0 + 2.0 * lng + 3.0 * lat + 0.2 * lat * lat + 0.1 * lng * lat
                    + 0.2 * System.Math.Sqrt(System.Math.Abs(lng));
            ret += (20.0 * System.Math.Sin(6.0 * lng * pi) + 20.0 * System.Math.Sin(2.0 * lng * pi)) * 2.0 / 3.0;
            ret += (20.0 * System.Math.Sin(lat * pi) + 40.0 * System.Math.Sin(lat / 3.0 * pi)) * 2.0 / 3.0;
            ret += (160.0 * System.Math.Sin(lat / 12.0 * pi) + 320 * System.Math.Sin(lat * pi / 30.0)) * 2.0 / 3.0;
            return ret;
        }

        /**
         * 经度转换
         * 
         * @param lng
         * @param lat
         * @return
         */
        public static double transformlng(double lng, double lat)
        {
            double ret = 300.0 + lng + 2.0 * lat + 0.1 * lng * lng + 0.1 * lng * lat + 0.1 * System.Math.Sqrt(System.Math.Abs(lng));
            ret += (20.0 * System.Math.Sin(6.0 * lng * pi) + 20.0 * System.Math.Sin(2.0 * lng * pi)) * 2.0 / 3.0;
            ret += (20.0 * System.Math.Sin(lng * pi) + 40.0 * System.Math.Sin(lng / 3.0 * pi)) * 2.0 / 3.0;
            ret += (150.0 * System.Math.Sin(lng / 12.0 * pi) + 300.0 * System.Math.Sin(lng / 30.0 * pi)) * 2.0 / 3.0;
            return ret;
        }

        /**
         * 判断是否在国内，不在国内不做偏移
         * 
         * @param lng
         * @param lat
         * @return
         */
        public static bool out_of_china(double lng, double lat)
        {
            if (lng < 72.004 || lng > 137.8347)
            {
                return true;
            }
            else if (lat < 0.8293 || lat > 55.8271)
            {
                return true;
            }
            return false;
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}