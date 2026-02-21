using System;
using System.IO;
using System.Data;
using System.Text;
using System.Diagnostics;
using System.Security;
using System.Security.Cryptography;


namespace NetEZ.Utility.Encryption
{
    /// <summary>
    /// 此类提供MD5，SHA1，SHA256，SHA512等四种算法，加密字串的长度依次增大。
    /// </summary>
    public class HashEncrypt
    {
        /// <summary>
        /// 字节数组转换为16进制表示的字符串
        /// </summary>
        private static string ByteArrayToHexString(byte[] buf)
        {
            return BitConverter.ToString(buf).Replace("-", "").ToLower();
        }

        /// <summary>
        /// 计算文件的算法签名值;目前只支持md5（默认）和sha1
        /// </summary>
        /// <param name="file"></param>
        /// <param name="algName"></param>
        /// <returns></returns>
        private static string GetFileHashCode(string file, string algName = "md5")
        {
            if (string.IsNullOrEmpty(file))
                return "";

            try
            {
                if (!File.Exists(file))
                    return "";

                using (System.IO.FileStream fs = new System.IO.FileStream(file, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    byte[] hashBytes = HashData(fs, algName);
                    fs.Close();
                    return ByteArrayToHexString(hashBytes);
                }
            }
            catch { }

            return "";
        }

        public static string MD5Encrypt(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";

            byte[] tmpByte;
            MD5 md5 = new MD5CryptoServiceProvider();

            tmpByte = md5.ComputeHash(System.Text.Encoding.ASCII.GetBytes(input));
            string result = BitConverter.ToString(tmpByte);
            result = result.Replace("-", "");
            md5.Clear();
            md5 = null;

            return result;
        }

        /// <summary>
        /// 计算哈希值
        /// </summary>
        /// <param name="stream">要计算哈希值的 Stream</param>
        /// <param name="algName">算法:sha1,md5</param>
        /// <returns>哈希值字节数组</returns>
        private static byte[] HashData(System.IO.Stream stream, string algName = "md5")
        {
            if (stream == null)
                return null;

            System.Security.Cryptography.HashAlgorithm algorithm = null;
            if (string.IsNullOrEmpty(algName))
                algName = "md5";
            
            if (string.Compare(algName, "sha1", true) == 0)
            {
                algorithm = System.Security.Cryptography.SHA1.Create();
            }
            else// if (string.Compare(algName, "md5", true) == 0)
            {
                //  默认使用md5
                algorithm = System.Security.Cryptography.MD5.Create();
            }
            return algorithm.ComputeHash(stream);
        }

        
        /// <summary>
        /// 获得文件md5签名值
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static string GetFileMD5(string file)
        {
            return GetFileHashCode(file,"md5");
        }

        /// <summary>
        /// 获得文件sha1签名值
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static string GetFileSHA1(string file)
        {
            return GetFileHashCode(file, "sha1");
        }

        public   string SHA1Encrypt(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";

            byte[] tmpByte;
            SHA1 sha1 = new SHA1Managed();

            tmpByte = sha1.ComputeHash(System.Text.Encoding.ASCII.GetBytes(input));
            string strResult = BitConverter.ToString(tmpByte);
            strResult = strResult.Replace("-", "");
            sha1.Clear();
            sha1 = null;
            return strResult;
        }

        public static string SHA256Encrypt(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";
            byte[] tmpByte;
            SHA256 sha256 = new SHA256Managed();

            tmpByte = sha256.ComputeHash(System.Text.Encoding.ASCII.GetBytes(input));
            string strResult = BitConverter.ToString(tmpByte);
            strResult = strResult.Replace("-", "");
            sha256.Clear();
            sha256 = null;
            return strResult;

        }

        public static string SHA512Encrypt(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";

            byte[] tmpByte;
            SHA512 sha512 = new SHA512Managed();

            tmpByte = sha512.ComputeHash(System.Text.Encoding.ASCII.GetBytes(input));
            string strResult = BitConverter.ToString(tmpByte);
            strResult = strResult.Replace("-", "");
            sha512.Clear();
            sha512 = null;
            return strResult;

        }

        /// <summary>
        /// 使用DES加密
        /// </summary>
        /// <param name="originalValue">待加密的字符串</param>
        /// <param name="key">密钥(最大长度8)</param>
        /// <param name="iv">初始化向量(最大长度8)</param>
        /// <returns>加密后的字符串</returns>
        public static string DESEncrypt(string originalValue, string key, string iv)
        {
            //将key和IV处理成8个字符

            iv += "12345678";
            key = key.Substring(0, 8);
            iv = iv.Substring(0, 8);

            SymmetricAlgorithm sa;
            ICryptoTransform ct;
            MemoryStream ms;
            CryptoStream cs;
            byte[] byt;

            sa = new DESCryptoServiceProvider();
            sa.Key = System.Text.Encoding.UTF8.GetBytes(key);
            sa.IV = System.Text.Encoding.UTF8.GetBytes(iv);
            ct = sa.CreateEncryptor();

            byt = System.Text.Encoding.UTF8.GetBytes(originalValue);

            ms = new MemoryStream();
            cs = new CryptoStream(ms, ct, CryptoStreamMode.Write);
            cs.Write(byt, 0, byt.Length);
            cs.FlushFinalBlock();

            cs.Close();

            return Convert.ToBase64String(ms.ToArray());
        }

        public static string DESEncrypt(string originalValue, string key)
        {
            return DESEncrypt(originalValue, key, key);
        }

        /// <summary>
        /// 使用DES解密（Added by niehl 2005-4-6）
        /// </summary>
        /// <param name="encryptedValue">待解密的字符串</param>
        /// <param name="key">密钥(最大长度8)</param>
        /// <param name="iv">m初始化向量(最大长度8)</param>
        /// <returns>解密后的字符串</returns>
        public static string DESDecrypt(string encryptedValue, string key, string iv)
        {
            //将key和IV处理成8个字符
            key += "12345678";
            iv += "12345678";
            key = key.Substring(0, 8);
            iv = iv.Substring(0, 8);

            SymmetricAlgorithm sa;
            ICryptoTransform ct;
            MemoryStream ms;
            CryptoStream cs;
            byte[] byt;

            sa = new DESCryptoServiceProvider();
            sa.Key = System.Text.Encoding.UTF8.GetBytes(key);
            sa.IV = System.Text.Encoding.UTF8.GetBytes(iv);
            ct = sa.CreateDecryptor();

            byt = Convert.FromBase64String(encryptedValue);

            ms = new MemoryStream();
            cs = new CryptoStream(ms, ct, CryptoStreamMode.Write);
            cs.Write(byt, 0, byt.Length);
            cs.FlushFinalBlock();

            cs.Close();

            return System.Text.Encoding.UTF8.GetString(ms.ToArray());

        }

        public static string DESDecrypt(string encryptedValue, string key)
        {
            return DESDecrypt(encryptedValue, key, key);

        }
    }
}
