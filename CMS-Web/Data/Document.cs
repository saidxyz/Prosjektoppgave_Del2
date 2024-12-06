namespace CMS_Web.Components.Models // Adjust the namespace as per your project structure
{
    public class Document
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public int FolderId { get; set; }
    }
}