using System.Xml.Serialization;

using MKVHelper.Utilities;

namespace MKVHelper.Serialization {
    public class ChapterAtom {
        [XmlElement("ChapterUID")]
        public int ChapterUID { get; set; }

        private string _chapterTimeStart;
        [XmlElement("ChapterTimeStart")]
        public string ChapterTimeStart  {
            get => _chapterTimeStart;
            set => _chapterTimeStart = TimeUtilities.TrimTrailingZeros(value);
        }

        private string _chapterTimeEnd;
        [XmlElement("ChapterTimeEnd")]
        public string ChapterTimeEnd {
            get => _chapterTimeEnd;
            set => _chapterTimeEnd = TimeUtilities.TrimTrailingZeros(value);
        }

        [XmlElement("ChapterDisplay")]
        public ChapterDisplay ChapterDisplay { get; set; }

        public double GetChapterTimeStartSeconds() {
            return TimeUtilities.ConvertToSeconds(ChapterTimeStart);
        }

        public double GetChapterTimeEndSeconds() {
            return TimeUtilities.ConvertToSeconds(ChapterTimeEnd);
        }
    }
}
