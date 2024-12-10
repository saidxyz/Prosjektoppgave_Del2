namespace CMS_Web.Data.Models;

public class UpdateDocumentDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public int? FolderId { get; set; }
    public string? FolderName { get; set; }
}