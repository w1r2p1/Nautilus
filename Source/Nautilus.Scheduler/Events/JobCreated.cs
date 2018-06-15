﻿//--------------------------------------------------------------------------------------------------
// <copyright file="CreateJobFail.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Scheduler.Events
{
    using System.Runtime.CompilerServices;
    using Quartz;
    using Nautilus.Core.Validation;

    /// <summary>
    ///     Job created event
    /// </summary>
    public class JobCreated : JobEvent
    {
        public JobCreated(
            JobKey jobKey,
            TriggerKey triggerKey,
            object job) : base(jobKey, triggerKey)
        {
            Debug.NotNull(job, nameof(job));
            Debug.NotNull(triggerKey, nameof(triggerKey));
            Debug.NotNull(job, nameof(job));

            this.Job = job;
        }

        public object Job { get; }

        public override string ToString()
        {
            return string.Format("{0} with trigger {1} has been created.", JobKey, TriggerKey);
        }
    }

    /// <summary>
    ///     Job removed event
    /// </summary>
    public class JobRemoved : JobEvent
    {
        public JobRemoved(
            JobKey jobKey,
            TriggerKey triggerKey,
            object job) : base(jobKey, triggerKey)
        {
            Debug.NotNull(job, nameof(job));
            Debug.NotNull(triggerKey, nameof(triggerKey));
            Debug.NotNull(job, nameof(job));

            this.Job = job;
        }

        public object Job { get; }

        public override string ToString()
        {
            return string.Format("{0} with trigger {1} has been removed.", JobKey, TriggerKey);
        }
    }
}
