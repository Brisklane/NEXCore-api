namespace Crm.Application.DTOs;

public class ContactListDto
{
    public Guid Id { get; set; }
    public string ListName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDynamic { get; set; }
    public string? FilterCriteria { get; set; }
    public Guid? OwnerId { get; set; }
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateContactListDto
{
    public string ListName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDynamic { get; set; }
    public string? FilterCriteria { get; set; }
    public Guid? OwnerId { get; set; }
}

public class UpdateContactListDto : CreateContactListDto { }

public class ContactListMemberDto
{
    public Guid Id { get; set; }
    public Guid ContactListId { get; set; }
    public Guid? ContactId { get; set; }
    public string? ContactName { get; set; }
    public Guid? LeadId { get; set; }
    public string? LeadName { get; set; }
    public string MemberType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateContactListMemberDto
{
    public Guid? ContactId { get; set; }
    public Guid? LeadId { get; set; }
    public string MemberType { get; set; } = "Contact";
}
