using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ZoologAPP.Models;

namespace ZoologAPP.Services;

public class AuthService
{
    const string UsersKey = "users_db";
    const string CurrentKey = "current_user_id";

    public User? CurrentUser { get; private set; }
    public bool IsLoggedIn => CurrentUser != null;

    public AuthService()
    {
        var users = LoadUsers();

        // demo-account beim ersten start
        if (users.Count == 0)
        {
            users.Add(new User
            {
                Username = "Demo",
                Email = "demo@zoolog.app",
                PasswordHash = Hash("demo123"),
                Role = "admin"
            });
            SaveUsers(users);
        }

        var savedId = Preferences.Get(CurrentKey, null);
        if (savedId != null)
            CurrentUser = users.FirstOrDefault(u => u.Id == savedId);
    }

    public (bool ok, string error) Login(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return (false, "Bitte alle Felder ausfüllen");

        var user = LoadUsers().FirstOrDefault(u =>
            u.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));

        if (user == null || user.PasswordHash != Hash(password))
            return (false, "E-Mail oder Passwort falsch");

        CurrentUser = user;
        Preferences.Set(CurrentKey, user.Id);
        return (true, "");
    }

    public (bool ok, string error) Register(string username, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(username)) return (false, "Benutzername fehlt");
        if (string.IsNullOrWhiteSpace(email)) return (false, "E-Mail fehlt");
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return (false, "Passwort mind. 6 Zeichen");

        var users = LoadUsers();
        if (users.Any(u => u.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase)))
            return (false, "E-Mail bereits registriert");

        users.Add(new User
        {
            Username = username.Trim(),
            Email = email.Trim().ToLower(),
            PasswordHash = Hash(password),
            Role = "user"
        });
        SaveUsers(users);
        return (true, "");
    }

    public void Logout()
    {
        CurrentUser = null;
        Preferences.Remove(CurrentKey);
    }

    List<User> LoadUsers()
    {
        var json = Preferences.Get(UsersKey, "[]");
        return JsonSerializer.Deserialize<List<User>>(json) ?? [];
    }

    void SaveUsers(List<User> users) =>
        Preferences.Set(UsersKey, JsonSerializer.Serialize(users));

    static string Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input + "z0ol0g_s4lt"));
        return Convert.ToHexString(bytes);
    }
}
