using System;

public class Duration : Window
{
    DispatcherTimer dt = new DispatcherTimer();
    Stopwatch stopWatch = new Stopwatch();
    string currentTime = string.Empty;
    public Duration()
    {
        InitializeComponent();
        dt.Tick += new EventHandler(dt_Tick);
        dt.Interval = new TimeSpan(0, 0, 0, 0, 1);
    }

    void dt_Tick(object sender, EventArgs e)
    {
        if (stopWatch.IsRunning)
        {
            TimeSpan ts = stopWatch.Elapsed;
            currentTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            ClockTextBlock.Text = currentTime;
        }
    }
    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        stopWatch.Start();
        dt.Start();
    }
}