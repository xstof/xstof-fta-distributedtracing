
using System.Text.Json.Serialization;
using System;

namespace eg_webhook_api{

    public class GridEvent<T> where T: class
    {
        public string Id { get; set;}
        public string EventType { get; set;}
        public string Subject {get; set;}
        public DateTime EventTime { get; set; } 
        public T Data { get; set; } 
        public string Topic { get; set; }
    }

    public class CloudEvent<T> where T : class
    {
        [JsonPropertyName("specversion")]
        public string SpecVersion { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("subject")]
        public string Subject { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("time")]
        public string Time { get; set; }

        [JsonPropertyName("data")]
        public T Data { get; set; }

    }
}