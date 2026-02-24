using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TalkToMe.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddLocalization();
builder.Services.AddSingleton<ToastService>();

await builder.Build().RunAsync();
