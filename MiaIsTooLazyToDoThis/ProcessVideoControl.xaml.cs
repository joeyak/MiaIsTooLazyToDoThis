using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace MiaIsTooLazyToDoThis
{
    public partial class ProcessVideoControl : UserControl
    {
        private enum Status
        {
            Sneeze,
            Wait,
            Anner,
            Lolly,
            Soap,
            What,
            Kikyo,
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
            };

            ChooseButton.Click += async (o, e) => await ChooseFile();
            ProcessoButton.Click += async (o, e) => await ProcessFile();

            Dispatcher.ShutdownStarted += (o, e) =>
            {
                try
                {
                    Directory.Delete(_model.Dir, true);
                }
                catch { } // If it can't be deleted as it's shutting down, then oh well
            };

            SetStatus(Status.Sneeze, "");
        }

        private async Task ChooseFile()
        {
            var ofd = new Forms.OpenFileDialog();
            if (ofd.ShowDialog() == Forms.DialogResult.OK)
            {
                _model.Info = new FileInfo(ofd.FileName);
                await _model.GetFileInfo();
            }

            ProcessoButton.IsEnabled = !(_model.Info is null);
        }

        private async Task ProcessFile()
        {
            StatusLabel.ToolTip = "Process Info";
            var start = DateTime.Now;

            var sw = Stopwatch.StartNew();
            void SetTooltip(string msg, bool reset = true)
            {
                StatusLabel.ToolTip = $"{StatusLabel.ToolTip}\n{msg} => {sw.Elapsed}";
                sw.Restart();
            }

            SetStatus(Status.Wait, "Cleaning Up");
            _model.ClearTempDir();

            SetStatus(Status.Anner, "Getting Active Times");
            SortedSet<int> activeTimes = await _model.GetActiveTimes();
            List<TimeRange> activeRanges = _model.GetActiveTimeRanges(activeTimes);
            SetTooltip($"Frame Range Count: {activeRanges.Count}");

            SetStatus(Status.Lolly, "Splitting Videos");
            await Task.Run(() => _model.SplitVideo(activeRanges));
            SetTooltip($"Video Split");

            SetStatus(Status.Soap, "Stitching Videos Together");
            await Task.Run(() => _model.StitchVideos());
            SetTooltip("Stiching");

            SetStatus(Status.What, "Done Processing...What?");

            SetTooltip($"Time to process: {(DateTime.Now - start)}", false);
        }

        private void SetStatus(Status status, string message)
        {
            // Hide all images
            foreach (ImageInfo info in _images.Values)
            {
                info.Image.Visibility = System.Windows.Visibility.Hidden;
            }
            _images[status].Image.Visibility = System.Windows.Visibility.Visible;
            StatusLabel.Content = message;
            StatusLabel.Foreground = new SolidColorBrush(_images[status].Color);
            InvalidateVisual();
        }

        private void OpenTempDir_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Process.Start("explorer.exe", _model.Dir);
        }
    }
}
