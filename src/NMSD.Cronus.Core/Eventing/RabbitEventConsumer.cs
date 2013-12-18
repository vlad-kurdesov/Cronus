using System;
using System.IO;
using NMSD.Cronus.Core.Eventing;
using NMSD.Cronus.Core.Messaging;
using NMSD.Cronus.Core.Multithreading.Work;
using NMSD.Cronus.Core.UnitOfWork;
using NSMD.Cronus.RabbitMQ;
using NMSD.Protoreg;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace NMSD.Cronus.Core.Eventing
{
    public class RabbitEventConsumer : RabbitConsumer<IEvent, IMessageHandler>
    {
        private Plumber plumber;
        private WorkPool pool;

        private readonly ProtoregSerializer serialiser;

        public RabbitEventConsumer(ProtoregSerializer serialiser)
        {
            this.serialiser = serialiser;
            queueFactory = new QueuePerHandlerFactory();
        }

        public override void Start(int numberOfWorkers)
        {
            plumber = new Plumber();

            //  Think about this
            queueFactory.Compile(plumber.RabbitConnection);
            // Think about this

            pool = new WorkPool("defaultPool", numberOfWorkers);
            foreach (var queueInfo in queueFactory.Headers)
            {
                pool.AddWork(new ConsumerWork(this, queueInfo.Key));
            }

            pool.StartCrawlers();
        }

        public override void Stop()
        {
            pool.Stop();
        }


        private class ConsumerWork : IWork
        {
            private RabbitEventConsumer consumer;
            private readonly string queueName;

            public ConsumerWork(RabbitEventConsumer consumer, string queueName)
            {
                this.queueName = queueName;
                this.consumer = consumer;
            }

            public DateTime ScheduledStart { get; set; }

            public void Start()
            {
                try
                {
                    using (var channel = consumer.plumber.RabbitConnection.CreateModel())//ConnectionClosedException??
                    {
                        QueueingBasicConsumer newQueueingBasicConsumer = new QueueingBasicConsumer();
                        channel.BasicConsume(queueName, false, newQueueingBasicConsumer);

                        while (true)
                        {
                            for (int i = 0; i < 100; i++)
                            {
                                StartNewUnitOfWork();

                                try
                                {
                                    var rawMessage = newQueueingBasicConsumer.Queue.DequeueNoWait(null);
                                    IEvent message;
                                    if (rawMessage != null)
                                    {
                                        using (var stream = new MemoryStream(rawMessage.Body))
                                        {
                                            message = consumer.serialiser.Deserialize(stream) as IEvent;
                                        }

                                        if (consumer.Handle(message, unitOfWork))
                                            channel.BasicAck(rawMessage.DeliveryTag, false);
                                    }
                                }
                                catch (EndOfStreamException)
                                {
                                    ScheduledStart = DateTime.UtcNow.AddMilliseconds(1000);
                                    break;
                                }
                                catch (AlreadyClosedException)
                                {
                                    ScheduledStart = DateTime.UtcNow.AddMilliseconds(1000);
                                    break;
                                }
                                catch (OperationInterruptedException)
                                {
                                    ScheduledStart = DateTime.UtcNow.AddMilliseconds(1000);
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    throw ex;
                                }
                                EndUnitOfWork();
                            }
                        }


                    }
                }
                catch (OperationInterruptedException)
                {
                    ScheduledStart = DateTime.UtcNow.AddMilliseconds(1000);

                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            public IUnitOfWorkPerBatch unitOfWork;
            public void StartNewUnitOfWork()
            {
                unitOfWork = consumer.UnitOfWorkFactory.NewBatch();
                if (unitOfWork != null)
                {
                    unitOfWork.Begin();
                }
            }
            public void EndUnitOfWork()
            {
                if (unitOfWork != null)
                {
                    unitOfWork.Commit();
                    unitOfWork.Dispose();
                    unitOfWork = null;
                }
            }
        }
    }
}