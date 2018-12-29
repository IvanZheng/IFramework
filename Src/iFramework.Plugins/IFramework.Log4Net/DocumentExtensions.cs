using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace IFramework.Log4Net
{
    internal static class DocumentExtensions
    {
        public static XDocument ToXDocument(this XmlDocument xmlDocument)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                xmlDocument.Save((Stream) memoryStream);
                memoryStream.Seek(0L, SeekOrigin.Begin);
                return XDocument.Load((Stream) memoryStream);
            }
        }

        public static XmlDocument ToXmlDocument(this XDocument xDocument)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                xDocument.Save((Stream) memoryStream);
                memoryStream.Seek(0L, SeekOrigin.Begin);
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load((Stream) memoryStream);
                return xmlDocument;
            }
        }
    }
}
