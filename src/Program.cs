using System.Collections.Generic;
using System.IO;
using CommandLine;

using MKVHelper.Serialization;
using MKVHelper.Utilities;

namespace MKVHelper {
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

            List<(int StartIndex, int EndIndex)> chapterRanges = [];
            List<(string StartTimestamp, string EndTimestamp)> episodeTimestamps = [];
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
            foreach ((int StartIndex, int EndIndex) chapterRange in chapterRanges) {
                int count = chapterRange.EndIndex - chapterRange.StartIndex + 1;
                List<ChapterAtom> adjustedChapterAtoms = chapterAtoms.GetRange(chapterRange.StartIndex, count);
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

                SplitVideoFile(opts.InputFile, episodeTimestamps[i].StartTimestamp, episodeTimestamps[i].EndTimestamp, episodeChapters[i], outputFile);
            }

            return 0;
        }

        public static Chapters GetChapters(string inputFile) {
            string command = "mkvextract";
            string arguments = $"chapters \"{inputFile}\"";
            string xml = ProcessUtilities.RunProcess(command, arguments).StandardOutput;
            return ChapterSerializer.DeserializeXmlToChapters(xml);
        }

        public static void SplitVideoFile(string inputFile, string startTimestamp, string endTimeStamp, Chapters chapters, string outputFile) {
            string chapterFile = Path.GetTempFileName();
            string xml = ChapterSerializer.SerializeChaptersToXml(chapters);
            File.WriteAllText(chapterFile, xml);

            string command = "mkvmerge";
            string arguments = $"--output \"{outputFile}\" --split parts:{startTimestamp}-{endTimeStamp} --chapters \"{chapterFile}\" --no-chapters \"{inputFile}\"";
            ProcessUtilities.RunProcess(command, arguments);

            File.Delete(chapterFile);
        }
    }
}
