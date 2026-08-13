// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageFormatter.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   A class that builds the texts the form shows in its text boxes.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MQTTnet.TestApp.WinForm.Logic;

/// <summary>
/// A class that builds the texts the form shows in its text boxes.
/// </summary>
public static class MessageFormatter
{
    /// <summary>
    /// Creates the message that the generate button writes into the publish text box.
    /// </summary>
    /// <param name="timestamp">The timestamp to write into the message.</param>
    /// <returns>A JSON object with the timestamp in the round trip format.</returns>
    public static string CreateGeneratedMessage(DateTimeOffset timestamp)
    {
        return $"{{\"dt\":\"{timestamp:O}\"}}";
    }

    /// <summary>
    /// Creates the line that is written into the subscriber text box for a received message.
    /// </summary>
    /// <param name="timestamp">The time the message was received.</param>
    /// <param name="topic">The topic of the message.</param>
    /// <param name="payload">The payload of the message as a string.</param>
    /// <param name="qualityOfServiceLevel">The quality of service level of the message.</param>
    /// <returns>The line to prepend to the subscriber text box.</returns>
    public static string FormatReceivedMessage(DateTimeOffset timestamp, string topic, string? payload, MqttQualityOfServiceLevel qualityOfServiceLevel)
    {
        return $"Timestamp: {timestamp:O} | Topic: {topic} | Payload: {payload} | QoS: {qualityOfServiceLevel}";
    }
}
