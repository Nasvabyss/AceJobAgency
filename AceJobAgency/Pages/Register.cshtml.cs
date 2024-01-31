using AceJobAgency.Model;
using AceJobAgency.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace AceJobAgency.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IWebHostEnvironment hostingEnvironment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RegisterModel> _logger; // Add this field
        private readonly AuthDbContext _context; // Add this field


        [BindProperty]
        public Register RModel { get; set; }

        [BindProperty]
        public string RecaptchaResponseRegister { get; set; }

        public string SiteKey { get; } = "6LfG4lwpAAAAANktSN1c4ig7odTgvKDLQjRVk0Rz";

        private readonly EncryptionService _encryptionService;

        public RegisterModel(UserManager<ApplicationUser> userManager,
                         SignInManager<ApplicationUser> signInManager,
                         IWebHostEnvironment hostingEnvironment,
                         IConfiguration configuration,
                         EncryptionService encryptionService,
                         ILogger<RegisterModel> logger,
                         AuthDbContext context) // Add ILogger parameter
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.hostingEnvironment = hostingEnvironment;
            _configuration = configuration;
            _encryptionService = encryptionService;
            _logger = logger; // Initialize the logger
            _context = context;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Password complexity validation
            if (!IsPasswordComplex(RModel.Password))
            {
                ModelState.AddModelError("RModel.Password", "Password does not meet complexity requirements.");
                return Page();
            }

            // Email format validation
            if (!new EmailAddressAttribute().IsValid(RModel.Email))
            {
                ModelState.AddModelError("RModel.Email", "Invalid email format.");
                return Page();
            }

            // Age validation (must be above 18)
            var age = DateTime.Today.Year - RModel.DateOfBirth.Year;
            if (RModel.DateOfBirth > DateTime.Today.AddYears(-age)) age--;
            if (age < 18)
            {
                ModelState.AddModelError("RModel.DateOfBirth", "You must be at least 18 years old.");
            }

            // NRIC validation (1 letter at start, 1 letter at end, 7 digits in between)
            if (!Regex.IsMatch(RModel.NRIC, @"^[A-Z][0-9]{7}[A-Z]$"))
            {
                ModelState.AddModelError("RModel.NRIC", "Invalid NRIC format.");
            }

            // Check if there are any validation errors before proceeding
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {

                // reCAPTCHA validation
                var client = new HttpClient();
                var recaptchaVerificationUrl = $"https://www.google.com/recaptcha/api/siteverify?secret={_configuration["ReCaptcha:SecretKey"]}&response={RecaptchaResponseRegister}";
                var response = await client.PostAsync(recaptchaVerificationUrl, null);
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var recaptchaResult = JsonConvert.DeserializeObject<RecaptchaResponse>(jsonResponse);
                var existingUser = await userManager.FindByEmailAsync(RModel.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError(string.Empty, "Email already in use.");
                    return Page();
                }

                if (!recaptchaResult.Success)
                {
                    ModelState.AddModelError(string.Empty, "reCAPTCHA validation failed");
                    return Page();
                }

                var user = new ApplicationUser
                {
                    UserName = RModel.Email,
                    Email = RModel.Email,
                    FirstName = RModel.FirstName,
                    LastName = RModel.LastName,
                    Gender = RModel.Gender,
                    DateOfBirth = RModel.DateOfBirth,
                    NRIC = _encryptionService.Encrypt(RModel.NRIC),
                    WhoAmI = RModel.WhoAmI,
                    CurrentSessionId = Guid.NewGuid().ToString(), // Set a default value for CurrentSessionId
                };

                // Check if a resume has been uploaded
                if (RModel.Resume != null)
                {
                    var uploadsFolderPath = Path.Combine(hostingEnvironment.WebRootPath, "resumes");
                    if (!Directory.Exists(uploadsFolderPath))
                    {
                        Directory.CreateDirectory(uploadsFolderPath);
                    }

                    // Generate a unique file name to avoid name conflicts
                    var uniqueFileName = Path.GetRandomFileName();
                    var filePath = Path.Combine(uploadsFolderPath, uniqueFileName);

                    try
                    {
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await RModel.Resume.CopyToAsync(fileStream);
                        }

                        // Save the path and the original file name in the user object
                        user.Resume = uniqueFileName; // Save the path to the file
                        user.ResumeOriginalName = RModel.Resume.FileName; // Save the original file name
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError(string.Empty, "Error uploading file: " + ex.Message);
                        return Page();
                    }
                }

                var result = await userManager.CreateAsync(user, RModel.Password);
                if (result.Succeeded)
                {
                    await _context.LogAuditAsync(user.Id, "Registration", "New user registered");
                    return RedirectToPage("/Login");
                }
                else
                {
                    // Handle failure
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }

            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2601)
                {
                    ModelState.AddModelError(string.Empty, "An account with this email already exists.");
                }
                else
                {
                    _logger.LogError(ex, "An error occurred while creating the user."); // Use the logger here
                    ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                }
            }
            return Page();
        }

        private bool IsPasswordComplex(string password)
        {
            bool hasUpperCase = password.Any(char.IsUpper);
            bool hasLowerCase = password.Any(char.IsLower);
            bool hasNumbers = password.Any(char.IsDigit);
            bool hasSpecialChars = password.Any(ch => !char.IsLetterOrDigit(ch));
            bool isLongEnough = password.Length >= 12;

            return hasUpperCase && hasLowerCase && hasNumbers && hasSpecialChars && isLongEnough;
        }

        public class RecaptchaResponse
        {
            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("error-codes")]
            public List<string> ErrorCodes { get; set; }
        }
    }
}