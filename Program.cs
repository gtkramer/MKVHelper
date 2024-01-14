using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using CommandLine;

namespace MKVHelper {
    [XmlRoot("Chapters")]
    public class Chapters {
        [XmlElement("EditionEntry")]
        public EditionEntry EditionEntry { get; set; }
    }

    public class EditionEntry {
        [XmlElement("ChapterAtom")]
        public List<ChapterAtom> ChapterAtoms { get; set; }
    }

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

    public class ChapterDisplay {
        [XmlElement("ChapterString")]
        public string ChapterString { get; set; }

        [XmlElement("ChapterLanguage")]
        public string ChapterLanguage { get; set; }
    }

    public class Program {
        [Verb("split", HelpText = "Split a combined MKV file into multiple episodes")]
        private class SplitOptions {
            #pragma warning disable 8618
            [Option('i', "input", Required = true, HelpText = "Input video file")]
            public string InputFile { get; set; }
            #pragma warning restore 8618

            [Option('t', "episode-chapter-threshold", Required = false, Default = 360, HelpText = "Duration threshold (seconds) for main episodes")]
            public double EpisodeChapterThreshold { get; set; }

            [Option('a', "additional-chapters", Required = false, Default = 2, HelpText = "Number of chapters to include after second half of an episode")]
            public int AdditionalChapters { get; set; }

            [Option('c', "start-chapter", Required = false, Default = 1, HelpText = "The chapter at which to start processing episodes")]
            public int StartChapter { get; set; }

            [Option('s', "season-num", Required = false, Default = 1, HelpText = "Season number to name output file")]
            public int SeasonNum { get; set; }

            [Option('e', "start-episode-num", Required = false, Default = 1, HelpText = "Starting episode number")]
            public int StartEpisodeNum { get; set; }

            #pragma warning disable 8618
            [Option('n', "series-name", Required = true, HelpText = "Name of series")]
            public string SeriesName { get; set; }
            #pragma warning restore 8618
        }

        [Verb("print-chapters", HelpText = "Print chapters extracted from MKV file")]
        private class PrintChaptersOptions {
            #pragma warning disable 8618
            [Option('i', "input", Required = true, HelpText = "Input video file")]
            public string InputFile { get; set; }
            #pragma warning restore 8618

            [Option('t', "episode-chapter-threshold", Required = false, Default = 360, HelpText = "Duration threshold (seconds) for main episodes")]
            public double EpisodeChapterThreshold { get; set; }
        }

        public static int Main(string[] args) {
            return Parser.Default.ParseArguments<SplitOptions, PrintChaptersOptions>(args)
                .MapResult(
                    (SplitOptions opts) => RunSplitOptions(opts),
                    //(PrintChaptersOptions opts) => RunPrintChaptersOptions(opts),
                    errs => 1
                );
        }

