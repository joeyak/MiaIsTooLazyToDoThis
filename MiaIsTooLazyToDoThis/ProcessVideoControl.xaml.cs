using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MiaIsTooLazyToDoThis
{
    public partial class ProcessVideoControl : UserControl
    {
        private const string _prefixInfo = "INFO";
        private const string _prefixError = "ERROR";
        private const string _logFile = "log.txt";

        private string _info = string.Empty;

        private enum Status
        {
            Sneeze,
            Wait,
            Anner,
            Lolly,
            Soap,
            What,
            Kikyo,
            LudicrousSpeed,
        }

        private class ImageInfo
        {
            public Image Image { get; set; }
            public Color Color { get; set; }
            public ImageInfo(Image image, Color color)
                => (Image, Color) = (image, color);
        }

        private readonly Dictionary<Status, ImageInfo> _images;
        private readonly ProcessVideoViewModel _model;

        public ProcessVideoControl()
        {
            _model = new ProcessVideoViewModel();
            DataContext = _model;

            if (File.Exists(_logFile))
            {
                File.WriteAllText(_logFile.Replace(".txt", ".old.txt"), File.ReadAllText(_logFile));
            }
            File.WriteAllText(_logFile, $"[{DateTime.Now}] {_prefixInfo}:: Starting ProcessVideo Window\r\n");

            InitializeComponent();

            _images = new Dictionary<Status, ImageInfo>()
            {
                {Status.Sneeze, new ImageInfo(SneezeImage, Color.FromRgb(245, 245, 245)) }, // Purple - mlg
                {Status.Wait, new ImageInfo(WaitImage, Color.FromRgb(39, 8, 102)) }, // Some kind of green - idk?
                {Status.Anner, new ImageInfo(AnnerImage, Color.FromRgb(0, 205, 242)) }, // Blue - Anner
                {Status.Lolly, new ImageInfo(LollyImage, Color.FromRgb(14, 239, 179)) }, // Sea Green + Teal
                {Status.Soap, new ImageInfo(SoapImage, Color.FromRgb(203, 109, 81)) }, // Copper Red - Lolly
                {Status.What, new ImageInfo(WhatImage, Color.FromRgb(240, 0, 0)) }, // Red - vital
                {Status.Kikyo, new ImageInfo(KikyoImage, Color.FromRgb(0, 0, 128)) }, // Navy - Kikyo
                {Status.LudicrousSpeed, new ImageInfo(LudicrousSpeedImage, Color.FromRgb(42, 139, 35)) }, // Forest Green - Zorch
            };

            ChooseButton.Click += async (o, e) => await ChooseFile();
            ProcessoButton.Click += async (o, e) => await WrapError(ProcessFile);
            SpeedupButton.Click += async (o, e) => await WrapError(SpeedupFile);

            CutPanelButton.Click += (o, e) => ChangePanelVisibility(false);
            SpeedupPanelButton.Click += (o, e) => ChangePanelVisibility(true);

            Dispatcher.ShutdownStarted += (o, e) =>
            {
                try
                {
                    Directory.Delete(_model.Dir, true);
                }
                catch { } // If it can't be deleted as it's shutting down, then oh well
            };

            SetStatus(Status.Sneeze, "");
            ChangePanelVisibility(true);
        }

        private async Task WrapError(Func<Task> func)
        {
            try
            {
                await func.Invoke();
            }
            catch (Exception e)
            {
                Log(_prefixError, e.Message);
                Log(_prefixError, e.StackTrace ?? "");
                Process.Start("notepad.exe", _logFile);
                throw;
            }
        }

        private async Task ChooseFile()
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() ?? false)
            {
                await SetFile(ofd.FileName);
            }

            var enableButtons = !(_model.Info is null);
            ProcessoButton.IsEnabled = enableButtons;
            SpeedupButton.IsEnabled = enableButtons;
        }

        private async Task SetFile(string path)
        {
            _model.Info = new FileInfo(path);
            await _model.GetFileInfo();
        }

        private async Task ProcessFile()
        {
            _info = "Process Info";
            var start = DateTime.Now;

            var sw = Stopwatch.StartNew();
            void AddToInfo(string msg, bool addTime = true)
            {
                _info += "\n" + msg;

                if (addTime)
                {
                    _info += $" => " + sw.Elapsed.ToString();
                    sw.Restart();
                }
            }

            SetStatus(Status.Wait, "Cleaning Up");
            _model.ClearTempDir();

            SetStatus(Status.Anner, "Getting Active Times");
            var activeTimes = await _model.GetActiveTimes();
            if (activeTimes.Count == 0)
            {
                SetStatus(Status.What, "Um...no active times found");
                return;
            }

            var activeRanges = _model.GetActiveTimeRanges(activeTimes);
            AddToInfo($"Frame Range Count: {activeRanges.Count}");

            SetStatus(Status.Lolly, "Splitting Videos");
            await Task.Run(() => _model.SplitVideo(activeRanges));
            AddToInfo($"Video Split");

            SetStatus(Status.Soap, "Stitching Videos Together");
            // This is async so the UI doesn't block, but the method
            // to call is not async
            var newFile = await Task.Run(() => _model.StitchVideos());
            AddToInfo("Stiching");

            await SetFile(newFile);

            ChangePanelVisibility(false);

            SetStatus(Status.What, "Done Processing...What?");

            AddToInfo($"Time to process: {(DateTime.Now - start)}", false);
        }

        private void ChangePanelVisibility(bool enableCut)
        {
            CutPanelButton.IsEnabled = enableCut;
            CutPanel.Visibility = enableCut ?
                Visibility.Visible :
                Visibility.Collapsed;

            SpeedupPanelButton.IsEnabled = !enableCut;
            SpeedPanel.Visibility = enableCut ?
                Visibility.Collapsed :
                Visibility.Visible;
        }

        private async Task SpeedupFile()
        {
            _info = "Speedup Info";
            SetStatus(Status.Kikyo, "Speeding Up Video");

            var start = DateTime.Now;
            await Task.Run(() => _model.SpeedupVideo());

            SetStatus(Status.LudicrousSpeed, "LUDICROUS SPEED REACHED!");
        }

        private void SetStatus(Status status, string message)
        {
            Log(_prefixInfo, message);

            // Hide all images
            foreach (var info in _images.Values)
            {
                info.Image.Visibility = Visibility.Hidden;
            }
            _images[status].Image.Visibility = Visibility.Visible;
            StatusLabel.Text = message;
            StatusLabel.Foreground = new SolidColorBrush(_images[status].Color);
            InvalidateVisual();
        }

        private void Log(string prefix, string message)
            => File.AppendAllText(_logFile, $"[{DateTime.Now}] {prefix}:: {message}\r\n");

        private void OpenTempDir_Click(object sender, RoutedEventArgs e)
            => Process.Start("explorer.exe", _model.Dir);

        private void MenuItem_Click(object sender, RoutedEventArgs e)
            => MessageBox.Show(_info);
    }
}
