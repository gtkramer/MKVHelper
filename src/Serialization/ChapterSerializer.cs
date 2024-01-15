using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace MKVHelper.Serialization {
    public class ChapterSerializer {
        public static Chapters DeserializeXmlToChapters(string xml) {
            XmlSerializer serializer = new(typeof(Chapters));
            using StringReader reader = new(xml);
            object? deserialized = serializer.Deserialize(reader);
            return (Chapters)(deserialized ?? throw new InvalidOperationException("Deserialization returned null."));
        }

        public static string SerializeChaptersToXml(Chapters chapters) {
            XmlSerializer serializer = new(typeof(Chapters));
            XmlSerializerNamespaces namespaces = new();
            namespaces.Add(string.Empty, string.Empty); // To remove the xmlns:xsi and xmlns:xsd

            using StringWriter stringWriter = new();
            using XmlTextWriter xmlWriter = new(stringWriter) {
                Formatting = Formatting.Indented
            };
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteDocType("Chapters", null, "matroskachapters.dtd", null);
            serializer.Serialize(xmlWriter, chapters, namespaces);
            return stringWriter.ToString();
        }
    }
}
