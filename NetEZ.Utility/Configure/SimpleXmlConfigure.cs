using System;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace NetEZ.Utility.Configure
{
    /// <summary>
    /// 采用XML格式的简单配置文件,配置数据层级分为Section -> Item两级
    /// </summary>
    public class SimpleXmlConfigure
    {
        private const string _ROOT_FOLDER_DEFAULT = "xconfig2";
        private static ConcurrentDictionary<string, SimpleXmlConfigure> _ConfigTable = new ConcurrentDictionary<string, SimpleXmlConfigure>();
        protected Dictionary<string, List<KeyValuePair<string,string>>> _Sections = new Dictionary<string, List<KeyValuePair<string,string>>>();

        public const string MSSQL_CONFIGURE_NAME = "mssql.xml";
        public const string MySQL_CONFIGURE_NAME = "mysql.xml";
        public const string RabbitMQ_CONFIGURE_NAME = "rabbitmq.xml";
        public const string Zookeeper_CONFIGURE_NAME = "zookeeper.xml";

        public SimpleXmlConfigure(string filePath, string rootFolder = "")
        {
            if (!LoadFile(filePath,rootFolder))
                throw new Exception("loading file failed.");
        }

        public static SimpleXmlConfigure GetConfig(string configName, string rootFolder = "")
        {
            if (string.IsNullOrEmpty(rootFolder))
                rootFolder = _ROOT_FOLDER_DEFAULT;

            if (string.IsNullOrEmpty(configName))
                return null;

            configName = configName.Trim().ToLower();
            if (configName.Length < 1)
                return null;

            SimpleXmlConfigure config = null;
            if (_ConfigTable.TryGetValue(configName, out config))
                return config;

            try
            {
                string path = string.Format("c:\\{0}\\{1}", rootFolder, configName);
                config = new SimpleXmlConfigure(path);
                _ConfigTable.AddOrUpdate(configName, config, (k, v) => v);
                return config;
            }
            catch { }
            finally { }

            return null;
        }

        /// <summary>
        /// 获得默认mssql的配置文件
        /// </summary>
        /// <returns></returns>
        public static SimpleXmlConfigure GetMSSQLConfig()
        { 
            return GetConfig(MSSQL_CONFIGURE_NAME);
        }

        /// <summary>
        /// 获得默认mysql的配置文件
        /// </summary>
        /// <returns></returns>
        public static SimpleXmlConfigure GetMySqlConfig()
        {
            return GetConfig(MySQL_CONFIGURE_NAME);
        }

        /// <summary>
        /// 获取配置组
        /// </summary>
        /// <param name="sectionName"></param>
        /// <returns></returns>
        public List<KeyValuePair<string,string>> GetSection(string sectionName)
        {
            if (_Sections.Count < 1 || string.IsNullOrEmpty(sectionName))
                return null;

            sectionName = sectionName.ToLower();
            if (_Sections.ContainsKey(sectionName))
                return _Sections[sectionName];

            return null;
        }

        /// <summary>
        /// 获取配置条目
        /// </summary>
        /// <param name="sectionName"></param>
        /// <param name="idx"></param>
        /// <returns></returns>
        public virtual string GetItemValue(string sectionName, int idx)
        {
            List<KeyValuePair<string, string>> section = GetSection(sectionName);
            if (section != null && idx >= 0 && idx < section.Count)
                return section[idx].Value;
            return "";
        }

        public virtual string GetItemValue(string sectionName, string item)
        {
            List<KeyValuePair<string, string>> section = GetSection(sectionName);
            if (section != null && !string.IsNullOrEmpty(item))
            {
                foreach (KeyValuePair<string, string> kv in section)
                {
                    if (string.Compare(kv.Key, item, true) == 0)
                        return kv.Value;
                }
            }
                
            return "";
        }

        public virtual bool GetItemInt32Value(string sectionName, string item, int defaultValue, out int intVal)
        {
            intVal = defaultValue;
            string strVal = GetItemValue(sectionName, item);
            if (!string.IsNullOrEmpty(strVal))
            {
                if (Int32.TryParse(strVal, out intVal))
                {
                    return true;
                }
            }

            return false;
        }

        public virtual bool LoadFile(string filePath,string root="root")
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            try 
            {
                if (!File.Exists(filePath))
                    return false;

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(filePath);

                if (string.IsNullOrEmpty(root))
                    root = "/root";
                else
                {
                    if (!root.StartsWith("/"))
                        root = "/" + root;
                }


                XmlNode rootNode = xmlDoc.SelectSingleNode(root);

                XmlNodeList sectionNodeList = rootNode.ChildNodes;
                string sectionName = "";
                foreach (XmlNode sectionNode in sectionNodeList)
                {
                    sectionName = sectionNode.Name.ToLower().Trim();
                    if (sectionNode.ChildNodes.Count < 1)
                        continue;
                    //NameValueCollection nvc = new NameValueCollection();
                    List<KeyValuePair<string, string>> nvc = new List<KeyValuePair<string, string>>();

                    XmlNodeList itemNodeList = sectionNode.ChildNodes;
                    foreach (XmlNode itemNode in itemNodeList)
                    {
                        KeyValuePair<string, string> kv = new KeyValuePair<string, string>(itemNode.Name.ToLower().Trim(), itemNode.InnerText);
                        nvc.Add(kv);
                        //nvc[itemNode.Name.ToLower().Trim()] = itemNode.InnerText;
                    }
                    _Sections[sectionName] = nvc;
                }

                return true;
            }
            catch { }

            return false;
        }

        
    }
}
