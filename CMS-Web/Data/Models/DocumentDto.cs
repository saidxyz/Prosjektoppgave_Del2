namespace CMS_Web.Data.Models
{
    public class DocumentDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? ContentType { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? IdentityUserId { get; set; }
        public int? FolderId { get; set; }
        public string? FolderName { get; set; }
    }


    public class CreateDocumentDto
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? ContentType { get; set; }
        public string? IdentityUserId { get; set; }
        public int? FolderId { get; set; }
        public string? FolderName { get; set; }
    }

    public class UpdateDocumentDto
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? ContentType { get; set; }
        public int? FolderId { get; set; }
        public string? FolderName { get; set; }
    }
}
