using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.Imports.Commands;
using Application.Imports.Queries;
using DataTransferObjects.Common;
using DataTransferObjects.Import;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using SharedKernel.Enums;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Imports;

internal sealed class ImportEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("imports")
            .RequireAuthorization()
            .WithTags("Imports");

        // Upload and validate import file
        group.MapPost("upload", UploadImportFile)
            .DisableAntiforgery()
            .WithName("UploadImportFile")
            .WithSummary("Upload a CSV file for customer import");

        // Confirm and process import
        group.MapPost("{importJobId:guid}/confirm", ConfirmImport)
            .WithName("ConfirmImport")
            .WithSummary("Confirm and process the validated import");

        // Get import job status
        group.MapGet("{importJobId:guid}/status", GetImportStatus)
            .WithName("GetImportStatus")
            .WithSummary("Get the status of an import job");

        // Get import job errors
        group.MapGet("{importJobId:guid}/errors", GetImportErrors)
            .WithName("GetImportErrors")
            .WithSummary("Get validation/processing errors for an import job");

        // Cancel import job
        group.MapPost("{importJobId:guid}/cancel", CancelImport)
            .WithName("CancelImport")
            .WithSummary("Cancel a pending import job");

        // Get user's import history
        group.MapGet("", GetImportHistory)
            .WithName("GetImportHistory")
            .WithSummary("Get user's import job history");
    }

    private static async Task<IResult> UploadImportFile(
        IUserContext userContext,
        ICommandHandler<CreateImportJobCommand, Guid> handler,
        IFormFile file,
        CancellationToken cancellationToken,
        string? importSource = null,
        bool skipDuplicates = false)
    {
        // Validate file
        if (file == null || file.Length == 0)
        {
            return Results.BadRequest(new { error = "No file was uploaded" });
        }

        if (file.Length > 50 * 1024 * 1024) // 50MB limit
        {
            return Results.BadRequest(new { error = "File size exceeds 50MB limit" });
        }

        var allowedExtensions = new[] { ".csv", ".xlsx", ".xls" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(fileExtension))
        {
            return Results.BadRequest(new
            {
                error = $"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}"
            });
        }

        var command = new CreateImportJobCommand(
            userContext.UserId,
            file,
            ImportJobType.Customers,
            importSource ?? "manual",
            skipDuplicates
        );

        Result<Guid> result = await handler.Handle(command, cancellationToken);

        return result.Match(
            importJobId => Results.Ok(new
            {
                ImportJobId = importJobId,
                Message = "File uploaded successfully and validation started",
                Status = ImportJobStatus.Validating.ToString()
            }),
            CustomResults.Problem
        );
    }

    private static async Task<IResult> ConfirmImport(
        Guid importJobId,
        ICommandHandler<ConfirmImportJobCommand, bool> handler,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmImportJobCommand(importJobId);
        Result<bool> result = await handler.Handle(command, cancellationToken);

        return result.Match(
            success => Results.Ok(new
            {
                ImportJobId = importJobId,
                Message = "Import confirmed and processing started",
                Status = ImportJobStatus.Processing.ToString()
            }),
            CustomResults.Problem
        );
    }

    private static async Task<IResult> GetImportStatus(
        Guid importJobId,
        IQueryHandler<GetImportJobQuery, ImportJobResponse> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetImportJobQuery(importJobId);
        Result<ImportJobResponse> result = await handler.Handle(query, cancellationToken);

        return result.Match(Results.Ok, CustomResults.Problem);
    }

    private static async Task<IResult> GetImportErrors(
        Guid importJobId,
        IQueryHandler<GetImportJobErrorsQuery, List<ImportErrorResponse>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetImportJobErrorsQuery(importJobId);
        Result<List<ImportErrorResponse>> result = await handler.Handle(query, cancellationToken);

        return result.Match(Results.Ok, CustomResults.Problem);
    }

    private static async Task<IResult> CancelImport(
        Guid importJobId,
        ICommandHandler<CancelImportJobCommand, bool> handler,
        CancellationToken cancellationToken)
    {
        var command = new CancelImportJobCommand(importJobId);
        Result<bool> result = await handler.Handle(command, cancellationToken);

        return result.Match(
            success => Results.Ok(new
            {
                ImportJobId = importJobId,
                Message = "Import job cancelled successfully",
                Status = ImportJobStatus.Cancelled.ToString()
            }),
            CustomResults.Problem
        );
    }

    private static async Task<IResult> GetImportHistory(
        IUserContext userContext,
        IQueryHandler<GetUserImportHistoryQuery, PagedResult<ImportJobSummaryResponse>> handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetUserImportHistoryQuery(page, pageSize);
        Result<PagedResult<ImportJobSummaryResponse>> result = await handler.Handle(query, cancellationToken);

        return result.Match(Results.Ok, CustomResults.Problem);
    }
}
