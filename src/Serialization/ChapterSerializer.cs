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

            // Writing data to memory automatically produces a UTF-8 value for the encoding attribute in the generated XML header
            using MemoryStream memoryStream = new();
            using StreamWriter streamWriter = new(memoryStream);
            using XmlTextWriter xmlWriter = new(streamWriter) {
                Formatting = Formatting.Indented
            };
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteDocType("Chapters", null, "matroskachapters.dtd", null);
            serializer.Serialize(xmlWriter, chapters, namespaces);

            // Rewind the memory stream to read from the beginning
            memoryStream.Position = 0;
            using StreamReader reader = new(memoryStream);
            return reader.ReadToEnd();
        }
    }
}
