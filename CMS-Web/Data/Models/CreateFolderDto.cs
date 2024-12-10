namespace CMS_Web.Data.Models;

public class CreateFolderDto
{
    public string Name { get; set; } = null!;
    public int? ParentFolderId { get; set; }

}