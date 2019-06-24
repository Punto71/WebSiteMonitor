using WebSiteMonitor.Service;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using WebSiteMonitor.Service.Auth;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using WebSiteMonitor.Service.Support;
using System.Configuration;

namespace WebSiteMonitor {
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private readonly NotifyIcon _myNotifyIcon;
        private readonly ObservableCollection<string> _messageList;
        private readonly IDisposable _webServer;

        public MainWindow() {
            InitializeComponent();
            _myNotifyIcon = new NotifyIcon();
            _messageList = new ObservableCollection<string>();
            _myNotifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
            _myNotifyIcon.MouseDoubleClick += MyNotifyIconMouseDoubleClick;
            MessageList.ItemsSource = _messageList;
            Logger.MessageListChanged += LoggerMessageListChanged;
            var ip = ConfigurationManager.AppSettings.Get("WebServerAddress");
            var port = int.Parse(ConfigurationManager.AppSettings.Get("WebServerPort"));
            _webServer = Server.Start(port,ip);
        }

        private void LoggerMessageListChanged(object sender, string e) {
            if (!string.IsNullOrWhiteSpace(e)) {
                SupportUtils.DoEvents(new Action(() => {
                    _messageList.Add(e);
                    MessageList.ScrollIntoView(e);
                }));
            }
        }

        void MyNotifyIconMouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e) {
            _myNotifyIcon.Visible = false;
            this.ShowInTaskbar = true;
            this.WindowState = WindowState.Normal;
            this.Show();
            this.Activate();
        }

        private void Window_StateChanged(object sender, EventArgs e) {
            if (this.WindowState == System.Windows.WindowState.Maximized)
                this.WindowState = System.Windows.WindowState.Normal;
        }

        private void Window_Closing(object sender, CancelEventArgs e) {
            _myNotifyIcon.Visible = false;
            Logger.AddInfo("Stop web server...");
            _webServer.Dispose();
            Logger.AddInfo("Stop ping workers...");
            PingWorker.Instance.StopAll();
            Logger.AddInfo("Stop session manager...");
            SessionManager.Instance.Dispose();
        }

        private void ToTray_Click(object sender, RoutedEventArgs e) {
            this.WindowState = System.Windows.WindowState.Minimized;
            this.ShowInTaskbar = false;
            _myNotifyIcon.Visible = true;
        }

        private void Close_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) {
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void TopButton_Click(object sender, RoutedEventArgs e) {
            if (LockIcon.IsVisible) {
                LockIcon.Visibility = System.Windows.Visibility.Hidden;
                UnlockIcon.Visibility = System.Windows.Visibility.Visible;
                this.Topmost = true;
                this.ShowInTaskbar = false;
            } else {
                LockIcon.Visibility = System.Windows.Visibility.Visible;
                UnlockIcon.Visibility = System.Windows.Visibility.Hidden;
                this.Topmost = false;
                this.ShowInTaskbar = true;
            }
        }

        private void Border_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e) {
            this.DragMove();
            WindowSticking(this);
        }

        public void WindowSticking(Window win) {
            int screenH = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height;
            int screenW = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width;
            if (win.Top <= 0 || win.Left <= 0 || win.Top + win.Height >= screenH || win.Left + win.Width >= screenW) {
                if (win.Left + win.Width > screenW)
                    win.Left = screenW - win.Width;
                if (win.Top + win.Height > screenH)
                    win.Top = screenH - win.Height;
                if (win.Left < 0)
                    this.Left = 0;
                if (win.Top < 0)
                    win.Top = 0;

            }
        }
    }
}
