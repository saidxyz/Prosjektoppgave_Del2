using CMS_Project.Controllers;
using CMS_Project.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq; 
using CMS_Project.Models.DTOs;
using Microsoft.EntityFrameworkCore;


public class DocumentControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<IFolderService> _folderServiceMock;
    private readonly Mock<IDocumentService> _documentServiceMock;
    private readonly Mock<ILogger<DocumentController>> _loggerMock;
    private readonly DocumentController _controller;
    private readonly ClaimsPrincipal _userPrincipal;

    public DocumentControllerTests()
    {
        _documentServiceMock = new Mock<IDocumentService>();
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<DocumentController>>();
        _folderServiceMock = new Mock<IFolderService>();
        _controller = new DocumentController(
            _documentServiceMock.Object,
            _userServiceMock.Object,
            _loggerMock.Object,
            _folderServiceMock.Object);
    }

    private DocumentController CreateControllerWithUser(int userId)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] 
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "mock"));
        
        var controller = new DocumentController(
            _documentServiceMock.Object,
            _userServiceMock.Object,
            _loggerMock.Object,
            _folderServiceMock.Object
        );

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = user }
        };

        return controller;
    }
    
    [Fact]
    public async Task GetAllDocuments_ReturnsOk_WithValidDocuments()
    {
        var userId = 1;
        var documents = new List<DocumentDto>
        {
            new DocumentDto
            {
                DocumentId = 1, 
                Title = "Test Document 1",
                Content = "Content of document 1",
                ContentType = "text/plain",
                CreatedDate = DateTime.UtcNow
            },
            new DocumentDto
            {
                DocumentId = 2, 
                Title = "Test Document 2",
                Content = "Content of document 2",
                ContentType = "text/plain",
                CreatedDate = DateTime.UtcNow
            }
        };
        
        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(userId);

        _userServiceMock.Setup(u => u.GetUserDtoByIdAsync(userId))
            .ReturnsAsync(new UserDto
            {
                UserId = userId,
                Username = "testuser",
                Email = "testuser@example.com",
                CreatedDate = DateTime.UtcNow
            });

        _documentServiceMock.Setup(d => d.GetAllDocumentsAsync(userId))
            .ReturnsAsync(documents); 

        // Act
        var result = await _controller.GetAllDocuments();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = JObject.FromObject(okResult.Value);

        Assert.Equal(userId, (int)response["user"]["userId"]);
        Assert.Equal("testuser", (string)response["user"]["username"]);
        Assert.Equal("testuser@example.com", (string)response["user"]["email"]);
        Assert.NotNull(response["user"]["createdDate"]);

        var documentsArray = (JArray)response["documents"];
        Assert.Equal(2, documentsArray.Count);

        var firstDocument = documentsArray[0];
        Assert.Equal(1, (int)firstDocument["documentId"]);
        Assert.Equal("Test Document 1", (string)firstDocument["title"]);

        var secondDocument = documentsArray[1];
        Assert.Equal(2, (int)secondDocument["documentId"]);
        Assert.Equal("Test Document 2", (string)secondDocument["title"]);
    }
    
    
    [Fact]
    public async Task GetAllDocuments_ReturnsStatusCode500_WhenUserNotFound()
    {
        // Arrange
        var userId = 1;

        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(userId);
        _userServiceMock.Setup(u => u.GetUserDtoByIdAsync(userId)).ReturnsAsync((UserDto)null); // Simulate user not found

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "mock"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Act
        var result = await _controller.GetAllDocuments();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result); 
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal("User not found.", statusCodeResult.Value); 
    }

    [Fact]
    public async Task GetAllDocuments_ReturnsStatusCode500_WhenExceptionIsThrown()
    {
        // Arrange
        var userId = 1;
        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>())).ThrowsAsync(new Exception("Test exception"));
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "mock"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Act
        var result = await _controller.GetAllDocuments();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result); 
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal("An unexpected error occurred.", statusCodeResult.Value); 
    }
    
    
    [Fact]
    public async Task UpdateDocument_ReturnsBadRequest_WhenModelStateIsInvalid()
    {
        // Arrange
        var documentId = 1;
        var updateDocumentDto = new UpdateDocumentDto();  // Invalid model
        _controller.ModelState.AddModelError("Title", "Required");

        // Act
        var result = await _controller.UpdateDocument(documentId, updateDocumentDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var serializableError = Assert.IsType<SerializableError>(badRequestResult.Value);
        var errors = (string[])serializableError["Title"];
        Assert.Contains("Required", errors);
    }


    [Fact]
    public async Task UpdateDocument_ReturnsStatusCode500_WhenUserIdNotFound()
    {
        // Arrange
        var documentId = 1;
        var updateDocumentDto = new UpdateDocumentDto();
        
        _userServiceMock.Setup(u => u.GetUserIdAsync(It.IsAny<string>())).ReturnsAsync(-1);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, "1") }, "mock"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Act
        var result = await _controller.UpdateDocument(documentId, updateDocumentDto);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result); 
        Assert.Equal(500, statusCodeResult.StatusCode); 
        var response = Assert.IsType<string>(statusCodeResult.Value); 
        Assert.Equal("UserId not found. User might not exist.", response); 
    }

    
    [Fact]
    public async Task UpdateDocument_ReturnsNoContent_WhenUpdateIsSuccessful()
    {
        // Arrange
        var documentId = 1;
        var updateDocumentDto = new UpdateDocumentDto
        {
        };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1")
        }));

        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
        };
        _userServiceMock.Setup(u => u.GetUserIdAsync(It.IsAny<string>())).ReturnsAsync(1);
        _documentServiceMock.Setup(d => d.UpdateDocumentAsync(documentId, updateDocumentDto, 1))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateDocument(documentId, updateDocumentDto);

        // Assert
        var noContentResult = Assert.IsType<NoContentResult>(result);
    }


    [Fact]
    public async Task UpdateDocument_ReturnsStatusCode500_WhenDbUpdateConcurrencyExceptionIsThrown()
    {
        // Arrange
        var documentId = 1;
        var updateDocumentDto = new UpdateDocumentDto();

        _userServiceMock.Setup(u => u.GetUserIdAsync(It.IsAny<string>())).ReturnsAsync(1); 
        _documentServiceMock.Setup(d => d.UpdateDocumentAsync(It.IsAny<int>(), It.IsAny<UpdateDocumentDto>(), It.IsAny<int>()))
            .ThrowsAsync(new DbUpdateConcurrencyException()); 

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, "1") }, "mock"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Act
        var result = await _controller.UpdateDocument(documentId, updateDocumentDto);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result); 
        Assert.Equal(500, statusCodeResult.StatusCode); 
    }


    [Fact]
    public async Task UpdateDocument_ReturnsStatusCode500_WhenExceptionIsThrown()
    {
        // Arrange
        var documentId = 1;
        var updateDocumentDto = new UpdateDocumentDto();
        _userServiceMock.Setup(u => u.GetUserIdAsync(It.IsAny<string>())).ReturnsAsync(1);
        _documentServiceMock.Setup(d => d.UpdateDocumentAsync(It.IsAny<int>(), It.IsAny<UpdateDocumentDto>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Something went wrong"));
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, "1") }, "mock"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Act
        var result = await _controller.UpdateDocument(documentId, updateDocumentDto);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result); 
        Assert.Equal(500, statusCodeResult.StatusCode);  
    }


    
    [Fact]
    public async Task CreateDocument_ValidData_ReturnsCreatedAtAction()
    {
        // Arrange
        var documentCreateDto = new DocumentCreateDto
        {
            Title = "New Document",
            Content = "This is a new document."
        };
        var userId = 1;

        var createdDocumentResponse = new DocumentResponseDto
        {
            Document = new DocumentDetailDto
            {
                DocumentId = 1,
                Title = documentCreateDto.Title,
                Content = documentCreateDto.Content
            }
        };

        _userServiceMock
            .Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(userId);

        _documentServiceMock
            .Setup(d => d.CreateDocumentAsync(documentCreateDto, userId))
            .ReturnsAsync(createdDocumentResponse);

        var controller = CreateControllerWithUser(userId);

        // Act
        var result = await controller.CreateDocument(documentCreateDto);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
        var documentResponseDto = Assert.IsType<DocumentResponseDto>(createdAtActionResult.Value);
        Assert.Equal(createdDocumentResponse.Document.DocumentId, documentResponseDto.Document.DocumentId);
    }
    

    [Fact]
    public async Task CreateDocument_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var documentCreateDto = new DocumentCreateDto { Title = "", Content = "" };
    
        var controller = CreateControllerWithUser(1);
        controller.ModelState.AddModelError("Title", "Title is required.");
    
        // Act
        var result = await controller.CreateDocument(documentCreateDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
    }


    [Fact]
    public async Task CreateDocument_ArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var documentCreateDto = new DocumentCreateDto
        {
            Title = "Invalid Document",
            Content = "This document will fail due to invalid data."
        };
        var userId = 1;

        _userServiceMock
            .Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(userId);

        _documentServiceMock
            .Setup(d => d.CreateDocumentAsync(documentCreateDto, userId))
            .ThrowsAsync(new ArgumentException("Invalid document data"));

        var controller = CreateControllerWithUser(userId);

        // Act
        var result = await controller.CreateDocument(documentCreateDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorObject = Assert.IsType<ErrorResponse>(badRequestResult.Value); 
        Assert.Equal("Invalid document data", errorObject.Message); 
    }
    
    
    [Fact]
    public async Task GetDocumentById_ReturnsOk_WhenDocumentFound()
    {
        // Arrange
        var documentId = 1;
        var userId = 1;

        var documentDetail = new DocumentDetailDto { Content = "Content" }; 
        var folderDto = new FolderDto { Name = "Test Folder" };
    
        var documentResponse = new DocumentResponseDto
        {
            Document = documentDetail,
            Folder = folderDto
        };
        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(userId);
        _documentServiceMock.Setup(d => d.GetDocumentByIdAsync(documentId, userId)).ReturnsAsync(documentResponse);

        // Act
        var result = await _controller.GetDocumentById(documentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<DocumentResponseDto>(okResult.Value);
        Assert.NotNull(returnValue);
        
        Assert.Equal("Test Folder", returnValue.Folder.Name); 
        Assert.Equal("Content", returnValue.Document.Content); 
    }
    
    [Fact]
    public async Task GetDocumentById_ReturnsNotFound_WhenKeyNotFoundExceptionIsThrown()
    {
        // Arrange
        var documentId = 1;
        var userId = 1;
        var expectedMessage = "Document not found in the database.";

        // Mock only the required behavior
        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(userId);
        _documentServiceMock.Setup(d => d.GetDocumentByIdAsync(documentId, userId))
            .ThrowsAsync(new KeyNotFoundException(expectedMessage));

        // Act
        var result = await _controller.GetDocumentById(documentId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = notFoundResult.Value as dynamic;
        Assert.NotNull(response); 
    }

    [Fact]
    public void Constructor_InitializesProperties_Correctly()
    {
        // Arrange
        var folder = new FolderDto { Name = "Test Folder" };
        var document = new DocumentDetailDto { Title = "Test Document", Content = "Some Content" };

        // Act
        var response = new DocumentResponseDto(folder, document);

        // Assert
        Assert.NotNull(response.Folder);
        Assert.Equal("Test Folder", response.Folder?.Name);
        Assert.NotNull(response.Document);
        Assert.Equal("Test Document", response.Document.Title);
        Assert.Equal("Some Content", response.Document.Content);
    }

    [Fact]
    public void Constructor_UsesDefaults_WhenNoArgumentsPassed()
    {
        // Act
        var response = new DocumentResponseDto();

        // Assert
        Assert.Null(response.Folder);
        Assert.NotNull(response.Document);
        Assert.Equal(string.Empty, response.Document.Title); 
        Assert.Equal(string.Empty, response.Document.Content); 
    }
    
    [Fact]
    public async Task GetDocumentById_ReturnsNotFound_WhenDocumentNotFound()
    {
        // Arrange
        var documentId = 1;
        var userId = 1;
        var expectedMessage = $"Document with ID {documentId} was not found or does not belong to the user.";

        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(userId);
        _documentServiceMock.Setup(d => d.GetDocumentByIdAsync(documentId, userId)).ReturnsAsync((DocumentResponseDto)null);

        // Act
        var result = await _controller.GetDocumentById(documentId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var value = notFoundResult.Value as dynamic;
        Assert.NotNull(value);
    }

    
    [Fact]
    public async Task GetDocumentById_ReturnsInternalServerError_WhenExceptionIsThrown()
    {
        // Arrange
        var documentId = 1;
        var userId = 1;
        var expectedMessage = "An unexpected error occurred.";

        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(userId);
        _documentServiceMock.Setup(d => d.GetDocumentByIdAsync(documentId, userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetDocumentById(documentId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal($"An unexpected error occurred: Database error", statusCodeResult.Value);
    }

    
    [Fact]
    public async Task CreateDocument_DbUpdateException_ReturnsInternalServerError()
    {
        // Arrange
        var documentCreateDto = new DocumentCreateDto
        {
            Title = "New Document",
            Content = "This document will fail due to a database error."
        };
        var userId = 1;
        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(userId);
        _documentServiceMock.Setup(d => d.CreateDocumentAsync(documentCreateDto, userId)).ThrowsAsync(new DbUpdateException("Database error"));
        var controller = CreateControllerWithUser(userId);

        // Act
        var result = await controller.CreateDocument(documentCreateDto);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);  // Expecting ObjectResult for 500 errors
        Assert.Equal(500, statusCodeResult.StatusCode);  // Check if the status code is 500
        Assert.Equal("A database error occurred.", statusCodeResult.Value);  // Check the error message
    }
    
    [Fact]
    public async Task CreateDocument_FolderNotFound_ReturnsBadRequest()
    {
        // Arrange
        var documentCreateDto = new DocumentCreateDto
        {
            Title = "New Document",
            Content = "This document belongs to a non-existent folder.",
            FolderId = 999 
        };
        var userId = 1;
        _userServiceMock
            .Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(userId);
        
        _folderServiceMock
            .Setup(f => f.GetFolderByIdAsync(999, userId))
            .ReturnsAsync((FolderDetailDto)null);  // Folder not found

        var controller = CreateControllerWithUser(userId);

        // Act
        var result = await controller.CreateDocument(documentCreateDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorObject = JObject.FromObject(badRequestResult.Value);
        Assert.Equal("Specified folder does not exist or does not belong to the user.", errorObject["error"]["message"].ToString());
    }

    
    [Fact]
    public async Task CreateDocument_UnhandledException_ReturnsInternalServerError()
    {
        // Arrange
        var documentCreateDto = new DocumentCreateDto
        {
            Title = "Unhandled Exception",
            Content = "This document will cause an unexpected error."
        };
        var userId = 1;

        _userServiceMock
            .Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(userId);

        _documentServiceMock
            .Setup(d => d.CreateDocumentAsync(documentCreateDto, userId))
            .ThrowsAsync(new Exception("Unexpected error"));

        var controller = CreateControllerWithUser(userId);

        // Act
        var result = await controller.CreateDocument(documentCreateDto);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);  
        Assert.Equal(500, objectResult.StatusCode);  
        Assert.Equal("An unexpected error occurred.", objectResult.Value); 
    }


    
    
    [Fact]
    public async Task CreateDocument_UnexpectedError_ReturnsInternalServerError()
    {
        // Arrange
        var documentCreateDto = new DocumentCreateDto
        {
            Title = "New Document",
            Content = "This document will fail due to an unexpected error."
        };
        var userId = 1;
        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(userId);
        _documentServiceMock.Setup(d => d.CreateDocumentAsync(documentCreateDto, userId)).ThrowsAsync(new Exception("Unexpected error"));
        var controller = CreateControllerWithUser(userId);

        // Act
        var result = await controller.CreateDocument(documentCreateDto);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result); 
        Assert.Equal(500, statusCodeResult.StatusCode);  
        Assert.Equal("An unexpected error occurred.", statusCodeResult.Value);
    }

    
    [Fact]
    public async Task CreateDocument_ValidData_ReturnsCreatedDocument()
    {
        int userId = 1;
        var createdDocumentResponse = new DocumentResponseDto
        {
            Document = new DocumentDetailDto
            {
                DocumentId = 123, 
                Title = "New Document",
                Content = "Document Content",
                ContentType = "text/plain",
                CreatedDate = DateTime.Now,
                FolderId = 1,
                Folder = new FolderDto { FolderId = 1, Name = "Test Folder" }
            }
        };
        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(userId);
        _folderServiceMock.Setup(f => f.GetFolderByIdAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new FolderDetailDto { FolderId = 1, Name = "Test Folder" });
        _documentServiceMock.Setup(d => d.CreateDocumentAsync(It.IsAny<DocumentCreateDto>(), userId)).ReturnsAsync(createdDocumentResponse);
        var controller = CreateControllerWithUser(userId);

        // Act
        var result = await controller.CreateDocument(new DocumentCreateDto { Title = "New Document", Content = "Document Content", FolderId = 1 });

        // Assert
        var actionResult = Assert.IsType<CreatedAtActionResult>(result);  
        var createdDocument = Assert.IsType<DocumentResponseDto>(actionResult.Value);

        Assert.Equal(123, createdDocument.Document.DocumentId); 
        Assert.Equal("New Document", createdDocument.Document.Title); 
    }
    
    
    [Fact]
    public async Task GetDocumentById_ValidId_ReturnsDocument()
    {
        // Arrange
        int documentId = 1;
        int userId = 1;

        var expectedDocument = new DocumentDetailDto
        {
            DocumentId = documentId,
            Title = "Test Document",
            Content = "This is a test document.",
            ContentType = "text/plain",
            CreatedDate = DateTime.UtcNow,
            FolderId = 1,
            Folder = new FolderDto { FolderId = 1, Name = "Test Folder" }
        };

        var documentResponseDto = new DocumentResponseDto
        {
            Document = expectedDocument
        };

        _documentServiceMock.Setup(d => d.GetDocumentByIdAsync(documentId, userId))
            .ReturnsAsync(documentResponseDto);

        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(userId);

        var controller = CreateControllerWithUser(userId);

        // Act
        var result = await controller.GetDocumentById(documentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result); 
        var actualDocument = Assert.IsType<DocumentResponseDto>(okResult.Value);

        Assert.NotNull(actualDocument);
        Assert.NotNull(actualDocument.Document);
        Assert.Equal(expectedDocument.DocumentId, actualDocument.Document.DocumentId);
        Assert.Equal(expectedDocument.Title, actualDocument.Document.Title);
        Assert.Equal(expectedDocument.Content, actualDocument.Document.Content);
        Assert.Equal(expectedDocument.ContentType, actualDocument.Document.ContentType);
        Assert.Equal(expectedDocument.FolderId, actualDocument.Document.FolderId);

        Assert.NotNull(actualDocument.Document.Folder);
        Assert.Equal(expectedDocument.Folder.FolderId, actualDocument.Document.Folder.FolderId);
        Assert.Equal(expectedDocument.Folder.Name, actualDocument.Document.Folder.Name);

        _documentServiceMock.Verify(d => d.GetDocumentByIdAsync(documentId, userId), Times.Once);
        _userServiceMock.Verify(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }
    
    
    [Fact]
    public async Task CreateDocument_InvalidData_ReturnsBadRequest()
    {
        // Arrange
        var documentCreateDto = new DocumentCreateDto { Title = "", Content = "Content" }; // Invalid Title
        var userId = 1;
    
        var controller = CreateControllerWithUser(userId);
        controller.ModelState.AddModelError("Title", "Title is required.");

        // Act
        var result = await controller.CreateDocument(documentCreateDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }
    
    
    [Fact]
    public async Task GetDocumentById_ValidId_ReturnsOk()
    {
        // Arrange
        var documentId = 1;
        var userId = 1;
        
        var documentDto = new DocumentDetailDto
        { 
            DocumentId = documentId, 
            Title = "Test Document", 
            Content = "Content"
        };

        var documentResponseDto = new DocumentResponseDto
        {
            Document = documentDto
        };
        
        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(userId);
        _documentServiceMock.Setup(d => d.GetDocumentByIdAsync(documentId, userId)).ReturnsAsync(documentResponseDto);

        var controller = CreateControllerWithUser(userId);

        // Act
        var result = await controller.GetDocumentById(documentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var resultDto = Assert.IsType<DocumentResponseDto>(okResult.Value);
        Assert.Equal(documentId, resultDto.Document.DocumentId);
        Assert.Equal("Test Document", resultDto.Document.Title);
    }
    
    [Fact]
    public async Task GetDocumentById_DocumentNotFound_ReturnsNotFound()
    {
        // Arrange
        var documentId = 1;
        var userId = 1;
        
        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(userId);
        
        _documentServiceMock.Setup(d => d.GetDocumentByIdAsync(documentId, userId))
            .ReturnsAsync((DocumentResponseDto)null); 

        var controller = CreateControllerWithUser(userId);

        // Act
        var result = await controller.GetDocumentById(documentId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
        Assert.Contains("Document with ID", notFoundResult.Value.ToString());
    }
    
    
    [Fact]
    public async Task UpdateDocument_DocumentNotFound_ReturnsNotFound()
    {
        // Arrange
        var documentId = 1;
        var userId = 1;
        var updateDto = new UpdateDocumentDto { Title = "Updated Title", Content = "Updated Content" };

        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(userId);
        _documentServiceMock.Setup(d => d.UpdateDocumentAsync(documentId, updateDto, userId)).ReturnsAsync(false);

        var controller = CreateControllerWithUser(userId);

        // Act
        var result = await controller.UpdateDocument(documentId, updateDto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }
    
    
    [Fact]
    public async Task DeleteDocument_ValidId_ReturnsNoContent()
    {
        // Arrange
        var documentId = 1;
        var userId = 1;

        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(userId);
        _documentServiceMock.Setup(d => d.DeleteDocumentAsync(documentId, userId)).ReturnsAsync(true);

        var controller = CreateControllerWithUser(userId);

        // Act
        var result = await controller.DeleteDocument(documentId);

        // Assert
        var noContentResult = Assert.IsType<NoContentResult>(result);
        Assert.Equal(204, noContentResult.StatusCode);
    }
    [Fact]
    public async Task DeleteDocument_DocumentNotFound_ReturnsNotFound()
    {
        // Arrange
        var documentId = 1;
        var userId = 1;

        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(userId);
        _documentServiceMock.Setup(d => d.DeleteDocumentAsync(documentId, userId)).ReturnsAsync(false);

        var controller = CreateControllerWithUser(userId);

        // Act
        var result = await controller.DeleteDocument(documentId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
        Assert.Contains("not found", notFoundResult.Value.ToString());
    }
    
    [Fact]
    public async Task DeleteDocument_Success_ReturnsNoContent()
    {
        // Arrange
        var documentId = 1;
        var userId = 1;
        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(userId);
        _documentServiceMock.Setup(d => d.DeleteDocumentAsync(documentId, userId)).ReturnsAsync(true);  // Document successfully deleted

        var controller = CreateControllerWithUser(userId);

        // Act
        var result = await controller.DeleteDocument(documentId);

        // Assert
        var noContentResult = Assert.IsType<NoContentResult>(result);  // Expecting HTTP 204
    }

    
    [Fact]
    public async Task DeleteDocument_UserIdNotFound_ReturnsInternalServerError()
    {
        // Arrange
        var documentId = 1;
        var userId = -1;  // Simulate invalid user ID
        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(userId);
    
        var controller = CreateControllerWithUser(userId);

        // Act
        var result = await controller.DeleteDocument(documentId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);  
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("UserId not found. User might not exist.", objectResult.Value);
    }

    
    [Fact]
    public async Task DeleteDocument_SuccessfulDeletion_ReturnsNoContent()
    {
        // Arrange
        var documentId = 1;
        var userId = 1;
        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(userId);
        _documentServiceMock.Setup(d => d.DeleteDocumentAsync(documentId, userId)).ReturnsAsync(true);

        var controller = CreateControllerWithUser(userId);

        // Act
        var result = await controller.DeleteDocument(documentId);

        // Assert
        var noContentResult = Assert.IsType<NoContentResult>(result);
        Assert.Equal(204, noContentResult.StatusCode);
    }

    [Fact]
    public async Task DeleteDocument_UnhandledException_ReturnsInternalServerError()
    {
        // Arrange
        var documentId = 1;
        var userId = 1;
        _userServiceMock.Setup(u => u.GetUserIdFromClaimsAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(userId);
        _documentServiceMock.Setup(d => d.DeleteDocumentAsync(documentId, userId)).ThrowsAsync(new Exception("Unexpected error"));

        var controller = CreateControllerWithUser(userId);

        // Act
        var result = await controller.DeleteDocument(documentId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result); 
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("An unexpected error occurred.", objectResult.Value);
    }

    [Fact]
    public async Task UpdateDocument_ValidData_ReturnsNoContent()
    {
        // Arrange
        var documentId = 1;
        var updateDocumentDto = new UpdateDocumentDto
        {
            Title = "Updated Document",
            Content = "Updated content"
        };
        var userId = 1;
        _userServiceMock.Setup(u => u.GetUserIdAsync(It.IsAny<string>())).ReturnsAsync(userId);
        _documentServiceMock.Setup(d => d.UpdateDocumentAsync(documentId, updateDocumentDto, userId)).ReturnsAsync(true);

        var controller = CreateControllerWithUser(userId);

        // Act
        var result = await controller.UpdateDocument(documentId, updateDocumentDto);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    
    [Fact]
    public async Task UpdateDocument_ModelStateInvalid_ReturnsBadRequest()
    {
        // Arrange
        var documentId = 1;
        var updateDocumentDto = new UpdateDocumentDto();
        var controller = CreateControllerWithUser(1);
        controller.ModelState.AddModelError("Title", "Required");

        // Act
        var result = await controller.UpdateDocument(documentId, updateDocumentDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }



    [Fact]
    public async Task UpdateDocument_UserNotFound_ReturnsInternalServerError()
    {
        // Arrange
        var documentId = 1;
        var updateDocumentDto = new UpdateDocumentDto
        {
            Title = "Updated Document",
            Content = "Updated content"
        };
        var userId = -1; 
        _userServiceMock.Setup(u => u.GetUserIdAsync(It.IsAny<string>())).ReturnsAsync(userId);
        _documentServiceMock.Setup(d => d.UpdateDocumentAsync(documentId, updateDocumentDto, userId))
            .ThrowsAsync(new UnauthorizedAccessException("UserId not found"));

        var controller = CreateControllerWithUser(userId);

        // Act
        var result = await controller.UpdateDocument(documentId, updateDocumentDto);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode); 
        var value = Assert.IsType<string>(objectResult.Value);  
        Assert.Equal("UserId not found. User might not exist.", value);
    }
    
    
    [Fact]
    public async Task UpdateDocument_UnauthorizedAccess_ReturnsUnauthorized()
    {
        // Arrange
        var documentId = 1;
        var updateDocumentDto = new UpdateDocumentDto
        {
            Title = "Updated Document",
            Content = "Updated content"
        };
        var userId = 1;
        _userServiceMock.Setup(u => u.GetUserIdAsync(It.IsAny<string>())).ReturnsAsync(userId);
        _documentServiceMock.Setup(d => d.UpdateDocumentAsync(documentId, updateDocumentDto, userId))
            .ThrowsAsync(new UnauthorizedAccessException("Unauthorized access"));

        var controller = CreateControllerWithUser(userId);

        // Act
        var result = await controller.UpdateDocument(documentId, updateDocumentDto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);  // Ensure UnauthorizedObjectResult
        var value = JObject.FromObject(unauthorizedResult.Value);  // Use JObject to access anonymous properties
        var message = value["message"].ToString();
        Assert.Equal("Unauthorized access", message);  // Check if the message matches
    }
    
    [Fact]
    public async Task UpdateDocument_GeneralError_ReturnsInternalServerError()
    {
        // Arrange
        var documentId = 1;
        var updateDocumentDto = new UpdateDocumentDto
        {
            Title = "Updated Document",
            Content = "Updated content"
        };
        var userId = 1;
        _userServiceMock.Setup(u => u.GetUserIdAsync(It.IsAny<string>())).ReturnsAsync(userId);
        _documentServiceMock.Setup(d => d.UpdateDocumentAsync(documentId, updateDocumentDto, userId))
            .ThrowsAsync(new Exception("Unexpected error"));

        var controller = CreateControllerWithUser(userId);

        // Act
        var result = await controller.UpdateDocument(documentId, updateDocumentDto);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode); 
        Assert.Equal("Unexpected error occured.", objectResult.Value);
    }
    
}