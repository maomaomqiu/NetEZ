using System;
using System.IO;
using System.Reflection;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetEZ.Utility.Logger;
using Newtonsoft.Json;

namespace NetEZ.Core.Protocol.JMP
{
    /// <summary>
    /// JM格式消息处理器
    /// | 1 byte = Signal length;1 - 255| Signal (ascii string) | 4 bytes = Body length;可以为0 | Body(utf8 json string),body可以没有
    /// </summary>
    public class JMPParser:IProtocolParser
    {
        protected ProtocolParserPolicy _Policy = ProtocolParserPolicy.ErrorStoped;
        protected Logger _Logger = null;
        private static readonly object _Lock = new object();
        private static ConcurrentDictionary<string, Type> _JMPTypeTable = new ConcurrentDictionary<string, Type>();

        private static ConcurrentDictionary<string, ConcurrentDictionary<string, Type>> _ModuleJMPTypeTablePool = new ConcurrentDictionary<string, ConcurrentDictionary<string, Type>>();
        private static List<string> _IgnoredPrefix = new List<string>();

        private string _ModuleName = string.Empty;

        protected void SetLogger(Logger logger)
        {
            _Logger = logger;
        }

        private void InitParser(List<string> assemList, Logger logger = null)
        {
            SetLogger(logger);
            bool coreAssemLoaded = false;
            if (assemList != null)
            {
                foreach (string assem in assemList)
                {
                    if (string.Compare(assem, "NetEZ.Core", true) == 0)
                        coreAssemLoaded = true;
                    JMPParser.RegisterAssembly(assem, _ModuleName);
                }
            }
            if (!coreAssemLoaded)
                JMPParser.RegisterAssembly("NetEZ.Core", _ModuleName);
        }

        public JMPParser(Logger logger = null)
        {
            InitParser(null, logger);
        }

        public JMPParser(List<string> assemList, Logger logger = null)
        {
            InitParser(assemList, logger);
        }

