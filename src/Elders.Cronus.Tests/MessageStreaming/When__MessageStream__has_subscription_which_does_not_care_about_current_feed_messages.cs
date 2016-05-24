using System.Collections.Generic;
using System.Linq;
using Elders.Cronus.DomainModeling;
using Elders.Cronus.Tests.TestModel;
using Machine.Specifications;
using Elders.Cronus.MessageProcessingMiddleware;

namespace Elders.Cronus.Tests.MessageStreaming
{
    [Subject("")]
    public class When__MessageStream__has_subscription_which_does_not_care_about_current_feed_messages
    {
        Establish context = () =>
            {
                handlerFacotry = new CalculatorHandlerFactory();

                var messageHandlerMiddleware = new MessageHandlerMiddleware(handlerFacotry);
                var messageSubscriptionMiddleware = new MessageSubscriptionsMiddleware();
                var transportMiddleware = new TransportMessageProcessorMiddleware(messageSubscriptionMiddleware);
                messageStream = new CronusMessageProcessorMiddleware("test", transportMiddleware);


                var subscription1 = new TestSubscriber(typeof(CalculatorNumber1), typeof(StandardCalculatorAddHandler), messageHandlerMiddleware);
                var subscription2 = new TestSubscriber(typeof(CalculatorNumber1), typeof(ScientificCalculatorHandler), messageHandlerMiddleware);
                var subscription3 = new TestSubscriber(typeof(CalculatorNumber2), typeof(StandardCalculatorSubstractHandler), messageHandlerMiddleware);

                messages = new List<TransportMessage>();
                for (int i = 1; i < numberOfMessages + 1; i++)
                {
                    messages.Add(new TransportMessage(new Message(new CalculatorNumber2(i))));
                }
                messageSubscriptionMiddleware.Subscribe(subscription1);
                messageSubscriptionMiddleware.Subscribe(subscription2);
                messageSubscriptionMiddleware.Subscribe(subscription3);
            };

        Because of = () =>
            {
                feedResult = messageStream.Invoke(messages);
            };

        It should_feed_only_interested_handlers = () => handlerFacotry.State.Total.ShouldEqual(Enumerable.Range(1, numberOfMessages).Sum() * -1);
        It should_report_about_all_successes = () => feedResult.SuccessfulMessages.Count().ShouldEqual(numberOfMessages);

        static IFeedResult feedResult;
        static int numberOfMessages = 100;
        static CronusMessageProcessorMiddleware messageStream;
        static List<TransportMessage> messages;
        static CalculatorHandlerFactory handlerFacotry;
    }
}
