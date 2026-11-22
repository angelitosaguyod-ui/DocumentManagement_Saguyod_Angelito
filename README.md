# Document Management REST API

A simple REST API for uploading, listing, viewing, downloading, and deleting documents.
Built with ASP.NET Core 8, Entity Framework Core, and SQL Server.

## Prerequisites

- .NET 8 SDK
- SQL Server (full, Express, or LocalDB)
- Visual Studio 2022 (optional)

## Database Setup

1. Open `DocumentManagement.Api/appsettings.json`.

2. Update the `Server` value in `DefaultConnection` to match your SQL Server:
   - Full SQL Server: `Server=localhost`
   - SQL Server Express: `Server=localhost\\SQLEXPRESS`
   - LocalDB: `Server=(localdb)\\MSSQLLocalDB`

3. Create the database (choose one option):

   **Option A: Using Visual Studio**
   1. Open the solution in Visual Studio.
   2. Go to **Tools > NuGet Package Manager > Package Manager Console**.
   3. Run:
      ```
      Update-Database
      ```

   **Option B: Without Visual Studio (command line)**
   1. Open a terminal (Command Prompt or PowerShell) in the solution folder (the folder containing the `.sln` file).
   2. Install the EF Core tool:
      ```
      dotnet tool install --global dotnet-ef --version 8.*
      ```
   3. Create the database:
      ```
      dotnet ef database update --project DocumentManagement.Api
      ```

The connection string uses Windows authentication, so no password is stored.

## Running the Application

- Visual Studio: press **F5**. Swagger opens in the browser.
- Command line: `dotnet run --project DocumentManagement.Api`, then open `/swagger`.

Uploaded files are saved in the `Uploads` folder (set in `appsettings.json` under `FileStorage:Path`).

## Running the Tests

- Visual Studio: **Test → Test Explorer → Run All**
- Command line: `dotnet test`

Tests use an in-memory database and a temporary folder, so they don't need SQL Server.

## Endpoints

| Method | URL | Description | Responses |
|---|---|---|---|
| POST | `/api/documents` | Upload a file (multipart/form-data, field `file`) | 201, 400 |
| GET | `/api/documents` | List all documents (metadata only) | 200 |
| GET | `/api/documents/{id}` | Get one document's metadata | 200, 404 |
| GET | `/api/documents/{id}/download` | Download the file | 200, 404 |
| DELETE | `/api/documents/{id}` | Delete the document and its file | 204, 404 |

## Design Decisions

- **Architecture:** a single controller (`DocumentsController`) holds all endpoint logic. This keeps the project simple and appropriate for its small scope.
- **Data access:** Entity Framework Core with SQL Server, using code-first migrations.
- **File storage:** files are saved on disk; only metadata is stored in the database. Each file is saved as `{GUID}{extension}`, so user-supplied filenames are never used as paths. This prevents filename collisions and path manipulation (e.g. `../`).
- **Upload:** if saving metadata to the database fails, the saved file is deleted to avoid orphan files.
- **Delete:** the database record is deleted first, then the file. If the file can't be deleted (e.g. it's locked), the request still succeeds and a warning is logged, because a leftover file is harmless, while a record pointing to a missing file would break downloads.

## Assumptions and Validation

- Maximum file size: 10 MB.
- Allowed file types: `.pdf`,`.doc`, `.docx`, `.txt`, `.png`, `.jpg`, `.jpeg`.
- File type is checked by extension only, not by file contents.
- Empty or missing files are rejected with 400.
- No authentication or authorization (not required).

## Known Limitations

- Allowed file types and size limit are set in code, not in configuration.
- Files over ~30 MB are rejected by ASP.NET Core's built-in limit with 413, before reaching the API's own validation.
- Logs are shown in the console only and are not saved to a file.
- Leftover files from a failed delete are not cleaned up automatically.

## AI-Assisted Development

- Tools used:
    - Claude.
- How I used it:
    - as a step-by-step guide to learn ASP.NET Core while building the project. It explained each concept and suggested code, which I reviewed, ran, and tested at every step.
- What I reviewed or modified:
    - simplified the DbContext, switched from LocalDB to my local SQL Server, compared both delete orders before choosing one, added logging instead of an empty catch, tested every endpoint manually in Swagger
- Least confident about:
    - the test setup in `CustomWebApplicationFactory`, especially how it replaces the SQL Server database with an in-memory database and uses a temporary upload folder. I understand what it does, but I'm still learning how it works internally.