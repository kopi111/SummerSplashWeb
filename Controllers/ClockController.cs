using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerSplashWeb.Services;
using SummerSplashWeb.Models;
using Dapper;

namespace SummerSplashWeb.Controllers
{
    [Authorize]
    public class ClockController : Controller
    {
        private readonly IClockService _clockService;
        private readonly IUserService _userService;
        private readonly ILocationService _locationService;
        private readonly IScheduleService _scheduleService;
        private readonly ILogger<ClockController> _logger;

        // Clock in/out settings
        private const int EARLY_CLOCK_IN_MINUTES = 10;
        private const int GRACE_PERIOD_MINUTES = 30;

        public ClockController(
            IClockService clockService,
            IUserService userService,
            ILocationService locationService,
            IScheduleService scheduleService,
            ILogger<ClockController> logger)
        {
            _clockService = clockService;
            _userService = userService;
            _locationService = locationService;
            _scheduleService = scheduleService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                var activeShifts = await _clockService.GetActiveShiftsAsync();
                var todaysRecords = await _clockService.GetTodaysRecordsAsync();
                var locations = await _locationService.GetAllLocationsAsync();

                // Get today's schedules for late/absence checking
                var today = DateTime.Today;
                var todaysSchedules = await _scheduleService.GetSchedulesAsync(today, today.AddDays(1).AddSeconds(-1));

                ViewBag.Users = users;
                ViewBag.ActiveShifts = activeShifts;
                ViewBag.TodaysRecords = todaysRecords;
                ViewBag.Locations = locations;
                ViewBag.TodaysSchedules = todaysSchedules;
                ViewBag.EarlyClockInMinutes = EARLY_CLOCK_IN_MINUTES;
                ViewBag.GracePeriodMinutes = GRACE_PERIOD_MINUTES;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading clock records");
                ViewBag.Error = "Error loading clock data. Please try again.";
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> EmployeeClockHistory(int userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                    return NotFound();

                var start = startDate ?? DateTime.Now.AddDays(-7);
                var end = endDate ?? DateTime.Now;

                var clockRecords = await _clockService.GetClockRecordsByUserAsync(userId, start, end);
                var totalHours = await _clockService.GetTotalHoursWorkedAsync(userId, start, end);
                var locations = await _locationService.GetAllLocationsAsync();

                ViewBag.User = user;
                ViewBag.StartDate = start;
                ViewBag.EndDate = end;
                ViewBag.ClockRecords = clockRecords;
                ViewBag.TotalHours = totalHours;
                ViewBag.Locations = locations;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading clock history for user {UserId}", userId);
                TempData["Error"] = "Error loading clock history. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClockIn(int userId, int locationId)
        {
            try
            {
                var clockRecord = new ClockRecord
                {
                    UserId = userId,
                    LocationId = locationId,
                    ClockInTime = DateTime.Now,
                    CreatedAt = DateTime.Now
                };

                var result = await _clockService.ClockInAsync(clockRecord);

                if (result)
                {
                    TempData["Success"] = "Clocked in successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to clock in. Please try again.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clocking in user {UserId}", userId);
                TempData["Error"] = "Error clocking in. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> CurrentlyClocked()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                var activeShifts = await _clockService.GetActiveShiftsAsync();
                var locations = await _locationService.GetAllLocationsAsync();

                ViewBag.Users = users;
                ViewBag.ActiveShifts = activeShifts;
                ViewBag.Locations = locations;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading currently clocked in");
                TempData["Error"] = "Error loading data.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Punches(DateTime? date = null, string? filter = null)
        {
            try
            {
                var selectedDate = date ?? DateTime.Today;
                var users = await _userService.GetAllUsersAsync();
                var locations = await _locationService.GetAllLocationsAsync();

                // Get schedules for the selected date
                var schedules = await _scheduleService.GetSchedulesAsync(selectedDate, selectedDate.AddDays(1).AddSeconds(-1));

                // Get clock records for the selected date
                var clockRecords = await _clockService.GetTodaysRecordsAsync();
                // Filter to selected date if not today
                if (selectedDate.Date != DateTime.Today)
                {
                    using var connection = ((DatabaseService)Request.HttpContext.RequestServices.GetService<IDatabaseService>()!).CreateConnection();
                    var sql = @"SELECT cr.*, u.FirstName + ' ' + u.LastName AS UserName, jl.Name AS LocationName
                                FROM ClockRecords cr
                                INNER JOIN Users u ON cr.UserId = u.UserId
                                INNER JOIN JobLocations jl ON cr.LocationId = jl.LocationId
                                WHERE CAST(cr.ClockInTime AS DATE) = @Date
                                ORDER BY cr.ClockInTime DESC";
                    clockRecords = (await Dapper.SqlMapper.QueryAsync<ClockRecord>(connection, sql, new { Date = selectedDate.Date })).ToList();
                }

                ViewBag.SelectedDate = selectedDate;
                ViewBag.Users = users;
                ViewBag.Locations = locations;
                ViewBag.Schedules = schedules;
                ViewBag.ClockRecords = clockRecords;
                ViewBag.GracePeriodMinutes = GRACE_PERIOD_MINUTES;
                ViewBag.Filter = filter;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading punches");
                TempData["Error"] = "Error loading punches data.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Redirect old MissedPunches route to new Punches
        [HttpGet]
        public IActionResult MissedPunches(DateTime? date = null)
        {
            return RedirectToAction(nameof(Punches), new { date });
        }

        [HttpGet]
        public async Task<IActionResult> WorkHistoryByDay(DateTime? date = null)
        {
            try
            {
                var selectedDate = date ?? DateTime.Today;
                var users = await _userService.GetAllUsersAsync();
                var locations = await _locationService.GetAllLocationsAsync();

                // Get all clock records for the selected date
                using var connection = ((DatabaseService)Request.HttpContext.RequestServices.GetService<IDatabaseService>()!).CreateConnection();
                var sql = @"SELECT cr.*, u.FirstName + ' ' + u.LastName AS UserName, u.Position, jl.Name AS LocationName
                            FROM ClockRecords cr
                            INNER JOIN Users u ON cr.UserId = u.UserId
                            INNER JOIN JobLocations jl ON cr.LocationId = jl.LocationId
                            WHERE CAST(cr.ClockInTime AS DATE) = @Date
                            ORDER BY cr.ClockInTime DESC";
                var clockRecords = (await Dapper.SqlMapper.QueryAsync<ClockRecord>(connection, sql, new { Date = selectedDate.Date })).ToList();

                // Calculate totals
                var totalHours = clockRecords.Where(r => r.TotalHours.HasValue).Sum(r => r.TotalHours!.Value);
                var totalWorkers = clockRecords.Select(r => r.UserId).Distinct().Count();

                ViewBag.SelectedDate = selectedDate;
                ViewBag.Users = users;
                ViewBag.Locations = locations;
                ViewBag.ClockRecords = clockRecords;
                ViewBag.TotalHours = totalHours;
                ViewBag.TotalWorkers = totalWorkers;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading work history by day");
                TempData["Error"] = "Error loading work history data.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditRecord(int id)
        {
            try
            {
                using var connection = ((DatabaseService)Request.HttpContext.RequestServices.GetService<IDatabaseService>()!).CreateConnection();
                var sql = @"SELECT cr.*, u.FirstName + ' ' + u.LastName AS UserName, jl.Name AS LocationName
                            FROM ClockRecords cr
                            INNER JOIN Users u ON cr.UserId = u.UserId
                            INNER JOIN JobLocations jl ON cr.LocationId = jl.LocationId
                            WHERE cr.RecordId = @RecordId";
                var record = await Dapper.SqlMapper.QueryFirstOrDefaultAsync<ClockRecord>(connection, sql, new { RecordId = id });

                if (record == null)
                {
                    TempData["Error"] = "Record not found.";
                    return RedirectToAction(nameof(Index));
                }

                var locations = await _locationService.GetAllLocationsAsync();
                ViewBag.Locations = locations;

                return View(record);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading record for edit");
                TempData["Error"] = "Error loading record.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRecord(int id, ClockRecord record)
        {
            try
            {
                using var connection = ((DatabaseService)Request.HttpContext.RequestServices.GetService<IDatabaseService>()!).CreateConnection();

                // Calculate total hours if both times are provided
                decimal? totalHours = null;
                if (record.ClockOutTime.HasValue)
                {
                    totalHours = (decimal)(record.ClockOutTime.Value - record.ClockInTime).TotalHours;
                }

                var sql = @"UPDATE ClockRecords
                            SET ClockInTime = @ClockInTime, ClockOutTime = @ClockOutTime,
                                LocationId = @LocationId, TotalHours = @TotalHours
                            WHERE RecordId = @RecordId";

                await Dapper.SqlMapper.ExecuteAsync(connection, sql, new
                {
                    RecordId = id,
                    record.ClockInTime,
                    record.ClockOutTime,
                    record.LocationId,
                    TotalHours = totalHours
                });

                TempData["Success"] = "Clock record updated successfully.";
                return RedirectToAction(nameof(WorkHistoryByDay), new { date = record.ClockInTime.Date });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating record");
                TempData["Error"] = "Error updating record.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> WorkHistory(int? userId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                var locations = await _locationService.GetAllLocationsAsync();

                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today;

                ViewBag.Users = users;
                ViewBag.Locations = locations;
                ViewBag.StartDate = start;
                ViewBag.EndDate = end;
                ViewBag.SelectedUserId = userId;

                if (userId.HasValue)
                {
                    var user = await _userService.GetUserByIdAsync(userId.Value);
                    var clockRecords = await _clockService.GetClockRecordsByUserAsync(userId.Value, start, end.AddDays(1));
                    var totalHours = await _clockService.GetTotalHoursWorkedAsync(userId.Value, start, end.AddDays(1));

                    ViewBag.SelectedUser = user;
                    ViewBag.ClockRecords = clockRecords;
                    ViewBag.TotalHours = totalHours;

                    // Calculate summary stats
                    var totalDays = clockRecords.Select(r => r.ClockInTime.Date).Distinct().Count();
                    var avgHoursPerDay = totalDays > 0 ? (double)totalHours / totalDays : 0;

                    ViewBag.TotalDays = totalDays;
                    ViewBag.AvgHoursPerDay = avgHoursPerDay;
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading work history");
                TempData["Error"] = "Error loading work history data.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClockOut(int recordId)
        {
            try
            {
                var result = await _clockService.ClockOutAsync(recordId);

                if (result)
                {
                    TempData["Success"] = "Clocked out successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to clock out. Please try again.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clocking out record {RecordId}", recordId);
                TempData["Error"] = "Error clocking out. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
