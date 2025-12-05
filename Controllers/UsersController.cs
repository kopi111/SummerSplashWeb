using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerSplashWeb.Models;
using SummerSplashWeb.Services;

namespace SummerSplashWeb.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? search = null, string? position = null, bool? active = null)
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(search))
                {
                    users = users.Where(u =>
                        u.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        u.LastName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        u.Email.Contains(search, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                if (!string.IsNullOrWhiteSpace(position))
                {
                    users = users.Where(u => u.Position == position).ToList();
                }

                if (active.HasValue)
                {
                    users = users.Where(u => u.IsActive == active.Value).ToList();
                }

                ViewBag.Search = search;
                ViewBag.Position = position;
                ViewBag.Active = active;
                ViewBag.Positions = users.Select(u => u.Position).Distinct().Where(p => !string.IsNullOrEmpty(p)).ToList();

                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users");
                ViewBag.Error = "Error loading users. Please try again.";
                return View(new List<User>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user details for ID {UserId}", id);
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var success = await _userService.ApproveUserAsync(id);
                if (success)
                {
                    TempData["Success"] = "User approved successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to approve user.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving user {UserId}", id);
                TempData["Error"] = "An error occurred while approving the user.";
            }

            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            try
            {
                var success = await _userService.DeactivateUserAsync(id);
                if (success)
                {
                    TempData["Success"] = "User deactivated successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to deactivate user.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user {UserId}", id);
                TempData["Error"] = "An error occurred while deactivating the user.";
            }

            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            try
            {
                var success = await _userService.ActivateUserAsync(id);
                if (success)
                {
                    TempData["Success"] = "User activated successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to activate user.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating user {UserId}", id);
                TempData["Error"] = "An error occurred while activating the user.";
            }

            return RedirectToAction("Details", new { id });
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new User());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    ViewBag.Error = "Email is required.";
                    return View(user);
                }

                // Set default password hash (user will reset on first login)
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("TempPassword123!");
                user.Status = "Pending";
                user.CreatedAt = DateTime.Now;

                var result = await _userService.CreateUserAsync(user);
                if (result)
                {
                    TempData["Success"] = $"Employee {user.FirstName} {user.LastName} created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ViewBag.Error = "Failed to create employee.";
                    return View(user);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating employee");
                ViewBag.Error = "An error occurred while creating the employee.";
                return View(user);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateInviteLink(string email, string position)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    TempData["Error"] = "Email is required to generate an invite link.";
                    return RedirectToAction(nameof(Index));
                }

                var inviteCode = await _userService.GenerateInviteLinkAsync(email, position ?? "Employee");

                // The invite link would be your app's URL + the invite code
                var inviteLink = $"{Request.Scheme}://{Request.Host}/Account/Register?invite={inviteCode}";

                TempData["Success"] = $"Invite link generated for {email}.";
                TempData["InviteLink"] = inviteLink;

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invite link");
                TempData["Error"] = "Failed to generate invite link.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user for edit {UserId}", id);
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User user)
        {
            try
            {
                if (id != user.UserId)
                {
                    return BadRequest();
                }

                // Only update specific fields
                var success = await _userService.UpdateUserAsync(user);
                if (success)
                {
                    TempData["Success"] = "User updated successfully.";
                    return RedirectToAction("Details", new { id });
                }
                else
                {
                    ViewBag.Error = "Failed to update user.";
                    return View(user);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                ViewBag.Error = "An error occurred while updating the user.";
                return View(user);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _userService.DeleteUserAsync(id);
                if (success)
                {
                    TempData["Success"] = "User deleted successfully.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Error"] = "Failed to delete user.";
                    return RedirectToAction("Details", new { id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                TempData["Error"] = "An error occurred while deleting the user.";
                return RedirectToAction("Details", new { id });
            }
        }
    }
}
