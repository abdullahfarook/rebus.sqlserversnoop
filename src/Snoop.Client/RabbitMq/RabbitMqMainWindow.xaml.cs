using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace Snoop.Client.RabbitMq
{
    public partial class RabbitMqMainWindow
    {
        private RabbitMqMainWindowViewModel _viewModel;
        private const string filePath = "rabbitmqsnoopusersettings.txt";
        
        public RabbitMqMainWindow()
        {
            InitializeComponent();

            DataContext = _viewModel = new RabbitMqMainWindowViewModel();

            var settings = ReadSettings();
            if(settings.Any()) _viewModel.Connections.Clear();

            settings.ForEach(x => _viewModel.Connections.Add(new RabbitMqConnectionViewModel(){ConnectionString = x}));

            Closing += OnClosing;
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            SaveSettings();
        }

        private List<string> ReadSettings()
        {
            if(!File.Exists(filePath)) return new List<string>();

            return File.ReadAllLines(filePath).ToList();
        }

        private void SaveSettings()
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using (var file = File.Create(filePath))
            using (var writer = new StreamWriter(file))
            {
                foreach (var connection in _viewModel.Connections)
                {
                    writer.WriteLine(connection.ConnectionString);
                }

                writer.Flush();
            }
        }

        private void UIElement_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox {Visibility: Visibility.Visible} textBox)
                textBox.Focus();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel.OnLoadedCommand.Execute();
        }
    }
}


