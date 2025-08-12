using Microsoft.Extensions.AI;
using System.ComponentModel;

var ollamaEndpoint = "http://localhost:11434";
var chatModel = "llama3.2:3b";

IChatClient client = new OllamaChatClient(
    endpoint: ollamaEndpoint,
    modelId: chatModel)
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

ChatOptions options = new ChatOptions
{
    Tools = [
        AIFunctionFactory.Create(GetTheWeather),
        AIFunctionFactory.Create(GetCityInfo)
    ]    
};

var question = "Solve 2+2. Provide an accurate and short answer";
Console.WriteLine($"question: {question}");
var response = await client.GetResponseAsync(question, options);
Console.WriteLine($"response: {response}");

Console.WriteLine();

question = "Do I need an umbrella today in my city?. Provide an accurate and short answer with city and country name.";
Console.WriteLine($"question: {question}");
response = await client.GetResponseAsync(question, options);
Console.WriteLine($"response: {response}");



[Description("Get the weather")]
static string GetTheWeather()
{
    Console.WriteLine("\tGetTheWeather function invoked.");

    var temperature = Random.Shared.Next(5, 20);
    var conditions = Random.Shared.Next(0, 1) == 0 ? "sunny" : "rainy";
    var weather = $"The weather is {temperature} degrees C and {conditions}.";
    Console.WriteLine($"\tGetTheWeather result: {weather}.");
    return weather;
}

[Description("Get the city and country info")]
static string GetCityInfo()
{
    Console.WriteLine("GetCityInfo function invoked.");
    return "you are in beautiful city of Sarajevo, capital of Bosnia";
}