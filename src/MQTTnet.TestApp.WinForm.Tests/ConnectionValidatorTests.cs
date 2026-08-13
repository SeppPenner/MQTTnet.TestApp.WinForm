// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConnectionValidatorTests.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   A class to test the <see cref="ConnectionValidator" /> class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MQTTnet.TestApp.WinForm.Tests;

/// <summary>
/// A class to test the <see cref="ConnectionValidator"/> class.
/// </summary>
[TestClass]
public class ConnectionValidatorTests
{
    /// <summary>
    /// Checks whether a client that sends the expected credentials and a long enough identifier is accepted.
    /// </summary>
    [TestMethod]
    public void GetReasonCodeWithValidValuesReturnsSuccess()
    {
        var reasonCode = ConnectionValidator.GetReasonCode("ClientPublisher", ConnectionValidator.UserName, ConnectionValidator.Password);

        Assert.AreEqual(MqttConnectReasonCode.Success, reasonCode);
    }

    /// <summary>
    /// Checks whether both client identifiers the application uses itself pass the length rule. If they did not, the
    /// application could not connect to its own broker.
    /// </summary>
    /// <param name="clientId">The client identifier to check.</param>
    [TestMethod]
    [DataRow("ClientPublisher")]
    [DataRow("ClientSubscriber")]
    public void GetReasonCodeAcceptsTheClientIdsOfTheApplication(string clientId)
    {
        var reasonCode = ConnectionValidator.GetReasonCode(clientId, ConnectionValidator.UserName, ConnectionValidator.Password);

        Assert.AreEqual(MqttConnectReasonCode.Success, reasonCode);
    }

    /// <summary>
    /// Checks whether a client identifier that is too short is refused, including the empty one and none at all.
    /// </summary>
    /// <param name="clientId">The client identifier to check.</param>
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("Client")]
    [DataRow("123456789")]
    public void GetReasonCodeWithAShortClientIdReturnsClientIdentifierNotValid(string? clientId)
    {
        var reasonCode = ConnectionValidator.GetReasonCode(clientId, ConnectionValidator.UserName, ConnectionValidator.Password);

        Assert.AreEqual(MqttConnectReasonCode.ClientIdentifierNotValid, reasonCode);
    }

    /// <summary>
    /// Checks whether an identifier of exactly the minimum length is still accepted.
    /// </summary>
    [TestMethod]
    public void GetReasonCodeWithTheMinimumClientIdLengthReturnsSuccess()
    {
        var clientId = new string('a', ConnectionValidator.MinimumClientIdLength);

        var reasonCode = ConnectionValidator.GetReasonCode(clientId, ConnectionValidator.UserName, ConnectionValidator.Password);

        Assert.AreEqual(MqttConnectReasonCode.Success, reasonCode);
    }

    /// <summary>
    /// Checks whether a wrong user name is refused.
    /// </summary>
    [TestMethod]
    public void GetReasonCodeWithAWrongUserNameReturnsBadUserNameOrPassword()
    {
        var reasonCode = ConnectionValidator.GetReasonCode("ClientPublisher", "somebody", ConnectionValidator.Password);

        Assert.AreEqual(MqttConnectReasonCode.BadUserNameOrPassword, reasonCode);
    }

    /// <summary>
    /// Checks whether a wrong password is refused.
    /// </summary>
    [TestMethod]
    public void GetReasonCodeWithAWrongPasswordReturnsBadUserNameOrPassword()
    {
        var reasonCode = ConnectionValidator.GetReasonCode("ClientPublisher", ConnectionValidator.UserName, "secret");

        Assert.AreEqual(MqttConnectReasonCode.BadUserNameOrPassword, reasonCode);
    }

    /// <summary>
    /// Checks whether a client that sends no credentials at all is refused.
    /// </summary>
    [TestMethod]
    public void GetReasonCodeWithoutCredentialsReturnsBadUserNameOrPassword()
    {
        var reasonCode = ConnectionValidator.GetReasonCode("ClientPublisher", null, null);

        Assert.AreEqual(MqttConnectReasonCode.BadUserNameOrPassword, reasonCode);
    }
}
