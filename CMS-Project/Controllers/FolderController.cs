﻿using CMS_Project.Models.Entities;
using CMS_Project.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using CMS_Project.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace CMS_Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FolderController : ControllerBase
    {
        private readonly IFolderService _folderService;
        private readonly ILogger<FolderController> _logger;
        private readonly IUserService _userService;
        

        public FolderController(
            IUserService userService, 
            IFolderService folderService, 
            ILogger<FolderController> logger)
        {
            _userService = userService;
            _folderService = folderService;
            _logger = logger;
        }
        
        [HttpGet("folders-with-documents")]
        public async Task<IActionResult> GetFoldersWithDocuments()
        {
            var folders = await _folderService.GetFoldersWithDocumentsAsync();
            return Ok(folders);
        }
        [HttpPost("create-folder")]
        public async Task<IActionResult> CreateFolder([FromBody] FolderCreateDto  folderCreateDto)
        {
            if (!ModelState.IsValid) {
                _logger.LogWarning("Attempted to create a folder with invalid data.");
                return BadRequest(ModelState);
            }
            try
            {
                var userId = await _userService.GetUserIdFromClaimsAsync(User);
                _logger.LogInformation("Received CreateFolder request: {@folderCreateDto}", folderCreateDto);
                
                var folder = new Folder
                {
                    Name = folderCreateDto.Name,
                    ParentFolderId = folderCreateDto.ParentFolderId,
                    UserId = userId,
                    CreatedDate = DateTime.UtcNow
                };
                
                await _folderService.CreateFolderAsync(folder);
                _logger.LogInformation($"Folder created with ID {folder.Id}.");
                
                var folderDto = MapToFolderDto(folder);

                return CreatedAtAction(nameof(GetFolder), new { id = folder.Id }, folderDto);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "An error occurred while creating the folder.");
                return Conflict(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while creating the folder.");
                return StatusCode(500, "Unexpected error occured.");
            }
        }
        
        
        
        [HttpGet("all")]
        public async Task<IActionResult> GetFolders()
        {
            var userId = await _userService.GetUserIdFromClaimsAsync(User);
            var folderDtos = await _folderService.GetAllFoldersAsDtoAsync(userId);
            var user = await _userService.GetUserByIdAsync(userId);

            var response = new GetFoldersResponse
            {
                User = new UserDto
                {
                    UserId = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    CreatedDate = user.CreatedDate
                },
                Folders = folderDtos
            };

            return Ok(response);
        }
        private FolderDto MapToFolderDto(Folder folder)
        {
            return new FolderDto
            {
                FolderId = folder.Id,
                Name = folder.Name,
                CreatedDate = folder.CreatedDate,
                ParentFolderId = folder.ParentFolderId,
                ChildrenFolders = folder.ChildrenFolders.Select(MapToFolderDto).ToList() ?? new List<FolderDto>()
            };
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFolder(int id)
        {
            try
            {
                var userId = await _userService.GetUserIdFromClaimsAsync(User);
                var folderDetailDto = await _folderService.GetFolderByIdAsync(id, userId);
                
                return Ok(folderDetailDto);

            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving folder with ID {id}.");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
        
        
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFolder(int id, [FromBody] UpdateFolderDto updateFolderDto)
        { 
            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"Attempted to update folder with ID {id} with invalid data.");
                return BadRequest(ModelState);
            }
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var userId = await _userService.GetUserIdAsync(claims.Value);
            if (userId == -1)
            {
                return StatusCode(500, "UserId not found. User might not exist.");
            }
            
            try
            {
                var result = await _folderService.UpdateFolderAsync(id, updateFolderDto, userId);
                if (!result)
                {
                    _logger.LogWarning($"Folder with ID {id} was not found for update.");
                    return NotFound(new { message = $"Mappe med ID {id} ble ikke funnet." });
                }

                _logger.LogInformation($"Folder with ID {id} was updated successfully.");
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return Conflict(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while updating folder with ID {id}.");
                return StatusCode(500, "An unexpected Error occured.");
            }
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFolder(int id)
        {
            var userId = await _userService.GetUserIdFromClaimsAsync(User);
    
            if (userId == -1)
            {
                return StatusCode(500, "UserId not found. User might not exist.");
            }

            try
            {
                var result = await _folderService.DeleteFolderAsync(id, userId);

                if (!result)
                {
                    _logger.LogWarning($"Folder with ID {id} was not found or does not belong to the user.");
                    return NotFound(new { message = $"Folder with ID {id} was not found or does not belong to the user." });
                }

                _logger.LogInformation($"Folder with ID {id} successfully deleted.");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while deleting folder with ID {id}.");
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }
        
    }
}
