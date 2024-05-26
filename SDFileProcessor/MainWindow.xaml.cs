using SDFileProcessor.Processor;
using System.Windows;

namespace SDFileProcessor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly System.Timers.Timer timer;
        private readonly FileProcessor processor;

        public MainWindow()
        {
            timer = new System.Timers.Timer();
            timer.Elapsed += Timer_Elapsed;
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100).TotalMilliseconds;
            timer.AutoReset = true;

            processor = new FileProcessor(@"K:\SD webui\outputs\txt2img-images");
            InitializeComponent();

            SetStartStopButtonText();
            timer.Start();
        }

        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            SetLastCheckTime();
            SetFileStats();
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (processor.IsRunning())
            {
                processor.Stop();
            } else
            {
                processor.Start();
            }
            SetStartStopButtonText();
        }

        private void SetLastCheckTime()
        {
            DateTime? lastCheck = processor.GetLastCheck();
            if (lastCheck != null)
            {
                Dispatcher.Invoke(() =>
                {
                    LastUpdateTimeLabel.Content = lastCheck.ToString();
                });
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    LastUpdateTimeLabel.Content = "Never";
                });
            }
        }

        private void SetFileStats()
        {
            FileStats? fileStats = processor.GetFileStats();
            if (fileStats != null)
            {
                Dispatcher.Invoke(() =>
                {
                    FileStatusLabel.Content = fileStats.ToString();
                });
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    FileStatusLabel.Content = "None";
                });
            }
        }

        private void SetStartStopButtonText()
        {
            if (processor.IsRunning())
            {
                Dispatcher.Invoke(() =>
                {
                    StartStopButton.Content = "Stop";
                });
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    StartStopButton.Content = "Start";
                });
            }
        }

        private void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            processor.Retry(ProcessingStatus.Unrecoverable);
        }

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
            timer.Stop();
		}
	}
}