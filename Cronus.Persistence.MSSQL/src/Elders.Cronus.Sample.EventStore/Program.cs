﻿using System.Configuration;
using System.Reflection;
using Elders.Cronus.Persistence.MSSQL;
using Elders.Cronus.Persistence.MSSQL.Config;
using Elders.Cronus.Pipeline;
using Elders.Cronus.Pipeline.Config;
using Elders.Cronus.Pipeline.Hosts;
using Elders.Cronus.Pipeline.Transport.RabbitMQ.Config;
using Elders.Cronus.Sample.Collaboration.Users;
using Elders.Cronus.Sample.Collaboration.Users.Commands;
using Elders.Cronus.Sample.Collaboration.Users.Events;
using Elders.Cronus.Sample.IdentityAndAccess.Accounts;
using Elders.Cronus.Sample.IdentityAndAccess.Accounts.Commands;

namespace Elders.Cronus.Sample.EventStore
{
    class Program
    {
        static CronusHost host;
        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();
            DatabaseManager.DeleteDatabase(ConfigurationManager.ConnectionStrings["cronus_es"].ConnectionString);
            UseCronusHost();
            System.Console.WriteLine("Started Event store");
            System.Console.ReadLine();
            host.Stop();
        }

        static void UseCronusHost()
        {
            var cfg = new CronusSettings();
            cfg.UseContractsFromAssemblies(new Assembly[] { Assembly.GetAssembly(typeof(RegisterAccount)), Assembly.GetAssembly(typeof(CreateUser)), Assembly.GetAssembly(typeof(UserState)), Assembly.GetAssembly(typeof(AccountState)) });

            cfg
                .UsePipelineEventPublisher(publisher => publisher.UseRabbitMqTransport())
                .UsePipelineCommandPublisher(publisher => publisher.UseRabbitMqTransport())
                .UsePipelineEventStorePublisher(publisher => publisher.UseRabbitMqTransport());

            const string IAA = "IdentityAndAccess";
            cfg
                .UseMsSqlEventStore(eventStore => eventStore
                    .SetConnectionStringName("cronus_es")
                    .SetAggregateStatesAssembly(typeof(AccountState))
                    .WithNewStorageIfNotExists())
                .UseEventStoreConsumable(IAA, consumable => consumable
                    .SetNumberOfConsumers(2)
                    .UseRabbitMqTransport()
                    .EventStoreConsumer(consumer => consumer
                        .SetConsumeSuccessStrategy(new EndpointPostConsumeStrategy.EventStorePublishEventsOnSuccessPersist((cfg as IHaveEventPublisher).EventPublisher.Value))
                        .UseEventStoreHandler((cfg as IHaveEventStores).EventStores[IAA], typeof(RegisterAccount).Assembly)));
            //.SetConsumerBatchSize(100)));

            const string Collaboration = "Collaboration";
            cfg
                .UseMsSqlEventStore(eventStore => eventStore
                    .SetConnectionStringName("cronus_es")
                    .SetAggregateStatesAssembly(Assembly.GetAssembly(typeof(UserState)))
                    .WithNewStorageIfNotExists())
                .UseEventStoreConsumable(Collaboration, consumable => consumable

                    .SetNumberOfConsumers(2)
                    .UseRabbitMqTransport()
                    .EventStoreConsumer(consumer => consumer
                        .SetConsumeSuccessStrategy(new EndpointPostConsumeStrategy.EventStorePublishEventsOnSuccessPersist((cfg as IHaveEventPublisher).EventPublisher.Value))
                        .UseEventStoreHandler((cfg as IHaveEventStores).EventStores[IAA], typeof(UserCreated).Assembly)));

            host = new CronusHost(cfg.GetInstance());
            host.Start();
        }

    }
}