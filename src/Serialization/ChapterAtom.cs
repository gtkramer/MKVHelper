using System;
using System.Xml.Serialization;

namespace MKVHelper.Serialization {
    public class ChapterAtom {
        [XmlElement("ChapterUID")]
        public int ChapterUID { get; set; }

        private string _chapterTimeStart;
        [XmlElement("ChapterTimeStart")]
        public string ChapterTimeStart  {
            get => _chapterTimeStart;
            set => _chapterTimeStart = TrimTrailingZeros(value);
        }

        private string _chapterTimeEnd;
        [XmlElement("ChapterTimeEnd")]
        public string ChapterTimeEnd {
            get => _chapterTimeEnd;
            set => _chapterTimeEnd = TrimTrailingZeros(value);
        }

        [XmlElement("ChapterDisplay")]
        public ChapterDisplay ChapterDisplay { get; set; }

        public double GetChapterTimeStartSeconds() {
            return ConvertTimeStringToSeconds(ChapterTimeStart);
        }

        public double GetChapterTimeEndSeconds() {
            return ConvertTimeStringToSeconds(ChapterTimeEnd);
        }

        private static double ConvertTimeStringToSeconds(string timeString) {
            if (TimeSpan.TryParse(timeString, out TimeSpan timeSpan)) {
                return timeSpan.TotalSeconds;
            }
            else {
                throw new FormatException("Invalid time format");
            }
        }

        private static string TrimTrailingZeros(string timeString) {
            return timeString.TrimEnd('0').TrimEnd('.');
        }
    }
}
