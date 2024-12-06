namespace CMS_Project.Models.DTOs
{
    public class DocumentResponseDto
    {
        public FolderDto? Folder { get; set; }
        public DocumentDetailDto Document { get; set; }

        // Constructor to initialize both properties
        public DocumentResponseDto(FolderDto? folder = null, DocumentDetailDto document = null)
        {
            Folder = folder;
            Document = document ?? new DocumentDetailDto();
        }
    }
}