        public JMPParser(string moduleName, List<string> assemList, Logger logger = null)
        {
            if (moduleName != null)
                _ModuleName = moduleName.Trim().ToLower();

            InitParser(assemList, logger);
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
        /// 从字节数组中读取消息;注意，这里的byte[] 已经去掉了表示消息长度的4个字节
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="signal"></param>
        /// <param name="json"></param>
        /// <returns></returns>
        private static bool ReadBytes(byte[] buf, out string signal, out string json)
        {
            signal = "";
            json = "";

            int offset = 0;

            if (buf == null || buf.Length < 1)
                return false;

            int signalLen = (int)buf[offset ++];

            //  signal长度大于0
            if (signalLen < 1)
                return false;

            if (offset + signalLen + 4 > buf.Length)
                return false;

            signal = Encoding.ASCII.GetString(buf, offset, signalLen);
            offset += signalLen;

            int jsonLen = BitConverter.ToInt32(buf, offset);
            offset += 4;

            //  json 长度必须>= 0,而且读完该长度后必须等于buf长度
            if (jsonLen < 0 || offset + jsonLen != buf.Length)
                return false;

            //  json 可能为空字符串
            if (jsonLen > 0)
                json = Encoding.UTF8.GetString(buf, offset, jsonLen);

            return true;
        }

        private static Type GetInstanceTypeFromSignal(string signal, string moduleName = "")
        {
            Type type = null;

            if (string.IsNullOrEmpty(signal))
                return null;

            string key = signal.ToLower();
            if (string.IsNullOrEmpty(moduleName))
            {
                if (_JMPTypeTable.TryGetValue(key, out type))
                    return type;
            }
            else
            {
                ConcurrentDictionary<string, Type> typeTable = null;
                if (!_ModuleJMPTypeTablePool.TryGetValue(moduleName, out typeTable))
                    return type;

                if (typeTable.TryGetValue(key, out type))
                    return type;
            }

            return null;
        }

        public static void RegisterAssembly(string asmName,string moduleName="")
        {
            asmName = !string.IsNullOrEmpty(asmName) ? asmName.Trim().ToLower() : "";
            if (string.IsNullOrEmpty(asmName))
                return;

            try
            {
                lock (_Lock)
                {
                    //_AssemblyTable[asmName] = System.Reflection.Assembly.Load(asmName);
                    Assembly asmb = System.Reflection.Assembly.Load(asmName);
                    foreach (Type t in asmb.ExportedTypes)
                    {
                        if (t.IsAbstract)
                            continue;

                        if (t.GetInterface("NetEZ.Core.Protocol.JMP.IJMPMessage", false) != null)
                        {
                            if (string.IsNullOrEmpty(moduleName))
                            {
                                _JMPTypeTable.AddOrUpdate(t.Name.ToLower(), t, (ky, vl) => t);
                            }
                            else
                            {
                                ConcurrentDictionary<string, Type> typeTable = null;
                                if (!_ModuleJMPTypeTablePool.TryGetValue(moduleName, out typeTable))
                                {
                                    typeTable = new ConcurrentDictionary<string, Type>();
                                    _ModuleJMPTypeTablePool.AddOrUpdate(moduleName, typeTable, (k, v) => v);
                                    _ModuleJMPTypeTablePool.TryGetValue(moduleName, out typeTable);
                                }
                                typeTable.AddOrUpdate(t.Name.ToLower(), t, (ky, vl) => t);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        public static void AppendIgnoredSignalPrefix(string prefix)
        {
            if (!string.IsNullOrEmpty(prefix))
                _IgnoredPrefix.Add(prefix);
        }

        /// <summary>
        /// 实现ParseMessageFromBytes的静态方法
        /// </summary>
        /// <param name="rawMsgBytes"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        private static bool ParseMessageFromBytes(byte[] rawMsgBytes, out IJMPMessage msg, string moduleName = "")
        {
            msg = null;

            if (rawMsgBytes == null || rawMsgBytes.Length < 1)
                return false;


            string signal = "";
            string json = "";

            if (ReadBytes(rawMsgBytes, out signal, out json))
            {
                //  尝试过滤掉特定前缀，如 "__rpccall:" , "__rpccallret:"
                if (_IgnoredPrefix.Count > 0)
                {
                    int idx = -1;
                    foreach (string prefix in _IgnoredPrefix)
                    {
                        if (prefix.Length < signal.Length)
                        {
                            bool find = true;
                            for (int i = 0; i < prefix.Length; i++)
                            {
                                if (prefix[i] != signal[i])
                                {
                                    find = false;
                                    break;
                                }
                            }
                            if (find)
                            {
                                idx = prefix.Length;
                                break;
                            }
                        }
                        else
                            continue;
                    }

                    if (idx > 0)
                    {
                        //  过滤后的signal
                        signal = signal.Substring(idx);
                    }
                }

                Type t = GetInstanceTypeFromSignal(signal, moduleName);
                if (t != null)
                {
                    if (string.IsNullOrEmpty(json))
                        json = "{}";

                    try
                    {
                        msg = (IJMPMessage)JsonConvert.DeserializeObject(json, t);

                        if (msg != null)
                        {
                            return true;
                        }
                    }
                    catch { }
                }
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
            IJMPMessage jmpMsg = null;
            bool ret = JMPParser.ParseMessageFromBytes(rawMsgBytes, out jmpMsg, _ModuleName);
            if (ret)
                msg = jmpMsg;

            return ret;
        }

        /// <summary>
        /// GetMessageBytes的实现静态方法
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static byte[] GetMessageBytes(IJMPMessage msg,out int bytes)
        {
            byte[] buf = null;
            bytes = 0;

            if (msg == null)
                return null;

            try
            {
                //| 1 byte = Signal length;1 - 255| Signal (ascii string) | 4 bytes = Body length;可以为0 | Body(utf8 json string),body可以没有

                string signal = msg.GetSignal();
                if (signal == null || signal.Length < 1)
                    return null;

                string json = msg.GetInstanceJson();
                //string json = "{\"code\":0,\"msg\":\"abcdefghijklmn\"}";

                int signalLen = Encoding.ASCII.GetByteCount(signal);
                int jsonLen = Encoding.UTF8.GetByteCount(json);

                int msgLen = 1 + signalLen + 4 + jsonLen;

                using (MemoryStream ms = new MemoryStream())
                {
                    ms.SetLength(msgLen + 4);
                    ms.Write(BitConverter.GetBytes(msgLen), 0, 4);
                    ms.WriteByte((byte)signalLen);
                    for (int i = 0; i < signal.Length; i++)
                    {
                        ms.WriteByte((byte)signal[i]);
                    }
                    //ms.Write(Encoding.ASCII.GetBytes(signal), 0, signalLen);
                    ms.Write(BitConverter.GetBytes(jsonLen), 0, 4);
                    ms.Write(Encoding.UTF8.GetBytes(json), 0, jsonLen);

                    buf = ms.GetBuffer();
                    bytes = (int)ms.Length;
                }
            }
            catch(Exception ex)
            {
                string err = ex.Message;
            }

            return buf;
        }

        
        /// <summary>
        /// 实现接口定义
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public byte[] GetMessageBytes(IMessage msg,out int bytes)
        {
            bytes = 0;
            try 
            {
                IJMPMessage jmpMsg = (IJMPMessage)msg;
                return JMPParser.GetMessageBytes(jmpMsg,out bytes);
            }
            catch { }

            return null;
        }
    }
}
