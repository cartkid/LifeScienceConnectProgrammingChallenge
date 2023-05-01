// See https://aka.ms/new-console-template for more information
using System.Net.Http.Headers;

using HttpClient client = new HttpClient();
client.DefaultRequestHeaders.Accept.Clear();

await DoWorkAsync(client);

static async Task DoWorkAsync(HttpClient client)
{

    var result = await client.GetStreamAsync()
}

Console.WriteLine("Hello, World!");
