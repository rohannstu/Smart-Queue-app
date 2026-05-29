using Microsoft.EntityFrameworkCore;
using SmartQueue.Infrastructure.Data.Context;
using SmartQueue.Infrastructure.Interfaces;
using SmartQueue.Infrastructure.Interceptors;
using SmartQueue.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddScoped<TenantInterceptor>();

builder.Services.AddDbContext<SmartQueueDbContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");

    options.UseNpgsql(connectionString);
    options.AddInterceptors(serviceProvider.GetRequiredService<TenantInterceptor>());
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
