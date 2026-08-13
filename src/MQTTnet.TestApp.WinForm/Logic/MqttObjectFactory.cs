// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MqttObjectFactory.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   A class that creates the MQTTnet objects the form works with.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MQTTnet.TestApp.WinForm.Logic;

/// <summary>
/// A class that creates the MQTTnet objects the form works with: the options of the built in broker, the options of
/// the two clients and the message that is published.
/// </summary>
public static class MqttObjectFactory
{
    /// <summary>
    /// The host both clients connect to. The broker runs in the same process.
    /// </summary>
    public const string Host = "localhost";

    /// <summary>
    /// The keep alive period of both clients.
    /// </summary>
    public static readonly TimeSpan KeepAlivePeriod = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Creates the options of the built in broker.
    /// </summary>
    /// <param name="port">The port the broker listens on.</param>
    /// <returns>The server options.</returns>
    public static MqttServerOptions CreateServerOptions(int port)
    {
        var options = new MqttServerOptions();

        // IsEnabled is false in a freshly created MqttServerOptions. Without this line StartAsync succeeds without
        // opening a listener, so no client can ever reach this broker.
        options.DefaultEndpointOptions.IsEnabled = true;
        options.DefaultEndpointOptions.Port = port;
        options.EnablePersistentSessions = true;
        return options;
    }

    /// <summary>
    /// Creates the options of one of the two clients.
    /// </summary>
    /// <param name="clientId">The client identifier. It has to be at least
    /// <see cref="ConnectionValidator.MinimumClientIdLength"/> characters long, otherwise the broker refuses it.</param>
    /// <param name="port">The port of the broker.</param>
    /// <returns>The client options.</returns>
    public static MqttClientOptions CreateClientOptions(string clientId, int port)
    {
        // The certificate flags do nothing while UseTls is false, they are kept to show where they belong.
        var tlsOptions = new MqttClientTlsOptions
        {
            UseTls = false,
            IgnoreCertificateChainErrors = true,
            IgnoreCertificateRevocationErrors = true,
            AllowUntrustedCertificates = true
        };

        return new MqttClientOptionsBuilder()
            .WithClientId(clientId)
            .WithTcpServer(Host, port)
            .WithProtocolVersion(MqttProtocolVersion.V311)
            .WithTlsOptions(tlsOptions)
            .WithCleanSession()
            .WithKeepAlivePeriod(KeepAlivePeriod)
            .WithCredentials(ConnectionValidator.UserName, ConnectionValidator.Password)
            .Build();
    }

    /// <summary>
    /// Creates the message the publish button sends.
    /// </summary>
    /// <param name="topic">The topic to publish to.</param>
    /// <param name="payload">The payload as it stands in the publish text box.</param>
    /// <returns>The application message, published with the retain flag so that a later subscriber still gets it.</returns>
    public static MqttApplicationMessage CreateApplicationMessage(string topic, string payload)
    {
        return new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(Encoding.UTF8.GetBytes(payload))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag()
            .Build();
    }
}
