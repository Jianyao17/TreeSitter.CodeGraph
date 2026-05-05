using GithubAnalyzer.Fixtures.Models;

namespace GithubAnalyzer.Fixtures.Services;

public class UserService
{
    private readonly List<User> _users = new();

    public UserService()
    {
    }

    public User FindById(int id)
    {
        return _users.FirstOrDefault(u => u.Id == id)
            ?? throw new InvalidOperationException("User not found.");
    }

    public List<User> GetAll()
    {
        return _users.ToList();
    }

    public void Save(User user)
    {
        _users.Add(user);
    }
}
