using CMS_Web.Components.Models;
using System.Net.Http.Json;

public class CmsApiService
{
    private readonly HttpClient _httpClient;

    public CmsApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Folder>> GetFoldersAsync()
    {
        // Fetch folders from the API
        var apiFolders = await _httpClient.GetFromJsonAsync<List<CMS_Project.Models.Entities.Folder>>("api/Folder/all");

        // Map API model to Blazor model
        var folders = apiFolders?.Select(f => new Folder
        {
            Id = f.Id,
            Name = f.Name,
            CreatedDate = f.CreatedDate,
            ParentFolderId = f.ParentFolderId,
            ChildrenFolders = f.ChildrenFolders?.Select(c => new Folder
            {
                Id = c.Id,
                Name = c.Name,
                CreatedDate = c.CreatedDate,
                ParentFolderId = c.ParentFolderId
            }).ToList() ?? new List<Folder>()
        }).ToList() ?? new List<Folder>();

        return folders;
    }
}