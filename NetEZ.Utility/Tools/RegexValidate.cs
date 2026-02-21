using System;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;

namespace NetEZ.Utility.Tools
{
    public class RegexValidate
    {
        private static Regex RegPhone = new Regex("^[0-9]+[-]?[0-9]+[-]?[0-9]$");
        private static Regex RegNumber = new Regex("^[0-9]+$");
        private static Regex RegNumberSign = new Regex("^[+-]?[0-9]+$");
        private static Regex RegDecimal = new Regex("^[0-9]+[.]?[0-9]+$");
        private static Regex RegDecimalSign = new Regex("^[+-]?[0-9]+[.]?[0-9]+$"); //等价于^[+-]?\d+[.]?\d+$
        private static Regex RegEmail = new Regex("^[\\w-]+@[\\w-]+\\.(com|net|org|edu|mil|tv|biz|info)$");//w 英文字母或数字的字符串，和 [a-zA-Z0-9] 语法一样 
        private static Regex RegCHZN = new Regex("[\u4e00-\u9fa5]");
        private static Regex RegEnCnNum = new Regex("^[A-Za-z_\u4e00-\u9fa5][A-Za-z0-9_\u4e00-\u9fa5]$");



        public RegexValidate( )
        {
        }

        #region     规范IP地址
        /// <summary>
        /// 规范IP地址
        /// </summary>
        /// <param name="ip">带处理IP地址</param>
        /// <returns>返回格式为xxx.xxx.xxx.xxx</returns>
        public static string ParseIP( string ip )
        {
            string[] ary = ip.Split('.');
            return string.Format("{0:d3}.{1:d3}.{2:d3}.{3:d3}", int.Parse(ary[0]), int.Parse(ary[1]), int.Parse(ary[2]), int.Parse(ary[3]));
        }
        #endregion

        #region     判断字符串是否是时间
        /// <summary>
        /// 判断字符串是否是时间
        /// </summary>
        /// <param name="timeval">将判断字符串</param>
        /// <returns></returns>
        public static bool IsTime( string timeval )
        {
            return Regex.IsMatch(timeval, "^((([0-1]?[0-9])|(2[0-3])):([0-5]?[0-9])(:[0-5]?[0-9])?)$");
        }

        #endregion

        #region     检测是否是正确的Url
        /// <summary>
        /// 检测是否是正确的Url
        /// </summary>
        /// <param name="strUrl">要验证的Url</param>
        /// <returns>判断结果</returns>
        public static bool IsURL( string strUrl )
        {
            return Regex.IsMatch(strUrl, @"^(http|https)\://([a-zA-Z0-9\.\-]+(\:[a-zA-Z0-9\.&%\$\-]+)*@)*((25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9])\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[0-9])|localhost|([a-zA-Z0-9\-]+\.)*[a-zA-Z0-9\-]+\.(com|edu|gov|int|mil|net|org|biz|arpa|info|name|pro|aero|coop|museum|[a-zA-Z]{1,10}))(\:[0-9]+)*(/($|[a-zA-Z0-9\.\,\?\'\\\+&%\$#\=~_\-]+))*$");
        }
        #endregion

        #region 数字字符串检查
        public static bool IsPhone( string inputData )
        {
            Match m = RegPhone.Match(inputData);
            return m.Success;
        }
        /// <summary>
        /// 检查Request查询字符串的键值，是否是数字，最大长度限制
        /// </summary>
        /// <param name="req">Request</param>
        /// <param name="inputKey">Request的键值</param>
        /// <param name="maxLen">最大长度</param>
        /// <returns>返回Request查询字符串</returns>
        public static string FetchInputDigit(string retVal, int maxLen )
        {
            retVal = SqlText(retVal, maxLen);
            if (retVal == null || !IsNumber(retVal))
                retVal = string.Empty;
            return retVal;
        }
        /// <summary>
        /// 是否数字字符串
        /// </summary>
        /// <param name="inputData">输入字符串</param>
        /// <returns></returns>
        public static bool IsNumber( string inputData )
        {
            Match m = RegNumber.Match(inputData);
            return m.Success;
        }

