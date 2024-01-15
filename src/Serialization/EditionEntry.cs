using System.Collections.Generic;
using System.Xml.Serialization;

namespace MKVHelper.Serialization {
    public class EditionEntry {
        [XmlElement("ChapterAtom")]
        public List<ChapterAtom> ChapterAtoms { get; set; }
    }
}
