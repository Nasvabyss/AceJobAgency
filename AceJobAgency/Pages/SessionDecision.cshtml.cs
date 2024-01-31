using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using AceJobAgency.Model;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace AceJobAgency.Pages
{
    public class SessionDecisionModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AuthDbContext _context;

        public SessionDecisionModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, AuthDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        public async Task<IActionResult> OnPostContinueNewSessionAsync()
        {
            var userId = TempData["UserId"] as string;
            if (userId != null)
            {
                // Terminate all other sessions
                var sessions = await _context.UserSessions.Where(us => us.UserId == userId).ToListAsync();
                sessions.ForEach(s => s.IsActive = false);
                await _context.SaveChangesAsync();

                // Sign in the user and create a new session
                var user = await _userManager.FindByIdAsync(userId);
                await SignInUserAsync(user);
                return RedirectToPage("/Index");
            }
            return RedirectToPage("/Login");
        }

        public async Task<IActionResult> OnPostKeepExistingSessionAsync()
        {
            // Sign out the current user
            await _signInManager.SignOutAsync();
            return RedirectToPage("/Login");
        }

        private async Task SignInUserAsync(ApplicationUser user)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);

            var newSession = new UserSession
            {
                UserId = user.Id,
                SessionId = Guid.NewGuid().ToString(),
                LoginTime = DateTime.UtcNow,
                IsActive = true    
            };

            _context.UserSessions.Add(newSession);
            await _context.SaveChangesAsync();
        }
    }
}
