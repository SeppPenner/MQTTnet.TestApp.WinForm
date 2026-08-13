// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PortParser.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   A class that reads the port out of the text the user typed.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MQTTnet.TestApp.WinForm.Logic;

/// <summary>
/// A class that reads the port out of the text the user typed.
/// </summary>
public static class PortParser
{
    /// <summary>
    /// The lowest port number a socket can be bound to.
    /// </summary>
    public const int MinimumPort = 1;

    /// <summary>
    /// The highest port number a socket can be bound to.
    /// </summary>
    public const int MaximumPort = 65535;

    /// <summary>
    /// Tries to read a usable port out of the given text.
    /// </summary>
    /// <param name="text">The text of the port text box.</param>
    /// <param name="port">The port that was read, zero if the text does not hold one.</param>
    /// <returns><c>true</c> if the text holds a number that a socket can be bound to, otherwise <c>false</c>.</returns>
    public static bool TryParsePort(string? text, out int port)
    {
        port = 0;

        if (!int.TryParse(text?.Trim(), out var parsedPort))
        {
            return false;
        }

        if (parsedPort < MinimumPort || parsedPort > MaximumPort)
        {
            return false;
        }

        port = parsedPort;
        return true;
    }
}
