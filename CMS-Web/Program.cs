using CMS_Web.Components.Services; // Adjust the namespace as per your project structure

var builder = WebApplication.CreateBuilder(args);

// Register HttpClient with base address
builder.Services.AddHttpClient<CmsApiService>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7238/");  // Set the base URL of your CMS API
});


// Register other services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();