// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MqttRoundtripTests.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   A class to test the broker and the two clients against each other, with the objects the form uses.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MQTTnet.TestApp.WinForm.Tests;

/// <summary>
/// A class to test the broker and the two clients against each other, with the objects the form uses. The tests bind
/// a real socket on the loopback interface, they need no network and no broker outside the test process.
/// </summary>
[TestClass]
public class MqttRoundtripTests
{
    /// <summary>
    /// The topic both text boxes of the form are filled with.
    /// </summary>
    private const string Topic = "brand/type/group/code";

    /// <summary>
    /// The time a test waits for a message before it counts as lost.
    /// </summary>
    private static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Checks whether a message published by the publisher arrives at the subscriber, which is what the form does
    /// when the three start buttons, the subscribe button and the publish button are pressed.
    /// </summary>
    [TestMethod]
    [Timeout(60000)]
    public async Task ThePublishedMessageArrivesAtTheSubscriber()
    {
        var port = GetFreeTcpPort();
        using var server = CreateStartedServer(port, out var startTask);
        await startTask;

        var factory = new MqttClientFactory();
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var subscriber = factory.CreateMqttClient();
        subscriber.ApplicationMessageReceivedAsync += args =>
        {
            received.TrySetResult(args.ApplicationMessage.ConvertPayloadToString() ?? string.Empty);
            return Task.CompletedTask;
        };

        await subscriber.ConnectAsync(MqttObjectFactory.CreateClientOptions("ClientSubscriber", port));
        Assert.IsTrue(subscriber.IsConnected);

        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(new MqttTopicFilter { Topic = Topic })
            .Build();
        await subscriber.SubscribeAsync(subscribeOptions);

        using var publisher = factory.CreateMqttClient();
        await publisher.ConnectAsync(MqttObjectFactory.CreateClientOptions("ClientPublisher", port));
        Assert.IsTrue(publisher.IsConnected);

        var payload = MessageFormatter.CreateGeneratedMessage(DateTimeOffset.Now);
        var publishResult = await publisher.PublishAsync(MqttObjectFactory.CreateApplicationMessage(Topic, payload));
        Assert.IsTrue(publishResult.IsSuccess);

        var finished = await Task.WhenAny(received.Task, Task.Delay(ReceiveTimeout));
        Assert.AreSame(received.Task, finished, "The subscriber did not receive the published message.");
        Assert.AreEqual(payload, await received.Task);

        await server.StopAsync();
    }

    /// <summary>
    /// Checks whether the retained flag does its work: a subscriber that connects after the message was published
    /// still gets it.
    /// </summary>
    [TestMethod]
    [Timeout(60000)]
    public async Task AMessagePublishedBeforeTheSubscriptionIsStillDelivered()
    {
        var port = GetFreeTcpPort();
        using var server = CreateStartedServer(port, out var startTask);
        await startTask;

        var factory = new MqttClientFactory();

        using var publisher = factory.CreateMqttClient();
        await publisher.ConnectAsync(MqttObjectFactory.CreateClientOptions("ClientPublisher", port));

        var payload = MessageFormatter.CreateGeneratedMessage(DateTimeOffset.Now);
        await publisher.PublishAsync(MqttObjectFactory.CreateApplicationMessage(Topic, payload));

        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscriber = factory.CreateMqttClient();
        subscriber.ApplicationMessageReceivedAsync += args =>
        {
            received.TrySetResult(args.ApplicationMessage.ConvertPayloadToString() ?? string.Empty);
            return Task.CompletedTask;
        };

        await subscriber.ConnectAsync(MqttObjectFactory.CreateClientOptions("ClientSubscriber", port));
        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(new MqttTopicFilter { Topic = Topic })
            .Build();
        await subscriber.SubscribeAsync(subscribeOptions);

        var finished = await Task.WhenAny(received.Task, Task.Delay(ReceiveTimeout));
        Assert.AreSame(received.Task, finished, "The retained message was not delivered to the late subscriber.");
        Assert.AreEqual(payload, await received.Task);

        await server.StopAsync();
    }

    /// <summary>
    /// Checks whether the broker really refuses a client identifier that is too short, instead of only the validator
    /// saying so. A refused connection does not throw in MQTTnet 5, it comes back as a result code, which is why the
    /// form has to look at it.
    /// </summary>
    [TestMethod]
    [Timeout(60000)]
    public async Task TheBrokerRefusesAShortClientId()
    {
        var port = GetFreeTcpPort();
        using var server = CreateStartedServer(port, out var startTask);
        await startTask;

        using var client = new MqttClientFactory().CreateMqttClient();

        var result = await client.ConnectAsync(MqttObjectFactory.CreateClientOptions("short", port));

        Assert.AreEqual(MqttClientConnectResultCode.ClientIdentifierNotValid, result.ResultCode);
        Assert.IsFalse(client.IsConnected);

        await server.StopAsync();
    }

    /// <summary>
    /// Checks whether the broker refuses a client that sends no credentials.
    /// </summary>
    [TestMethod]
    [Timeout(60000)]
    public async Task TheBrokerRefusesAClientWithoutCredentials()
    {
        var port = GetFreeTcpPort();
        using var server = CreateStartedServer(port, out var startTask);
        await startTask;

        using var client = new MqttClientFactory().CreateMqttClient();
        var options = new MqttClientOptionsBuilder()
            .WithClientId("ClientWithoutCredentials")
            .WithTcpServer(MqttObjectFactory.Host, port)
            .WithProtocolVersion(MqttProtocolVersion.V311)
            .Build();

        var result = await client.ConnectAsync(options);

        Assert.AreEqual(MqttClientConnectResultCode.BadUserNameOrPassword, result.ResultCode);
        Assert.IsFalse(client.IsConnected);

        await server.StopAsync();
    }

    /// <summary>
    /// Creates a broker with the options of the form and starts it.
    /// </summary>
    /// <param name="port">The port the broker listens on.</param>
    /// <param name="startTask">The task of the start, it has to be awaited by the caller.</param>
    /// <returns>The started server.</returns>
    private static MqttServer CreateStartedServer(int port, out Task startTask)
    {
        var server = new MqttServerFactory().CreateMqttServer(MqttObjectFactory.CreateServerOptions(port));
        server.ValidatingConnectionAsync += args =>
        {
            args.ReasonCode = ConnectionValidator.GetReasonCode(args.ClientId, args.UserName, args.Password);
            return Task.CompletedTask;
        };

        startTask = server.StartAsync();
        return server;
    }

    /// <summary>
    /// Gets a port that is free at this moment, so that a test run does not collide with a broker that already runs
    /// on this machine.
    /// </summary>
    /// <returns>The free port.</returns>
    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }
}
