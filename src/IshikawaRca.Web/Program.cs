using IshikawaRca.Infrastructure;
using IshikawaRca.Web.Security;
using IshikawaRca.Web.Services;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.Configure<StandaloneRcaAuthenticationOptions>(builder.Configuration.GetSection("RcaSecurity"));
builder.Services
    .AddAuthentication(StandaloneRcaAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, StandaloneRcaAuthenticationHandler>(
        StandaloneRcaAuthenticationHandler.SchemeName,
        _ => { });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentRcaUserContext, CurrentRcaUserContext>();
builder.Services.AddIshikawaRcaInfrastructure(builder.Configuration);
builder.Services.Configure<EvidenceStorageOptions>(builder.Configuration.GetSection("EvidenceStorage"));
builder.Services.AddSingleton<IEvidenceFileStorage, EvidenceFileStorage>();
builder.Services.AddSingleton<IRcaPdfReportService, RcaPdfReportService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
