// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PortParserTests.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   A class to test the <see cref="PortParser" /> class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MQTTnet.TestApp.WinForm.Tests;

/// <summary>
/// A class to test the <see cref="PortParser"/> class.
/// </summary>
[TestClass]
public class PortParserTests
{
    /// <summary>
    /// Checks whether the default port of the form is read.
    /// </summary>
    [TestMethod]
    public void TryParsePortWithTheDefaultPortReturnsTrue()
    {
        var result = PortParser.TryParsePort("1883", out var port);

        Assert.IsTrue(result);
        Assert.AreEqual(1883, port);
    }

    /// <summary>
    /// Checks whether surrounding spaces are ignored, the form trims the text before it uses it as well.
    /// </summary>
    [TestMethod]
    public void TryParsePortIgnoresSurroundingSpaces()
    {
        var result = PortParser.TryParsePort("  1883  ", out var port);

        Assert.IsTrue(result);
        Assert.AreEqual(1883, port);
    }

    /// <summary>
    /// Checks whether both ends of the allowed range are accepted.
    /// </summary>
    /// <param name="text">The text to read.</param>
    /// <param name="expectedPort">The port that is expected to come out.</param>
    [TestMethod]
    [DataRow("1", PortParser.MinimumPort)]
    [DataRow("65535", PortParser.MaximumPort)]
    public void TryParsePortAcceptsTheBordersOfTheRange(string text, int expectedPort)
    {
        var result = PortParser.TryParsePort(text, out var port);

        Assert.IsTrue(result);
        Assert.AreEqual(expectedPort, port);
    }

    /// <summary>
    /// Checks whether text that is no port at all is refused. A port outside the range has to be refused as well,
    /// binding a socket to it would throw when the server is started.
    /// </summary>
    /// <param name="text">The text to read.</param>
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("abc")]
    [DataRow("18a3")]
    [DataRow("0")]
    [DataRow("-1")]
    [DataRow("65536")]
    [DataRow("99999999999")]
    public void TryParsePortWithUnusableTextReturnsFalse(string? text)
    {
        var result = PortParser.TryParsePort(text, out var port);

        Assert.IsFalse(result);
        Assert.AreEqual(0, port);
    }
}
