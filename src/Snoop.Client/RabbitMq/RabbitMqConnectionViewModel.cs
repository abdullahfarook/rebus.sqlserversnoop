using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;
using Prism.Commands;
using Prism.Mvvm;
using Snoop.Client.RabbitMq;

namespace Snoop.Client.RabbitMq
{
    public class RabbitMqConnectionViewModel : BindableBase
    {
        private readonly RabbitMqService _rabbitMqService;

        private string _connectionString;
        private bool _isEditing;
        private int _numberOfMessages;
        private RabbitMqQueueViewModel _selectedQueue;
        private ObservableCollection<RabbitMqQueueViewModel> _queues;

        public DelegateCommand<RabbitMqQueueViewModel> RemoveCommand { get; set; }
        public DelegateCommand<RabbitMqQueueViewModel> ReloadMessagesCommand { get; set; }
        public DelegateCommand<RabbitMqQueueViewModel> PurgeCommand { get; set; }
        public DelegateCommand<RabbitMqQueueViewModel> ReturnAllToSourceQueueCommand { get; set; }
        public DelegateCommand EditCommand { get; set; }
        public DelegateCommand SaveCommand { get; set; }

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public ObservableCollection<RabbitMqQueueViewModel> Queues
        {
            get => _queues;
            set => SetProperty(ref _queues, value);
        }

        public RabbitMqQueueViewModel SelectedQueue
        {
            get => _selectedQueue;
            set
            {
                if (SetProperty(ref _selectedQueue, value)) TryGetMessages();
            }
        }

        [UsedImplicitly]
        public int NumberOfMessages
        {
            get => _numberOfMessages;
            set => SetProperty(ref _numberOfMessages, value);
        }

        public string ConnectionString
        {
            get => _connectionString;
            set => SetProperty(ref _connectionString, value);
        }

        public RabbitMqConnectionViewModel()
        {
            Queues = new ObservableCollection<RabbitMqQueueViewModel>();
            RemoveCommand = new DelegateCommand<RabbitMqQueueViewModel>(Remove);
            ReloadMessagesCommand = new DelegateCommand<RabbitMqQueueViewModel>(ReloadMessages);
            PurgeCommand = new DelegateCommand<RabbitMqQueueViewModel>(PurgeMessages);
            ReturnAllToSourceQueueCommand = new DelegateCommand<RabbitMqQueueViewModel>(ReturnAllMessagesToSourceQueue);
            EditCommand = new DelegateCommand(Edit);
            SaveCommand = new DelegateCommand(Save);

            _rabbitMqService = new RabbitMqService();
        }

        private void Save()
        {
            IsEditing = false;
            LoadQueues();
        }

        private void Edit()
        {
            IsEditing = true;
        }

        public async void LoadQueues()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString)) return;
            if (Queues.Any()) return;

            var queueModels = await _rabbitMqService.GetQueuesAsync();
            var queueViewModels = queueModels.Select(q => new RabbitMqQueueViewModel(q.QueueName, q.MessageCount, q.ConsumerCount, q.ConnectionString));
            Queues = new ObservableCollection<RabbitMqQueueViewModel>(queueViewModels);
        }

        private async void ReturnAllMessagesToSourceQueue(RabbitMqQueueViewModel obj)
        {
            await obj.ReturnAllToSourceQueue();
        }

        private void PurgeMessages(RabbitMqQueueViewModel obj)
        {
            obj.Purge();
        }

        private void ReloadMessages(RabbitMqQueueViewModel obj)
        {
            obj.ReloadMessages();
        }

        private void Remove(RabbitMqQueueViewModel queueViewModel)
        {
            Queues.Remove(queueViewModel);
            SelectedQueue = null;
        }

        private void TryGetMessages()
        {
            SelectedQueue?.LoadMessages();
        }
    }
}