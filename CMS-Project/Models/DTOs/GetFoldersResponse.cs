namespace CMS_Project.Models.DTOs;

public class GetFoldersResponse
{
    public UserDto User { get; set; }
    public IEnumerable<FolderDto> Folders { get; set; }
}

