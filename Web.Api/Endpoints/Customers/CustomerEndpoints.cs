using Application.Abstractions.Messaging;
using Application.Customers.Commands;
using Application.Customers.Queries;
using DataTransferObjects.Common;
using DataTransferObjects.Customers;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using SharedKernel.Enums;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Customers;

internal sealed class CustomerEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("customers")
            .RequireAuthorization()
            .WithTags(Tags.Customers);

        // Get all customers with filtering, search, and pagination
        group.MapGet("", GetCustomers)
            .WithName("GetCustomers")
            .WithSummary("Get customers with filtering, search, and pagination");

        // Get customer by ID
        group.MapGet("{customerId:guid}", GetCustomerById)
            .WithName("GetCustomerById")
            .WithSummary("Get a specific customer by ID");

        // Get customers at risk
        group.MapGet("at-risk", GetCustomersAtRisk)
            .WithName("GetCustomersAtRisk")
            .WithSummary("Get customers with high churn risk");

        // Get customer activities
        group.MapGet("{customerId:guid}/activities", GetCustomerActivities)
            .WithName("GetCustomerActivities")
            .WithSummary("Get activities for a specific customer");

        group.MapPost("delete", DeleteCustomers)
             .WithName("DeleteCustomers")
            .WithSummary("Delete multiple customers and all their data");
    }

    private static async Task<IResult> GetCustomers(
        IQueryHandler<GetCustomersQuery, PagedResult<CustomerResponse>> handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] SubscriptionStatus? subscriptionStatus = null,
        [FromQuery] SubscriptionPlan? plan = null,
        [FromQuery] PaymentStatus? paymentStatus = null,
        [FromQuery] ChurnRiskLevel? churnRiskLevel = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false)
    {
        var query = new GetCustomersQuery(
            page,
            pageSize,
            search,
            subscriptionStatus,
            plan,
            paymentStatus,
            churnRiskLevel,
            sortBy,
            sortDescending
        );

        Result<PagedResult<CustomerResponse>> result = await handler.Handle(query, cancellationToken);

        return result.Match(Results.Ok, CustomResults.Problem);
    }

    private static async Task<IResult> GetCustomerById(
        Guid customerId,
        IQueryHandler<GetCustomerByIdQuery, CustomerDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetCustomerByIdQuery(customerId);
        Result<CustomerDetailResponse> result = await handler.Handle(query, cancellationToken);

        return result.Match(Results.Ok, CustomResults.Problem);
    }

    private static async Task<IResult> GetCustomersAtRisk(
        IQueryHandler<GetCustomersAtRiskQuery, PagedResult<CustomerResponse>> handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] ChurnRiskLevel minRiskLevel = ChurnRiskLevel.Medium)
    {
        var query = new GetCustomersAtRiskQuery(page, pageSize, minRiskLevel);
        Result<PagedResult<CustomerResponse>> result = await handler.Handle(query, cancellationToken);

        return result.Match(Results.Ok, CustomResults.Problem);
    }

    private static async Task<IResult> GetCustomerActivities(
        Guid customerId,
        IQueryHandler<GetCustomerActivitiesQuery, PagedResult<CustomerActivityResponse>> handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetCustomerActivitiesQuery(customerId, page, pageSize);
        Result<PagedResult<CustomerActivityResponse>> result = await handler.Handle(query, cancellationToken);

        return result.Match(Results.Ok, CustomResults.Problem);
    }

    public sealed record DeleteCustomersRequest(List<Guid> CustomerIds);
    private static async Task<IResult> DeleteCustomers(
    [FromBody] DeleteCustomersRequest request,
    ICommandHandler<DeleteCustomersCommand, DeleteCustomersResponse> handler,
    CancellationToken cancellationToken)
    {
        var command = new DeleteCustomersCommand(request.CustomerIds ?? new List<Guid>());
        Result<DeleteCustomersResponse> result = await handler.Handle(command, cancellationToken);

        return result.Match(
            response => Results.Ok(new
            {
                Message = $"Deletion completed. {response.SuccessfullyDeleted} of {response.TotalRequested} customers deleted successfully.",
                TotalRequested = response.TotalRequested,
                SuccessfullyDeleted = response.SuccessfullyDeleted,
                Failed = response.Failed,
                Errors = response.Errors.Take(10),
                HasMoreErrors = response.Errors.Count > 10
            }),
            CustomResults.Problem);
    }
}