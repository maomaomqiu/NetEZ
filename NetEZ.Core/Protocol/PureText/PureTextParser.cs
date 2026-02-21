using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetEZ.Utility.Logger;
using Newtonsoft.Json;

namespace NetEZ.Core.Protocol.PureText
{
    public class PureTextParser : IProtocolParser
    {
        protected ProtocolParserPolicy _Policy = ProtocolParserPolicy.ErrorStoped;
        protected Logger _Logger = null;

        protected void SetLogger(Logger logger)
        {
            _Logger = logger;
        }

        public PureTextParser(Logger logger = null)
        {
            SetLogger(logger);
        }

        protected void Error(string msg)
        {
            if (_Logger != null)
                _Logger.Error(msg);
        }

        protected void Info(string msg)
        {
            if (_Logger != null)
                _Logger.Info(msg);
        }

        protected void Debug(string msg)
        {
            if (_Logger != null)
                _Logger.Debug(msg);
        }

        /// <summary>
        /// GetMessageBytes的实现静态方法
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static byte[] GetMessageBytes(string msg,out int bytes)
        {
            bytes = 0;
            if (string.IsNullOrEmpty(msg))
                return null;

            int bodyLen = Encoding.UTF8.GetByteCount(msg);
            byte[] buf = new byte[4 + bodyLen];
            Buffer.BlockCopy(BitConverter.GetBytes(bodyLen), 0, buf, 0, 4);
            Buffer.BlockCopy(Encoding.UTF8.GetBytes(msg), 0, buf, 4, bodyLen);
            bytes = buf.Length;

            return buf;
        }

        /// <summary>
        /// ParseMessageFromBytes的实现静态方法
        /// </summary>
        /// <param name="rawMsgBytes"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static bool ParseMessageFromBytes(byte[] rawMsgBytes, out string msg)
        {
            msg = "";

            if (rawMsgBytes == null || rawMsgBytes.Length < 1)
                return false;

            msg = Encoding.UTF8.GetString(rawMsgBytes);
            if (!string.IsNullOrEmpty(msg))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 实现接口定义
        /// </summary>
        /// <param name="rawMsgBytes"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool ParseMessageFromBytes(byte[] rawMsgBytes, out IMessage msg)
        {
            msg = null;

            string msgStr = string.Empty;
            bool ret = ParseMessageFromBytes(rawMsgBytes, out msgStr);
            if (ret)
                msg = new PureTextMessage(msgStr);

            return ret;
        }

        /// <summary>
        /// 实现接口定义
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public byte[] GetMessageBytes(IMessage msg,out int bytes)
        {
            return GetMessageBytes(msg.ToString(), out bytes);
        }
    }
}
