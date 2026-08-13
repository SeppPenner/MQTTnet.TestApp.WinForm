// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConnectionValidator.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   A class that decides which clients the built in broker accepts.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MQTTnet.TestApp.WinForm.Logic;

/// <summary>
/// A class that decides which clients the built in broker accepts.
/// </summary>
public static class ConnectionValidator
{
    /// <summary>
    /// The user name every client has to send. This is a test application, the value is meant to be visible.
    /// </summary>
    public const string UserName = "username";

    /// <summary>
    /// The password every client has to send. This is a test application, the value is meant to be visible.
    /// </summary>
    public const string Password = "password";

    /// <summary>
    /// The minimum length of an accepted client identifier. The two built in clients are named accordingly.
    /// </summary>
    public const int MinimumClientIdLength = 10;

    /// <summary>
    /// Gets the reason code the broker answers a connection attempt with.
    /// </summary>
    /// <param name="clientId">The client identifier sent by the client.</param>
    /// <param name="userName">The user name sent by the client.</param>
    /// <param name="password">The password sent by the client.</param>
    /// <returns>
    /// <see cref="MqttConnectReasonCode.Success"/> if the client may connect, otherwise the reason for the refusal.
    /// </returns>
    public static MqttConnectReasonCode GetReasonCode(string? clientId, string? userName, string? password)
    {
        if (string.IsNullOrEmpty(clientId) || clientId.Length < MinimumClientIdLength)
        {
            return MqttConnectReasonCode.ClientIdentifierNotValid;
        }

        if (userName != UserName)
        {
            return MqttConnectReasonCode.BadUserNameOrPassword;
        }

        if (password != Password)
        {
            return MqttConnectReasonCode.BadUserNameOrPassword;
        }

        return MqttConnectReasonCode.Success;
    }
}
