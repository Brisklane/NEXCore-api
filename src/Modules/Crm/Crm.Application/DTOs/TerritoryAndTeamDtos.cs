namespace Crm.Application.DTOs;

public class TerritoryDto
{
    public Guid Id { get; set; }
    public string TerritoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentTerritoryId { get; set; }
    public Guid? OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTerritoryDto
{
    public string TerritoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentTerritoryId { get; set; }
    public Guid? OwnerId { get; set; }
}

public class UpdateTerritoryDto : CreateTerritoryDto { }

public class TeamDto
{
    public Guid Id { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ManagerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TeamMemberDto> Members { get; set; } = [];
}

public class CreateTeamDto
{
    public string TeamName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ManagerId { get; set; }
}

public class UpdateTeamDto : CreateTeamDto { }

public class TeamMemberDto
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }
    public string? TeamRole { get; set; }
    public string? AccountAccessLevel { get; set; }
    public string? OpportunityAccessLevel { get; set; }
    public string? CaseAccessLevel { get; set; }
}

public class CreateTeamMemberDto
{
    public Guid UserId { get; set; }
    public string? TeamRole { get; set; }
    public string? AccountAccessLevel { get; set; }
    public string? OpportunityAccessLevel { get; set; }
    public string? CaseAccessLevel { get; set; }
}
