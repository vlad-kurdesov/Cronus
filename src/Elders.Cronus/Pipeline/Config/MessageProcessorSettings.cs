using System;
using System.Collections.Generic;
using Elders.Cronus.DomainModeling;
using Elders.Cronus.IocContainer;
using Elders.Cronus.MessageProcessing;

namespace Elders.Cronus.Pipeline.Config
{
    public class MessageProcessorSettings<TContract> : SettingsBuilder, IMessageProcessorSettings<TContract> where TContract : IMessage
    {
        public MessageProcessorSettings(ISettingsBuilder builder, Func<Type, bool> discriminator) : base(builder)
        {
            this.discriminator = discriminator;
        }
        private Func<Type, bool> discriminator;

        Dictionary<Type, List<Tuple<Type, Func<Type, object>>>> IMessageProcessorSettings<TContract>.HandlerRegistrations { get; set; }

        string IMessageProcessorSettings<TContract>.MessageProcessorName { get; set; }

        public override void Build()
        {
            var builder = this as ISettingsBuilder;
            var processorSettings = this as IMessageProcessorSettings<TContract>;
            Func<IMessageProcessor> messageHandlerProcessorFactory = () =>
            {
                IMessageProcessor handler = new MessageProcessor(processorSettings.MessageProcessorName, builder.Container);

                foreach (var reg in (this as IMessageProcessorSettings<TContract>).HandlerRegistrations)
                {
                    foreach (var item in reg.Value)
                    {
                        if (discriminator == null || discriminator(item.Item1))
                            handler.Subscribe(new MessageProcessorSubscription(reg.Key, item.Item1, item.Item2));
                    }
                }
                return handler;
            };
            builder.Container.RegisterSingleton<IMessageProcessor>(() => messageHandlerProcessorFactory(), builder.Name);
        }
    }

    public static class MessageProcessorWithSafeBatchSettingsExtensions
    {
        public static T UseProjections<T>(this T self, Action<MessageProcessorSettings<IEvent>> configure) where T : IConsumerSettings<IEvent>
        {
            MessageProcessorSettings<IEvent> settings = new MessageProcessorSettings<IEvent>(self, t => typeof(IProjection).IsAssignableFrom(t));
            (settings as IMessageProcessorSettings<IEvent>).MessageProcessorName = "Projections";
            if (configure != null)
                configure(settings);

            (settings as ISettingsBuilder).Build();
            return self;
        }

        public static T UsePorts<T>(this T self, Action<MessageProcessorSettings<IEvent>> configure) where T : PortConsumerSettings
        {
            MessageProcessorSettings<IEvent> settings = new MessageProcessorSettings<IEvent>(self, t => typeof(IPort).IsAssignableFrom(t));
            (settings as IMessageProcessorSettings<IEvent>).MessageProcessorName = "Ports";
            if (configure != null)
                configure(settings);

            (settings as ISettingsBuilder).Build();
            return self;
        }

        public static T UseApplicationServices<T>(this T self, Action<MessageProcessorSettings<ICommand>> configure) where T : IConsumerSettings<ICommand>
        {
            MessageProcessorSettings<ICommand> settings = new MessageProcessorSettings<ICommand>(self, t => typeof(IAggregateRootApplicationService).IsAssignableFrom(t));
            (settings as IMessageProcessorSettings<IEvent>).MessageProcessorName = "Commands";
            if (configure != null)
                configure(settings);

            (settings as ISettingsBuilder).Build();
            return self;
        }
    }
}
