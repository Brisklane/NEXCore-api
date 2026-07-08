namespace Crm.Application.DTOs;

public class NoteDto
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string Body { get; set; } = string.Empty;
    public Guid ParentId { get; set; }
    public string ParentType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateNoteDto
{
    public string? Title { get; set; }
    public string Body { get; set; } = string.Empty;
    public Guid ParentId { get; set; }
    public string ParentType { get; set; } = string.Empty;
}

public class UpdateNoteDto
{
    public string? Title { get; set; }
    public string Body { get; set; } = string.Empty;
}

public class AttachmentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? FileExtension { get; set; }
    public long FileSizeBytes { get; set; }
    public string? ContentType { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public Guid ParentId { get; set; }
    public string ParentType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateAttachmentDto
{
    public string FileName { get; set; } = string.Empty;
    public string? FileExtension { get; set; }
    public long FileSizeBytes { get; set; }
    public string? ContentType { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public Guid ParentId { get; set; }
    public string ParentType { get; set; } = string.Empty;
    public string? Description { get; set; }
}
