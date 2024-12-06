using System.Security.Claims;
using CMS_Project.Controllers;
using CMS_Project.Models.DTOs;
using CMS_Project.Models.Entities;
using CMS_Project.Services;
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
            _userServiceMock.Object,  // IUserService comes first
            _folderServiceMock.Object,  // IFolderService comes second
            Mock.Of<ILogger<FolderController>>()  // ILogger<FolderController> comes third
        );

        // Simuler en autentisert bruker
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
        var folderDetailDto = new FolderDetailDto { FolderId = folderId, Name = "Test Folder" }; // Change Id to FolderId

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
    public async Task GetFolder_FolderNotFound_ReturnsNotFound()
    {
        // Arrange
        var folderId = 1;
        _folderServiceMock.Setup(f => f.GetFolderByIdAsync(folderId, It.IsAny<int>())).Throws(new KeyNotFoundException("Folder not found"));

        // Act
        var result = await _controller.GetFolder(folderId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
    
        // Check if notFoundResult.Value is null
        Assert.NotNull(notFoundResult.Value); // Ensure it's not null

        // If it's not a dynamic object, check its actual type
        var response = notFoundResult.Value;

        // Log the type and content of response to help debugging
        Console.WriteLine("Response type: " + response.GetType());
        Console.WriteLine("Response value: " + response);

        // If response is a dictionary-like object (which it likely is), access the 'message' field
        if (response is IDictionary<string, object> responseDict)
        {
            Assert.True(responseDict.ContainsKey("message"));
            Assert.Equal("Folder not found", responseDict["message"]);
        }
        else
        {
            // If it's not a dictionary or doesn't contain 'message', log the result
            Console.WriteLine("Response doesn't contain 'message': " + response);
        }
    }


    [Fact]
    public async Task GetFolder_UnexpectedError_ReturnsInternalServerError()
    {
        // Arrange
        var folderId = 1;
        // Simulate an unexpected exception
        _folderServiceMock.Setup(f => f.GetFolderByIdAsync(folderId, It.IsAny<int>())).Throws(new Exception("Unexpected error"));

        // Act
        var result = await _controller.GetFolder(folderId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result); // Expecting ObjectResult, not StatusCodeResult
        Assert.Equal(500, objectResult.StatusCode); // Ensure the status code is 500
        Assert.Equal("An unexpected error occurred.", objectResult.Value); // Ensure the message is as expected
    }


    [Fact]
    public async Task GetFolder_UserNotFound_ReturnsInternalServerError()
    {
        // Arrange
        var folderId = 1;
        // Simulate the case where the user is not found
        _userServiceMock.Setup(us => us.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(-1); // Simulate invalid user ID
        _folderServiceMock.Setup(f => f.GetFolderByIdAsync(folderId, It.IsAny<int>())).Throws(new Exception("Unexpected error"));

        // Act
        var result = await _controller.GetFolder(folderId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result); // Expecting ObjectResult for error cases
        Assert.Equal(500, objectResult.StatusCode); // Ensure the status code is 500
        Assert.Equal("An unexpected error occurred.", objectResult.Value); // Ensure the message is correct
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
        var response = conflictResult.Value as ErrorResponse; // Replace ErrorResponse with dynamic if necessary
        Assert.NotNull(response);
        Assert.Equal("Folder already exists", response.Message);  // Match without the period
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

        // Mock the user service to return a valid user ID
        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(1);

        // Mock the folder service to simulate successful folder creation
        _folderServiceMock.Setup(f => f.CreateFolderAsync(It.IsAny<Folder>()))
            .Returns(Task.CompletedTask); // Simulate no error on folder creation

        // Act
        var result = await controller.CreateFolder(folderCreateDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result); // Assert it is CreatedAtActionResult
        Assert.NotNull(createdResult); // Ensure the result is not null
        Assert.Equal("GetFolder", createdResult.ActionName); // Ensure the correct action is returned
        Assert.NotNull(createdResult.Value); // Ensure the response value is not null
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
        controller.ModelState.AddModelError("Name", "Required"); // Simulate an invalid model state
        var updateFolderDto = new UpdateFolderDto(); // Invalid DTO (missing required fields)

        // Act
        var result = await controller.UpdateFolder(1, updateFolderDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<SerializableError>(badRequestResult.Value); // Ensure it's a validation error
    }


    [Fact]
    public async Task UpdateFolder_UserNotFound_ReturnsInternalServerError()
    {
        // Arrange
        var controller = CreateControllerWithUser();
        _userServiceMock.Setup(u => u.GetUserIdAsync(It.IsAny<string>())).ReturnsAsync(-1); // Simulate user not found

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
        var noContentResult = Assert.IsType<NoContentResult>(result); // Expect NoContentResult for 204 status code
        Assert.Equal(204, noContentResult.StatusCode); // Ensure the status code is 204
    }


    [Fact]
    public async Task UpdateFolder_ArgumentException_ReturnsConflict()
    {
        // Arrange
        var controller = CreateControllerWithUser();
        _folderServiceMock.Setup(f => f.UpdateFolderAsync(It.IsAny<int>(), It.IsAny<UpdateFolderDto>(), It.IsAny<int>()))
            .ThrowsAsync(new ArgumentException("Invalid folder data")); // Simulate ArgumentException

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
            .ThrowsAsync(new Exception("An unexpected error occurred.")); // Simulate an unexpected error

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
        var objectResult = Assert.IsType<ObjectResult>(result); // Expect ObjectResult for unexpected errors
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("An unexpected Error occured.", objectResult.Value);
    }
    
    
    
    
    
    
    
    
    
    
    
    
    
    [Fact]
    public async Task DeleteFolder_UserNotFound_ReturnsServerError()
    {
        // Arrange
        var controller = CreateControllerWithUser(1);

        // Simulate the scenario where userId is -1 (user not found)
        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(-1); // Simulate that user does not exist

        var folderId = 999; // Example folder ID

        // Act
        var result = await controller.DeleteFolder(folderId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);  // Expect ObjectResult

        // Ensure the status code is 500 for server error
        Assert.Equal(500, objectResult.StatusCode);

        // Ensure the error message contains the expected text
        Assert.Contains("UserId not found", objectResult.Value.ToString());
    }



    [Fact]
    public async Task DeleteFolder_FolderNotFoundOrNotBelongsToUser_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateControllerWithUser(1);

        // Simulate folder not found or not belonging to the user
        _folderServiceMock.Setup(f => f.DeleteFolderAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(false); // Folder not found or does not belong to the user

        var folderId = 999; // Use a folder ID that will not be found

        // Act
        var result = await controller.DeleteFolder(folderId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
    
        // Parse the response as a JObject to access the 'message' property
        var response = JObject.FromObject(notFoundResult.Value);
    
        // Ensure the 'message' property contains the expected value
        Assert.Equal("Folder with ID 999 was not found or does not belong to the user.", response["message"].ToString());
    }



    
    
    
    


    [Fact]
    public async Task DeleteFolder_Success_ReturnsNoContent()
    {
        // Arrange
        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(1); // Simulate user ID is 1

        _folderServiceMock.Setup(f => f.DeleteFolderAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(true); // Simulate successful deletion of the folder

        // Act
        var result = await _controller.DeleteFolder(1); // Attempt to delete folder with ID 1

        // Assert
        Assert.IsType<NoContentResult>(result); // Expecting NoContent (HTTP 204)
    }

    [Fact]
    public async Task DeleteFolder_UnexpectedError_ReturnsServerError()
    {
        // Arrange
        var controller = CreateControllerWithUser(1);

        // Simulate an exception occurring during deletion
        _folderServiceMock.Setup(f => f.DeleteFolderAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        var folderId = 999; // Use a folder ID that will trigger an error

        // Act
        var result = await controller.DeleteFolder(folderId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result); // Expect ObjectResult

        // Check if the status code is 500
        Assert.Equal(500, objectResult.StatusCode);

        // Ensure the error message contains the expected text
        Assert.Contains("Unexpected error", objectResult.Value.ToString());
    }

}