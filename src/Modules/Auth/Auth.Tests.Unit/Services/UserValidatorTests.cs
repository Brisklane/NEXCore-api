using Auth.Application.DTOs;
using Auth.Infrastructure.Persistence;
using Auth.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Auth.Tests.Unit.Services;

/// <summary>
/// Unit tests for UserValidator.
/// Uses a fresh in-memory AuthDbContext per test to allow uniqueness checks.
/// All field combinations that can cause a failure are exercised individually
/// so each test has exactly one reason to fail.
/// </summary>
public class UserValidatorTests : AuthUnitTestFixture
{
    // ------------------------------------------------------------------ factory
    private static (UserValidator validator, AuthDbContext db) CreateSut()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AuthDbContext(options);
        return (new UserValidator(db), db);
    }

    /// <summary>A fully-valid DTO � mutate individual fields per test.</summary>
    private static ValidateUserDto ValidDto() => new()
    {
        UserName = "valid@example.com",
        Email = "valid@example.com",
        FullName = "Valid User",
        Password = "ValidPass123!",
        Gender = "Male",
        DateOfBirth = "1990-01-01",
        PhoneNumber = "+1234567890",
        Address = "123 Test Street"
    };

    // ==================================================================
    // HAPPY PATH
    // ==================================================================

    [Fact]
    public async Task ValidateAsync_AllFieldsValid_ReturnsSuccess()
    {
        var (validator, _) = CreateSut();
        var result = await validator.ValidateAsync(ValidDto());

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Validation passed.");
    }

    // ==================================================================
    // EMAIL
    // ==================================================================

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_EmptyEmail_ReturnsFailed(string email)
    {
        var (validator, _) = CreateSut();
        var dto = ValidDto(); dto.Email = email;

        var result = await validator.ValidateAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Email is required.");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("invalid@")]
    [InlineData("@nodomain.com")]
    [InlineData("missing-at-sign.com")]
    public async Task ValidateAsync_InvalidEmailFormat_ReturnsFailed(string email)
    {
        var (validator, _) = CreateSut();
        var dto = ValidDto(); dto.Email = email;

        var result = await validator.ValidateAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid email format.");
    }

    [Fact]
    public async Task ValidateAsync_DuplicateEmail_ReturnsFailed()
    {
        var (validator, db) = CreateSut();

        // Seed an existing user with the same email
        db.Users.Add(new UserBuilder()
            .WithEmail("valid@example.com")
            .WithUsername("existinguser")
            .Build());
        await db.SaveChangesAsync();

        var result = await validator.ValidateAsync(ValidDto());

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Email is already registered.");
    }

    // ==================================================================
    // USERNAME
    // ==================================================================

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_EmptyUsername_ReturnsFailed(string username)
    {
        var (validator, _) = CreateSut();
        var dto = ValidDto(); dto.UserName = username;

        var result = await validator.ValidateAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Username is required.");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("invalid@")]
    [InlineData("@nodomain.com")]
    public async Task ValidateAsync_HandleStyleUsername_ReturnsSuccess(string username)
    {
        // A username may be a plain handle or an email; non-email handles are valid.
        var (validator, _) = CreateSut();
        var dto = ValidDto(); dto.UserName = username;

        var result = await validator.ValidateAsync(dto);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_UsernameTooShort_ReturnsFailed()
    {
        var (validator, _) = CreateSut();
        var dto = ValidDto(); dto.UserName = "ab"; // 2 chars, below minimum of 3

        var result = await validator.ValidateAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Username must be between 3 and 50 characters.");
    }

    [Fact]
    public async Task ValidateAsync_UsernameTooLong_ReturnsFailed()
    {
        var (validator, _) = CreateSut();
        var dto = ValidDto(); dto.UserName = new string('x', 45) + "@a.com"; // 51 chars

        var result = await validator.ValidateAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Username must be between 3 and 50 characters.");
    }

    [Fact]
    public async Task ValidateAsync_DuplicateUsername_ReturnsFailed()
    {
        var (validator, db) = CreateSut();

        db.Users.Add(new UserBuilder()
            .WithUsername("valid@example.com")   // same as ValidDto()
            .WithEmail("other@example.com")
            .Build());
        await db.SaveChangesAsync();

        var result = await validator.ValidateAsync(ValidDto());

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Username is already taken.");
    }

    // ==================================================================
    // PASSWORD
    // ==================================================================

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_EmptyPassword_ReturnsFailed(string password)
    {
        var (validator, _) = CreateSut();
        var dto = ValidDto(); dto.Password = password;

        var result = await validator.ValidateAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Password is required.");
    }

    [Fact]
    public async Task ValidateAsync_PasswordTooShort_ReturnsFailed()
    {
        var (validator, _) = CreateSut();
        var dto = ValidDto(); dto.Password = "Ab1!";  // 4 chars

        var result = await validator.ValidateAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Password must be at least 8 characters.");
    }

    [Fact]
    public async Task ValidateAsync_PasswordNoUppercase_ReturnsFailed()
    {
        var (validator, _) = CreateSut();
        var dto = ValidDto(); dto.Password = "alllower1!";

        var result = await validator.ValidateAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Password must contain at least one uppercase letter.");
    }

    [Fact]
    public async Task ValidateAsync_PasswordNoLowercase_ReturnsFailed()
    {
        var (validator, _) = CreateSut();
        var dto = ValidDto(); dto.Password = "ALLUPPER1!";

        var result = await validator.ValidateAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Password must contain at least one lowercase letter.");
    }

    [Fact]
    public async Task ValidateAsync_PasswordNoDigit_ReturnsFailed()
    {
        var (validator, _) = CreateSut();
        var dto = ValidDto(); dto.Password = "NoDigits!A";

        var result = await validator.ValidateAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Password must contain at least one digit.");
    }

    [Fact]
    public async Task ValidateAsync_PasswordNoSpecialChar_ReturnsFailed()
    {
        var (validator, _) = CreateSut();
        var dto = ValidDto(); dto.Password = "NoSpecial1A";

        var result = await validator.ValidateAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Password must contain at least one special character.");
    }

    // ==================================================================
    // FULL NAME
    // ==================================================================

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_EmptyFullName_ReturnsFailed(string fullName)
    {
        var (validator, _) = CreateSut();
        var dto = ValidDto(); dto.FullName = fullName;

        var result = await validator.ValidateAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Full name is required.");
    }

    [Fact]
    public async Task ValidateAsync_FullNameTooShort_ReturnsFailed()
    {
        var (validator, _) = CreateSut();
        var dto = ValidDto(); dto.FullName = "A"; // 1 char

        var result = await validator.ValidateAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Full name must be at least 2 characters.");
    }

    // ==================================================================
    // GENDER
    // ==================================================================

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_EmptyGender_ReturnsSuccess(string gender)
    {
        // Gender is optional — an empty/whitespace value is accepted.
        var (validator, _) = CreateSut();
        var dto = ValidDto(); dto.Gender = gender;

        var result = await validator.ValidateAsync(dto);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_InvalidGenderValue_ReturnsFailed()
    {
        var (validator, _) = CreateSut();
        var dto = ValidDto(); dto.Gender = "Robot";

        var result = await validator.ValidateAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Gender must be Male, Female, or Other.");
    }

    [Theory]
    [InlineData("Male")]
    [InlineData("Female")]
    [InlineData("Other")]
    [InlineData("male")]    // case-insensitive
    [InlineData("FEMALE")]
    public async Task ValidateAsync_ValidGenderValues_ReturnSuccess(string gender)
    {
        var (validator, _) = CreateSut();
        var dto = ValidDto(); dto.Gender = gender;

        var result = await validator.ValidateAsync(dto);

        result.Success.Should().BeTrue();
    }

    // ==================================================================
    // DATE OF BIRTH
    // ==================================================================

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_EmptyDateOfBirth_ReturnsSuccess(string dob)
    {
        // Date of birth is optional — an empty/whitespace value is accepted.
        var (validator, _) = CreateSut();
        var dto = ValidDto(); dto.DateOfBirth = dob;

        var result = await validator.ValidateAsync(dto);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_InvalidDateFormat_ReturnsFailed()
    {
        var (validator, _) = CreateSut();
        var dto = ValidDto(); dto.DateOfBirth = "not-a-date";

        var result = await validator.ValidateAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid date of birth format.");
    }

    [Fact]
    public async Task ValidateAsync_Under18_ReturnsFailed()
    {
        var (validator, _) = CreateSut();
        var dto = ValidDto();
        dto.DateOfBirth = DateTime.UtcNow.AddYears(-17).ToString("yyyy-MM-dd");

        var result = await validator.ValidateAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Admin must be at least 18 years old.");
    }

    // ==================================================================
    // PHONE NUMBER
    // ==================================================================

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_EmptyPhoneNumber_ReturnsSuccess(string phone)
    {
        // Phone number is an optional profile field: a blank value passes validation.
        var (validator, _) = CreateSut();
        var dto = ValidDto(); dto.PhoneNumber = phone;

        var result = await validator.ValidateAsync(dto);

        result.Success.Should().BeTrue();
    }

    [Theory]
    [InlineData("123")]           // too short (3 digits)
    [InlineData("1234567890123456")] // too long (16 digits)
    public async Task ValidateAsync_InvalidPhoneLength_ReturnsFailed(string phone)
    {
        var (validator, _) = CreateSut();
        var dto = ValidDto(); dto.PhoneNumber = phone;

        var result = await validator.ValidateAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Phone number must be between 10 and 15 digits.");
    }

    // ==================================================================
    // ADDRESS
    // ==================================================================

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_EmptyAddress_ReturnsSuccess(string address)
    {
        // Address is an optional profile field: a blank value passes validation.
        var (validator, _) = CreateSut();
        var dto = ValidDto(); dto.Address = address;

        var result = await validator.ValidateAsync(dto);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_AddressTooShort_ReturnsFailed()
    {
        var (validator, _) = CreateSut();
        var dto = ValidDto(); dto.Address = "123"; // 3 chars, below minimum of 5

        var result = await validator.ValidateAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Address must be at least 5 characters.");
    }
}
