using GPA.Application.Implementation;
using GPA.Application.Interface;
using GPA.Application.Services;
using GPA.Infrastructure.HttpClients;
using GPA.Infrastructure.Persistence;
using GPA.Infrastructure.Persistence.Implementation;
using GPA.Infrastructure.Persistence.Interface;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);
builder.Environment.EnvironmentName = "Development";

System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)
    ));

// Program.cs
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});



builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient<IUverifyService, UverifyService>();

builder.Services.AddScoped<IPolicyRepository, PolicyRepository>();
builder.Services.AddScoped<IIndividualPolicyRepository, IndividualPolicyRepository>();
builder.Services.AddScoped<ICorporatePolicyRepository, CorporatePolicyRepository>();
builder.Services.AddScoped<ICorporatePolicyBeneficiaryRepository, CorporatePolicyBeneficiaryRepository>();
builder.Services.AddScoped<IPolicyProvisioningJobReadRepository, PolicyProvisioningJobReadRepository>();

builder.Services.AddScoped<IPolicyCreationService, PolicyCreationService>();
builder.Services.AddScoped<IPolicyProvisioningJobReadService, PolicyProvisioningJobReadService>();


builder.Services.AddScoped<IIndividualPolicyService, IndividualPolicyService>();
builder.Services.AddScoped<ICorporatePolicyService, CorporatePolicyService>();
builder.Services.AddScoped<IPolicyCertificateService, PolicyCertificateService>();
builder.Services.AddSingleton<ITemplatePathProvider, WebTemplatePathProvider>();
builder.Services.AddScoped<IPolicyNaicomSyncService, PolicyNaicomSyncService>();

builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();


builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();



builder.Services.AddHttpClient<IPaystackClient, PaystackClient>((sp, http) =>
{
    var config = sp.GetRequiredService<IConfiguration>();

    http.BaseAddress = new Uri("https://api.paystack.co/");

    http.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", config["Paystack:SecretKey"]);

    http.DefaultRequestHeaders.Accept.Clear();
    http.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    SslProtocols =
        System.Security.Authentication.SslProtocols.Tls12
});

builder.Services.AddHttpClient<IUniwareClient , UniwareClient>((sp, http) =>
{
    var config = sp.GetRequiredService<IConfiguration>();

    http.BaseAddress = new Uri("https://uniware.unitrustinsurance.com/");
    http.Timeout = TimeSpan.FromMinutes(5);
    http.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", config["Uniware:Token"]);

    http.DefaultRequestHeaders.Accept.Clear();
    http.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    SslProtocols =
        System.Security.Authentication.SslProtocols.Tls12 |
        System.Security.Authentication.SslProtocols.Tls13
});

builder.Services.AddHostedService<PolicyProvisioningWorker>();
builder.Services.AddHostedService<PolicyNaicomSyncWorker>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Frame-Options"] = "DENY";

    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Landing}/{id?}");

app.Run();
