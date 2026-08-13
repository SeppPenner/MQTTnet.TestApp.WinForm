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
    /// The client identifier of the publisher.
    /// </summary>
    private const string PublisherClientId = "ClientPublisher";

    /// <summary>
    /// The client identifier of the subscriber.
    /// </summary>
    private const string SubscriberClientId = "ClientSubscriber";

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
    /// The timer that keeps the button states up to date.
    /// </summary>
    private readonly Timer timer;

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

        this.timer = new Timer
        {
            AutoReset = true,
            Interval = 1000
        };

        this.timer.Elapsed += this.TimerElapsed!;
        this.timer.Start();
    }

    /// <summary>
    /// Handles the form closing event. Stops the timer first, so that it can no longer call into a form that is
    /// about to be disposed.
    /// </summary>
    /// <param name="e">The event args.</param>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        this.timer.Stop();
        this.timer.Dispose();
        base.OnFormClosing(e);
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
    /// Shows the given exception. Every button handler is an async void method, so an exception that is not caught
    /// here does not travel to a caller, it terminates the application.
    /// </summary>
    /// <param name="ex">The exception to show.</param>
    private static void ShowError(Exception ex)
    {
        ShowError(ex.Message);
    }

    /// <summary>
    /// Shows the given error message.
    /// </summary>
    /// <param name="message">The message to show.</param>
    private static void ShowError(string message)
    {
        MessageBox.Show(message, "Error Occurs", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    /// <summary>
    /// The method that handles the button click to generate a message.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private void ButtonGeneratePublishedMessageClick(object sender, EventArgs e)
    {
        this.TextBoxPublish.Text = MessageFormatter.CreateGeneratedMessage(DateTimeOffset.Now);
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
            var message = MqttObjectFactory.CreateApplicationMessage(this.TextBoxTopicPublished.Text.Trim(), this.TextBoxPublish.Text);

            if (this.mqttClientPublisher is not null)
            {
                await this.mqttClientPublisher.PublishAsync(message);
            }
        }
        catch (Exception ex)
        {
            ShowError(ex);
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
        try
        {
            var mqttFactory = new MqttClientFactory();
            var options = MqttObjectFactory.CreateClientOptions(PublisherClientId, int.Parse(this.TextBoxPort.Text.Trim()));

            if (options.ChannelOptions is null)
            {
                throw new InvalidOperationException();
            }

            this.mqttClientPublisher = mqttFactory.CreateMqttClient();
            this.mqttClientPublisher.ConnectedAsync += OnPublisherConnected;
            this.mqttClientPublisher.DisconnectedAsync += OnPublisherDisconnected;
            this.mqttClientPublisher.ApplicationMessageReceivedAsync += this.HandleReceivedApplicationMessage;

            var result = await this.mqttClientPublisher.ConnectAsync(options);

            if (result.ResultCode != MqttClientConnectResultCode.Success)
            {
                this.DisposePublisher();
                ShowError($"The broker refused the publisher: {result.ResultCode}.");
            }
        }
        catch (Exception ex)
        {
            this.DisposePublisher();
            ShowError(ex);
        }
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

        try
        {
            await this.mqttClientPublisher.DisconnectAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            this.DisposePublisher();
        }
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

        try
        {
            var options = MqttObjectFactory.CreateServerOptions(int.Parse(this.TextBoxPort.Text.Trim()));

            this.mqttServer = new MqttServerFactory().CreateMqttServer(options);
            this.mqttServer.ValidatingConnectionAsync += ValidateConnectionAsync;

            await this.mqttServer.StartAsync();
        }
        catch (Exception ex)
        {
            this.DisposeServer();
            ShowError(ex);
        }
    }

    /// <summary>
    /// Validates the connection.
    /// </summary>
    /// <param name="args">The arguments.</param>
    private static Task ValidateConnectionAsync(ValidatingConnectionEventArgs args)
    {
        args.ReasonCode = ConnectionValidator.GetReasonCode(args.ClientId, args.UserName, args.Password);
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

        try
        {
            await this.mqttServer.StopAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            this.DisposeServer();
        }
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

        try
        {
            var topicFilter = new MqttTopicFilter { Topic = this.TextBoxTopicSubscribed.Text.Trim() };
            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(topicFilter)
                .Build();
            await this.mqttClientSubscriber.SubscribeAsync(subscribeOptions);
            MessageBox.Show("Topic " + this.TextBoxTopicSubscribed.Text.Trim() + " is subscribed", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    /// <summary>
    /// The method that handles the button click to start the subscriber.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private async void ButtonSubscriberStartClick(object sender, EventArgs e)
    {
        try
        {
            var mqttFactory = new MqttClientFactory();
            var options = MqttObjectFactory.CreateClientOptions(SubscriberClientId, int.Parse(this.TextBoxPort.Text.Trim()));

            if (options.ChannelOptions is null)
            {
                throw new InvalidOperationException();
            }

            this.mqttClientSubscriber = mqttFactory.CreateMqttClient();
            this.mqttClientSubscriber.ConnectedAsync += OnSubscriberConnected;
            this.mqttClientSubscriber.DisconnectedAsync += OnSubscriberDisconnected;
            this.mqttClientSubscriber.ApplicationMessageReceivedAsync += this.OnSubscriberMessageReceived;

            var result = await this.mqttClientSubscriber.ConnectAsync(options);

            if (result.ResultCode != MqttClientConnectResultCode.Success)
            {
                this.DisposeSubscriber();
                ShowError($"The broker refused the subscriber: {result.ResultCode}.");
            }
        }
        catch (Exception ex)
        {
            this.DisposeSubscriber();
            ShowError(ex);
        }
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

        try
        {
            await this.mqttClientSubscriber.DisconnectAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            this.DisposeSubscriber();
        }
    }

    /// <summary>
    /// Disposes the publisher client and clears the field, so that the start button becomes usable again.
    /// </summary>
    private void DisposePublisher()
    {
        this.mqttClientPublisher?.Dispose();
        this.mqttClientPublisher = null;
    }

    /// <summary>
    /// Disposes the subscriber client and clears the field, so that the start button becomes usable again.
    /// </summary>
    private void DisposeSubscriber()
    {
        this.mqttClientSubscriber?.Dispose();
        this.mqttClientSubscriber = null;
    }

    /// <summary>
    /// Disposes the server and clears the field. Stopping the server alone does not release the listening socket,
    /// which would make a restart on the same port fail.
    /// </summary>
    private void DisposeServer()
    {
        this.mqttServer?.Dispose();
        this.mqttServer = null;
    }

    /// <summary>
    /// Handles the received application message event.
    /// </summary>
    /// <param name="x">The MQTT application message received event args.</param>
    private Task HandleReceivedApplicationMessage(MqttApplicationMessageReceivedEventArgs x)
    {
        this.ShowReceivedMessage(x);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the received subscriber message event.
    /// </summary>
    /// <param name="x">The MQTT application message received event args.</param>
    private Task OnSubscriberMessageReceived(MqttApplicationMessageReceivedEventArgs x)
    {
        this.ShowReceivedMessage(x);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Writes a received message as the newest line into the subscriber text box.
    /// </summary>
    /// <param name="x">The MQTT application message received event args.</param>
    private void ShowReceivedMessage(MqttApplicationMessageReceivedEventArgs x)
    {
        if (this.IsDisposed || !this.IsHandleCreated)
        {
            return;
        }

        var item = MessageFormatter.FormatReceivedMessage(
            DateTimeOffset.Now,
            x.ApplicationMessage.Topic,
            x.ApplicationMessage.ConvertPayloadToString(),
            x.ApplicationMessage.QualityOfServiceLevel);
        this.BeginInvoke((MethodInvoker)delegate { this.TextBoxSubscriber.Text = item + Environment.NewLine + this.TextBoxSubscriber.Text; });
    }

    /// <summary>
    /// The method that handles the text changes in the text box.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private void TextBoxPortTextChanged(object sender, EventArgs e)
    {
        if (PortParser.TryParsePort(this.TextBoxPort.Text, out var parsedPort))
        {
            this.port = parsedPort.ToString();
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
        if (this.IsDisposed || !this.IsHandleCreated)
        {
            return;
        }

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
