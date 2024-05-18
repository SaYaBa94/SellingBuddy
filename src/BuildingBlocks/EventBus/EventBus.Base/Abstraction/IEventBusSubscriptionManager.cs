using EventBus.Base.Events;
using System;
using System.Collections.Generic;

namespace EventBus.Base.Abstraction
{
    public interface IEventBusSubscriptionManager
    {
        public bool IsEmpty { get; }

        event EventHandler<string> OnEventRemoved;

        void AddSubscription<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;
        void RemoveSubscription<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;
        bool HasSubscriptionsForEvent<T>() where T : IntegrationEvent;
        bool HasSubscriptionsForEvent(string eventName);
        Type GetEventTypeByName(string eventName);
        void Clear();
        IEnumerable<SubscriptionInfo> GetHandlerForEvent<T>() where T : IntegrationEvent;
        IEnumerable<SubscriptionInfo> GetHandlerForEvent(string eventName); 
        string GetEventKey<T>();
    }
}
