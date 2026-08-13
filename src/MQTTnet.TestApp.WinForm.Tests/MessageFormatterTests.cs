// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageFormatterTests.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   A class to test the <see cref="MessageFormatter" /> class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MQTTnet.TestApp.WinForm.Tests;

/// <summary>
/// A class to test the <see cref="MessageFormatter"/> class.
/// </summary>
[TestClass]
public class MessageFormatterTests
{
    /// <summary>
    /// The timestamp both tests work with.
    /// </summary>
    private static readonly DateTimeOffset Timestamp = new(2026, 8, 13, 20, 35, 43, TimeSpan.FromHours(2));

    /// <summary>
    /// Checks whether the generated message is a JSON object that holds the timestamp under the key dt. Up to version
    /// 1.0.7.0 the same timestamp was written twice into that value.
    /// </summary>
    [TestMethod]
    public void CreateGeneratedMessageWritesTheTimestampOnce()
    {
        var message = MessageFormatter.CreateGeneratedMessage(Timestamp);

        using var document = JsonDocument.Parse(message);
        var value = document.RootElement.GetProperty("dt").GetString();

        Assert.AreEqual(Timestamp.ToString("O"), value);
    }

    /// <summary>
    /// Checks whether the generated message can be read back into the timestamp it was made of.
    /// </summary>
    [TestMethod]
    public void CreateGeneratedMessageUsesTheRoundTripFormat()
    {
        var message = MessageFormatter.CreateGeneratedMessage(Timestamp);

        using var document = JsonDocument.Parse(message);
        var value = document.RootElement.GetProperty("dt").GetString();

        Assert.AreEqual(Timestamp, DateTimeOffset.Parse(value!));
    }

    /// <summary>
    /// Checks whether a received message is shown with its timestamp, topic, payload and quality of service level.
    /// </summary>
    [TestMethod]
    public void FormatReceivedMessageWritesAllPartsOfTheMessage()
    {
        var line = MessageFormatter.FormatReceivedMessage(Timestamp, "brand/type/group/code", "{\"dt\":\"asdf\"}", MqttQualityOfServiceLevel.AtLeastOnce);

        Assert.AreEqual(
            "Timestamp: 2026-08-13T20:35:43.0000000+02:00 | Topic: brand/type/group/code | Payload: {\"dt\":\"asdf\"} | QoS: AtLeastOnce",
            line);
    }

    /// <summary>
    /// Checks whether a message without a payload is shown as a line as well instead of throwing.
    /// </summary>
    [TestMethod]
    public void FormatReceivedMessageWithoutAPayloadStillWritesTheTopic()
    {
        var line = MessageFormatter.FormatReceivedMessage(Timestamp, "brand/type/group/code", null, MqttQualityOfServiceLevel.AtMostOnce);

        StringAssert.Contains(line, "Topic: brand/type/group/code");
        StringAssert.Contains(line, "Payload:  |");
    }
}
