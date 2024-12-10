namespace CMS_Web.Data.Models;

public class FolderWithDocumentsDto
{
    public int FolderId { get; set; }
    public string FolderName { get; set; }
    public List<DocumentDto> Documents { get; set; } = new List<DocumentDto>();
}
