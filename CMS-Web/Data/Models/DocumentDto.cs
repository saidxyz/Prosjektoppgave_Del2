namespace Client.Data.Models
{
    public class DocumentDto
    {

        public int? DocumentId { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? ContentType { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class DocumentCreateDto
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? ContentType { get; set; }
        public int? FolderId { get; set; }
    }
    
    public class UpdateDocumentDto
    {

        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? ContentType { get; set; }
        public int? FolderId { get; set; }
    }
}
