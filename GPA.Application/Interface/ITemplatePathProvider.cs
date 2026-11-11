using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Application.Interface
{
    public interface ITemplatePathProvider
    {
        string GetPolicyTemplatePath();
        string GetPaymentProofTemplatePath();
    }

}
