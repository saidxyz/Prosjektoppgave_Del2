namespace CMS_Web.Components.Models
{
    public class Folder
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public int? ParentFolderId { get; set; }
        public List<Folder> ChildrenFolders { get; set; } = new List<Folder>();
    }
}