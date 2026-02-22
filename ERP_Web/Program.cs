using AntDesign;
using AntDesign.ProLayout;
using ERP_Web.Client.Pages;
using ERP_Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAntDesign();
builder.Services.Configure<ProSettings>(builder.Configuration.GetSection("ProSettings"));

// Program.cs
builder.Services.AddAntDesign();
//builder.Services.AddScoped<IComponentIdGenerator, GuidComponentIdGenerator>();


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

//#if (full)
//builder.Services.AddInteractiveStringLocalizer();
//builder.Sbuilder.Services.AddLocalization();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(ERP_Web.Client._Imports).Assembly);

app.Run();
