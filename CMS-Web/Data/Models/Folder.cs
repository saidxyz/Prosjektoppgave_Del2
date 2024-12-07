namespace CMS_Web.Data.Models
{
    public class Folder
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? ParentId { get; set; }  // Null for top-level folders
        public List<Folder> Subfolders { get; set; } = new List<Folder>();
    }
}