        /// <summary>
        /// 是否数字字符串 可带正负号
        /// </summary>
        /// <param name="inputData">输入字符串</param>
        /// <returns></returns>
        public static bool IsNumberSign( string inputData )
        {
            Match m = RegNumberSign.Match(inputData);
            return m.Success;
        }
        /// <summary>
        /// 是否是浮点数
        /// </summary>
        /// <param name="inputData">输入字符串</param>
        /// <returns></returns>
        public static bool IsDecimal( string inputData )
        {
            Match m = RegDecimal.Match(inputData);
            return m.Success;
        }
        /// <summary>
        /// 是否是浮点数 可带正负号
        /// </summary>
        /// <param name="inputData">输入字符串</param>
        /// <returns></returns>
        public static bool IsDecimalSign( string inputData )
        {
            Match m = RegDecimalSign.Match(inputData);
            return m.Success;
        }

        #endregion

        #region 中文检测

        /// <summary>
        /// 检测是否有中文字符
        /// </summary>
        /// <param name="inputData"></param>
        /// <returns></returns>
        public static bool IsHasCHZN( string inputData )
        {
            Match m = RegCHZN.Match(inputData);
            return m.Success;
        }

        #endregion

        #region 邮件地址
        /// <summary>
        /// 是否是浮点数 可带正负号
        /// </summary>
        /// <param name="inputData">输入字符串</param>
        /// <returns></returns>
        public static bool IsEmail( string inputData )
        {
            Match m = RegEmail.Match(inputData);
            return m.Success;
        }

        #endregion

        #region 检查字符串最大长度，返回指定长度的串

        /// <summary>
        /// 检查字符串最大长度，返回指定长度的串
        /// </summary>
        /// <param name="sqlInput">输入字符串</param>
        /// <param name="maxLength">最大长度</param>
        /// <returns></returns>			
        public static string SqlText( string sqlInput, int maxLength )
        {
            if (sqlInput != null && sqlInput != string.Empty)
            {
                sqlInput = sqlInput.Trim( );
                if (sqlInput.Length > maxLength)//按最大长度截取字符串
                    sqlInput = sqlInput.Substring(0, maxLength);
            }
            return sqlInput;
        }

        public static string ByteText(string str, int len)
        {
            string result = string.Empty;// 最终返回的结果
            int byteLen = System.Text.Encoding.Default.GetByteCount(str);// 单字节字符长度
            int charLen = str.Length;// 把字符平等对待时的字符串长度
            int byteCount = 0;// 记录读取进度
            int pos = 0;// 记录截取位置
            if (byteLen > len)
            {
                for (int i = 0; i < charLen; i++)
                {
                    if (Convert.ToInt32(str.ToCharArray()[i]) > 255)// 按中文字符计算加2
                        byteCount += 2;
                    else// 按英文字符计算加1
                        byteCount += 1;
                    if (byteCount > len)// 超出时只记下上一个有效位置
                    {
                        pos = i;
                        break;
                    }
                    else if (byteCount == len)// 记下当前位置
                    {
                        pos = i + 1;
                        break;
                    }
                }

                if (pos >= 0)
                    result = str.Substring(0, pos);
            }
            else
                result = str;

            return result;
        }
        

        #endregion

        #region    字符串编码  HtmlEncode

        /// <summary>
        /// 字符串编码
        /// </summary>
        /// <param name="inputData"></param>
        /// <returns></returns>
        public static string HtmlEncode( string inputData )
        {
            return HttpUtility.HtmlEncode(inputData);
        }
        #endregion


        
        #region  字符串清理
        //字符串清理
        public static string InputText( string inputString, int maxLength )
        {
            StringBuilder retVal = new StringBuilder( );

            // 检查是否为空
            if (!string.IsNullOrEmpty(inputString))
            {
                inputString = inputString.Trim( );

                //检查长度
                if (inputString.Length > maxLength)
                    inputString = inputString.Substring(0, maxLength);

                //替换危险字符
                for (int i = 0; i < inputString.Length; i++)
                {
                    switch (inputString[i])
                    {
                        case '"':
                            retVal.Append("&quot;");
                            break;
                        case '<':
                            retVal.Append("&lt;");
                            break;
                        case '>':
                            retVal.Append("&gt;");
                            break;
                        default:
                            retVal.Append(inputString[i]);
                            break;
                    }
                }
                retVal.Replace("'", " ");// 替换单引号
            }
            return retVal.ToString( );

        }

