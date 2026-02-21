using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace NetEZ.Utility
{
    public class EzWebClient
    {
        public const string HTTP_REQUEST_METHOD_GET = "GET";
        public const string HTTP_REQUEST_METHOD_POST = "POST";

        //public static bool PostUrl(string url, NameValueCollection paramNvc ,out string resonseText)


        public static byte[] GetPostBytes(string postData, Encoding enc = null)
        {
            if (enc == null)
                enc = Encoding.UTF8;

            if (string.IsNullOrEmpty(postData))
                return null;

            byte[] buf = enc.GetBytes(postData);

            return buf;
        }

        public static byte[] GetPostBytes(NameValueCollection parameters, Encoding enc = null)
        {
            if (enc == null)
                enc = Encoding.UTF8;

            string paramContent = GetParamsContent(parameters, enc);
            if (string.IsNullOrEmpty(paramContent))
                return null;

            byte[] buf = enc.GetBytes(paramContent);

            return buf;
        }

        public static string GetParamsContent(NameValueCollection parameters, Encoding enc = null)
        {
            if (enc == null)
                enc = Encoding.UTF8;

            if (parameters == null || parameters.Count < 1)
                return "";

            StringBuilder sb = new StringBuilder();

            try
            {
                int k = 0;
                foreach (string key in parameters.AllKeys)
                {
                    if (k > 0)
                        sb.Append("&"); //  不是第一个参数
                    sb.AppendFormat("{0}={1}", key, HttpUtility.UrlEncode(parameters[key], enc));
                    k++;
                }
            }
            catch { }

            return sb.ToString();
        }

        public static HttpStatusCode DoPost(string url, string postData, Dictionary<string, string> cookies, out string resonseText, Encoding enc = null, SecurityProtocolType protocal = SecurityProtocolType.Tls)
        {
            resonseText = string.Empty;

            HttpWebRequest request = null;

            if (enc == null)
                enc = Encoding.UTF8;


            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                request.ProtocolVersion = HttpVersion.Version11;
                request.Method = "POST";    //  GET/POST
                request.AllowAutoRedirect = true;
                //request.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.2; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
                request.UserAgent = "";
                request.Accept = "*/*";
                //request.Headers["Authorization"] = "6B18980336C56756FE10992D978DEC6A95129FECEwlUxutFTG6o6QDuZsgOuEi7UwCvMw%3d%3d";
                if (cookies != null && cookies.Count > 0)
                {
                    request.CookieContainer = new CookieContainer();
                    foreach (KeyValuePair<string, string> item in cookies)
                    {
                        Cookie cookie = new Cookie(item.Key, item.Value, "/", new Uri(url).Host);
                        request.CookieContainer.Add(cookie);
                    }
                }

                if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                {
                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);

                    // 这里设置了协议类型。
                    ServicePointManager.SecurityProtocol = protocal;
                    request.KeepAlive = true;
                    ServicePointManager.CheckCertificateRevocationList = true;
                    ServicePointManager.DefaultConnectionLimit = 200;
                    ServicePointManager.Expect100Continue = false;
                }

                request.ContentType = "application/x-www-form-urlencoded";
                request.Referer = null;
                byte[] contentBytes = GetPostBytes(postData, enc);
                request.ContentLength = contentBytes != null ? contentBytes.Length : 0;

                if (request.ContentLength > 0)
                {
                    using (Stream reqStream = request.GetRequestStream())
                    {
                        reqStream.Write(contentBytes, 0, contentBytes.Length);
                        reqStream.Close();
                    }
                }

                //获取网页响应结果
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                using (Stream respStream = response.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(respStream))
                    {
                        resonseText = sr.ReadToEnd();
                        sr.Close();
                    }
                    respStream.Close();

                    return response.StatusCode;
                }
            }
            catch (Exception ex)
            {
                string err = ex.Message;
            }
            finally { }


            return HttpStatusCode.InternalServerError;
        }


        public static HttpStatusCode CallUrl(string url, NameValueCollection parameters, string method, Dictionary<string, string> cookies, out string resonseText, Encoding enc = null, SecurityProtocolType protocal = SecurityProtocolType.Tls)
        {
            resonseText = string.Empty;

            HttpWebRequest request = null;

            if (enc == null)
                enc = Encoding.UTF8;

            method = string.IsNullOrEmpty(method) ? HTTP_REQUEST_METHOD_GET : method;

            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                request.ProtocolVersion = HttpVersion.Version11;
                request.Method = method;    //  GET/POST
                request.AllowAutoRedirect = true;
                //request.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.2; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
                request.UserAgent = "";
                request.Accept = "*/*";
                request.Headers["Authorization"] = "6B18980336C56756FE10992D978DEC6A95129FECEwlUxutFTG6o6QDuZsgOuEi7UwCvMw%3d%3d";
                if (cookies != null && cookies.Count > 0)
                {
                    request.CookieContainer = new CookieContainer();
                    foreach (KeyValuePair<string, string> item in cookies)
                    {
                        Cookie cookie = new Cookie(item.Key, item.Value, "/", new Uri(url).Host);
                        request.CookieContainer.Add(cookie);
                    }
                }

                if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                {
                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);

                    // 这里设置了协议类型。
                    ServicePointManager.SecurityProtocol = protocal;
                    request.KeepAlive = true;
                    ServicePointManager.CheckCertificateRevocationList = true;
                    ServicePointManager.DefaultConnectionLimit = 200;
                    ServicePointManager.Expect100Continue = false;
                }

                if (string.Compare(method, HTTP_REQUEST_METHOD_GET, true) == 0)
                {
                    string paramContent = GetParamsContent(parameters, enc);
                    if (!string.IsNullOrEmpty(paramContent))
                    {
                        if (url.IndexOf('?') > 0)
                            url += "&" + paramContent;      //  url有参数
                        else
                            url += "?" + paramContent;      //  url没参数
                    }
                }
                else
                {
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.Referer = null;
                    byte[] contentBytes = GetPostBytes(parameters, enc);
                    request.ContentLength = contentBytes != null ? contentBytes.Length : 0;

                    if (request.ContentLength > 0)
                    {
                        using (Stream reqStream = request.GetRequestStream())
                        {
                            reqStream.Write(contentBytes, 0, contentBytes.Length);
                            reqStream.Close();
                        }
                    }
                }

                //获取网页响应结果
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                using (Stream respStream = response.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(respStream))
                    {
                        resonseText = sr.ReadToEnd();
                        sr.Close();
                    }
                    respStream.Close();

                    return response.StatusCode;
                }
            }
            catch (Exception ex)
            {
                string err = ex.Message;
            }
            finally { }


            return HttpStatusCode.InternalServerError;
        }

        public static HttpStatusCode CallUrl(string url, NameValueCollection parameters, string method, out string resonseText, Encoding enc = null, SecurityProtocolType protocal = SecurityProtocolType.Tls)
        {
            return CallUrl(url, parameters, method, null, out resonseText, enc, protocal);
        }

        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受  
        }
    }
}
