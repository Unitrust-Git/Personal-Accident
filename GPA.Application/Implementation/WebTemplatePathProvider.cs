using GPA.Application.Interface;
using Microsoft.Extensions.Hosting;
using System.IO;
using IOPath = System.IO.Path;

namespace GPA.Application.Implementation
{
    public sealed class WebTemplatePathProvider : ITemplatePathProvider
    {
        private readonly IHostEnvironment _env;

        public WebTemplatePathProvider(IHostEnvironment env)
        {
            _env = env;
        }

        public string GetPolicyTemplatePath()
            => IOPath.Combine(GetWebRoot(), "templates", "Latest Policy Certificate 2.pdf");

        public string GetPaymentProofTemplatePath()
            => IOPath.Combine(GetWebRoot(), "templates", "payment-proof-template.pdf");

        private string GetWebRoot()
        {
            return IOPath.Combine(_env.ContentRootPath, "wwwroot");
        }
    }
}
