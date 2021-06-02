﻿namespace WebApi.Services
{
    using System;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using WebApi.Entities;
    using WebApi.Models;

    internal class RefreshServerInfoService : BaseRefreshService
    {
        private readonly PersistentService persistentService;
        private readonly ServerService server;
        private readonly AppSettings appSettings;

        public RefreshServerInfoService(
            ILogger<RefreshServerInfoService> logger,
            IServiceProvider serviceProvider,
            PersistentService persistentService,
            ServerService server,
            IOptions<AppSettings> appSettings)
            : base(logger, serviceProvider)
        {
            this.persistentService = persistentService;
            this.server = server;
            this.appSettings = appSettings.Value;
        }

        protected override string ServiceName => nameof(RefreshServerInfoService);
        protected override int DefaultIntervalSeconds => 120;
        protected override int DelayStartSeconds => 3;

        protected override int GetIntervalSeconds() => 5;

        protected override async Task DoWorkAsync()
        {
            var si = JsonSerializer.Serialize(await this.server.GetServersInfo());
            await this.persistentService.LogEntityAsync(new MachineStateEntity { MachinesJson = si });
        }
    }
}