using System;
using System.Data;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Xml;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Serialization;


namespace NetEZ.Utility.Tools
{
    public class Converter
    {
        public static DateTime _ZeroTime = new DateTime(1970, 1, 1);
        public static byte[] GetBytes(int[] intArray)
        {
            byte[] result = null;
            if (intArray == null || intArray.Length < 1)
                return result;

            int intSize = 4, pos = 0;
            result = new byte[intArray.Length * intSize];
            byte[] intBuf = new byte[intSize];

            for (int i = 0; i < intArray.Length; i++)
            {
                intBuf = BitConverter.GetBytes(intArray[i]);
                Array.Reverse(intBuf);
                Array.Copy(intBuf, 0, result, pos, intSize);
                pos += intSize;
            }

            return result;
        }

        /// <summary>
        /// byte数组转int数组
        /// </summary>
        /// <param name="byteArray"></param>
        /// <returns></returns>
        public static int[] GetIntArr(byte[] byteArray)
        {
            int[] result = null;
            if (byteArray == null || byteArray.Length < 1 || byteArray.Length % 4 != 0)
                return result;

            int intSize = 4;
            result = new int[byteArray.Length % intSize];
            List<int> list = new List<int>();

            for (int i = 0; i < byteArray.Length / intSize; i++)
            {
                byte[] byteBuf = new byte[intSize];
                Array.Copy(byteArray, i * 4, byteBuf, 0, intSize);
                Array.Reverse(byteBuf);
                list.Add(BitConverter.ToInt32(byteBuf, 0));
            }

            return list.ToArray();
        }

