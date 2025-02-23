using System;
using System.Threading;
using Couchbase.Core;
using Couchbase.Core.Configuration.Server;
using Couchbase.Core.Configuration.Server.Streaming;
using Couchbase.Core.DI;
using Couchbase.Core.IO.HTTP;
using Couchbase.IntegrationTests.Fixtures;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Couchbase.IntegrationTests.Configuration.Server.Streaming
{
    public class HttpStreamConfigListenerTests : IClassFixture<ClusterFixture>
    {
        private readonly ClusterFixture _fixture;

        public HttpStreamConfigListenerTests(ClusterFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void When_Config_Published_Subscriber_Receives_Config()
        {
            using var autoResetEvent = new AutoResetEvent(false);

            using var tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(10));

            using var context = new ClusterContext(tokenSource, _fixture.ClusterOptions);
            var httpClientFactory = context.ServiceProvider.GetRequiredService<ICouchbaseHttpClientFactory>();

            var handler = new Mock<IConfigHandler>();
            handler
                .Setup(m => m.Publish(It.IsAny<BucketConfig>()))
                .Callback((BucketConfig config) =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    autoResetEvent.Set();
                });

            using var listener = new HttpStreamingConfigListener("default", _fixture.ClusterOptions, httpClientFactory, handler.Object,
                new Mock<ILogger<HttpStreamingConfigListener>>().Object);

            listener.StartListening();
            Assert.True(autoResetEvent.WaitOne(TimeSpan.FromSeconds(5)));
        }
    }
}
