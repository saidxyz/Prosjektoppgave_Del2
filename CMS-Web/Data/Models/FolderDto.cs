namespace CMS_Web.Data.Models
{
    public class FolderDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int? ParentFolderId { get; set; }

    }
    public class CreateFolderDto
    {
        public string Name { get; set; } = null!;
        public int? ParentFolderId { get; set; }

    }




}