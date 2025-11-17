using System.Windows;
using Snoop.Client.RabbitMq;

namespace Snoop.Client
{
    public partial class LauncherWindow
    {
        public LauncherWindow()
        {
            InitializeComponent();
        }

        private void SqlServerButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            Close();
        }

        private void RabbitMqButton_Click(object sender, RoutedEventArgs e)
        {
            var rabbitMqWindow = new RabbitMqMainWindow();
            rabbitMqWindow.Show();
            Close();
        }
    }
}


