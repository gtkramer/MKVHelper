using System.Xml.Serialization;

namespace MKVHelper.Serialization {
    [XmlRoot("Chapters")]
    public class Chapters {
        [XmlElement("EditionEntry")]
        public EditionEntry EditionEntry { get; set; }
    }
}
