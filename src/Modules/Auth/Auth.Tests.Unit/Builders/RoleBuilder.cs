using Auth.Domain.Entities;

namespace Auth.Tests.Unit.Builders;

/// <summary>
/// Builder pattern for creating Role test data
/// </summary>
public class RoleBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _code = "ADMIN";
    private string _name = "TestRole";
    private string _description = "Test role description";
    private bool _isActive = true;

    public RoleBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public RoleBuilder WithCode(string code)
    {
        _code = code;
        return this;
    }

    public RoleBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public RoleBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public RoleBuilder WithIsActive(bool isActive)
    {
        _isActive = isActive;
        return this;
    }

    public Role Build()
    {
        return new Role
        {
            Id = _id,
            Code = _code,
            Name = _name,
            Description = _description,
            IsActive = _isActive
        };
    }
}
