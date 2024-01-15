using System.Xml.Serialization;

namespace MKVHelper.Serialization {
    public class ChapterDisplay {
        [XmlElement("ChapterString")]
        public string ChapterString { get; set; }

        [XmlElement("ChapterLanguage")]
        public string ChapterLanguage { get; set; }
    }
}
