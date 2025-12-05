using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerSplashWeb.Services;
using SummerSplashWeb.Models;

namespace SummerSplashWeb.Controllers
{
    [Authorize]
    public class LocationsController : Controller
    {
        private readonly ILocationService _locationService;
        private readonly IUserService _userService;
        private readonly ILogger<LocationsController> _logger;

        public LocationsController(
            ILocationService locationService,
            IUserService userService,
            ILogger<LocationsController> logger)
        {
            _locationService = locationService;
            _userService = userService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var locations = await _locationService.GetAllLocationsAsync();
                return View(locations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading locations");
                ViewBag.Error = "Error loading locations. Please try again.";
                return View(new List<JobLocation>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var location = await _locationService.GetLocationByIdAsync(id);
                if (location == null)
                    return NotFound();

                // Load contacts for this location
                var contacts = await _locationService.GetLocationContactsAsync(id);
                ViewBag.Contacts = contacts;

                return View(location);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading location details");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                // Get employees for supervisor dropdown
                var employees = await _userService.GetAllUsersAsync();
                ViewBag.Employees = employees;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading employees for location create");
                ViewBag.Employees = new List<User>();
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JobLocation location,
            string? Contact1Name, string? Contact1Role, string? Contact1Phone, string? Contact1Email,
            string? Contact2Name, string? Contact2Role, string? Contact2Phone, string? Contact2Email)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Create the location
                    var locationId = await _locationService.CreateLocationAsync(location);

                    // Create contacts if provided
                    if (!string.IsNullOrWhiteSpace(Contact1Name))
                    {
                        var contact1 = new LocationContact
                        {
                            LocationId = locationId,
                            ContactName = Contact1Name,
                            ContactRole = Contact1Role,
                            ContactPhone = Contact1Phone,
                            ContactEmail = Contact1Email,
                            IsPrimary = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _locationService.CreateLocationContactAsync(contact1);
                    }

                    if (!string.IsNullOrWhiteSpace(Contact2Name))
                    {
                        var contact2 = new LocationContact
                        {
                            LocationId = locationId,
                            ContactName = Contact2Name,
                            ContactRole = Contact2Role,
                            ContactPhone = Contact2Phone,
                            ContactEmail = Contact2Email,
                            IsPrimary = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _locationService.CreateLocationContactAsync(contact2);
                    }

                    TempData["Success"] = "Location created successfully!";
                    return RedirectToAction(nameof(Index));
                }

                // Reload employees if validation fails
                var employees = await _userService.GetAllUsersAsync();
                ViewBag.Employees = employees;
                return View(location);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating location");
                ModelState.AddModelError("", "Error creating location. Please try again.");

                var employees = await _userService.GetAllUsersAsync();
                ViewBag.Employees = employees;
                return View(location);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var location = await _locationService.GetLocationByIdAsync(id);
                if (location == null)
                    return NotFound();

                // Get employees for supervisor dropdown
                var employees = await _userService.GetAllUsersAsync();
                ViewBag.Employees = employees;

                // Get existing contacts
                var contacts = await _locationService.GetLocationContactsAsync(id);
                ViewBag.Contacts = contacts;

                return View(location);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading location for edit");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, JobLocation location,
            string? Contact1Name, string? Contact1Role, string? Contact1Phone, string? Contact1Email,
            string? Contact2Name, string? Contact2Role, string? Contact2Phone, string? Contact2Email)
        {
            try
            {
                if (id != location.LocationId)
                    return BadRequest();

                if (ModelState.IsValid)
                {
                    await _locationService.UpdateLocationAsync(location);

                    // Delete existing contacts and recreate
                    await _locationService.DeleteLocationContactsAsync(id);

                    if (!string.IsNullOrWhiteSpace(Contact1Name))
                    {
                        var contact1 = new LocationContact
                        {
                            LocationId = id,
                            ContactName = Contact1Name,
                            ContactRole = Contact1Role,
                            ContactPhone = Contact1Phone,
                            ContactEmail = Contact1Email,
                            IsPrimary = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _locationService.CreateLocationContactAsync(contact1);
                    }

                    if (!string.IsNullOrWhiteSpace(Contact2Name))
                    {
                        var contact2 = new LocationContact
                        {
                            LocationId = id,
                            ContactName = Contact2Name,
                            ContactRole = Contact2Role,
                            ContactPhone = Contact2Phone,
                            ContactEmail = Contact2Email,
                            IsPrimary = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _locationService.CreateLocationContactAsync(contact2);
                    }

                    TempData["Success"] = "Location updated successfully!";
                    return RedirectToAction(nameof(Index));
                }

                var employees = await _userService.GetAllUsersAsync();
                ViewBag.Employees = employees;
                return View(location);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating location");
                ModelState.AddModelError("", "Error updating location. Please try again.");

                var employees = await _userService.GetAllUsersAsync();
                ViewBag.Employees = employees;
                return View(location);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                // Delete contacts first
                await _locationService.DeleteLocationContactsAsync(id);
                await _locationService.DeleteLocationAsync(id);
                TempData["Success"] = "Location deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting location");
                TempData["Error"] = "Error deleting location. It may be in use.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
