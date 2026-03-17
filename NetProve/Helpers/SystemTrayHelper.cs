using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using NetProve.Localization;

namespace NetProve.Helpers
{
    /// <summary>
    /// Manages the system tray icon and context menu.
    /// Allows the app to minimize to tray and run in background.
    /// </summary>
    public sealed class SystemTrayHelper : IDisposable
    {
        private NotifyIcon? _notifyIcon;
        private readonly Window _mainWindow;
        private readonly LocalizationManager _loc = LocalizationManager.Instance;
        private bool _isExiting;

        public SystemTrayHelper(Window mainWindow)
        {
            _mainWindow = mainWindow;
            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Text = "NetProve — Performance Optimizer",
                Visible = false
            };

            // Try to load application icon, fallback to system icon
            try
            {
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(iconPath))
                    _notifyIcon.Icon = new Icon(iconPath);
                else
                    _notifyIcon.Icon = SystemIcons.Application;
            }
            catch
            {
                _notifyIcon.Icon = SystemIcons.Application;
            }

            // Context menu
            var menu = new ContextMenuStrip();

            var showItem = new ToolStripMenuItem("Show NetProve");
            showItem.Click += (s, e) => RestoreWindow();
            showItem.Font = new Font(showItem.Font, System.Drawing.FontStyle.Bold);
            menu.Items.Add(showItem);

            menu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => ExitApplication();
            menu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.DoubleClick += (s, e) => RestoreWindow();
        }

        /// <summary>
        /// Minimizes the window to system tray.
        /// </summary>
        public void MinimizeToTray()
        {
            _mainWindow.Hide();
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = true;
                _notifyIcon.ShowBalloonTip(
                    2000,
                    "NetProve",
                    _loc["AppRunning"],
                    ToolTipIcon.Info);
            }
        }

        /// <summary>
        /// Restores the window from system tray.
        /// </summary>
        public void RestoreWindow()
        {
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
            if (_notifyIcon != null)
                _notifyIcon.Visible = false;
        }

        /// <summary>
        /// Forces the application to exit (bypassing minimize-to-tray).
        /// </summary>
        public void ExitApplication()
        {
            _isExiting = true;
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
            System.Windows.Application.Current.Shutdown();
        }

        /// <summary>
        /// Whether the app is in the process of exiting (not just minimizing).
        /// </summary>
        public bool IsExiting => _isExiting;

        /// <summary>
        /// Updates the context menu labels based on current language.
        /// </summary>
        public void UpdateLanguage()
        {
            if (_notifyIcon?.ContextMenuStrip == null) return;
            var items = _notifyIcon.ContextMenuStrip.Items;
            if (items.Count >= 3)
            {
                items[0].Text = $"NetProve — {_loc["Show"]}";
                items[2].Text = _loc["Exit"];
            }
        }

        public void Dispose()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
        }
    }
}
