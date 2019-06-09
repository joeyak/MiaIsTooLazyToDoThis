using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Streams;

namespace MiaIsTooLazyToDoThis
{
    class ProcessVideoViewModel : INotifyPropertyChanged
    {
        private readonly string _ffmpegPath = string.Empty;
        private readonly string _concatFile = string.Empty;
        public readonly string Dir = string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;

        private FileInfo? _info;
        public FileInfo? Info
        {
            get { return _info; }
            set
            {
                if (!(value is null))
                {
                    _info = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Info)));
                }
            }
        }

        private VideoInfo _videoInfo;
        public VideoInfo VideoInfo
        {
            get { return _videoInfo; }
            set
            {
                _videoInfo = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VideoInfo)));
            }
        }

        private int _frame;
        public int Frame
        {
            get { return _frame; }
            set
            {
                _frame = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Frame)));
            }
        }

        private int _stitchSeconds = 3;
        public int StitchSeconds
        {
            get { return _stitchSeconds; }
            set
            {
                _stitchSeconds = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StitchSeconds)));
            }
        }

        private double _diffPercent = 0.9995;
        public double DiffPercent
        {
            get { return _diffPercent; }
            set
            {
                _diffPercent = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DiffPercent)));
            }
        }

        private double _videoOffset = 1.5;
        public double VideoOffset
        {
            get { return _videoOffset; }
            set
            {
                _videoOffset = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VideoOffset)));
            }
        }

        public ProcessVideoViewModel()
        {
            _ffmpegPath = Path.Combine(FFmpeg.ExecutablesPath, FFmpeg.FFmpegExecutableName);
            Dir = Path.Combine(Path.GetTempPath(), nameof(MiaIsTooLazyToDoThis));
            Directory.CreateDirectory(Dir);
            _concatFile = Path.Combine(Dir, "files.txt");
        }

        public async Task GetFileInfo()
        {
            if (!(_info is null))
            {
                IMediaInfo mediaInfo = await MediaInfo.Get(_info);
                IVideoStream video = mediaInfo.VideoStreams.First();
                if (!(video is null))
                {
                    VideoInfo = new VideoInfo()
                    {
                        Duration = video.Duration,
                        FrameRate = video.FrameRate
                    };
                }
            }
        }

        public async Task<SortedSet<int>> GetActiveTimes()
        {
            if (_info is null)
            {
                throw new Exception("File was empty");
            }

            var startInfo = new ProcessStartInfo()
            {
                FileName = _ffmpegPath,
                Arguments = string.Join(" ", new string[]
                {
                    "-y",
                    "-loglevel quiet",
                    $"-ss {_videoOffset}",
                    $"-i {_info.FullName}",
                    $"-i {_info.FullName}",
                    "-filter_complex ssim=stats_file=-",
                    "-f null",
                    "-"
                }),
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };

            var process = Process.Start(startInfo);
            StreamReader reader = process.StandardOutput;

            var activeTime = new SortedSet<int>();
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                Match match = Regex.Match(line, @"n:(\d*).*All:(\d\.\d*?)\D");
                var frame = int.Parse(match.Groups[1].Value);
                var diff = float.Parse(match.Groups[2].Value);
                if (diff < _diffPercent)
                {
                    activeTime.Add(frame);
                }
            }
            return activeTime;
        }

        public List<TimeRange> GetActiveTimeRanges(IEnumerable<int> activeTimes)
        {
            var times = new List<TimeRange>();
            var range = new TimeRange()
            {
                Start = activeTimes.First(),
                End = activeTimes.First(),
            };


            var pad = (int)(_stitchSeconds * _videoInfo.FrameRate);
            if (pad == 0)
            {
                pad = 1;
            }

            foreach (var time in activeTimes.Skip(1))
            {
                if (range.End + pad >= time)
                {
                    range.End = time;
                }
                else
                {
                    times.Add(range);
                    range = new TimeRange()
                    {
                        Start = time,
                        End = time
                    };
                }
            }
            // Make sure to get the last one
            times.Add(range);
            return times;
        }

        public void SplitVideo(List<TimeRange> ranges)
        {
            if (_info is null)
            {
                throw new Exception("File was empty");
            }

            var fileNames = new ConcurrentBag<string>();
            Parallel.For(0, ranges.Count(), new ParallelOptions() { MaxDegreeOfParallelism = 2 }, (i) =>
            {
                var fileName = Path.Combine(Dir, $"sp_{i}{Path.GetExtension(_info.Name)}");

                var startInfo = new ProcessStartInfo()
                {
                    FileName = _ffmpegPath,
                    Arguments = string.Join(" ", new string[]
                    {
                    "-y",
                    "-v quiet",
                    $"-i {_info.FullName}",
                    "-q:v 0",
                    $"-ss {((float)ranges[i].Start)/_videoInfo.FrameRate}",
                    $"-frames:v {ranges[i].End - ranges[i].Start}",
                    fileName,
                    }),
                    CreateNoWindow = true,
                };
                Process.Start(startInfo).WaitForExit();

                fileNames.Add("file " +fileName.Replace(@"\", @"\\"));
            });

            File.AppendAllLines(_concatFile, fileNames.OrderBy(x => x));
        }

        public void StitchVideos()
        {
            if (_info is null)
            {
                throw new Exception("File was empty");
            }

            var ext = Path.GetExtension(_info.Name);
            var startInfo = new ProcessStartInfo()
            {
                FileName = _ffmpegPath,
                Arguments = string.Join(" ", new string[]
                {
                    "-y",
                    "-v quiet",
                    "-f concat",
                    "-safe 0",
                    $"-i {_concatFile}",
                    "-q:v 0",
                    _info.FullName.Replace(ext, "_new" + ext),
                }),
                CreateNoWindow = true,
            };
            Process.Start(startInfo).WaitForExit();
        }

        public void ClearTempDir()
        {
            foreach (var file in Directory.GetFiles(Dir))
            {
                File.Delete(file);
            }
        }
    }
}