        #endregion

        #region     转换成 HTML code

        /// <summary>
        /// 转换成 HTML code
        /// </summary>
        /// <param name="str">string</param>
        /// <returns>string</returns>
        public static string Encode( string str )
        {
            str = str.Replace("&", "&amp;");
            str = str.Replace("'", "''");
            str = str.Replace("\"", "&quot;");
            str = str.Replace(" ", "&nbsp;");
            str = str.Replace("<", "&lt;");
            str = str.Replace(">", "&gt;");
            str = str.Replace("\n", "<br>");
            return str;
        }
        #endregion

        #region    解析html成 普通文本
        /// <summary>
        ///解析html成 普通文本
        /// </summary>
        /// <param name="str">string</param>
        /// <returns>string</returns>
        public static string Decode( string str )
        {
            str = str.Replace("<br>", "\n");
            str = str.Replace("&gt;", ">");
            str = str.Replace("&lt;", "<");
            str = str.Replace("&nbsp;", " ");
            str = str.Replace("&quot;", "\"");
            return str;
        }
        #endregion

        #region     sql字符清理
        /// <summary>
        /// sql字符清理
        /// </summary>
        /// <param name="sqlText"></param>
        /// <returns></returns>
        public static string SqlTextClear( string sqlText )
        {
            if (sqlText == null)
            {
                return null;
            }
            if (sqlText == "")
            {
                return "";
            }
            sqlText = sqlText.Replace(",", "");//去除,
            sqlText = sqlText.Replace("<", "");//去除<
            sqlText = sqlText.Replace(">", "");//去除>
            sqlText = sqlText.Replace("--", "");//去除--
            sqlText = sqlText.Replace("'", "");//去除'
            sqlText = sqlText.Replace("\"", "");//去除"
            sqlText = sqlText.Replace("=", "");//去除=
            sqlText = sqlText.Replace("%", "");//去除%
            sqlText = sqlText.Replace(" ", "");//去除空格
            return sqlText;
        }
        #endregion

        #region 是否由特定字符组成
        public static bool isContainSameChar( string strInput )
        {
            string charInput = string.Empty;
            if (!string.IsNullOrEmpty(strInput))
            {
                charInput = strInput.Substring(0, 1);
            }
            return isContainSameChar(strInput, charInput, strInput.Length);
        }
        #endregion

        #region  是否由特定字符组成
        /// <summary>
        ///    是否由特定字符组成
        /// </summary>
        /// <param name="strInput"></param>
        /// <param name="charInput"></param>
        /// <param name="lenInput"></param>
        /// <returns></returns>
        public static bool isContainSameChar( string strInput, string charInput, int lenInput )
        {
            if (string.IsNullOrEmpty(charInput))
            {
                return false;
            }
            else
            {
                Regex RegNumber = new Regex(string.Format("^([{0}])+$", charInput));
                //Regex RegNumber = new Regex(string.Format("^([{0}]{{1}})+$", charInput,lenInput));
                Match m = RegNumber.Match(strInput);
                return m.Success;
            }
        }
        #endregion

        #region 检查输入的参数是不是某些定义好的特殊字符：这个方法目前用于密码输入的安全检查
        /// <summary>
        /// 检查输入的参数是不是某些定义好的特殊字符：这个方法目前用于密码输入的安全检查
        /// </summary>
        public static bool isContainSpecChar( string strInput )
        {
            string[] list = new string[] { "123456", "654321" };
            bool result = new bool( );
            for (int i = 0; i < list.Length; i++)
            {
                if (strInput == list[i])
                {
                    result = true;
                    break;
                }
            }
            return result;
        }
        #endregion

        #region 检查是否是英文或者数字或中文
        /// <summary>
        /// 检查是否是英文或者数字或中文
        /// </summary>
        public static bool isEnCnNum( string strInput )
        {
            Match m = RegEnCnNum.Match(strInput);
            return m.Success;

        }
        #endregion



    }
}
