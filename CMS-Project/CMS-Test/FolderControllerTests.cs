using System.Security.Claims;
using CMS_Project.Controllers;
using CMS_Project.Models.DTOs;
using CMS_Project.Models.Entities;
using CMS_Project.Services;
using CMS_Web.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Moq;

public class FolderControllerTests
{
    private Mock<IUserService> _userServiceMock;
    private Mock<IFolderService> _folderServiceMock;
    private Mock<ILogger<FolderController>> _loggerMock;
    private FolderController _controller;

    public FolderControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _folderServiceMock = new Mock<IFolderService>();
        _loggerMock = new Mock<ILogger<FolderController>>();
        
        _controller = new FolderController(
            _userServiceMock.Object,  
            _folderServiceMock.Object, 
            _loggerMock.Object        
        );
    }

    private FolderController CreateControllerWithUser(int userId = 1)
    {
        var controller = new FolderController(
            _userServiceMock.Object,  
            _folderServiceMock.Object, 
            Mock.Of<ILogger<FolderController>>()
        );
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }, "TestAuthType")
        );

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        return controller;
    }
    
    [Fact]
    public async Task GetFolder_Success_ReturnsOk()
    {
        // Arrange
        var folderId = 1;
        var userId = 1;
        var folderDetailDto = new FolderDetailDto { FolderId = folderId, Name = "Test Folder" };

        _userServiceMock.Setup(s => s.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(userId);
        _folderServiceMock.Setup(f => f.GetFolderByIdAsync(folderId, userId)).ReturnsAsync(folderDetailDto);

        // Act
        var result = await _controller.GetFolder(folderId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedFolder = Assert.IsType<FolderDetailDto>(okResult.Value);
        Assert.Equal(folderId, returnedFolder.FolderId); // Change Id to FolderId
        Assert.Equal("Test Folder", returnedFolder.Name);
    }

    [Fact]
    public async Task GetFolder_FolderNotFound_ReturnsNotFoundWithErrorMessage()
    {
        // Arrange
        var folderId = 1;
        var errorMessage = "Folder not found";

        // Mock the service to throw KeyNotFoundException
        _folderServiceMock
            .Setup(f => f.GetFolderByIdAsync(folderId, It.IsAny<int>()))
            .ThrowsAsync(new KeyNotFoundException(errorMessage));

        // Act
        var result = await _controller.GetFolder(folderId);

        // Assert 
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
    
        // Assert that the response value is not null
        Assert.NotNull(notFoundResult.Value);
        dynamic responseContent = notFoundResult.Value;
        _folderServiceMock.Verify(f => f.GetFolderByIdAsync(folderId, It.IsAny<int>()), Times.Once);
    }


    [Fact]
    public async Task GetFolder_ServiceThrowsException_ReturnsInternalServerError()
    {
        var folderId = 1;
        var userId = 123;

        _folderServiceMock.Setup(f => f.GetFolderByIdAsync(folderId, userId))
            .ThrowsAsync(new Exception("Unexpected error"));

        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(userId);

        var result = await _controller.GetFolder(folderId);

        var serverErrorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, serverErrorResult.StatusCode);
        Assert.Equal("An unexpected error occurred.", serverErrorResult.Value);
    }
    
    

    [Fact]
    public async Task GetFolder_UnexpectedError_ReturnsInternalServerError()
    {
        // Arrange
        var folderId = 1;
        _folderServiceMock.Setup(f => f.GetFolderByIdAsync(folderId, It.IsAny<int>())).Throws(new Exception("Unexpected error"));

        // Act
        var result = await _controller.GetFolder(folderId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result); 
        Assert.Equal(500, objectResult.StatusCode); 
        Assert.Equal("An unexpected error occurred.", objectResult.Value);
    }


    [Fact]
    public async Task GetFolder_UserNotFound_ReturnsInternalServerError()
    {
        // Arrange
        var folderId = 1;
        _userServiceMock.Setup(us => us.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(-1);
        _folderServiceMock.Setup(f => f.GetFolderByIdAsync(folderId, It.IsAny<int>())).Throws(new Exception("Unexpected error"));

        // Act
        var result = await _controller.GetFolder(folderId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result); 
        Assert.Equal(500, objectResult.StatusCode); 
        Assert.Equal("An unexpected error occurred.", objectResult.Value); 
    }
    
    // Test for CreateFolder
    [Fact]
    public async Task CreateFolder_ValidData_ReturnsCreatedAtAction()
    {
        // Arrange
        var controller = CreateControllerWithUser(1);
        var folderDto = new FolderCreateDto
        {
            Name = "New Folder",
            ParentFolderId = null
        };
        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(1);

        _folderServiceMock.Setup(f => f.CreateFolderAsync(It.IsAny<Folder>()))
            .Callback<Folder>(folder =>
            {
                folder.Id = 100;
            })
            .Returns(Task.CompletedTask);

        // Act
        var result = await controller.CreateFolder(folderDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(FolderController.GetFolder), createdResult.ActionName);

        var returnedFolder = Assert.IsType<FolderDto>(createdResult.Value);
        Assert.Equal(100, returnedFolder.FolderId);
        Assert.Equal("New Folder", returnedFolder.Name);
    }
        
    [Fact]
    public async Task CreateFolder_ValidInput_ReturnsCreatedAtAction()
    {
        // Arrange
        var controller = CreateControllerWithUser(1);

        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(1);

        _folderServiceMock.Setup(f => f.CreateFolderAsync(It.IsAny<Folder>()))
            .Returns(Task.CompletedTask);

        var folderDto = new FolderCreateDto { Name = "New Folder", ParentFolderId = null };

        // Act
        var result = await controller.CreateFolder(folderDto);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
        var returnedFolder = Assert.IsType<FolderDto>(createdAtActionResult.Value);

        Assert.Equal("New Folder", returnedFolder.Name);
        _folderServiceMock.Verify(f => f.CreateFolderAsync(It.IsAny<Folder>()), Times.Once);
    }

    [Fact]
    public async Task CreateFolder_InvalidInput_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateControllerWithUser(1);
        controller.ModelState.AddModelError("Name", "Name is required");

        var folderDto = new FolderCreateDto { Name = null, ParentFolderId = null };

        // Act
        var result = await controller.CreateFolder(folderDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task CreateFolder_ThrowsArgumentException_ReturnsConflict()
    {
        // Arrange
        var controller = CreateControllerWithUser(1);

        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(1);

        _folderServiceMock.Setup(f => f.CreateFolderAsync(It.IsAny<Folder>()))
            .ThrowsAsync(new ArgumentException("Folder already exists"));

        var folderDto = new FolderCreateDto { Name = "Duplicate Folder", ParentFolderId = null };

        // Act
        var result = await controller.CreateFolder(folderDto);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = conflictResult.Value as ErrorResponse; 
        Assert.NotNull(response);
        Assert.Equal("Folder already exists", response.Message); 
    }

    [Fact]
    public async Task CreateFolder_UnexpectedException_ReturnsInternalServerError()
    {
        // Arrange
        var controller = CreateControllerWithUser(1);

        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(1);

        _folderServiceMock.Setup(f => f.CreateFolderAsync(It.IsAny<Folder>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        var folderDto = new FolderCreateDto { Name = "Valid Folder", ParentFolderId = null };

        // Act
        var result = await controller.CreateFolder(folderDto);

        // Assert
        var internalServerErrorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, internalServerErrorResult.StatusCode);
    }

    [Fact]
    public async Task CreateFolder_ReturnsConflict_WhenFolderCannotBeCreated()
    {
        // Arrange
        var controller = CreateControllerWithUser(1);
        var folderCreateDto = new FolderCreateDto { Name = "TestFolder", ParentFolderId = null };

        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(1);

        _folderServiceMock.Setup(f => f.CreateFolderAsync(It.IsAny<Folder>()))
            .ThrowsAsync(new ArgumentException("Folder already exists."));

        // Act
        var result = await controller.CreateFolder(folderCreateDto);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ErrorResponse>(conflictResult.Value);

        Assert.Equal("Folder already exists.", response.Message);
    }

    [Fact]
    public async Task CreateFolder_ReturnsCreatedAtAction_WhenFolderIsCreated()
    {
        // Arrange
        var folderCreateDto = new FolderCreateDto { Name = "NewFolder", ParentFolderId = null };
        var controller = CreateControllerWithUser(1);
        
        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(1);

        _folderServiceMock.Setup(f => f.CreateFolderAsync(It.IsAny<Folder>()))
            .Returns(Task.CompletedTask); 

        // Act
        var result = await controller.CreateFolder(folderCreateDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result); 
        Assert.NotNull(createdResult); 
        Assert.Equal("GetFolder", createdResult.ActionName);
        Assert.NotNull(createdResult.Value);
    }
    
    // Test for GetFolders
    [Fact]
    public async Task GetFolders_ReturnsOkWithFolders()
    {
        // Arrange
        var controller = CreateControllerWithUser(1);

        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(1);

        _userServiceMock.Setup(u => u.GetUserByIdAsync(1))
            .ReturnsAsync(new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                CreatedDate = DateTime.UtcNow
            });


        _folderServiceMock.Setup(f => f.GetAllFoldersAsDtoAsync(1))
            .ReturnsAsync(new List<FolderDto>
            {
                new FolderDto { FolderId = 10, Name = "Folder1" },
                new FolderDto { FolderId = 20, Name = "Folder2" }
            });

        // Act
        var result = await controller.GetFolders();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as dynamic;
        Assert.NotNull(response);

        // Validate folders
        var folders = response.Folders as IEnumerable<FolderDto>;
        Assert.NotNull(folders);
        Assert.Equal(2, folders.Count());

        // Validate user
        Assert.Equal(1, (int)response.User.UserId);
        Assert.Equal("testuser", (string)response.User.Username);
        Assert.Equal("test@example.com", (string)response.User.Email);
    }
    
    // Test for GetFolder
    [Fact]
    public async Task GetFolder_ValidId_ReturnsOk()
    {
        // Arrange
        var controller = CreateControllerWithUser(1);

        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(1);

        _folderServiceMock.Setup(f => f.GetFolderByIdAsync(10, 1))
            .ReturnsAsync(new FolderDetailDto
            {
                FolderId = 10,
                Name = "Test Folder"
            });

        // Act
        var result = await controller.GetFolder(10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var folder = Assert.IsType<FolderDetailDto>(okResult.Value);
        Assert.Equal(10, folder.FolderId);
        Assert.Equal("Test Folder", folder.Name);
    }


    // Test for UpdateFolder
    [Fact]
    public async Task UpdateFolder_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        var controller = CreateControllerWithUser(1);
        var updateDto = new UpdateFolderDto { Name = "Updated Name" };

        _userServiceMock.Setup(u => u.GetUserIdAsync(It.IsAny<string>()))
            .ReturnsAsync(1);

        _folderServiceMock.Setup(f => f.UpdateFolderAsync(10, updateDto, 1))
            .ReturnsAsync(true);

        // Act
        var result = await controller.UpdateFolder(10, updateDto);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    
    [Fact]
    public async Task UpdateFolder_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateControllerWithUser();
        controller.ModelState.AddModelError("Name", "Required");
        var updateFolderDto = new UpdateFolderDto();

        // Act
        var result = await controller.UpdateFolder(1, updateFolderDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
    }
    
    [Fact]
    public async Task UpdateFolder_UserNotFound_ReturnsInternalServerError()
    {
        // Arrange
        var controller = CreateControllerWithUser();
        _userServiceMock.Setup(u => u.GetUserIdAsync(It.IsAny<string>())).ReturnsAsync(-1);

        var updateFolderDto = new UpdateFolderDto { Name = "Updated Folder" };

        // Act
        var result = await controller.UpdateFolder(1, updateFolderDto);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result); 
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal("UserId not found. User might not exist.", statusCodeResult.Value);
    }


    [Fact]
    public async Task UpdateFolder_FolderNotFound_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateControllerWithUser();
        _folderServiceMock.Setup(f => f.UpdateFolderAsync(It.IsAny<int>(), It.IsAny<UpdateFolderDto>(), It.IsAny<int>())).ReturnsAsync(false); // Folder not found

        var updateFolderDto = new UpdateFolderDto { Name = "Updated Folder" };

        // Act
        var result = await controller.UpdateFolder(1, updateFolderDto);

        // Assert that the result is NotFoundObjectResult
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
    
        // Ensure the value is not null
        Assert.NotNull(notFoundResult.Value);
    }
    
    [Fact]
    public async Task UpdateFolder_Success_ReturnsNoContent()
    {
        // Arrange
        var controller = CreateControllerWithUser();
        _folderServiceMock.Setup(f => f.UpdateFolderAsync(It.IsAny<int>(), It.IsAny<UpdateFolderDto>(), It.IsAny<int>())).ReturnsAsync(true); // Folder successfully updated

        var updateFolderDto = new UpdateFolderDto { Name = "Updated Folder" };

        // Act
        var result = await controller.UpdateFolder(1, updateFolderDto);

        // Assert
        var noContentResult = Assert.IsType<NoContentResult>(result);
        Assert.Equal(204, noContentResult.StatusCode);
    }
    [Fact]
    public async Task CreateFolder_DuplicateFolder_ReturnsConflict()
    {
        // Arrange
        var folderCreateDto = new FolderCreateDto { Name = "Duplicate Folder" };
        var exceptionMessage = "A folder with this name already exists.";

        _userServiceMock
            .Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(123);

        _folderServiceMock
            .Setup(f => f.CreateFolderAsync(It.IsAny<Folder>()))
            .Throws(new ArgumentException(exceptionMessage));

        // Act
        var result = await _controller.CreateFolder(folderCreateDto);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflictResult.StatusCode);
        var errorResponse = Assert.IsType<ErrorResponse>(conflictResult.Value);
        Assert.Equal(exceptionMessage, errorResponse.Message);
    }
    
    [Fact]
    public async Task CreateFolder_UnhandledException_ReturnsInternalServerError()
    {
        // Arrange
        var folderCreateDto = new FolderCreateDto { Name = "New Folder" };

        _userServiceMock
            .Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(123);

        _folderServiceMock
            .Setup(f => f.CreateFolderAsync(It.IsAny<Folder>()))
            .Throws(new Exception("Unexpected error"));

        // Act
        var result = await _controller.CreateFolder(folderCreateDto);

        // Assert
        var serverErrorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, serverErrorResult.StatusCode);
        Assert.Equal("Unexpected error occured.", serverErrorResult.Value);
    }



    [Fact]
    public async Task UpdateFolder_ArgumentException_ReturnsConflict()
    {
        // Arrange
        var controller = CreateControllerWithUser();
        _folderServiceMock.Setup(f => f.UpdateFolderAsync(It.IsAny<int>(), It.IsAny<UpdateFolderDto>(), It.IsAny<int>()))
            .ThrowsAsync(new ArgumentException("Invalid folder data"));

        var updateFolderDto = new UpdateFolderDto { Name = "Updated Folder" };

        // Act
        var result = await controller.UpdateFolder(1, updateFolderDto);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = conflictResult.Value as ErrorResponse;
        Assert.Equal("Invalid folder data", response?.Message);
    }
    
    [Fact]
    public async Task UpdateFolder_UnexpectedError_ReturnsInternalServerError()
    {
        // Arrange
        var controller = CreateControllerWithUser();
        _folderServiceMock.Setup(f => f.UpdateFolderAsync(It.IsAny<int>(), It.IsAny<UpdateFolderDto>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("An unexpected error occurred."));

        var updateFolderDto = new UpdateFolderDto { Name = "Updated Folder" };

        // Act
        var result = await controller.UpdateFolder(1, updateFolderDto);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal("An unexpected Error occured.", statusCodeResult.Value);
    }


    

    [Fact]
    public async Task UpdateFolder_UnexpectedError_ReturnsServerError()
    {
        // Arrange
        var controller = CreateControllerWithUser();
        _folderServiceMock.Setup(f => f.UpdateFolderAsync(It.IsAny<int>(), It.IsAny<UpdateFolderDto>(), It.IsAny<int>()))
                          .ThrowsAsync(new Exception("Unexpected error"));

        var updateFolderDto = new UpdateFolderDto { Name = "Folder" };

        // Act
        var result = await controller.UpdateFolder(1, updateFolderDto);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("An unexpected Error occured.", objectResult.Value);
    }
    
    
    
    
    
    
    
    
    
    
    
    
    
    [Fact]
    public async Task DeleteFolder_UserNotFound_ReturnsServerError()
    {
        // Arrange
        var controller = CreateControllerWithUser(1);
        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(-1);
        
        var folderId = 999;

        // Act
        var result = await controller.DeleteFolder(folderId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Contains("UserId not found", objectResult.Value.ToString());
    }



    [Fact]
    public async Task DeleteFolder_FolderNotFoundOrNotBelongsToUser_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateControllerWithUser(1);
        _folderServiceMock.Setup(f => f.DeleteFolderAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(false); 

        var folderId = 999;

        // Act
        var result = await controller.DeleteFolder(folderId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = JObject.FromObject(notFoundResult.Value);
        Assert.Equal("Folder with ID 999 was not found or does not belong to the user.", response["message"].ToString());
    }



    
    
    
    


    [Fact]
    public async Task DeleteFolder_Success_ReturnsNoContent()
    {
        // Arrange
        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(1); 

        _folderServiceMock.Setup(f => f.DeleteFolderAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteFolder(1); 

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteFolder_UnexpectedError_ReturnsServerError()
    {
        // Arrange
        var controller = CreateControllerWithUser(1);
        
        _folderServiceMock.Setup(f => f.DeleteFolderAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        var folderId = 999;

        // Act
        var result = await controller.DeleteFolder(folderId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Contains("Unexpected error", objectResult.Value.ToString());
    }

}