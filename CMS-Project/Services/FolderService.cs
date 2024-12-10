using System.Collections.Generic;
using System.Threading.Tasks;
using CMS_Project.Data;
using CMS_Project.Models.Entities;
using CMS_Project.Models.DTOs;
using CMS_Web.Data.Models;
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
        public async Task<IEnumerable<FolderDto>> GetFoldersByUserIdAsync(string userId)
        {
            int userIdInt = int.Parse(userId);
    
            var folders = await _context.Folders
                .Include(f => f.Documents)
                .Include(f => f.ChildrenFolders)
                .Where(f => f.UserId == userIdInt)
                .ToListAsync();
    
            return folders.Select(f => new FolderDto 
            {
                FolderId = f.Id,
                Name = f.Name
            });
        }
        
        public async Task<List<FolderWithDocumentsDto>> GetFoldersWithDocumentsAsync()
        {
            var folders = await _context.Folders
                .Include(f => f.Documents)
                .ToListAsync();

            return folders.Select(f => new FolderWithDocumentsDto
            {
                FolderId = f.Id,
                FolderName = f.Name,
                Documents = f.Documents.Select(d => new DocumentDto
                {
                    DocumentId = d.Id,
                    Title = d.Title,
                    ContentType = d.ContentType
                }).ToList()
            }).ToList();
        }

        
        public async Task<Folder> GetFolderByIdAsync(int id)
        {
            return await _context.Folders
                .Include(f => f.Documents)
                .Include(f => f.ChildrenFolders)
                .FirstOrDefaultAsync(f => f.Id == id);
        }
        
        public async Task<IEnumerable<Folder>> GetAllFoldersAsync(int userId)
        {
            return await _context.Folders
                .Include(f => f.Documents)
                .Include(f => f.User)
                .Where(f => f.UserId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Folder>> GetFoldersByUserIdAsync(int userId)
        {
            return await _context.Folders
                .Where(f => f.UserId == userId && f.ParentFolderId == null)
                .Include(f => f.ChildrenFolders)
                .ToListAsync();
        }
        
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

        public async Task<List<FolderDto>> GetAllFoldersAsDtoAsync(int userId)
        {
            var rootFolders = await _context.Folders
                .Where(f => f.UserId == userId && f.ParentFolderId == null)
                .ToListAsync();

            var folderDtos = rootFolders.Select(folder => MapToFolderDtoRecursively(folder)).ToList();
            return folderDtos;
        }
        

        private FolderDto MapToFolderDtoRecursively(Folder folder)
        {
            var folderDto = new FolderDto
            {
                FolderId = folder.Id,
                Name = folder.Name,
                CreatedDate = folder.CreatedDate,
                ParentFolderId = folder.ParentFolderId,
                ChildrenFolders = new List<FolderDto>()
            };

            var children = _context.Folders
                .Where(f => f.ParentFolderId == folder.Id)
                .ToList();

            foreach (var child in children)
            {
                folderDto.ChildrenFolders.Add(MapToFolderDtoRecursively(child));
            }

            return folderDto;
        }
        

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
        
        public async Task<bool> UpdateFolderAsync(int id, UpdateFolderDto updateFolderDto, int userId)
        {
            var folder = await _context.Folders.FindAsync(id);
            if (folder == null)
                throw new ArgumentException("folder not found.");
            
            if (folder.UserId != userId)
                throw new ArgumentException("User doesn't own folder.");

            if (updateFolderDto.ParentFolderId != null)
            {
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
                DeleteFolderRecursive(folder);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting folder with ID {id} for user {userId}");
                throw;
            }
        }

        
        private void DeleteFolderRecursive(Folder folder)
        {
            _context.Documents.RemoveRange(folder.Documents);
    
            foreach (var childFolder in folder.ChildrenFolders)
            {
                _context.Entry(childFolder).Collection(f => f.ChildrenFolders).Load();
                _context.Entry(childFolder).Collection(f => f.Documents).Load();
        
                DeleteFolderRecursive(childFolder);
            }
    
            _context.Folders.Remove(folder);
        }


        private async Task<bool> FolderExists(int id)
        {
            return await _context.Folders.AnyAsync(f => f.Id == id);
        }
        

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
