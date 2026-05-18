namespace QTC_Admin_Application.Models
{
    public class AppUser
    {
        public int AppUserId { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string UserRole { get; set; }
    }
}
