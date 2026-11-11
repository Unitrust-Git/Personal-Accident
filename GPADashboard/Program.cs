using GPA.Application.Implementation;
using GPA.Application.Implementation.DasboardServices;
using GPA.Application.Interface;
using GPA.Infrastructure.HttpClients;
using GPA.Infrastructure.Persistence;
using GPA.Infrastructure.Persistence.Implementation;
using GPA.Infrastructure.Persistence.Interface;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)
    ));

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "keys")))
    .SetApplicationName("PaDashboard");

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.ResponseType = "code";
    options.UsePkce = true;
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IPaymentTransactionReadService, PaymentTransactionReadService>();
builder.Services.AddScoped<IPolicyProvisioningJobReadRepository, PolicyProvisioningJobReadRepository>();
builder.Services.AddScoped<IPolicyProvisioningJobReadService, PolicyProvisioningJobReadService>();
builder.Services.AddScoped<IPolicyCertificateService, PolicyCertificateService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

builder.Services.AddScoped<IIndividualPolicyRepository, IndividualPolicyRepository>();
builder.Services.AddScoped<IPolicyRepository, PolicyRepository>();
builder.Services.AddScoped<IIndividualPolicyService, IndividualPolicyService>();
builder.Services.AddScoped<IPolicyProvisioningAdminService, PolicyProvisioningAdminService>();

builder.Services.AddScoped<ITemplatePathProvider, WebTemplatePathProvider>();
builder.Services.AddScoped<IIndividualPolicyService, IndividualPolicyService>();
builder.Services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
builder.Services.AddHttpClient<IUniwareClient, UniwareClient>((sp, http) =>
{
    var config = sp.GetRequiredService<IConfiguration>();

    http.BaseAddress = new Uri("https://uniware.unitrustinsurance.com/");
    http.Timeout = TimeSpan.FromMinutes(5);

    http.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", config["Uniware:Token"]);

    http.DefaultRequestHeaders.Accept.Clear();
    http.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
});


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=IndividualPolicies}/{action=Index}/{id?}");

app.Run();
