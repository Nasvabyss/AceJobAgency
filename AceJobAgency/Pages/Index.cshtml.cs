using AceJobAgency.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[Authorize]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly EncryptionService _encryptionService;
    private readonly AuthDbContext _context;

    public ApplicationUser CurrentUser { get; set; }

    public IndexModel(UserManager<ApplicationUser> userManager, EncryptionService encryptionService, AuthDbContext context)
    {
        _userManager = userManager;
        _encryptionService = encryptionService;
        _context = context;
    }
    public List<UserSession> ActiveSessions { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!User.Identity.IsAuthenticated)
        {
            // Redirect to login if the user is not authenticated
            return RedirectToPage("/Login");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            // Handle the case where the user ID is not found
            return RedirectToPage("/Error", new { errorMessage = "User ID not found in claims." });
        }

        CurrentUser = await _userManager.FindByIdAsync(userId);
        if (CurrentUser == null)
        {
            // Handle the case where the user is not found
            return RedirectToPage("/Error", new { errorMessage = "User not found." });
        }

        if (CurrentUser != null)
        {
            CurrentUser.NRIC = _encryptionService.Decrypt(CurrentUser.NRIC);
        }

        ActiveSessions = await _context.UserSessions
                          .Where(us => us.UserId == CurrentUser.Id && us.IsActive)
                          .ToListAsync();

        return Page();

    }
}
