// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MqttObjectFactoryTests.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   A class to test the <see cref="MqttObjectFactory" /> class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MQTTnet.TestApp.WinForm.Tests;

/// <summary>
/// A class to test the <see cref="MqttObjectFactory"/> class.
/// </summary>
[TestClass]
public class MqttObjectFactoryTests
{
    /// <summary>
    /// Checks whether the default endpoint of the broker is enabled. Up to version 1.0.7.0 it was not, which let
    /// StartAsync succeed without opening a listener, so no client could reach the broker at all.
    /// </summary>
    [TestMethod]
    public void CreateServerOptionsEnablesTheDefaultEndpoint()
    {
        var options = MqttObjectFactory.CreateServerOptions(1883);

        Assert.IsTrue(options.DefaultEndpointOptions.IsEnabled);
    }

    /// <summary>
    /// Checks whether the broker listens on the given port and keeps its sessions.
    /// </summary>
    [TestMethod]
    public void CreateServerOptionsUsesTheGivenPort()
    {
        var options = MqttObjectFactory.CreateServerOptions(18830);

        Assert.AreEqual(18830, options.DefaultEndpointOptions.Port);
        Assert.IsTrue(options.EnablePersistentSessions);
    }

    /// <summary>
    /// Checks whether the client options carry the identifier and the credentials the broker expects.
    /// </summary>
    [TestMethod]
    public void CreateClientOptionsUsesTheCredentialsTheBrokerExpects()
    {
        var options = MqttObjectFactory.CreateClientOptions("ClientPublisher", 1883);

        Assert.AreEqual("ClientPublisher", options.ClientId);
        Assert.IsNotNull(options.Credentials);
        Assert.AreEqual(ConnectionValidator.UserName, options.Credentials.GetUserName(options));
        Assert.AreEqual(ConnectionValidator.Password, Encoding.UTF8.GetString(options.Credentials.GetPassword(options) ?? []));
    }

    /// <summary>
    /// Checks whether the client talks MQTT 3.1.1 to the broker in the same process.
    /// </summary>
    [TestMethod]
    public void CreateClientOptionsUsesTheLocalBrokerWithoutTls()
    {
        var options = MqttObjectFactory.CreateClientOptions("ClientSubscriber", 18830);

        Assert.AreEqual(MqttProtocolVersion.V311, options.ProtocolVersion);
        Assert.AreEqual(MqttObjectFactory.KeepAlivePeriod, options.KeepAlivePeriod);
        Assert.IsTrue(options.CleanSession);

        var channelOptions = options.ChannelOptions as MqttClientTcpOptions;
        Assert.IsNotNull(channelOptions);
        Assert.IsFalse(channelOptions.TlsOptions.UseTls);

        var endpoint = channelOptions.RemoteEndpoint as DnsEndPoint;
        Assert.IsNotNull(endpoint);
        Assert.AreEqual(MqttObjectFactory.Host, endpoint.Host);
        Assert.AreEqual(18830, endpoint.Port);
    }

    /// <summary>
    /// Checks whether the published message keeps its payload, is sent with at least once and is retained, so that a
    /// subscriber that connects later still gets it.
    /// </summary>
    [TestMethod]
    public void CreateApplicationMessageRetainsThePayload()
    {
        var message = MqttObjectFactory.CreateApplicationMessage("brand/type/group/code", "{\"dt\":\"asdf\"}");

        Assert.AreEqual("brand/type/group/code", message.Topic);
        Assert.AreEqual("{\"dt\":\"asdf\"}", message.ConvertPayloadToString());
        Assert.AreEqual(MqttQualityOfServiceLevel.AtLeastOnce, message.QualityOfServiceLevel);
        Assert.IsTrue(message.Retain);
    }
}
