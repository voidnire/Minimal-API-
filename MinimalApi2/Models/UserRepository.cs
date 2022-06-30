namespace MinimalApi2.Models;

public class UserRepository
{
    public static List<User> Users = new()
        {
            new (){Name = "batman", Password="batmann", Role="adm"},
            new (){Name = "robin", Password ="robinn",Role="employee"},
        };

}
