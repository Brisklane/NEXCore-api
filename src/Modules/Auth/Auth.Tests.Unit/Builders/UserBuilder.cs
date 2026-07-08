using Auth.Domain.Entities;

namespace Auth.Tests.Unit.Builders;

/// <summary>
/// Builder pattern for creating User test data
/// </summary>
public class UserBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _username = "testuser";
    private string _email = "test@example.com";
    private string _fullName = "Test User";
    private string _firstName = "Test";
    private string _lastName = "User";
    private string _phoneNumber = "+1234567890";
    private string _gender = "M";
    private DateTime _dateOfBirth = DateTime.UtcNow.AddYears(-25);
    private string _address = "123 Test Street";
    private string _passwordHash = "hashedpassword";
    private bool _isActive = true;
    private bool _isEmailVerified = false;
    private bool _isLockedOut = false;
    private int _failedLoginAttempts = 0;
    private string? _employeeId = null;
    private string? _designation = null;
    private string? _department = null;

    public UserBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public UserBuilder WithUsername(string username)
    {
        _username = username;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserBuilder WithFullName(string fullName)
    {
        _fullName = fullName;
        return this;
    }

    public UserBuilder WithIsActive(bool isActive)
    {
        _isActive = isActive;
        return this;
    }

    public UserBuilder WithIsEmailVerified(bool isEmailVerified)
    {
        _isEmailVerified = isEmailVerified;
        return this;
    }

    public UserBuilder WithIsLockedOut(bool isLockedOut)
    {
        _isLockedOut = isLockedOut;
        return this;
    }

    public UserBuilder WithFailedLoginAttempts(int attempts)
    {
        _failedLoginAttempts = attempts;
        return this;
    }

    public UserBuilder WithDesignation(string designation)
    {
        _designation = designation;
        return this;
    }

    public UserBuilder WithDepartment(string department)
    {
        _department = department;
        return this;
    }

    public User Build()
    {
        return new User
        {
            Id = _id,
            Username = _username,
            Email = _email,
            FullName = _fullName,
            FirstName = _firstName,
            LastName = _lastName,
            PhoneNumber = _phoneNumber,
            Gender = _gender,
            DateOfBirth = _dateOfBirth,
            Address = _address,
            PasswordHash = _passwordHash,
            IsActive = _isActive,
            IsEmailVerified = _isEmailVerified,
            IsLockedOut = _isLockedOut,
            FailedLoginAttempts = _failedLoginAttempts,
            EmployeeId = _employeeId,
            Designation = _designation,
            Department = _department
        };
    }
}
