
using GPA.Application.Interface;
using GPA.Core.Dto.Uniware.Client;
using GPA.Infrastructure.HttpClients;
using Microsoft.AspNetCore.Mvc;

namespace GPA.Controllers
{
    [Route("GPA")]
    public class GPAController : Controller
    {
        private readonly IUverifyService _uverifyService;
        private readonly IUniwareClient _uniwareClient;

        public GPAController(
            IUverifyService uverifyService,
            IUniwareClient uniwareClient)
        {
            _uverifyService = uverifyService;
            _uniwareClient = uniwareClient;
        }

        public class VerifyNinRequest
        {
            public string Nin { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
        }


        [HttpPost("GetUserByNIN")]
        public async Task<IActionResult> GetUserByNIN([FromBody] VerifyNinRequest request)
        {

            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request"
                });
            }

            if (string.IsNullOrWhiteSpace(request.Nin))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "NIN is required"
                });
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Email is required"
                });
            }

            if (string.IsNullOrWhiteSpace(request.Phone))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Phone number is required"
                });
            }



            try
            {
                var response = await _uverifyService.GetUserByNINAsync(request.Nin);

                if (response == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "No response from verification service"
                    });
                }

                if (!response.Success || response.Data == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "NIN not found"
                    });
                }

                var customer = response.Data;

                var fullName = string.Join(" ",
                    new[]
                    {
                        customer.FirstName,
                        customer.MiddleName,
                        customer.LastName
                    }
                    .Where(x => !string.IsNullOrWhiteSpace(x)))
                    .Trim();

                if (string.IsNullOrWhiteSpace(fullName))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Unable to determine customer's full name from NIN verification. please contact support"
                    });
                }

                var createClientRequest = new CreateClientRequest
                {
                    surname = customer.LastName,

                    insured = fullName,

                    residence_addr = customer.Address?.AddressLine ?? "",

                    nationality = customer.Country ?? "NG",

                    state_residence = customer.Address?.State ?? "",

                    state_origin = customer.BirthState ?? "",

                    state_origin_lga = customer.BirthLGA ?? "",

                    dob_year = customer.DateOfBirth?.Year.ToString() ?? "",

                    dob_month = customer.DateOfBirth?.Month.ToString("00") ?? "",

                    dob_day = customer.DateOfBirth?.Day.ToString("00") ?? "",

                    gender = customer.Gender ?? "",

                    email = request.Email,
                    gsm = request.Phone,

                    nin = request.Nin,

                    id_type = "NIN",
                    id_no = request.Nin
                };


                var clientCode = await _uniwareClient.CreateClientAsync(createClientRequest);

                if (string.IsNullOrWhiteSpace(clientCode))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Unable to create client."
                    });
                }

                return Ok(new
                            {
                                success = true,

                                clientCode,

                                fullName = string.Join(" ",
                    new[]
                    {
                        customer.FirstName,
                        customer.MiddleName,
                        customer.LastName
                    }.Where(x => !string.IsNullOrWhiteSpace(x))),

                                email = request.Email,

                                phone = request.Phone,

                                nin = customer.IdNumber
                            });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An unexpected error occurred",
                    error = ex.Message
                });
            }
        }

        [HttpGet("VerifyBusiness")]
        public async Task<IActionResult> VerifyBusiness([FromQuery] string registrationNumber, [FromQuery] string countryCode = "NG")
        {
            if (string.IsNullOrWhiteSpace(registrationNumber))
                return BadRequest(new { success = false, message = "Registration number is required" });

            var result = await _uverifyService.VerifyBusinessAsync(registrationNumber, countryCode);

            return Ok(result);
        }

        [HttpGet("GetClo")]
        public async Task<IActionResult> GetClo(CancellationToken ct)
        {
            try
            {
                var result = await _uniwareClient.GetCloAsync(ct);

                if (result == null || result.CLO_LIST == null || !result.CLO_LIST.Any())
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "No CLO data returned from Uniware"
                    });
                }

                var cleaned = result.CLO_LIST
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.name) &&
                        !x.name.Contains("test", StringComparison.OrdinalIgnoreCase) &&
                        !x.name.Contains("demo", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                return Ok(new
                {
                    success = true,
                    CLO_LIST = cleaned
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to fetch CLO list",
                    error = ex.Message
                });
            }
        }

        [HttpGet("SearchClient")]
        public async Task<IActionResult> SearchClient([FromQuery] string email, [FromQuery] string phone)
        {
          if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Email is required"
                });
            }

            if (string.IsNullOrWhiteSpace(phone))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Phone number is required"
                });
            }

            try
            {
                var response = await _uniwareClient.FullSearchClientAsync(email, phone);

                if (response?.result?.record == null ||
                    !response.result.record.Any())
                {
                    return Ok(new
                    {
                        success = true,
                        exists = false,
                        clients = new List<object>()
                    });
                }


                var clients = response.result.record
                    .Select(x => new
                    {
                        clientCode = x.client_no,
                        name = x.name,
                        email = x.email,
                        phone = x.phone
                    })
                    .ToList();


                return Ok(new
                {
                    success = true,
                    exists = clients.Any(),
                    clients
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to search client",
                    error = ex.Message
                });
            }
        }

        [HttpPost("CreateClient")]
        public async Task<IActionResult> CreateClient( [FromBody] CreateClientRequest request)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request"
                });
            }


            try
            {
                var clientNo = await _uniwareClient.CreateClientAsync(request);


                if (string.IsNullOrWhiteSpace(clientNo))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Unable to create client"
                    });
                }


                return Ok(new
                {
                    success = true,
                    clientCode = clientNo
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

    }
}
