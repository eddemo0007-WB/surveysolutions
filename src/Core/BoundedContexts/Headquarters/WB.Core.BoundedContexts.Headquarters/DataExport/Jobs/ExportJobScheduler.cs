﻿using System;
using Quartz;

namespace WB.Core.BoundedContexts.Headquarters.DataExport.Jobs
{
    public class ExportJobScheduler
    {
        private readonly IScheduler scheduler;

        private readonly ExportSettings settings;

        public ExportJobScheduler(IScheduler scheduler, ExportSettings settings)
        {
            this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void Configure()
        {
            IJobDetail job = JobBuilder.Create<ExportJob>()
                .WithIdentity("export job", "Export")
                .StoreDurably(true)
                .Build();

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("export trigger", "Export")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(this.settings.BackgroundExportIntervalInSeconds)
                    .RepeatForever())
                .Build();

            this.scheduler.ScheduleJob(job, trigger);

            this.scheduler.AddJob(job, true);
        }
    }
}