        private static int RunSplitOptions(SplitOptions opts) {
            Chapters chapters = GetChapters(opts.InputFile);
            List<ChapterAtom> chapterAtoms = chapters.EditionEntry.ChapterAtoms;

            Dictionary<int, bool> chapterMainContent = [];
            foreach (ChapterAtom chapterAtom in chapterAtoms) {
                double duration = chapterAtom.GetChapterTimeEndSeconds() - chapterAtom.GetChapterTimeStartSeconds();
                chapterMainContent[chapterAtom.ChapterUID] = duration >= opts.EpisodeChapterThreshold;
            }

            List<(int, int)> chapterRanges = [];
            List<(string, string)> episodeTimestamps = [];
            int startIndex = 0;
            for (int i = 0, j = 1; i != chapterAtoms.Count; i++, j++) {
                if (chapterMainContent[chapterAtoms[i].ChapterUID] && j < chapterAtoms.Count && !chapterMainContent[chapterAtoms[j].ChapterUID]) {
                    int endIndex = i + opts.AdditionalChapters;
                    chapterRanges.Add((startIndex, endIndex));
                    episodeTimestamps.Add((chapterAtoms[startIndex].ChapterTimeStart, chapterAtoms[endIndex].ChapterTimeEnd));
                    startIndex = endIndex + 1;
                }
            }

            List<Chapters> episodeChapters = [];
            foreach ((int, int) chapterRange in chapterRanges) {
                int count = chapterRange.Item2 - chapterRange.Item1 + 1;
                List<ChapterAtom> adjustedChapterAtoms = chapterAtoms.GetRange(chapterRange.Item1, count);
                for (int i = 0; i != adjustedChapterAtoms.Count; i++) {
                    ChapterDisplay adjustedChapterDisplay = adjustedChapterAtoms[i].ChapterDisplay;
                    int chapterNum = i + 1;
                    adjustedChapterDisplay.ChapterString = $"Chapter {chapterNum}";
                    adjustedChapterDisplay.ChapterLanguage = "en";  //IETF BCP 47 language tag for English
                }
                EditionEntry adjustedEditionEntry = new(){ChapterAtoms = adjustedChapterAtoms};
                Chapters adjustedChapters = new(){EditionEntry = adjustedEditionEntry};
                episodeChapters.Add(adjustedChapters);
            }

            for (int i = 0; i != episodeTimestamps.Count; i++) {
                int episodeNum = opts.StartEpisodeNum + i;
                string fileName = opts.SeriesName + " - S" + opts.SeasonNum.ToString("D2") + "E" + episodeNum.ToString("D2") + ".mkv";
                string outputFile = Path.Combine(Path.GetDirectoryName(opts.InputFile), fileName);

                SplitVideoFile(opts.InputFile, episodeTimestamps[i].Item1, episodeTimestamps[i].Item2, episodeChapters[i], outputFile);
            }

            return 0;
        }

        public static Chapters GetChapters(string inputFile) {
            string command = "mkvextract";
            string arguments = $"chapters \"{inputFile}\"";
            string xml = GetProcessOutput(command, arguments);
            return DeserializeXmlToChapters(xml);
        }

        public static Chapters DeserializeXmlToChapters(string xml) {
            XmlSerializer serializer = new(typeof(Chapters));
            using StringReader reader = new(xml);
            object? deserialized = serializer.Deserialize(reader);
            return (Chapters)(deserialized ?? throw new InvalidOperationException("Deserialization returned null."));
        }

        public static void SplitVideoFile(string inputFile, string startTimestamp, string endTimeStamp, Chapters chapters, string outputFile) {
            string chapterFile = Path.GetTempFileName();
            SerializeChaptersToXml(chapters, chapterFile);

            string command = "mkvmerge";
            string arguments = $"--output \"{outputFile}\" --split parts:{startTimestamp}-{endTimeStamp} --chapters \"{chapterFile}\" --no-chapters \"{inputFile}\"";
            RunProcess(command, arguments);

            File.Delete(chapterFile);
        }

        private static void SerializeChaptersToXml(Chapters chapters, string filePath) {
            XmlSerializer serializer = new(typeof(Chapters));
            XmlSerializerNamespaces namespaces = new();
            namespaces.Add(string.Empty, string.Empty); // To remove the xmlns:xsi and xmlns:xsd

            using StreamWriter streamWriter = new(filePath);
            using XmlTextWriter xmlWriter = new(streamWriter);
            xmlWriter.Formatting = Formatting.Indented;
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteDocType("Chapters", null, "matroskachapters.dtd", null);
            serializer.Serialize(xmlWriter, chapters, namespaces);
        }

        public static string GetProcessOutput(string command, string arguments) {
            Process process = RunProcess(command, arguments);
            using StreamReader reader = process.StandardOutput;
            return reader.ReadToEnd();
        }

        public static Process RunProcess(string command, string arguments) {
            Console.WriteLine("Running command: " + command + " " + arguments);
            using Process process = new();
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            process.WaitForExit();
            return process;
        }
    }
}