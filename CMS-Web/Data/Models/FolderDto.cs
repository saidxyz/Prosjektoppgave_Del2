namespace Client.Data.Models
{
    class FolderDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int? ParentFolderId { get; set; }

    }
    class CreateFolderDto
    {
        public string Name { get; set; } = null!;
        public int ParentFolderId { get; set; }

    }


    class NavigatableFolderDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public FolderDto? ParentFolder { get; set; }
        public List<DocumentDto> Documents { get; set; } = null!;
        public string Url { get; set; } = null!;

    }
}
