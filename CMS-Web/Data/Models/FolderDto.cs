namespace CMS_Web.Data.Models;

public class FolderDto
{
    public int FolderId { get; set; }
    public string Name { get; set; } = null!;
    public int? ParentFolderId { get; set; }
    public DateTime? CreatedDate { get; set; }
    public List<FolderDto> ChildrenFolders { get; set; } = [];
}
