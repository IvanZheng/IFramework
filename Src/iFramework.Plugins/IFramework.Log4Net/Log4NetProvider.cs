using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using log4net;
using log4net.Config;
using log4net.Repository;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IFramework.Log4Net
{
    public class Log4NetProvider : ILoggerProvider
    {
        private readonly ILoggerRepository _loggerRepository;
        private readonly ConcurrentDictionary<string, Log4NetLogger> _loggers = new ConcurrentDictionary<string, Log4NetLogger>();
        private readonly Log4NetProviderOptions _options;

        public Log4NetProvider(Log4NetProviderOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (_options.Watch && _options.PropertyOverrides.Any())
            {
                throw new NotSupportedException("Watch cannot be true when you are overwriting config file values with values from configuration section.");
            }

            if (string.IsNullOrEmpty(_options.LoggerRepository))
            {
                Assembly repositoryAssembly = Assembly.GetExecutingAssembly();

                _loggerRepository = LogManager.CreateRepository(repositoryAssembly, typeof(Hierarchy));
            }
            else
            {
                _loggerRepository = LogManager.CreateRepository(_options.LoggerRepository, typeof(Hierarchy));
            }

            string str = _options.ConfigFile;
            if (!Path.IsPathRooted(str))
            {
                str = Path.Combine(AppContext.BaseDirectory, str);
            }

            string fullPath = Path.GetFullPath(str);
            if (_options.Watch)
            {
                XmlConfigurator.ConfigureAndWatch(_loggerRepository, new FileInfo(fullPath));
            }
            else
            {
                XmlDocument configXmlDocument = ParseLog4NetConfigFile(fullPath);
                if (_options.PropertyOverrides != null && _options.PropertyOverrides.Any())
                {
                    configXmlDocument = UpdateNodesWithOverridingValues(configXmlDocument, _options.PropertyOverrides);
                }

                XmlConfigurator.Configure(_loggerRepository, configXmlDocument.DocumentElement);
            }
        }

        private Log4NetProvider(string log4NetConfigFile, bool watch, IConfigurationSection configurationSection)
        {
            if (watch && configurationSection != null)
            {
                throw new NotSupportedException("Wach cannot be true if you are overwriting config file values with values from configuration section.");
            }
            _options = Log4NetProviderOptions.Default;
            Assembly repositoryAssembly = Assembly.GetExecutingAssembly();
            if (repositoryAssembly == null)
            {
                repositoryAssembly = GetCallingAssemblyFromStartup();
            }

            _loggerRepository = LogManager.CreateRepository(repositoryAssembly, typeof(Hierarchy));
            if (watch)
            {
                XmlConfigurator.ConfigureAndWatch(_loggerRepository, new FileInfo(Path.GetFullPath(log4NetConfigFile)));
            }
            else
            {
                XmlDocument configXml = ParseLog4NetConfigFile(log4NetConfigFile);
                if (configurationSection != null)
                {
                    configXml = UpdateNodesWithAdditionalConfiguration(configXml, configurationSection);
                }

                XmlConfigurator.Configure(_loggerRepository, configXml.DocumentElement);
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, key => CreateLoggerImplementation(key, _options));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                return;
            }

            _loggers.Clear();
        }

        private static XmlDocument UpdateNodesWithOverridingValues(XmlDocument configXmlDocument, IEnumerable<NodeInfo> overridingNodes)
        {
            IEnumerable<NodeInfo> nodeInfos = overridingNodes;
            if (nodeInfos == null)
            {
                return configXmlDocument;
            }

            XDocument xdocument = configXmlDocument.ToXDocument();
            foreach (NodeInfo nodeInfo in nodeInfos)
            {
                XElement node = xdocument.XPathSelectElement(nodeInfo.XPath);
                if (node != null)
                {
                    if (nodeInfo.NodeContent != null)
                    {
                        node.Value = nodeInfo.NodeContent;
                    }

                    AddOrUpdateAttributes(node, nodeInfo);
                }
            }

            return xdocument.ToXmlDocument();
        }

        private static XmlDocument UpdateNodesWithAdditionalConfiguration(XmlDocument configXml, IConfigurationSection configurationSection)
        {
            IEnumerable<NodeInfo> nodesInfo = configurationSection.Get<IEnumerable<NodeInfo>>();
            if (nodesInfo == null)
            {
                return configXml;
            }

            XDocument xdocument = configXml.ToXDocument();
            foreach (NodeInfo nodeInfo in nodesInfo)
            {
                XElement node = xdocument.XPathSelectElement(nodeInfo.XPath);
                if (node != null)
                {
                    if (nodeInfo.NodeContent != null)
                    {
                        node.Value = nodeInfo.NodeContent;
                    }

                    AddOrUpdateAttributes(node, nodeInfo);
                }
            }

            return xdocument.ToXmlDocument();
        }

        private static void AddOrUpdateAttributes(XElement node, NodeInfo nodeInfo)
        {
            if (nodeInfo?.Attributes == null)
            {
                return;
            }

            foreach (KeyValuePair<string, string> attribute1 in nodeInfo.Attributes)
            {
                KeyValuePair<string, string> attribute = attribute1;
                XAttribute xattribute = node.Attributes().FirstOrDefault(a => a.Name.LocalName.Equals(attribute.Key, StringComparison.OrdinalIgnoreCase));
                if (xattribute != null)
                {
                    xattribute.Value = attribute.Value;
                }
                else
                {
                    node.SetAttributeValue(attribute.Key, attribute.Value);
                }
            }
        }

        private static XmlDocument ParseLog4NetConfigFile(string filename)
        {
            using (FileStream fileStream = File.OpenRead(filename))
            {
                XmlReaderSettings settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit
                };
                XmlDocument xmlDocument = new XmlDocument();
                using (XmlReader reader = XmlReader.Create(fileStream, settings))
                {
                    xmlDocument.Load(reader);
                }

                return xmlDocument;
            }
        }

        private static Assembly GetCallingAssemblyFromStartup()
        {
            StackTrace stackTrace = new StackTrace(2);
            for (int index = 0; index < stackTrace.FrameCount; ++index)
            {
                MethodBase method = stackTrace.GetFrame(index).GetMethod();
                Type type = (object) method != null ? method.DeclaringType : null;
                if (string.Equals((object) type != null ? type.Name : null, "Startup", StringComparison.OrdinalIgnoreCase))
                {
                    return type.Assembly;
                }
            }

            return null;
        }

        private static FileInfo GetLog4NetConfigFile(string filename)
        {
            filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                    filename);
            return new FileInfo(filename);
        }


        private Log4NetLogger CreateLoggerImplementation(string name, Log4NetProviderOptions options)
        {
            return new Log4NetLogger(_loggerRepository.Name, name, options);
        }
    }
}