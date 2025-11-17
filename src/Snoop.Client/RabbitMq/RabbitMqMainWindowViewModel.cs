using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Prism.Commands;
using Prism.Mvvm;

namespace Snoop.Client.RabbitMq
{
    public class RabbitMqMainWindowViewModel : BindableBase
    {
        public DelegateCommand AddCommand { get; set; }
        public DelegateCommand<RabbitMqConnectionViewModel> DeleteCommand { get; set; }
        public DelegateCommand OnLoadedCommand { get; }

        public RabbitMqMainWindowViewModel()
        {
            Connections = new ObservableCollection<RabbitMqConnectionViewModel>();
            AddCommand = new DelegateCommand(Add);
            DeleteCommand = new DelegateCommand<RabbitMqConnectionViewModel>(Delete);
            OnLoadedCommand = new DelegateCommand(OnLoaded);
        }

        private void OnLoaded()
        {
            if (!Connections.Any())
                Add();
        }

        private void Add()
        {
            var connectionViewModel = new RabbitMqConnectionViewModel
            {
                IsEditing = true,
                ConnectionString = "amqp://guest:guest@localhost:5672"
            };
            Connections.Add(connectionViewModel);
            SelectedConnection = connectionViewModel;
        }

        private void Delete(RabbitMqConnectionViewModel connectionViewModel)
        {
            Connections.Remove(connectionViewModel);
        }

        private ObservableCollection<RabbitMqConnectionViewModel> _connections;
        public ObservableCollection<RabbitMqConnectionViewModel> Connections
        {
            get => _connections;
            set => SetProperty(ref _connections, value);
        }

        private RabbitMqConnectionViewModel _selectedConnection;
        public RabbitMqConnectionViewModel SelectedConnection
        {
            get => _selectedConnection;
            set => SetProperty(ref _selectedConnection, value);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            base.OnPropertyChanged(args);

            if (args.PropertyName == nameof(SelectedConnection))
                SelectedConnection?.LoadQueues();
        }
    }
}


