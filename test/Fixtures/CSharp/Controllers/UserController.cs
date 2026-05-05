using GithubAnalyzer.Fixtures.Services;
using GithubAnalyzer.Fixtures.Models;

namespace GithubAnalyzer.Fixtures.Controllers;

public class UserController
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }

    public User GetUser(int id)
    {
        return _userService.FindById(id);
    }

    public List<User> GetAllUsers()
    {
        return _userService.GetAll();
    }

    public void CreateUser(string name, string email)
    {
        var user = new User(name, email);
        _userService.Save(user);
    }
}