        /// <summary>
        /// 将数据行转化为指定对象类型的实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dr"></param>
        /// <returns></returns>
        public static T FillModel<T>(DataRow dr)
        {
            bool isOk = true;
            return FillModel<T>(dr, out isOk);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dr"></param>
        /// <param name="isOk"></param>
        /// <returns></returns>
        public static T FillModel<T>(DataRow dr, out bool isOk)
        {
            isOk = true;
            if (dr == null)
            {
                return default(T);
            }

            #region 如果是基元类型或值类型,直接返回
            if (typeof(T).IsPrimitive || typeof(T).IsValueType)
                return (T)dr[0];
            T model = (T)Activator.CreateInstance(typeof(T));
            #endregion

            try
            {
                for (int i = 0; i < dr.Table.Columns.Count; i++)
                {
                    try
                    {
                        PropertyInfo propertyInfo = model.GetType().GetProperty(dr.Table.Columns[i].ColumnName, BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.GetProperty);
                        if (propertyInfo != null && dr[i] != DBNull.Value)
                            propertyInfo.SetValue(model, dr[i], null);
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine(ex.StackTrace);
                    }
                }
            }
            catch
            {
                isOk = false;
            }

            return model;
        }

        public static void FillModel<T>(T model, DataRow dr)
        {
            // 如果T是基元类型，或者dr是null，这两种情况不能用该方法做，请用有返回值的FillModel
            if (dr == null || model.GetType().IsPrimitive)
            {
                return;
            }

            try
            {
                for (int i = 0; i < dr.Table.Columns.Count; i++)
                {
                    try
                    {
                        PropertyInfo propertyInfo = model.GetType().GetProperty(dr.Table.Columns[i].ColumnName,
                            BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance
                            | BindingFlags.GetProperty);
                        if (propertyInfo != null && dr[i] != DBNull.Value)
                            propertyInfo.SetValue(model, dr[i], null);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

        }

        /// <summary>
        /// 将数据集转化为指定对象类型的List实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static List<T> FillModelList<T>(DataTable dt)
        {
            bool isOk = true;
            return FillModelList<T>(dt, out isOk);
        }

        /// <summary>
        /// 将数据集转化为指定对象类型的List实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dt"></param>
        /// <param name="isOk"></param>
        /// <returns></returns>
        public static List<T> FillModelList<T>(DataTable dt, out bool isOk)
        {
            isOk = true;
            List<T> modelList = null;

            if (dt == null || dt.Rows.Count < 0)
                return modelList;

            try
            {
                if (dt.Rows.Count < 64)
                    modelList = new List<T>();
                else
                    modelList = new List<T>(dt.Rows.Count);

                bool tmp = true;
                foreach (DataRow dr in dt.Rows)
                {
                    T model = FillModel<T>(dr, out isOk);
                    if (!isOk)//如果循环过程出错记下来
                        tmp = isOk;

                    if (model != null)
                        modelList.Add(model);
                }
                isOk = tmp;
            }
            catch
            {
                isOk = false;
            }
            finally { }

            return modelList;
        }

        /// <summary>
        /// 将数据集转化为指定对象类型的数组实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static T[] FillModelArray<T>(DataTable dt)
        {
            bool isOk = true;
            return FillModelArray<T>(dt, out isOk);
        }

        /// <summary>
        /// 将数据集转化为指定对象类型的数组实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dt"></param>
        /// <param name="isOk"></param>
        /// <returns></returns>
        public static T[] FillModelArray<T>(DataTable dt, out bool isOk)
        {
            isOk = true;
            T[] result = null;
            List<T> modelList = FillModelList<T>(dt, out isOk);

            if (modelList != null && modelList.Count > 0)
            {
                result = new T[modelList.Count];
                modelList.CopyTo(result);
            }

            return result;
        }

        public static int Get1970TimeStamp(DateTime time, bool localTime = true)
        {
            if (localTime)
                return (int)time.Subtract(_ZeroTime).TotalSeconds;
            else
                return (int)time.ToUniversalTime().Subtract(_ZeroTime).TotalSeconds;
        }

        public static long Get1970TimeMillSecondStamp(DateTime time,bool localTime = true)
        {
            if (localTime)
                return (long)time.Subtract(_ZeroTime).TotalMilliseconds;
            else
                return (long)time.ToUniversalTime().Subtract(_ZeroTime).TotalMilliseconds;
        }

        public static DateTime GetDateTimeFrom1970TimeStamp(int ts)
        {
            return _ZeroTime.AddSeconds(ts);
        }

        public static DateTime GetDateTimeFrom1970MillSecondStamp(long ts)
        {
            return _ZeroTime.AddMilliseconds(ts);
        }

        public static DateTime GetLocalDateTimeFrom1970TimeStamp(int ts)
        {
            return _ZeroTime.AddSeconds(ts).ToLocalTime();
        }

        public static T parseFromXMLNode<T>(XmlNode node)
        {
            T result = default(T);
            Type objType = typeof(T);

            if (node == null)
                return result;

            try
            {
                result = (T)Activator.CreateInstance(objType);
                //Console.WriteLine("create instance");
                //  遍历子节点
                XmlNodeList xnl = node.ChildNodes;
                foreach (XmlNode subNode in xnl)
                {
                    PropertyInfo prop = objType.GetProperty(subNode.Name, BindingFlags.Default | BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                    if (prop != null)
                    {
                        //  找到属性
                        if (string.Compare(prop.PropertyType.Name, "String", true) == 0)
                        {
                            string propValue = subNode.InnerText.Trim();
                            prop.SetValue(result, propValue, null);
                        }
                        else if (string.Compare(prop.PropertyType.Name, "DateTime", true) == 0)
                        {
                            DateTime propValue = Convert.ToDateTime(subNode.InnerText);
                            prop.SetValue(result, propValue, null);
                        }
                        else if (string.Compare(prop.PropertyType.Name, "Int", true) == 0)
                        {
                            int propValue = Int32.Parse(subNode.InnerText);
                            prop.SetValue(result, propValue, null);
                        }
                        else if (string.Compare(prop.PropertyType.Name, "Int32", true) == 0)
                        {
                            int propValue = Int32.Parse(subNode.InnerText);
                            prop.SetValue(result, propValue, null);
                        }
                    }
                }

                foreach (XmlAttribute attr in node.Attributes)
                {
                    PropertyInfo prop = objType.GetProperty(attr.Name, BindingFlags.Default | BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                    if (prop != null)
                    {
                        //  找到属性
                        if (string.Compare(prop.PropertyType.Name, "String", true) == 0)
                        {
                            string propValue = attr.Value.Trim();
                            prop.SetValue(result, propValue, null);
                        }
                        else if (string.Compare(prop.PropertyType.Name, "DateTime", true) == 0)
                        {
                            DateTime propValue = Convert.ToDateTime(attr.Value);
                            prop.SetValue(result, propValue, null);
                        }
                        else if ((string.Compare(prop.PropertyType.Name, "Int", true) == 0) || (string.Compare(prop.PropertyType.Name, "Int32", true) == 0))
                        {
                            int propValue = Int32.Parse(attr.Value);
                            prop.SetValue(result, propValue, null);
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {

                //Console.WriteLine(ex.Message);
                result = default(T);
            }

            return result;
        }

        /// <summary>
        /// 解析一个xmlnode 标签名里面的内容及标签的属性组成一个类
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <returns></returns>
        public static T parseFromOneXMLNode<T>(XmlNode node)
        {
            T result = default(T);
            Type objType = typeof(T);

            if (node == null)
                return result;

            try
            {
                result = (T)Activator.CreateInstance(objType);
                //  遍历子节点

                PropertyInfo prop = objType.GetProperty(node.Name, BindingFlags.Default
                    | BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.Instance
                    | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (prop != null)
                {
                    //  找到属性
                    if (string.Compare(prop.PropertyType.Name, "String", true) == 0)
                    {
                        string propValue = node.InnerText.Trim();
                        prop.SetValue(result, propValue, null);
                    }
                    else if (string.Compare(prop.PropertyType.Name, "DateTime", true) == 0)
                    {
                        DateTime propValue = Convert.ToDateTime(node.InnerText);
                        prop.SetValue(result, propValue, null);
                    }
                    else if (string.Compare(prop.PropertyType.Name, "Int", true) == 0)
                    {
                        int propValue = Int32.Parse(node.InnerText);
                        prop.SetValue(result, propValue, null);
                    }
                    else if (string.Compare(prop.PropertyType.Name, "Int32", true) == 0)
                    {
                        int propValue = Int32.Parse(node.InnerText);
                        prop.SetValue(result, propValue, null);
                    }
                }


                foreach (XmlAttribute attr in node.Attributes)
                {
                    PropertyInfo prop_a = objType.GetProperty(attr.Name, BindingFlags.Default | BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                    if (prop_a != null)
                    {
                        //  找到属性
                        if (string.Compare(prop_a.PropertyType.Name, "String", true) == 0)
                        {
                            string propValue = attr.Value.Trim();
                            prop_a.SetValue(result, propValue, null);
                        }
                        else if (string.Compare(prop_a.PropertyType.Name, "DateTime", true) == 0)
                        {
                            DateTime propValue = Convert.ToDateTime(attr.Value);
                            prop_a.SetValue(result, propValue, null);
                        }
                        else if ((string.Compare(prop_a.PropertyType.Name, "Int", true) == 0) || (string.Compare(prop_a.PropertyType.Name, "Int32", true) == 0))
                        {
                            int propValue = Int32.Parse(attr.Value);
                            prop_a.SetValue(result, propValue, null);
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
                result = default(T);
            }

            return result;
        }

        public static T parseFromXMLNode<T>(string xmlString)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlString);
                return parseFromXMLNode<T>(doc.FirstChild);
            }
            catch { return default(T); }

        }

        public static string HtmlSubstring(string AHtml, int ALength)
        {
            string vReturn = "";
            int vLength = 0; // 增加的文字长度
            int vFlag = 0;   // 当前扫描的区域 0:普通区 1:标记区 // 不考虑在标记中出现<button value="<">情况
            foreach (char vChar in AHtml)
            {
                switch (vFlag)
                {
                    case 0: // 普通区
                        if (vChar == '<')
                        {
                            vReturn += vChar;
                            vFlag = 1;
                        }
                        else
                        {
                            vLength++;
                            if (vLength <= ALength)
                                vReturn += vChar;
                        }
                        break;
                    case 1: // 标记区
                        if (vChar == '>') vFlag = 0;
                        vReturn += vChar;
                        break;
                }
            }
            #region 删除无效标记 // "<span><b></b></span>" -> ""
            string vTemp = Regex.Replace(vReturn, @"<[^>^\/]*?><\/[^>]*?>", "", RegexOptions.IgnoreCase); // 删除空标记
            while (vTemp != vReturn)
            {
                vReturn = vTemp;
                vTemp = Regex.Replace(vReturn, @"<[^>\/]*?><\/[^>]*?>", "", RegexOptions.IgnoreCase); // 删除空标记
            }
            #endregion
            return vReturn;
        }

        public static string Unescape(string str)
        {
            StringBuilder sb = new StringBuilder();
            int len = str.Length;
            int i = 0;

            while (i != len)
            {
                if (Uri.IsHexEncoding(str, i))
                    sb.Append(Uri.HexUnescape(str, ref i));
                else
                    sb.Append(str[i++]);
            }

            return sb.ToString();



        }

        /// <summary>
        /// 获取友好时间
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string GetFriendlyTime(DateTime dt)
        {
            DateTime currTime = DateTime.Now;
            if (dt.ToString("yyyy-MM-dd") == currTime.ToString("yyyy-MM-dd"))
            {
                return dt.ToString("HH:mm");
            }
            if (dt.Year == currTime.Year && dt.Month == currTime.Month && currTime.Day - 1 == dt.Day)
            {
                return "昨天";
            }
            if (dt.Year == currTime.Year)
            {
                return dt.ToString("MM月dd日");
            }
            return dt.ToString("yyyy年MM月dd日");
        }


        public static T XmlDeserialize<T>(string str)
        {
            T rtn = default(T);
            try
            {
                XmlSerializer deser = new XmlSerializer(typeof(T));
                using (System.IO.StringReader writer = new System.IO.StringReader(str))
                {
                    rtn = (T)(deser.Deserialize(writer));
                    writer.Close();
                }
            }
            catch
            { }
            return rtn;
        }

        public static string GetXml(object obj, string root)
        {
            XmlSerializer serializer = new XmlSerializer(obj.GetType(), new XmlRootAttribute(!string.IsNullOrEmpty(root) ? root : "root"));
            StringBuilder sb = new StringBuilder();

            try
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("", "");
                XmlWriterSettings xwSetting = new XmlWriterSettings();
                xwSetting.Encoding = System.Text.Encoding.UTF8;
                using (XmlWriter xw = XmlWriter.Create(sb, xwSetting))
                {
                    serializer.Serialize(xw, obj, ns);
                    xw.Close();
                }
                return sb.ToString();
            }
            catch { }

            return "";
        }

        /// <summary>
        /// 将IP地址转为数值形式
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static long GetIPIntValue(IPAddress ip)
        {
            int x = 3;
            long value = 0;

            byte[] ipBytes = ip.GetAddressBytes();
            foreach (byte b in ipBytes)
            {
                value += (long)b << 8 * x--;
            }
            return value;
        }

        /// <summary>
        /// 将IP地址转为数值形式
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static long GetIPIntValue(string ip)
        {
            long result = 0;
            try
            {
                IPAddress ipAddr = IPAddress.Parse(ip);
                result = GetIPIntValue(ipAddr);
            }
            catch { }
            finally { }

            return result;
        }

        /// <summary>
        /// true=局域网IP
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static bool IsInnerIP(string ip)
        {
            bool result = false;
            try
            {
                long ipIntValue = GetIPIntValue(ip);

                if ((ipIntValue >> 24 == 0xa) || (ipIntValue >> 16 == 0xc0a8) || (ipIntValue >> 22 == 0x2b0))
                {
                    result = true;
                }
            }
            catch { }
            finally { }

            return result;
        }

        /// <summary>
        /// true=局域网IP
        /// </summary>
        /// <param name="ipIntValue"></param>
        /// <returns></returns>
        public static bool IsInnerIP(long ipIntValue)
        {
            bool result = false;
            if ((ipIntValue >> 24 == 0xa) || (ipIntValue >> 16 == 0xc0a8) || (ipIntValue >> 22 == 0x2b0))
            {
                result = true;
            }
            return result;
        }
    }
}
