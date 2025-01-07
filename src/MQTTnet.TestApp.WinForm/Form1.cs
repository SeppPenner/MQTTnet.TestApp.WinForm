// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Form1.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   The main form.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MQTTnet.TestApp.WinForm;

/// <summary>
/// The main form.
/// </summary>
public partial class Form1 : Form
{
    /// <summary>
    /// The publisher client.
    /// </summary>
    private IMqttClient? mqttClientPublisher;

    /// <summary>
    /// The subscriber client.
    /// </summary>
    private IMqttClient? mqttClientSubscriber;

    /// <summary>
    /// The MQTT server.
    /// </summary>
    private MqttServer? mqttServer;

    /// <summary>
    /// The port.
    /// </summary>
    private string port = "1883";

    /// <summary>
    /// Initializes a new instance of the <see cref="Form1"/> class.
    /// </summary>
    public Form1()
    {
        this.InitializeComponent();

        var timer = new Timer
        {
            AutoReset = true,
            Enabled = true,
            Interval = 1000
        };

        timer.Elapsed += this.TimerElapsed!;
    }

    /// <summary>
    /// Handles the publisher connected event.
    /// </summary>
    private static Task OnPublisherConnected(MqttClientConnectedEventArgs _)
    {
        MessageBox.Show("Publisher Connected", "ConnectHandler", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the publisher disconnected event.
    /// </summary>
    private static Task OnPublisherDisconnected(MqttClientDisconnectedEventArgs _)
    {
        MessageBox.Show("Publisher Disconnected", "ConnectHandler", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the subscriber connected event.
    /// </summary>
    private static Task OnSubscriberConnected(MqttClientConnectedEventArgs _)
    {
        MessageBox.Show("Subscriber Connected", "ConnectHandler", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the subscriber disconnected event.
    /// </summary>
    private static Task OnSubscriberDisconnected(MqttClientDisconnectedEventArgs _)
    {
        MessageBox.Show("Subscriber Disconnected", "ConnectHandler", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return Task.CompletedTask;
    }

    /// <summary>
    /// The method that handles the button click to generate a message.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private void ButtonGeneratePublishedMessageClick(object sender, EventArgs e)
    {
        var message = $"{{\"dt\":\"{DateTimeOffset.Now:G} {DateTimeOffset.Now:G}\"}}";
        this.TextBoxPublish.Text = message;
    }

    /// <summary>
    /// The method that handles the button click to publish a message.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private async void ButtonPublishClick(object sender, EventArgs e)
    {
        ((Button)sender).Enabled = false;

        try
        {
            var payload = Encoding.UTF8.GetBytes(this.TextBoxPublish.Text);
            var message = new MqttApplicationMessageBuilder().WithTopic(this.TextBoxTopicPublished.Text.Trim()).WithPayload(payload).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).WithRetainFlag().Build();

            if (this.mqttClientPublisher is not null)
            {
                await this.mqttClientPublisher.PublishAsync(message);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error Occurs", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        ((Button)sender).Enabled = true;
    }

    /// <summary>
    /// The method that handles the button click to start the publisher.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private async void ButtonPublisherStartClick(object sender, EventArgs e)
    {
        var mqttFactory = new MqttClientFactory();

        var tlsOptions = new MqttClientTlsOptions
        {
            UseTls = false,
            IgnoreCertificateChainErrors = true,
            IgnoreCertificateRevocationErrors = true,
            AllowUntrustedCertificates = true
        };

        var options = new MqttClientOptionsBuilder()
           .WithClientId("ClientPublisher")
           .WithTcpServer("localhost", int.Parse(this.TextBoxPort.Text.Trim()))
           .WithProtocolVersion(MqttProtocolVersion.V311)
           .WithTlsOptions(tlsOptions)
           .WithCleanSession()
           .WithKeepAlivePeriod(TimeSpan.FromSeconds(5))
           .WithCredentials("username", "password")
           .Build();

        if (options.ChannelOptions is null)
        {
            throw new InvalidOperationException();
        }

        this.mqttClientPublisher = mqttFactory.CreateMqttClient();
        this.mqttClientPublisher.ConnectedAsync += OnPublisherConnected;
        this.mqttClientPublisher.DisconnectedAsync += OnPublisherDisconnected;
        this.mqttClientPublisher.ApplicationMessageReceivedAsync += this.HandleReceivedApplicationMessage;

        await this.mqttClientPublisher.ConnectAsync(options);
    }

    /// <summary>
    /// The method that handles the button click to stop the publisher.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private async void ButtonPublisherStopClick(object sender, EventArgs e)
    {
        if (this.mqttClientPublisher is null)
        {
            return;
        }

        await this.mqttClientPublisher.DisconnectAsync();
        this.mqttClientPublisher.Dispose();
        this.mqttClientPublisher = null;
    }

    /// <summary>
    /// The method that handles the button click to start the server.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private async void ButtonServerStartClick(object sender, EventArgs e)
    {
        if (this.mqttServer is not null)
        {
            return;
        }
        
        var options = new MqttServerOptions();
        options.DefaultEndpointOptions.Port = int.Parse(this.TextBoxPort.Text.Trim());
        options.EnablePersistentSessions = true;

        this.mqttServer = new MqttServerFactory().CreateMqttServer(options);
        this.mqttServer.ValidatingConnectionAsync += this.ValidateConnectionAsync;

        try
        {
            await this.mqttServer.StartAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error Occurs", MessageBoxButtons.OK, MessageBoxIcon.Error);
            await this.mqttServer.StopAsync();
            this.mqttServer = null;
        }
    }

    /// <summary>
    /// Validates the connection.
    /// </summary>
    /// <param name="args">The arguments.</param>
    private Task ValidateConnectionAsync(ValidatingConnectionEventArgs args)
    {
        if (args.ClientId.Length < 10)
        {
            args.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
            return Task.CompletedTask;
        }

        if (args.UserName != "username")
        {
            args.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
            return Task.CompletedTask;
        }

        if (args.Password != "password")
        {
            args.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
            return Task.CompletedTask;
        }

        args.ReasonCode = MqttConnectReasonCode.Success;
        return Task.CompletedTask;
    }

    /// <summary>
    /// The method that handles the button click to stop the server.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private async void ButtonServerStopClick(object sender, EventArgs e)
    {
        if (this.mqttServer is null)
        {
            return;
        }

        await this.mqttServer.StopAsync();
        this.mqttServer = null;
    }

    /// <summary>
    /// The method that handles the button click to subscribe to a certain topic.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private async void ButtonSubscribeClick(object sender, EventArgs e)
    {
        if (this.mqttClientSubscriber is null)
        {
            return;
        }

        var topicFilter = new MqttTopicFilter { Topic = this.TextBoxTopicSubscribed.Text.Trim() };
        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(topicFilter)
            .Build();
        await this.mqttClientSubscriber.SubscribeAsync(subscribeOptions);
        MessageBox.Show("Topic " + this.TextBoxTopicSubscribed.Text.Trim() + " is subscribed", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>
    /// The method that handles the button click to start the subscriber.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private async void ButtonSubscriberStartClick(object sender, EventArgs e)
    {
        var mqttFactory = new MqttClientFactory();

        var tlsOptions = new MqttClientTlsOptions
        {
            UseTls = false,
            IgnoreCertificateChainErrors = true,
            IgnoreCertificateRevocationErrors = true,
            AllowUntrustedCertificates = true
        };

        var options = new MqttClientOptionsBuilder()
           .WithClientId("ClientSubscriber")
           .WithTcpServer("localhost", int.Parse(this.TextBoxPort.Text.Trim()))
           .WithProtocolVersion(MqttProtocolVersion.V311)
           .WithTlsOptions(tlsOptions)
           .WithCleanSession()
           .WithKeepAlivePeriod(TimeSpan.FromSeconds(5))
           .WithCredentials("username", "password")
           .Build();

        if (options.ChannelOptions is null)
        {
            throw new InvalidOperationException();
        }

        this.mqttClientSubscriber = mqttFactory.CreateMqttClient();
        this.mqttClientSubscriber.ConnectedAsync += OnSubscriberConnected;
        this.mqttClientSubscriber.DisconnectedAsync += OnSubscriberDisconnected;
        this.mqttClientSubscriber.ApplicationMessageReceivedAsync += this.OnSubscriberMessageReceived;

        await this.mqttClientSubscriber.ConnectAsync(options);
    }

    /// <summary>
    /// The method that handles the button click to stop the subscriber.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private async void ButtonSubscriberStopClick(object sender, EventArgs e)
    {
        if (this.mqttClientSubscriber is null)
        {
            return;
        }

        await this.mqttClientSubscriber.DisconnectAsync();
        this.mqttClientSubscriber.Dispose();
        this.mqttClientSubscriber = null;
    }

    /// <summary>
    /// Handles the received application message event.
    /// </summary>
    /// <param name="x">The MQTT application message received event args.</param>
    private Task HandleReceivedApplicationMessage(MqttApplicationMessageReceivedEventArgs x)
    {
        var item = $"Timestamp: {DateTimeOffset.Now:O} | Topic: {x.ApplicationMessage.Topic} | Payload: {x.ApplicationMessage.ConvertPayloadToString()} | QoS: {x.ApplicationMessage.QualityOfServiceLevel}";
        this.BeginInvoke((MethodInvoker)delegate { this.TextBoxSubscriber.Text = item + Environment.NewLine + this.TextBoxSubscriber.Text; });
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the received subscriber message event.
    /// </summary>
    /// <param name="x">The MQTT application message received event args.</param>
    private Task OnSubscriberMessageReceived(MqttApplicationMessageReceivedEventArgs x)
    {
        var item = $"Timestamp: {DateTimeOffset.Now:O} | Topic: {x.ApplicationMessage.Topic} | Payload: {x.ApplicationMessage.ConvertPayloadToString()} | QoS: {x.ApplicationMessage.QualityOfServiceLevel}";
        this.BeginInvoke((MethodInvoker)delegate { this.TextBoxSubscriber.Text = item + Environment.NewLine + this.TextBoxSubscriber.Text; });
        return Task.CompletedTask;
    }

    /// <summary>
    /// The method that handles the text changes in the text box.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private void TextBoxPortTextChanged(object sender, EventArgs e)
    {
        if (int.TryParse(this.TextBoxPort.Text.Trim(), out _))
        {
            this.port = this.TextBoxPort.Text.Trim();
        }
        else
        {
            this.TextBoxPort.Text = this.port;
            this.TextBoxPort.SelectionStart = this.TextBoxPort.Text.Length;
            this.TextBoxPort.SelectionLength = 0;
        }
    }

    /// <summary>
    /// The method that handles the timer events.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private void TimerElapsed(object sender, ElapsedEventArgs e)
    {
        this.BeginInvoke(
            (MethodInvoker)delegate
            {
                // Server
                this.TextBoxPort.Enabled = this.mqttServer is null;
                this.ButtonServerStart.Enabled = this.mqttServer is null;
                this.ButtonServerStop.Enabled = this.mqttServer is not null;

                // Publisher
                this.ButtonPublisherStart.Enabled = this.mqttClientPublisher is null;
                this.ButtonPublisherStop.Enabled = this.mqttClientPublisher is not null;

                // Subscriber
                this.ButtonSubscriberStart.Enabled = this.mqttClientSubscriber is null;
                this.ButtonSubscriberStop.Enabled = this.mqttClientSubscriber is not null;
            });
    }
}
