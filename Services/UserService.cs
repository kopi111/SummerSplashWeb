using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using SummerSplashWeb.Models;

namespace SummerSplashWeb.Services
{
    public class UserService : IUserService
    {
        private readonly IDatabaseService _databaseService;

        public UserService(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            using var connection = _databaseService.CreateConnection();
            var sql = "SELECT * FROM Users ORDER BY CreatedAt DESC";
            var users = await connection.QueryAsync<User>(sql);
            return users.ToList();
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            using var connection = _databaseService.CreateConnection();
            var sql = "SELECT * FROM Users WHERE UserId = @UserId";
            return await connection.QueryFirstOrDefaultAsync<User>(sql, new { UserId = userId });
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            using var connection = _databaseService.CreateConnection();
            var sql = @"
                UPDATE Users
                SET FirstName = @FirstName,
                    LastName = @LastName,
                    PhoneNumber = @PhoneNumber,
                    Position = @Position,
                    Address = @Address,
                    EmergencyContact = @EmergencyContact,
                    EmergencyPhone = @EmergencyPhone,
                    HireDate = @HireDate,
                    Status = @Status,
                    Notes = @Notes,
                    IsActive = CASE WHEN @Status = 'Terminated' THEN 0 ELSE 1 END,
                    IsApproved = CASE WHEN @Status = 'Approved' THEN 1 ELSE IsApproved END,
                    ProfilePicture = @ProfilePicture
                WHERE UserId = @UserId";

            var result = await connection.ExecuteAsync(sql, user);
            return result > 0;
        }

        public async Task<bool> CreateUserAsync(User user)
        {
            using var connection = _databaseService.CreateConnection();
            var sql = @"
                INSERT INTO Users (FirstName, LastName, Email, PasswordHash, PhoneNumber, Position,
                    Address, EmergencyContact, EmergencyPhone, HireDate, Status, Notes,
                    IsActive, IsApproved, CreatedAt)
                VALUES (@FirstName, @LastName, @Email, @PasswordHash, @PhoneNumber, @Position,
                    @Address, @EmergencyContact, @EmergencyPhone, @HireDate, @Status, @Notes,
                    1, 0, GETDATE());
                SELECT CAST(SCOPE_IDENTITY() as int)";

            var userId = await connection.ExecuteScalarAsync<int>(sql, user);
            return userId > 0;
        }

        public async Task<string> GenerateInviteLinkAsync(string email, string position)
        {
            // Generate a unique invite code
            var inviteCode = Guid.NewGuid().ToString("N").Substring(0, 16);

            using var connection = _databaseService.CreateConnection();
            var sql = @"
                INSERT INTO InviteLinks (Email, Position, InviteCode, CreatedAt, ExpiresAt, IsUsed)
                VALUES (@Email, @Position, @InviteCode, GETDATE(), DATEADD(day, 7, GETDATE()), 0)";

            await connection.ExecuteAsync(sql, new { Email = email, Position = position, InviteCode = inviteCode });

            return inviteCode;
        }

        public async Task<bool> ApproveUserAsync(int userId)
        {
            using var connection = _databaseService.CreateConnection();
            var sql = "UPDATE Users SET IsApproved = 1 WHERE UserId = @UserId";
            var result = await connection.ExecuteAsync(sql, new { UserId = userId });
            return result > 0;
        }

        public async Task<bool> AssignPositionAsync(int userId, string position)
        {
            using var connection = _databaseService.CreateConnection();
            var sql = "UPDATE Users SET Position = @Position WHERE UserId = @UserId";
            var result = await connection.ExecuteAsync(sql, new { UserId = userId, Position = position });
            return result > 0;
        }

        public async Task<bool> DeactivateUserAsync(int userId)
        {
            using var connection = _databaseService.CreateConnection();
            var sql = "UPDATE Users SET IsActive = 0 WHERE UserId = @UserId";
            var result = await connection.ExecuteAsync(sql, new { UserId = userId });
            return result > 0;
        }

        public async Task<bool> ActivateUserAsync(int userId)
        {
            using var connection = _databaseService.CreateConnection();
            var sql = "UPDATE Users SET IsActive = 1 WHERE UserId = @UserId";
            var result = await connection.ExecuteAsync(sql, new { UserId = userId });
            return result > 0;
        }

        public async Task<List<User>> GetUsersByPositionAsync(string position)
        {
            using var connection = _databaseService.CreateConnection();
            var sql = "SELECT * FROM Users WHERE Position = @Position AND IsActive = 1";
            var users = await connection.QueryAsync<User>(sql, new { Position = position });
            return users.ToList();
        }

        public async Task<List<User>> GetPendingApprovalsAsync()
        {
            using var connection = _databaseService.CreateConnection();
            var sql = "SELECT * FROM Users WHERE IsApproved = 0 ORDER BY CreatedAt DESC";
            var users = await connection.QueryAsync<User>(sql);
            return users.ToList();
        }

        public async Task<List<User>> GetPendingUsersAsync()
        {
            using var connection = _databaseService.CreateConnection();
            var sql = "SELECT * FROM Users WHERE IsApproved = 0 ORDER BY CreatedAt DESC";
            var users = await connection.QueryAsync<User>(sql);
            return users.ToList();
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            using var connection = _databaseService.CreateConnection();
            var sql = "DELETE FROM Users WHERE UserId = @UserId";
            var result = await connection.ExecuteAsync(sql, new { UserId = userId });
            return result > 0;
        }
    }
}
