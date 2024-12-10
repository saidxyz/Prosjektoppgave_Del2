namespace CMS_Web.Data.Models;

public class DocumentDetailDto
{
    public int DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public int? FolderId { get; set; }
        

    public FolderDto Folder { get; set; } = null!;
}