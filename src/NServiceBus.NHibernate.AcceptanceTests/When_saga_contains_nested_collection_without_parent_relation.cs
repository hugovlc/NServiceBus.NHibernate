﻿namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    // When_saga_contains_nested_collection_without_parent_relation - name getting too long for MSMQ
    public class When_saga_w_nested_coll_no_parent_rel : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_complete()
        {
            var result = await Scenario.Define<Context>(c => c.Id = Guid.NewGuid())
                .WithEndpoint<NHNestedCollNoParentRelationEP>(b => b.When((bus, context) => bus.SendLocal(new Message1 { SomeId = context.Id })))
                .Done(c => c.SagaCompleted)
                .Run();

            Assert.IsTrue(result.SagaCompleted);
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public bool SagaCompleted { get; set; }
        }

        public class NHNestedCollNoParentRelationEP : EndpointConfigurationBuilder
        {
            public NHNestedCollNoParentRelationEP()
            {
                EndpointSetup<DefaultServer>();
            }

            public class NHNestedCollectionWithoutParentRelationSaga : Saga<NHNestedCollectionWithoutParentRelationSagaData>, IHandleMessages<Message2>, IAmStartedByMessages<Message1>
            {
                public Task Handle(Message1 message, IMessageHandlerContext context)
                {
                    Data.SomeId = message.SomeId;
                    Data.RelatedData = new List<ChildData>
                    {
                        new ChildData
                        {
                            Name = "Foo1"
                        },
                        new ChildData
                        {
                            Name = "Foo2"
                        },
                        new ChildData
                        {
                            Name = "Foo3"
                        },
                        new ChildData
                        {
                            Name = "Foo4"
                        },
                    };

                    return context.SendLocal(new Message2
                    {
                        SomeId = message.SomeId
                    });
                }

                public Task Handle(Message2 message, IMessageHandlerContext context)
                {
                    MarkAsComplete();
                    return context.SendLocal(new SagaCompleted());
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<NHNestedCollectionWithoutParentRelationSagaData> mapper)
                {
                    mapper.MapSaga(s => s.SomeId)
                        .ToMessage<Message2>(m => m.SomeId)
                        .ToMessage<Message1>(m => m.SomeId);
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

        public class NHNestedCollectionWithoutParentRelationSagaData : ContainSagaData
        {
            public virtual Guid SomeId { get; set; }
            public virtual IList<ChildData> RelatedData { get; set; }
        }

        [Serializable]
        public class Message2 : IMessage
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
            public virtual string Name { get; set; }
        }

        [Serializable]
        public class SagaCompleted : IMessage
        {
        }

    }
}