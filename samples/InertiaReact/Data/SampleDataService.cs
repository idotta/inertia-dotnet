using InertiaReact.Models;

namespace InertiaReact.Data;

public class SampleDataService
{
    private readonly List<User> _users;
    private int _nextId = 4;

    public SampleDataService()
    {
        _users = new List<User>
        {
            new User { Id = 1, Name = "Alice Johnson", Email = "alice@example.com", Role = "Admin", CreatedAt = DateTime.UtcNow.AddDays(-30) },
            new User { Id = 2, Name = "Bob Smith", Email = "bob@example.com", Role = "User", CreatedAt = DateTime.UtcNow.AddDays(-20) },
            new User { Id = 3, Name = "Carol White", Email = "carol@example.com", Role = "User", CreatedAt = DateTime.UtcNow.AddDays(-10) }
        };
    }

    public List<User> GetUsers() => _users.ToList();

    public User? GetUser(int id) => _users.FirstOrDefault(u => u.Id == id);

    public User CreateUser(User user)
    {
        user.Id = _nextId++;
        user.CreatedAt = DateTime.UtcNow;
        _users.Add(user);
        return user;
    }

    public bool UpdateUser(int id, User user)
    {
        var existing = GetUser(id);
        if (existing == null) return false;

        existing.Name = user.Name;
        existing.Email = user.Email;
        existing.Role = user.Role;
        return true;
    }

    public bool DeleteUser(int id)
    {
        var user = GetUser(id);
        if (user == null) return false;

        _users.Remove(user);
        return true;
    }
}
