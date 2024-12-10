namespace CMS_Web.Data.Models;

public class NavigatableFolderDto
{
    public UserDto User { get; set; }
    public List<FolderDto> Folders { get; set; } = new List<FolderDto>();
}
