using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;

namespace WinKeyToo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly BackgroundWorker notificationWorker;
        private readonly Queue<string> notifications;

        public MainWindow()
        {
            InitializeComponent();
            notifications = new Queue<string>();
            notificationWorker = new BackgroundWorker {WorkerSupportsCancellation = true};
            notificationWorker.DoWork += NotificationWorkerDoWork;

            Closing += MainWindowClosing;
            Loaded += (sLoaded, eLoaded) => InitiateSplashScreen();
        }

        public MainWindow(object dataContext)
        {
            DataContext = dataContext;
            // Redundant to MainWindow() but necessary to set DataContext before InitializeComponent();
            InitializeComponent();
            notifications = new Queue<string>();
            notificationWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            notificationWorker.DoWork += NotificationWorkerDoWork;

            Closing += MainWindowClosing;
            Loaded += (sLoaded, eLoaded) => InitiateSplashScreen();
        }

        internal void AddNotification(string message)
        {
            notifications.Enqueue(message);
        }

        void NotificationWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            var isBalloonOpened = false;
            while (notificationWorker.IsBusy && !notificationWorker.CancellationPending)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
                if (notifications.Count <= 0 || isBalloonOpened) continue;
                isBalloonOpened = true;
                var message = notifications.Dequeue();
                WinKeyTooNotifyIcon.ShowBalloonTip("WinKeyToo", message, TaskbarNotification.BalloonIcon.Info);
                WinKeyTooNotifyIcon.TrayBalloonTipClosed += (oClosed, eClosed) =>
                                                                {
                                                                    isBalloonOpened = false;
                                                                };
            }
        }

        private void InitiateSplashScreen()
        {
            var timer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(3)};
            timer.Tick += (sTick, eTick) =>
                              {
                                  timer.Stop();
                                  Hide();
                              };
            timer.Start();

            var randomNumber = new Random().Next(1, 30);
            //System.Diagnostics.Trace.WriteLine(randomNumber);
            if (randomNumber == 7 || App.FirstTimeRun)
            {
                var donateBalloon = new DonationBalloon
                                        {
                                            BalloonText =
                                                "Please support the development of WinKeyToo.  Any help is greatly appreciated."
                                        };
                WinKeyTooNotifyIcon.ShowCustomBalloon(donateBalloon,
                                                      System.Windows.Controls.Primitives.PopupAnimation.Slide, 10000);
                donateBalloon.Unloaded += (sBalloon, eBalloon) => notificationWorker.RunWorkerAsync();
            }
            else notificationWorker.RunWorkerAsync();
        }

        void MainWindowClosing(object sender, CancelEventArgs e)
        {
            //clean up notifyicon (would otherwise stay open until application finishes)
            notificationWorker.CancelAsync();
            WinKeyTooNotifyIcon.Dispose();
        }
    }
}
