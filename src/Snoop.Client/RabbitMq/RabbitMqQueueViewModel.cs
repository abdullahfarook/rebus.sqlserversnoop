using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;

namespace Snoop.Client.RabbitMq
{
    public class RabbitMqQueueViewModel : BindableBase
    {
        private readonly string _connectionString;
        public DelegateCommand<MessageViewModel> DeleteMessageCommand { get; set; }
        public DelegateCommand<MessageViewModel> ReturnToSourceQueueCommand { get; set; }
        public DelegateCommand<MessageViewModel> SetVisibleNowCommand { get; set; }

        private readonly RabbitMqService _rabbitMqService;
        
        public RabbitMqQueueViewModel(string queueName, uint messageCount, uint consumerCount, string connectionString)
        {
            _connectionString = connectionString;
            QueueName = queueName;
            MessageCount = (int)messageCount;
            ConsumerCount = (int)consumerCount;
            Messages = new ObservableCollection<MessageViewModel>();
            _rabbitMqService = new RabbitMqService();
            DeleteMessageCommand = new DelegateCommand<MessageViewModel>(DeleteMessage);
            ReturnToSourceQueueCommand = new DelegateCommand<MessageViewModel>(ReturnToSourceQueue);
            SetVisibleNowCommand = new DelegateCommand<MessageViewModel>(SetVisibleNow);
        }

        public RabbitMqQueueViewModel()
        {
            Messages = new ObservableCollection<MessageViewModel>();
            _rabbitMqService = new RabbitMqService();
            DeleteMessageCommand = new DelegateCommand<MessageViewModel>(DeleteMessage);
            ReturnToSourceQueueCommand = new DelegateCommand<MessageViewModel>(ReturnToSourceQueue);
            SetVisibleNowCommand = new DelegateCommand<MessageViewModel>(SetVisibleNow);
        }

        private async void DeleteMessage(MessageViewModel messageViewModel)
        {
            if (SelectedMessage == messageViewModel)
            {
                SelectedMessage = null;
            }
            
            var success = await _rabbitMqService.DeleteMessageAsync(QueueName, messageViewModel.Id.ToString());
            if (success)
            {
                Messages.Remove(messageViewModel);
                MessageCount--;
            }
        }

        private async void ReturnToSourceQueue(MessageViewModel messageViewModel)
        {
            if (QueueName == messageViewModel.SourceQueue) return;

            if (SelectedMessage == messageViewModel)
            {
                SelectedMessage = null;
            }
            
            var success = await _rabbitMqService.ReturnMessageToSourceQueueAsync(QueueName, messageViewModel.SourceQueue, messageViewModel);
            if (success)
            {
                await ReloadMessagesAsync();
            }
        }

        private void SetVisibleNow(MessageViewModel messageViewModel)
        {
            // RabbitMQ doesn't have visibility concept, so this is a no-op
            // Kept for interface compatibility
        }

        private string _queueName;
        public string QueueName
        {
            get => _queueName;
            set => SetProperty(ref _queueName, value);
        }

        private int _messageCount;
        public int MessageCount
        {
            get => _messageCount;
            set => SetProperty(ref _messageCount, value);
        }

        private int _consumerCount;
        public int ConsumerCount
        {
            get => _consumerCount;
            set => SetProperty(ref _consumerCount, value);
        }

        private ObservableCollection<MessageViewModel> _messages;
        public ObservableCollection<MessageViewModel> Messages
        {
            get => _messages;
            set => SetProperty(ref _messages, value);
        }

        public async void LoadMessages()
        {
            await ReloadMessagesAsync();
        }

        public async Task ReloadMessagesAsync()
        {
            Messages = new ObservableCollection<MessageViewModel>(await _rabbitMqService.GetMessagesAsync(QueueName));
            MessageCount = Messages.Count;
        }

        public void ReloadMessages()
        {
            ReloadMessagesAsync().GetAwaiter().GetResult();
        }

        private MessageViewModel _selectedMessage;
        public MessageViewModel SelectedMessage
        {
            get => _selectedMessage;
            set => SetProperty(ref _selectedMessage, value);
        }

        public async void Purge()
        {
            var success = await _rabbitMqService.PurgeQueueAsync(QueueName);
            if (success)
            {
                await ReloadMessagesAsync();
            }
        }

        public async Task ReturnAllToSourceQueue()
        {
            await ReloadMessagesAsync();

            var messages = Messages.ToList();
            int messageCount = messages.Count;

            foreach (var messageViewModel in messages)
            {
                var success = await _rabbitMqService.ReturnMessageToSourceQueueAsync(QueueName, messageViewModel.SourceQueue, messageViewModel);
                if (success)
                {
                    messageCount--;
                    MessageCount = messageCount;
                }
            }

            await ReloadMessagesAsync();
        }

        public override string ToString()
        {
            return $"{QueueName} ({MessageCount} messages, {ConsumerCount} consumers)";
        }
    }
}
