using Web.Components;
using Web.Features.Chat;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddChatFeature(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler("/Error", createScopeForErrors: true);
app.UseHsts();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
