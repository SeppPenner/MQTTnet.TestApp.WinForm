namespace MQTTnet.TestApp.WinForm
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    using MQTTnet.Server;

    using Newtonsoft.Json;


    public class JsonServerStorage : IMqttServerStorage
    {
        private readonly string filename = Path.Combine(Directory.GetCurrentDirectory(), "Retained.json");

        public void Clear()
        {
            if (File.Exists(this.filename))
            {
                File.Delete(this.filename);
            }
        }

        public async Task<IList<MqttApplicationMessage>> LoadRetainedMessagesAsync()
        {
            await Task.CompletedTask;

            if (!File.Exists(this.filename))
            {
                return new List<MqttApplicationMessage>();
            }

            try
            {
                var json = File.ReadAllText(this.filename);
                return JsonConvert.DeserializeObject<List<MqttApplicationMessage>>(json);
            }
            catch
            {
                return new List<MqttApplicationMessage>();
            }
        }

        public async Task SaveRetainedMessagesAsync(IList<MqttApplicationMessage> messages)
        {
            await Task.CompletedTask;
            var json = JsonConvert.SerializeObject(messages);
            File.WriteAllText(this.filename, json);
        }
    }
}
