namespace CMS_Web.Data.Models;

public class DocumentDto
{
    public int DocumentId { get; set; }
    public string Title { get; set; }
    public string Content { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public int? FolderId { get; set; }
    public string? FolderName { get; set; } // Optional folder name
}