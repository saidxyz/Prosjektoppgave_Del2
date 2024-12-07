using CMS_Project.Models.Entities;

namespace CMS_Web.Service
{
    public class FolderService
    {
        private readonly HttpClient _httpClient;

        public FolderService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Method to get folders
        public async Task<List<Folder>> GetFoldersAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<List<Folder>>("https://localhost:7238/api/folders");
            return response ?? new List<Folder>();
        }
    }

}