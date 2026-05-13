// GraphQL input types for user profile updates, password changes, and user list pagination/filtering.
using HotChocolate;

namespace Server.GraphQL.Inputs;

[GraphQLName("UpdateUserInput")]
public class UpdateUserInput
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MiddleName { get; set; }
    public string? Phone { get; set; }
    public int? YearOfBirth { get; set; }
}

[GraphQLName("ChangePasswordByEmailCodeInput")]
public class ChangePasswordByEmailCodeInput
{
    public string Code { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

[GraphQLName("ResetUserPasswordInput")]
public class ResetUserPasswordInput
{
    public int UserId { get; set; }
    public string NewPassword { get; set; } = string.Empty;
}

[GraphQLName("UserPagination")]
public class UsersPaginationInput
{
    public int? First { get; set; } = 20;
    public string? After { get; set; }
    public string? Before { get; set; }
    public int? Last { get; set; }
}

[GraphQLName("UserFilter")]
public class UsersFilterInput
{
    public string? Search { get; set; }
    public bool? EmployeeOnly { get; set; }
}

[GraphQLName("UserBooksPagination")]
public class UserBooksPaginationInput
{
    public int? First { get; set; } = 20;
    public string? After { get; set; }
    public string? Before { get; set; }
    public int? Last { get; set; }
}
