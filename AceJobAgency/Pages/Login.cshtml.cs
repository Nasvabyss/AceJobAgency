using AceJobAgency.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace AceJobAgency.Pages
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoginModel> _logger;
        private readonly AuthDbContext _context;

        [BindProperty]
        public InputModel Input { get; set; }

        public string SiteKey => _configuration["ReCaptcha:SiteKey"];

        [BindProperty]
        public string RecaptchaResponseLogin { get; set; }

        public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IConfiguration configuration, ILogger<LoginModel> logger, AuthDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
            _context = context;
        }

        public void OnGet()
        {
            if (_signInManager.IsSignedIn(User))
            {
                Response.Redirect(Url.Content("~/"));
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var client = new HttpClient();
            var recaptchaVerificationUrl = $"https://www.google.com/recaptcha/api/siteverify?secret={_configuration["ReCaptcha:SecretKey"]}&response={RecaptchaResponseLogin}";
            var postTask = await client.PostAsync(recaptchaVerificationUrl, null);
            var jsonResult = await postTask.Content.ReadAsStringAsync();
            var recaptchaResult = JsonConvert.DeserializeObject<RecaptchaResponse>(jsonResult);

            if (!recaptchaResult.Success)
            {
                ModelState.AddModelError(string.Empty, "reCAPTCHA validation failed.");
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                _logger.LogWarning("Login attempt for non-existent user.");
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, isPersistent: false, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                // Log successful login
                await _context.LogAuditAsync(user.Id, "Login", "Successful login");

                var existingSession = await _context.UserSessions.FirstOrDefaultAsync(us => us.UserId == user.Id && us.IsActive);
                if (existingSession != null)
                {
                    TempData["ExistingSession"] = true;
                    TempData["UserId"] = user.Id;
                    return RedirectToPage("/SessionDecision");
                }
                else
                {
                    await CreateNewSession(user.Id);
                    return RedirectToPage("/Index");
                }
            }
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                    var remainingLockoutTime = lockoutEnd.HasValue ? lockoutEnd.Value - DateTimeOffset.UtcNow : TimeSpan.Zero;

                    _logger.LogWarning($"User account locked out: {Input.Email}");
                    TempData["LockoutEndTime"] = remainingLockoutTime.TotalMinutes > 0 ? remainingLockoutTime.ToString(@"mm\:ss") : string.Empty;
                    ModelState.AddModelError(string.Empty, "Account locked out. Please try again later.");
                    return Page();
                }
                else
                {
                    var attemptsLeft = await _userManager.GetAccessFailedCountAsync(user);
                    TempData["AttemptsLeft"] = 3 - attemptsLeft; // max attempts are 3
                    _logger.LogWarning($"Failed login attempt for {Input.Email}. Attempts left: {TempData["AttemptsLeft"]}");
                    ModelState.AddModelError(string.Empty, $"Invalid login attempt.");
                    return Page();
                }

            }
            else
            {
                // Log failed login
                await _context.LogAuditAsync(user.Id, "Login", "Failed login attempt");
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }
        }

        private async Task CreateNewSession(string userId)
        {
            var newSession = new UserSession
            {
                UserId = userId,
                SessionId = Guid.NewGuid().ToString(),
                LoginTime = DateTime.UtcNow,
                IsActive = true
            };

            _context.UserSessions.Add(newSession);
            await _context.SaveChangesAsync();
        }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public class RecaptchaResponse
        {
            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("error-codes")]
            public string[] ErrorCodes { get; set; }
        }
    }
}
