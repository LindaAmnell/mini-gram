
using mini_gramUI.Components;
using mini_gramUI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient("MinGramApi", client =>
{
    var apiUrl = builder.Configuration["ApiUrl"]
        ?? throw new InvalidOperationException("ApiUrl saknas i appsettings.json");
    client.BaseAddress = new Uri(apiUrl);
});

builder.Services.AddScoped<ApiClientService>();

var app = builder.Build();

app.UseStaticFiles();  
app.UseAntiforgery();   

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();