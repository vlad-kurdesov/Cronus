﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Elders.Cronus.MessageProcessing
{
    public class SubscriptionMiddleware
    {
        ConcurrentBag<ISubscriber> subscribers;

        public SubscriptionMiddleware()
        {
            subscribers = new ConcurrentBag<ISubscriber>();
        }

        public void Subscribe(ISubscriber subscriber)
        {
            if (ReferenceEquals(null, subscriber)) throw new ArgumentNullException(nameof(subscriber));
            if (ReferenceEquals(null, subscriber.MessageTypes)) throw new ArgumentNullException(nameof(subscriber.MessageTypes));
            if (subscriber.MessageTypes.Any() == false) throw new ArgumentException($"Subscirber '{subscriber.Id}' does not care about any message types. Any reason?");
            if (subscribers.Any(x => x.Id == subscriber.Id)) throw new ArgumentException($"There is already subscriber with id '{subscriber.Id}'");

            subscribers.Add(subscriber);
        }

        public IEnumerable<ISubscriber> GetInterestedSubscribers(CronusMessage message)
        {
            return Subscribers.Where(subscriber => subscriber.MessageTypes.Contains(message.Payload.GetType()));
        }

        public IEnumerable<ISubscriber> Subscribers { get { return subscribers.ToList(); } }

        public void UnsubscribeAll()
        {
            subscribers = new ConcurrentBag<ISubscriber>();
        }
    }
}
