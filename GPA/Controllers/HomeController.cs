using System.Diagnostics;
using GPA.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace GPA.Controllers
{
    public class HomeController : Controller
    {

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index(int? units)
        {
            ViewBag.Units = units ?? 1;
            return View();
        }

        public IActionResult Landing()
        {
            return View();
        }

        public PartialViewResult IndividualForm()
        {
            return PartialView("Forms/_IndividualForm");
        }

        public PartialViewResult CorporateForm()
        {
            return PartialView("Forms/_CorporateForm");
        }

        public IActionResult Invoice()
        {

            var invoiceJson = HttpContext.Session
                .GetString("invoiceData");


            if (string.IsNullOrEmpty(invoiceJson))
            {
                return RedirectToAction("Landing", "Home");
            }


            var model = JsonConvert
                .DeserializeObject<IndividualInvoiceViewModel>(
                    invoiceJson
                );


            return View(
                "InvoiceIndividual",
                model
            );

        }

        //public IActionResult Invoice()
        //{
        //    var accountType = HttpContext.Session.GetString("accountType");
        //    var nin = HttpContext.Session.GetString("nin");

        //    if (string.IsNullOrEmpty(accountType))
        //    {
        //        return RedirectToAction("Landing", "Home");
        //    }

        //    var invoiceId = HttpContext.Session.GetString("currentInvoiceId");

        //    if (string.IsNullOrEmpty(nin))
        //    {
        //        return RedirectToAction("Landing", "Home");
        //    }

        //    if (string.IsNullOrEmpty(invoiceId))
        //    {
        //        invoiceId = "INV-" + Guid.NewGuid().ToString("N")[..10].ToUpper();
        //        HttpContext.Session.SetString("currentInvoiceId", invoiceId);
        //    }

        //    if (accountType == "individual")
        //    {
        //        var unitsStr = HttpContext.Session.GetString("units") ?? "1";
        //        var units = int.TryParse(unitsStr, out var u) ? u : 1;

        //        var model = new IndividualInvoiceViewModel
        //        {
        //            InvoiceId = invoiceId,

        //            FirstName = HttpContext.Session.GetString("firstName"),
        //            LastName = HttpContext.Session.GetString("lastName"),
        //            NIN = HttpContext.Session.GetString("nin"),
        //            DOB = HttpContext.Session.GetString("dob"),
        //            Gender = HttpContext.Session.GetString("gender"),

        //            Email = HttpContext.Session.GetString("email"),
        //            Phone = HttpContext.Session.GetString("phone"),

        //            NextOfKinName = HttpContext.Session.GetString("nextOfKinName"),
        //            NextOfKinPhone = HttpContext.Session.GetString("nextOfKinPhone"),

        //            Units = units,
        //            UnitPrice = 1500,

        //            IsReferral = (HttpContext.Session.GetString("isReferral") ?? "")
        //                            .Equals("true", StringComparison.OrdinalIgnoreCase),
        //            CloCode = HttpContext.Session.GetString("cloCode"),
        //            CloName = HttpContext.Session.GetString("cloName"),

        //            Street = HttpContext.Session.GetString("street"),
        //            City = HttpContext.Session.GetString("city"),
        //            State = HttpContext.Session.GetString("state"),
        //            Town = HttpContext.Session.GetString("town"),  
        //            Country = HttpContext.Session.GetString("country") 
        //        };

        //        return View("InvoiceIndividual", model);
        //    }
        //    else
        //    {
        //        var companyJson = HttpContext.Session.GetString("companyData") ?? "{}";
        //        var companyData = JsonConvert.DeserializeObject<CorporateInvoiceViewModel>(companyJson);

        //        var insuredListJson = HttpContext.Session.GetString("insuredList") ?? "[]";
        //        companyData.InvoiceId = invoiceId;
        //        companyData.Beneficiaries = JsonConvert.DeserializeObject<List<CorporateInsuredViewModel>>(insuredListJson);

        //        return View("InvoiceCorporate", companyData);
        //    }
        //}


        [HttpPost]
        public IActionResult SetCorporateSession(string companyData, string insuredList)
        {

                HttpContext.Session.Remove("currentInvoiceId");

                HttpContext.Session.SetString("accountType", "corporate");
                HttpContext.Session.SetString("companyData", companyData);
                HttpContext.Session.SetString("insuredList", insuredList);
                return Ok();
            }

        [HttpPost]
        public IActionResult SetIndividualSession( [FromBody] IndividualInvoiceRequest request)
        {
            var invoiceId =
                "INV-" +
                Guid.NewGuid()
                .ToString("N")[..10]
                .ToUpper();


            var model = new IndividualInvoiceViewModel
            {
                InvoiceId = invoiceId,


                // Client reference
                ClientCode = request.ClientCode,


                // Display customer information
                FullName = request.FullName,
                Email = request.Email,
                Phone = request.Phone,


                // Coverage
                Units = request.Units,
                UnitPrice = 1500,


                // Additional information
                NextOfKinName = request.NextOfKinName,
                NextOfKinPhone = request.NextOfKinPhone,


                // Referral
                IsReferral = request.IsReferral,
                CloCode = request.CloCode,
                CloName = request.CloName
            };

            HttpContext.Session.SetString("currentInvoiceId", invoiceId);

            HttpContext.Session.SetString(
                "invoiceData",
                JsonConvert.SerializeObject(model)
            );


            return Ok(new
            {
                success = true,
                invoiceId
            });
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


    }
}
