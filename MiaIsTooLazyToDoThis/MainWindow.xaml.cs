using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Xabe.FFmpeg;

namespace MiaIsTooLazyToDoThis
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Task.Run(() =>
            {
                FFmpeg.ExecutablesPath = Path.GetFullPath("./ffmpeg");
                FFmpeg.GetLatestVersion().Wait();
            }).ContinueWith((_) =>
            {
                Dispatcher.Invoke(() =>
                {
                    MainGrid.Children.Clear();
                    MainGrid.Children.Add(new ProcessVideoControl());
                });
            });
        }

    }
}
