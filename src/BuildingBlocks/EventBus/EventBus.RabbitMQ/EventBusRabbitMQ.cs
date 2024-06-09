using EventBus.Base;
using EventBus.Base.Events;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.RabbitMQ
{
    public class EventBusRabbitMQ : BaseEventBus
    {
        private readonly IConnectionFactory connectionFactory;
        RabbitMQPersistentConnection persistentConnection;
        private readonly IModel consumerChannel;
        public EventBusRabbitMQ(IServiceProvider serviceProvider, EventBusConfig config) : base(serviceProvider, config)
        {
            if (config.Connection != null)
            {
                var connJson = JsonConvert.SerializeObject(EventBusConfig.Connection, new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                });
                connectionFactory = JsonConvert.DeserializeObject<ConnectionFactory>(connJson);

            }
            else
            {
                connectionFactory = new ConnectionFactory();
            }
            persistentConnection = new RabbitMQPersistentConnection(connectionFactory, config.ConnectionRetryCount);
            consumerChannel = CreateConsumerChannel();
            SubsManager.OnEventRemoved += SubsManager_OnEventRemoved;

        }

        private void SubsManager_OnEventRemoved(object sender, string eventName)
        {
            eventName=ProcessEventName(eventName);
            if (!persistentConnection.IsConnected)
            {
                persistentConnection.TryConnect();
            }
            consumerChannel.QueueUnbind(eventName, EventBusConfig.DefaultTopicName, eventName);
            if(SubsManager.IsEmpty)
            {
                consumerChannel.Close();
            }
        }

        public override void Publish(IntegrationEvent @event)
        {
           if (!persistentConnection.IsConnected)
            {
                persistentConnection.TryConnect();
            }
            var policy = Policy.Handle<BrokerUnreachableException>()
                 .Or<SocketException>()
                 .WaitAndRetry(EventBusConfig.ConnectionRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) => { });

            var eventName=@event.GetType().Name;
            eventName = ProcessEventName(eventName);

            consumerChannel.ExchangeDeclare(EventBusConfig.DefaultTopicName, "direct");

            var message=JsonConvert.SerializeObject(@event);
            var body=Encoding.UTF8.GetBytes(message);

            policy.Execute(() =>
            {
                var properties = consumerChannel.CreateBasicProperties();
                properties.DeliveryMode = 2;

                consumerChannel.QueueDeclare(GetSubName(eventName), true, false, false, null);
                consumerChannel.BasicPublish(EventBusConfig.DefaultTopicName, eventName, true, properties, body);
            });
        }

        public override void Subscribe<T, TH>()
        {
            var eventName = typeof(T).Name;
            eventName = ProcessEventName(eventName);

            if (!SubsManager.HasSubscriptionsForEvent(eventName))
            {
                if (!persistentConnection.IsConnected)
                {
                    persistentConnection.TryConnect();
                }

                consumerChannel.QueueDeclare(GetSubName(eventName), true, false, false, null);
                consumerChannel.QueueBind(GetSubName(eventName), EventBusConfig.DefaultTopicName, eventName);

            }
            SubsManager.AddSubscription<T, TH>();
            StartBasicConsume(eventName);
        }

        public override void UnSubscribe<T, TH>()
        {
            SubsManager.RemoveSubscription<T, TH>();
        }

        private IModel CreateConsumerChannel()
        {
            if (!persistentConnection.IsConnected)
            {
                persistentConnection.TryConnect();
            }
            var channel = persistentConnection.CreateModel();

            channel.ExchangeDeclare(EventBusConfig.DefaultTopicName, "direct");
            return channel;
        }

        private void StartBasicConsume(string eventName)
        {
            if (consumerChannel != null)
            {
                var consumer = new EventingBasicConsumer(consumerChannel);
                consumer.Received += Consumer_Received;

                consumerChannel.BasicConsume(GetSubName(eventName), false, consumer);
            }
        }

        private async void Consumer_Received(object sender, BasicDeliverEventArgs e)
        {
            var eventName = e.RoutingKey;
            eventName = ProcessEventName(eventName);
            var message = Encoding.UTF8.GetString(e.Body.Span);

            try
            {
                await ProcessEvent(eventName, message);
            }
            catch (Exception ex)
            {
                //logging
            }
            consumerChannel.BasicAck(e.DeliveryTag, false);
        }
    }
}
