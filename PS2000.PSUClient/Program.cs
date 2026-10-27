using MQTTnet;

const string brokerHost = "localhost";
const int brokerPort = 1883;
const string psuId = "1";
var topicPrefix = $"/PSU/PSU2000/{psuId}";

var factory = new MqttClientFactory();
using var client = factory.CreateMqttClient();
