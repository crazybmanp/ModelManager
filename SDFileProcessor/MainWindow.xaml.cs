using SDFileProcessor.Processor;
using System.Windows;

namespace SDFileProcessor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Timers.Timer timer;
        FileProcessor processor;

        public MainWindow()
        {
            timer = new System.Timers.Timer();
            timer.Elapsed += Timer_Elapsed;
            timer.Interval = new TimeSpan(0, 0, 1).TotalMilliseconds;
            timer.AutoReset = true;

            processor = new FileProcessor(@"K:\SD webui\outputs\txt2img-images");
            InitializeComponent();

            setStartStopButtonText();
            timer.Start();
        }

        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            setLastCheckTime();
            setFileStats();
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
            setStartStopButtonText();
        }

        private void setLastCheckTime()
        {
            DateTime? lastCheck = processor.GetLastCheck();
            if (lastCheck != null)
            {
                this.Dispatcher.Invoke(() =>
                {
                    LastUpdateTimeLabel.Content = lastCheck.ToString();
                });
            }
            else
            {
                this.Dispatcher.Invoke(() =>
                {
                    LastUpdateTimeLabel.Content = "Never";
                });
            }
        }

        private void setFileStats()
        {
            FileStats? fileStats = processor.GetFileStats();
            if (fileStats != null)
            {
                this.Dispatcher.Invoke(() =>
                {
                    FileStatusLabel.Content = fileStats.ToString();
                });
            }
            else
            {
                this.Dispatcher.Invoke(() =>
                {
                    FileStatusLabel.Content = "None";
                });
            }
        }

        private void setStartStopButtonText()
        {
            if (processor.IsRunning())
            {
                this.Dispatcher.Invoke(() =>
                {
                    StartStopButton.Content = "Stop";
                });
            }
            else
            {
                this.Dispatcher.Invoke(() =>
                {
                    StartStopButton.Content = "Start";
                });
            }
        }

        private void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            processor.retry(ProcessingStatus.Unrecoverable);
        }
    }
}