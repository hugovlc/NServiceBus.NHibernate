﻿namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using SagaPersisters.NHibernate;

    public class When_saga_contains_nested_collection_with_row_version : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_persist_correctly()
        {
            var result = await Scenario.Define<Context>(c => c.Id = Guid.NewGuid())
                .WithEndpoint<NHNestedCollRowVerEP>(b => b.When(async (bus, context) =>
                {
                    await bus.SendLocal(new Message1
                    {
                        SomeId = context.Id
                    }).ConfigureAwait(false);
                    await bus.SendLocal(new Message2
                    {
                        SomeId = context.Id
                    }).ConfigureAwait(false);
                    await bus.SendLocal(new Message3
                    {
                        SomeId = context.Id
                    }).ConfigureAwait(false);
                }))
                .Done(c => c.SagaCompleted)
                .Run();

            Assert.IsTrue(result.SagaCompleted);
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public bool SagaCompleted { get; set; }
        }

        public class NHNestedCollRowVerEP : EndpointConfigurationBuilder
        {
            public NHNestedCollRowVerEP()
            {
                EndpointSetup<DefaultServer>(c => c.LimitMessageProcessingConcurrencyTo(1));
            }

            public class NHNestedCollectionWithRowVersionSaga : Saga<NHNestedColRowVerSagaData>, IAmStartedByMessages<Message2>, IAmStartedByMessages<Message3>, IAmStartedByMessages<Message1>
            {
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<NHNestedColRowVerSagaData> mapper)
                {
                    mapper.MapSaga(s => s.SomeId)
                        .ToMessage<Message2>(m => m.SomeId)
                        .ToMessage<Message3>(m => m.SomeId)
                        .ToMessage<Message1>(m => m.SomeId);
                }

                Task PerformSagaCompletionCheck(IMessageHandlerContext context)
                {
                    if (Data.RelatedData == null)
                    {
                        Data.RelatedData = new List<ChildData>
                                       {
                                           new ChildData{NHNestedColRowVerSagaData = Data},
                                           new ChildData{NHNestedColRowVerSagaData = Data},
                                           new ChildData{NHNestedColRowVerSagaData = Data}
                                       };
                    }

                    if (Data.MessageOneReceived && Data.MessageTwoReceived && Data.MessageThreeReceived)
                    {
                        MarkAsComplete();
                        return context.SendLocal(new SagaCompleted());
                    }
                    return Task.FromResult(0);
                }

                public Task Handle(Message1 message, IMessageHandlerContext context)
                {
                    Data.SomeId = message.SomeId;
                    Data.MessageOneReceived = true;
                    return PerformSagaCompletionCheck(context);
                }
                public Task Handle(Message2 message, IMessageHandlerContext context)
                {
                    Data.SomeId = message.SomeId;
                    Data.MessageTwoReceived = true;
                    return PerformSagaCompletionCheck(context);
                }
                public Task Handle(Message3 message, IMessageHandlerContext context)
                {
                    Data.SomeId = message.SomeId;
                    Data.MessageThreeReceived = true;
                    return PerformSagaCompletionCheck(context);
                }

            }
        }

        public class CompletionHandler : IHandleMessages<SagaCompleted>
        {
            Context testContext;

            public CompletionHandler(Context testContext)
            {
                this.testContext = testContext;
            }

            public Task Handle(SagaCompleted message, IMessageHandlerContext context)
            {
                testContext.SagaCompleted = true;

                return Task.FromResult(0);
            }
        }

#pragma warning disable NSB0012
        public class NHNestedColRowVerSagaData : IContainSagaData
        {
            [RowVersion]
            public virtual DateTime Version { get; set; }
            public virtual Guid Id { get; set; }
            public virtual string Originator { get; set; }
            public virtual string OriginalMessageId { get; set; }
            public virtual Guid SomeId { get; set; }
            public virtual bool MessageTwoReceived { get; set; }
            public virtual bool MessageOneReceived { get; set; }
            public virtual bool MessageThreeReceived { get; set; }
            public virtual IList<ChildData> RelatedData { get; set; }
        }
#pragma warning restore

        [Serializable]
        public class Message2 : IMessage
        {
            public Guid SomeId { get; set; }
        }

        [Serializable]
        public class Message3 : IMessage
        {
            public Guid SomeId { get; set; }
        }

        [Serializable]
        public class Message1 : IMessage
        {
            public Guid SomeId { get; set; }
        }

        public class ChildData
        {
            public virtual Guid Id { get; set; }
            public virtual NHNestedColRowVerSagaData NHNestedColRowVerSagaData { get; set; }
        }
        [Serializable]
        public class SagaCompleted : IMessage
        {
        }
    }


}