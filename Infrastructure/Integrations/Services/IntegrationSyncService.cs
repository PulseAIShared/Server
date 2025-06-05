using Application.Abstractions.Data;
using DataTransferObjects.Integrations;
using Domain.Integration;
using Hangfire;
using Infrastructure.Integrations.Models;
using Infrastructure.Integrations.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Enums;


namespace Infrastructure.Integrations.Services
{
    public class IntegrationSyncService : IIntegrationSyncService
    {
        private readonly IApplicationDbContext _context;
        private readonly IIntegrationServiceFactory _integrationFactory;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ILogger<IntegrationSyncService> _logger;

        public IntegrationSyncService(
            IApplicationDbContext context,
            IIntegrationServiceFactory integrationFactory,
            IBackgroundJobClient backgroundJobClient,
            ILogger<IntegrationSyncService> logger)
        {
            _context = context;
            _integrationFactory = integrationFactory;
            _backgroundJobClient = backgroundJobClient;
            _logger = logger;
        }

        public async Task SyncAllIntegrationsAsync()
        {
            var integrations = await _context.Integrations
                .Where(i => i.Status == IntegrationStatus.Connected)
                .ToListAsync();

            _logger.LogInformation("Starting sync for {Count} connected integrations", integrations.Count);

            foreach (var integration in integrations)
            {
                try
                {
                    await SyncIntegrationAsync(integration.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync integration {IntegrationId} of type {Type}",
                        integration.Id, integration.Type);
                }
            }
        }

        public async Task SyncIntegrationAsync(Guid integrationId)
        {
            var integration = await _context.Integrations.FindAsync(integrationId);
            if (integration == null)
            {
                _logger.LogWarning("Integration {IntegrationId} not found", integrationId);
                return;
            }

            if (integration.Status != IntegrationStatus.Connected)
            {
                _logger.LogWarning("Integration {IntegrationId} is not connected (status: {Status})",
                    integrationId, integration.Status);
                return;
            }

            try
            {
                var integrationService = _integrationFactory.GetService(integration.Type);

                // Test connection first
                var isConnected = await integrationService.TestConnectionAsync(integration);
                if (!isConnected)
                {
                    integration.Status = IntegrationStatus.Error;
                    integration.LastSyncError = "Connection test failed before sync";
                    await _context.SaveChangesAsync();
                    return;
                }

                // Perform sync
                var syncOptions = new SyncOptions
                {
                    IncrementalSync = true,
                    BatchSize = 100
                };

                var result = await integrationService.SyncCustomersAsync(integration, syncOptions);

                _logger.LogInformation("Sync completed for integration {IntegrationId}. " +
                    "Processed: {Processed}, New: {New}, Updated: {Updated}, Errors: {Errors}",
                    integrationId, result.ProcessedRecords, result.NewRecords, result.UpdatedRecords, result.ErrorRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync integration {IntegrationId}", integrationId);

                integration.Status = IntegrationStatus.Error;
                integration.LastSyncError = ex.Message;
                await _context.SaveChangesAsync();
            }
        }

        public async Task ScheduleAutomaticSyncAsync(Guid integrationId)
        {
            var integration = await _context.Integrations.FindAsync(integrationId);
            if (integration == null) return;

            var syncIntervalHours = GetSyncInterval(integration);

            // Schedule recurring job
            RecurringJob.AddOrUpdate(
                $"sync-integration-{integrationId}",
                () => SyncIntegrationAsync(integrationId),
                $"0 */{syncIntervalHours} * * *"); // Every X hours

            _logger.LogInformation("Scheduled automatic sync for integration {IntegrationId} every {Hours} hours",
                integrationId, syncIntervalHours);
        }

        public async Task DisableAutomaticSyncAsync(Guid integrationId)
        {
            RecurringJob.RemoveIfExists($"sync-integration-{integrationId}");

            _logger.LogInformation("Disabled automatic sync for integration {IntegrationId}", integrationId);
        }

        private int GetSyncInterval(Integration integration)
        {
            if (integration.Configuration?.TryGetValue("auto_sync_interval", out var intervalStr) == true &&
                int.TryParse(intervalStr, out var interval))
            {
                return interval;
            }

            return 24; // Default to 24 hours
        }
    }
}