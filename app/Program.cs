using app.Models;
using app.Monitoring;
using app.RouteHandlers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("ConnStr");

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

builder.Services.AddHttpClient<HttpMonitorChecker>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

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