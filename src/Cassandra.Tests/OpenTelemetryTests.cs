﻿//
//      Copyright (C) DataStax Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//

using System.Diagnostics;
using System.Linq;
using Cassandra.OpenTelemetry;
using NUnit.Framework;

namespace Cassandra.Tests
{
    [TestFixture]
    public class OpenTelemetryTests
    {
        private static readonly string otelActivityKey = "otel_activity";
        private static readonly string dbNamespaceTag = "db.namespace";
        private static readonly string dbQueryTextTag = "db.query.text";

        [Test]
        public void OpenTelemetryRequestTrackerOnStartAsync_StatementIsNull_DbQueryTextAndDbNamespaceTagsAreNotIncluded()
        {
            using (var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == CassandraActivitySourceHelper.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded
            })
            {
                ActivitySource.AddActivityListener(listener);
                
                var cassandraInstrumentationOptions = new CassandraInstrumentationOptions { IncludeDatabaseStatement = true };
                var requestTracker = new OpenTelemetryRequestTracker(cassandraInstrumentationOptions);
                IStatement statement = null;
                var requestTrackingInfo = new RequestTrackingInfo(statement);

                requestTracker.OnStartAsync(requestTrackingInfo);

                requestTrackingInfo.Items.TryGetValue(otelActivityKey, out object context);

                var activity = context as Activity;

                Assert.NotNull(activity);
                Assert.Null(activity.Tags.FirstOrDefault(x => x.Key == dbNamespaceTag).Value);
                Assert.Null(activity.Tags.FirstOrDefault(x => x.Key == dbQueryTextTag).Value);
            }
        }

        [Test]
        public void OpenTelemetryRequestTrackerOnStartAsync_ListenerNotSampling_ActivityIsNull()
        {
            using (var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == CassandraActivitySourceHelper.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.None
            })
            {
                ActivitySource.AddActivityListener(listener);

                var cassandraInstrumentationOptions = new CassandraInstrumentationOptions { IncludeDatabaseStatement = true };
                var requestTracker = new OpenTelemetryRequestTracker(cassandraInstrumentationOptions);
                IStatement statement = null;
                var requestTrackingInfo = new RequestTrackingInfo(statement);

                requestTracker.OnStartAsync(requestTrackingInfo);

                requestTrackingInfo.Items.TryGetValue(otelActivityKey, out object context);

                var activity = context as Activity;

                Assert.Null(activity);
            }
        }

        [Test]
        public void OpenTelemetryRequestTrackerOnNodeStartAsync_StatementIsNull_DbQueryTextAndDbNamespaceTagsAreNotIncluded()
        {
            using (var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == CassandraActivitySourceHelper.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded
            })
            {
                ActivitySource.AddActivityListener(listener);

                var cassandraInstrumentationOptions = new CassandraInstrumentationOptions { IncludeDatabaseStatement = true };
                var requestTracker = new OpenTelemetryRequestTracker(cassandraInstrumentationOptions);
                
                IStatement statement = null;
                var requestTrackingInfo = new RequestTrackingInfo(statement);

                var host = new Host(new System.Net.IPEndPoint(1, 9042), new ConstantReconnectionPolicy(1));
                var hostTrackingInfo = new HostTrackingInfo() { Host = host };

                requestTracker.OnStartAsync(requestTrackingInfo);
                requestTracker.OnNodeStartAsync(requestTrackingInfo, hostTrackingInfo);

                requestTrackingInfo.Items.TryGetValue($"{otelActivityKey}.{host.HostId}", out object context);

                var activity = context as Activity;

                Assert.NotNull(activity);
                Assert.Null(activity.Tags.FirstOrDefault(x => x.Key == dbNamespaceTag).Value);
                Assert.Null(activity.Tags.FirstOrDefault(x => x.Key == dbQueryTextTag).Value);
            }
        }

        [Test]
        public void OpenTelemetryRequestTrackerOnNodeStartAsync_ListenerNotSampling_ActivityIsNull()
        {
            using (var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == CassandraActivitySourceHelper.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.None
            })
            {
                ActivitySource.AddActivityListener(listener);

                var cassandraInstrumentationOptions = new CassandraInstrumentationOptions { IncludeDatabaseStatement = true };
                var requestTracker = new OpenTelemetryRequestTracker(cassandraInstrumentationOptions);
                
                IStatement statement = null;
                var requestTrackingInfo = new RequestTrackingInfo(statement);
                
                var host = new Host(new System.Net.IPEndPoint(1, 9042), new ConstantReconnectionPolicy(1));
                var hostTrackingInfo = new HostTrackingInfo() { Host = host };

                requestTracker.OnStartAsync(requestTrackingInfo);
                requestTracker.OnNodeStartAsync(requestTrackingInfo, hostTrackingInfo);

                requestTrackingInfo.Items.TryGetValue($"{otelActivityKey}.{host.HostId}", out object context);

                var activity = context as Activity;

                Assert.Null(activity);
            }
        }
    }
}