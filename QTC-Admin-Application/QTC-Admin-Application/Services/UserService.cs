using Microsoft.Data.SqlClient;
using QTC_Admin_Application.Models;

namespace QTC_Admin_Application.Services;

public class UserService
{
    private readonly string _conn;

    public UserService(IConfiguration config)
    {
        _conn = config.GetConnectionString("DefaultConnection");
    }

    
    public AppUser? ValidateUser(string username, string password)
    {
        using var conn = new SqlConnection(_conn);
        conn.Open();

        Console.WriteLine("ENTERED USERNAME: " + username);
        Console.WriteLine("ENTERED PASSWORD: " + password);

        using var cmd = new SqlCommand(
            "SELECT * FROM AppUser WHERE Username = @username",
            conn);

        cmd.Parameters.AddWithValue("@username", username);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
        {
            Console.WriteLine("❌ USER NOT FOUND IN DATABASE");
            return null;
        }

        var dbPassword = reader["PasswordHash"]?.ToString();

        Console.WriteLine("DB PASSWORD: " + dbPassword);

        if (dbPassword != password)
        {
            Console.WriteLine("❌ PASSWORD DOES NOT MATCH");
            return null;
        }

        Console.WriteLine("✅ LOGIN SUCCESS");

        return new AppUser
        {
            AppUserId = (int)reader["UserId"],
            Username = reader["Username"]?.ToString(),
            PasswordHash = dbPassword,
            UserRole = reader["UserRole"]?.ToString()
        };
    }

    public List<AppUser> GetAllUsers()
    {
        var users = new List<AppUser>();

        using var conn = new SqlConnection(_conn);
        conn.Open();

        using var cmd = new SqlCommand(
            "SELECT UserId, Username, UserRole FROM AppUser",
            conn);

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            users.Add(new AppUser
            {
                AppUserId = (int)reader["UserId"],
                Username = reader["Username"]?.ToString(),
                UserRole = reader["UserRole"]?.ToString()
            });
        }

        return users;
    }
}
