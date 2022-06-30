namespace MinimalApi2.Services;

public class UserService : IUserService
{
    public User Get(UserLogin userLogin)
    {
        User user = UserRepository.Users.FirstOrDefault(o => o.Name.Equals(userLogin.Name, StringComparison.OrdinalIgnoreCase) && o.Password.Equals(userLogin.Password));

        return user;
    }
}
