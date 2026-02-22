using AntDesign.ProLayout;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAntDesign();
//builder.Services.Configure<ProSettings>(builder.Configuration.GetSection("ProSettings"));


await builder.Build().RunAsync();
