using app.Models;
using app.Monitoring;
using app.RouteHandlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Security.AntiSSRF;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("ConnStr");

var policy = new AntiSSRFPolicy(PolicyConfigOptions.ExternalOnlyLatest);

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddValidation();

builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddEntityFrameworkStores<PulseDbContext>();

builder.Services.AddDbContext<PulseDbContext>(options => 
{
    options.UseNpgsql(connectionString);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<MonitorWorker>();
builder.Services.AddScoped<CheckResultWriter>();

builder.Services.AddHttpClient<HttpMonitorChecker>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(10);
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = policy.GetHandler();
        handler.AllowAutoRedirect = false;
        handler.UseCookies = false;
        return handler;
    });

builder.Services.AddScoped<DueMonitorProcessor>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.RegisterServiceRoutes();
app.RegisterMonitorRoutes();
app.MapIdentityApi<ApplicationUser>();

app.UseAuthentication();
app.UseAuthorization();

app.Run();