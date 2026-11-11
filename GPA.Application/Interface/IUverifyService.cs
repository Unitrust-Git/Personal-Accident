using GPA.Application.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Application.Interface
{
    public interface IUverifyService
    {
        Task<UverifyResponse?> GetUserByNINAsync(string nin);
        Task<BusinessVerificationResponse> VerifyBusinessAsync(string registrationNumber,string countryCode = "NG",string? registrationName = null,bool premium = false);
    }
}
