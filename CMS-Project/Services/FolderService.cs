using System.Collections.Generic;
using System.Threading.Tasks;
using CMS_Project.Data;
using CMS_Project.Models.Entities;
using CMS_Project.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CMS_Project.Services
{
    public class FolderService : IFolderService
    {
        private readonly CMSContext _context;
        private readonly ILogger<FolderService> _logger; 
        private IFolderService _folderServiceImplementation;

        public FolderService(CMSContext context,  ILogger<FolderService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a folder by its ID, including documents and child folders.
        /// </summary>
        /// <param name="id">The ID of the folder to retrieve.</param>
        /// <returns>The folder if found; null otherwise.</returns>
        public async Task<Folder> GetFolderByIdAsync(int id)
        {
            return await _context.Folders
                .Include(f => f.Documents)
                .Include(f => f.ChildrenFolders)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        /// <summary>
        /// Gets all folders for a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user whose folders are to be retrieved.</param>
        /// <returns>A list of folders belonging to the specified user.</returns>
        public async Task<IEnumerable<Folder>> GetAllFoldersAsync(int userId)
        {
            return await _context.Folders
                .Include(f => f.Documents)
                .Include(f => f.User)
                .Where(f => f.UserId == userId)
                .ToListAsync();
        }
        
        /// <summary>
        /// Retrieves all top-level folders (those without a parent folder) for a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user whose top-level folders are to be retrieved.</param>
        /// <returns>A list of top-level folders belonging to the specified user, including their child folders.</returns>
        public async Task<IEnumerable<Folder>> GetFoldersByUserIdAsync(int userId)
        {
            return await _context.Folders
                .Where(f => f.UserId == userId && f.ParentFolderId == null)
                .Include(f => f.ChildrenFolders)
                .ToListAsync();
        }
        
        /// <summary>
        /// Retrieves a folder by ID for a specific user with detailed information.
        /// </summary>
        /// <param name="folderId">The ID of the folder to retrieve.</param>
        /// <param name="userId">The ID of the user who owns the folder.</param>
        /// <returns>A FolderDetailDto containing detailed folder information.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the folder is not found or does not belong to the user.</exception>
        public async Task<FolderDetailDto> GetFolderByIdAsync(int folderId, int userId)
        {
            var folder = await _context.Folders
                .Include(f => f.ChildrenFolders)
                .Include(f => f.Documents)
                .FirstOrDefaultAsync(f => f.Id == folderId && f.UserId == userId);

            if (folder == null)
            {
                throw new KeyNotFoundException("Folder not found or does not belong to the user.");
            }

            var folderDetailDto = MapToFolderDetailDto(folder);
            return folderDetailDto;
        }
        
        /// <summary>
        /// GET all folders where UserId = Documents-User.Id
        /// </summary>
        /// <param name="UserId"></param>
        /// <returns>List of folders by given user id</returns>
        public async Task<List<FolderDto>> GetAllFoldersAsDtoAsync(int userId)
        {
            var rootFolders = await _context.Folders
                .Where(f => f.UserId == userId && f.ParentFolderId == null)
                .ToListAsync();

            var folderDtos = rootFolders.Select(folder => MapToFolderDtoRecursively(folder)).ToList();
            return folderDtos;
        }
        
        /// <summary>
        /// Recursively maps a folder and its children to FolderDto.
        /// </summary>
        /// <param name="folder">The folder to map.</param>
        /// <returns>A FolderDto representing the folder and its children.</returns>
        private FolderDto MapToFolderDtoRecursively(Folder folder)
        {
            // Map basic properties of the folder
            var folderDto = new FolderDto
            {
                FolderId = folder.Id,
                Name = folder.Name,
                CreatedDate = folder.CreatedDate,
                ParentFolderId = folder.ParentFolderId,
                ChildrenFolders = new List<FolderDto>()
            };

            // Retrieve children folders and map them recursively
            var children = _context.Folders
                .Where(f => f.ParentFolderId == folder.Id)
                .ToList(); // Execute query here to avoid EF tracking issues in recursion

            foreach (var child in children)
            {
                folderDto.ChildrenFolders.Add(MapToFolderDtoRecursively(child));
            }

            return folderDto;
        }
        
        /// <summary>
        /// CREATE folder by Dto and checks ownership. First folder needs parentFolderId to be null!!
        /// </summary>
        /// <param name="folderDto"></param>
        /// <param name="userId"></param>
        /// <returns>document created</returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task CreateFolderAsync(Folder folder)
        {
            if (folder.ParentFolderId.HasValue)
            {
                var parentFolder = await _context.Folders
                    .FirstOrDefaultAsync(f => f.Id == folder.ParentFolderId && f.UserId == folder.UserId);
                
                if (parentFolder == null)
                {
                    throw new ArgumentException("Parent folder not found or does not belong to the user.");
                }
            }
            _context.Folders.Add(folder);
            await _context.SaveChangesAsync();
        }
        
        
        /// <summary>
        /// Updates a folder by its ID for a specific user.
        /// </summary>
        /// <param name="id">The ID of the folder to update.</param>
        /// <param name="updateFolderDto">Data for updating the folder.</param>
        /// <param name="userId">The ID of the user who owns the folder.</param>
        /// <returns>True if the update is successful; false otherwise.</returns>
        /// <exception cref="ArgumentException">Thrown if the folder or parent folder does not belong to the user.</exception>
        public async Task<bool> UpdateFolderAsync(int id, UpdateFolderDto updateFolderDto, int userId)
        {
            var folder = await _context.Folders.FindAsync(id);
            if (folder == null)
                throw new ArgumentException("folder not found.");
            
            if (folder.UserId != userId)
                throw new ArgumentException("User doesn't own folder.");

            if (updateFolderDto.ParentFolderId != null)
            {
                //check if user owns parent folder.
                var parentfolder = await _context.Folders.FirstAsync(f => f.Id == updateFolderDto.ParentFolderId);
                if (folder.ParentFolderId == null)
                    if (parentfolder.UserId != userId)
                        throw new ArgumentException("User doesn't own parent folder.");
            }

            folder.Name = updateFolderDto.Name;
            folder.ParentFolderId = updateFolderDto.ParentFolderId;

            _context.Entry(folder).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await FolderExists(id))
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }

            return true;
        }
        
        /// <summary>
        /// Deletes a folder by its ID for a specific user, including all child folders and documents.
        /// </summary>
        /// <param name="id">The ID of the folder to delete.</param>
        /// <param name="userId">The ID of the user who owns the folder.</param>
        /// <returns>True if deletion is successful; false otherwise.</returns>
        public async Task<bool> DeleteFolderAsync(int id, int userId)
        {
            try
            {
                var folder = await _context.Folders
                    .Include(f => f.ChildrenFolders)
                    .Include(f => f.Documents)
                    .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
        
                if (folder == null)
                {
                    return false;
                }

                // Delete all subfolders and documents recursively
                DeleteFolderRecursive(folder);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception (you can use your logger here)
                _logger.LogError(ex, $"Error deleting folder with ID {id} for user {userId}");
                throw; // Re-throw the exception to be handled by the controller
            }
        }



        // Helper Methods
        
        /// <summary>
        /// Recursively deletes a folder and all its child folders and documents.
        /// </summary>
        /// <param name="folder">The folder to delete recursively.</param>
        private void DeleteFolderRecursive(Folder folder)
        {
            // Delete all documents in folder
            _context.Documents.RemoveRange(folder.Documents);
    
            // Recursively delete all child folders
            foreach (var childFolder in folder.ChildrenFolders)
            {
                // Load child folder's children and documents
                _context.Entry(childFolder).Collection(f => f.ChildrenFolders).Load();
                _context.Entry(childFolder).Collection(f => f.Documents).Load();
        
                // Recursive call
                DeleteFolderRecursive(childFolder);
            }
    
            // Delete the current folder
            _context.Folders.Remove(folder);
        }

        
        /// <summary>
        /// Checks if a folder with the specified ID exists.
        /// </summary>
        /// <param name="id">The ID of the folder to check.</param>
        /// <returns>True if the folder exists; false otherwise.</returns>
        private async Task<bool> FolderExists(int id)
        {
            return await _context.Folders.AnyAsync(f => f.Id == id);
        }
        
        /// <summary>
        /// Maps a Folder entity to a FolderDetailDto.
        /// </summary>
        /// <param name="folder">The folder to map.</param>
        /// <returns>A FolderDetailDto containing detailed folder information.</returns>
        private FolderDetailDto MapToFolderDetailDto(Folder folder)
        {
            return new FolderDetailDto
            {
                FolderId = folder.Id,
                Name = folder.Name,
                CreatedDate = folder.CreatedDate,
                ParentFolderId = folder.ParentFolderId,
                Documents = folder.Documents.Select(d => new DocumentDto
                {
                    DocumentId = d.Id,
                    Title = d.Title,
                    Content = d.Content,
                    ContentType = d.ContentType,
                    CreatedDate = d.CreatedDate
                }).ToList(),
                ChildrenFolders = folder.ChildrenFolders.Select(MapToFolderDto).ToList()
            };
        }
        
        
        /// <summary>
        /// Maps a Folder entity to a FolderDto.
        /// </summary>
        /// <param name="folder">The folder to map.</param>
        /// <returns>A FolderDto object.</returns>
        private FolderDto MapToFolderDto(Folder folder)
        {
            return new FolderDto
            {
                FolderId = folder.Id,
                Name = folder.Name,
                CreatedDate = folder.CreatedDate,
                ParentFolderId = folder.ParentFolderId,
                ChildrenFolders = folder.ChildrenFolders?.Select(MapToFolderDto).ToList() ?? new List<FolderDto>()
            };
        }
        
    }
}
