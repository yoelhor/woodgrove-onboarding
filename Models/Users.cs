namespace Woodgrove.Onboarding.Models;

public class WgUsers
{
    public string NextPage { get; set; }
    public List<WgUser> Users { get; set; }

    public WgUsers()
    {
        this.Users = new List<WgUser>();
    }
}

public class WgUser
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string GivenName { get; set; }
    public string Surname { get; set; }
    public string EmployeeId { get; set; }
    public string EmployeeHireDate { get; set; }
    public string Department { get; set; }
    public string jobTitle { get; set; }
    public string Email { get; set; }
    public string UPN { get; set; }
    public string ManagerUpn { get; set; }
}





