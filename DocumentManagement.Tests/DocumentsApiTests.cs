using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DocumentManagement.Api.Models;

namespace DocumentManagement.Tests;

public class DocumentsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DocumentsApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // Helper: builds a fake file upload
    private static MultipartFormDataContent CreateFile(string fileName, int sizeInBytes)
    {
        var fileContent = new ByteArrayContent(new byte[sizeInBytes]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

        var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", fileName);
        return form;
    }

    [Fact]
    public async Task Upload_ValidFile_Returns201()
    {
        var form = CreateFile("test.txt", 100);

        var response = await _client.PostAsync("/api/documents", form);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ExistingDocument_Returns200()
    {
        // Upload a file first
        var uploadResponse = await _client.PostAsync("/api/documents", CreateFile("test.txt", 100));
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<Document>();

        // Get it by its ID
        var response = await _client.GetAsync($"/api/documents/{uploaded!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_NonexistentDocument_Returns404()
    {
        var response = await _client.GetAsync($"/api/documents/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Upload_FileTooLarge_Returns400()
    {
        var form = CreateFile("big.txt", 11 * 1024 * 1024); // 11 MB

        var response = await _client.PostAsync("/api/documents", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingDocument_Returns204AndIsGone()
    {
        // Upload a file first
        var uploadResponse = await _client.PostAsync("/api/documents", CreateFile("test.txt", 100));
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<Document>();

        // Delete it
        var deleteResponse = await _client.DeleteAsync($"/api/documents/{uploaded!.Id}");

        // Check it's really gone
        var getResponse = await _client.GetAsync($"/api/documents/{uploaded.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}