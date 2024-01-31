using AceJobAgency.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AceJobAgency.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager; // Add UserManager
        private readonly AuthDbContext _context; // Add AuthDbContext

        public LogoutModel(SignInManager<ApplicationUser> signInManager, ILogger<LogoutModel> logger, UserManager<ApplicationUser> userManager, AuthDbContext context) // Inject UserManager and AuthDbContext
        {
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager; // Initialize UserManager
            _context = context; // Initialize AuthDbContext
        }

        public IActionResult OnGet()
        {
            return RedirectToPage("/Login");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("Attempting to log out user.");

            try
            {
                var userId = _userManager.GetUserId(User);
                var activeSession = await _context.UserSessions.FirstOrDefaultAsync(us => us.UserId == userId && us.IsActive);
                if (activeSession != null)
                {
                    activeSession.IsActive = false;
                    await _context.SaveChangesAsync();
                }

                await _signInManager.SignOutAsync();
                await _context.LogAuditAsync(userId, "Logout", "User logged out");
                _logger.LogInformation("User logged out successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during logout.");
            }

            return RedirectToPage("/Login");
        }
    }
}
