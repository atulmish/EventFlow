﻿// The MIT License (MIT)
// 
// Copyright (c) 2015-2016 Rasmus Mikkelsen
// Copyright (c) 2015-2016 eBay Software Foundation
// https://github.com/rasmus/EventFlow
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using System.Threading;
using System.Threading.Tasks;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Snapshots;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.TestHelpers.Suites
{
    public abstract class TestSuiteForSnapshotStore : IntegrationTest
    {
        [Test]
        public void GetSnapshotAsync_NoneExistingSnapshotReturnsNull()
        {
            // Act
            var committedSnapshot = SnapshotPersistence.GetSnapshotAsync(typeof (ThingyAggregate), ThingyId.New, CancellationToken.None).Result;

            // Assert
            committedSnapshot.Should().BeNull();
        }

        [Test]
        public void DeleteSnapshotAsync_GetSnapshotAsync_NoneExistingSnapshotDoesNotThrow()
        {
            // Act + Assert
            Assert.DoesNotThrow(() => SnapshotPersistence.DeleteSnapshotAsync(typeof(ThingyAggregate), ThingyId.New, CancellationToken.None).Wait());
        }

        [Test]
        public void PurgeSnapshotsAsync_NoneExistingSnapshotDoesNotThrow()
        {
            // Act + Assert
            Assert.DoesNotThrow(() => SnapshotPersistence.PurgeSnapshotsAsync(typeof(ThingyAggregate), CancellationToken.None).Wait());
        }

        [Test]
        public void PurgeSnapshotsAsync_EmptySnapshotStoreDoesNotThrow()
        {
            // Act + Assert
            Assert.DoesNotThrow(() => SnapshotPersistence.PurgeSnapshotsAsync(CancellationToken.None).Wait());
        }

        [Test]
        public async Task NoSnapshotsAreCreatedWhenCommittingFewEvents()
        {
            // Arrange
            var thingyId = ThingyId.New;
            await PublishPingCommandsAsync(thingyId, ThingyAggregate.SnapshotEveryVersion - 1).ConfigureAwait(false);

            // Act
            var thingySnapshot = await LoadSnapshotAsync(thingyId).ConfigureAwait(false);

            // Assert
            thingySnapshot.Should().BeNull();
        }

        [Test]
        public async Task SnapshotIsCreatedWhenCommittingManyEvents()
        {
            // Arrange
            var thingyId = ThingyId.New;
            const int pingsSent = ThingyAggregate.SnapshotEveryVersion + 1;
            await PublishPingCommandsAsync(thingyId, pingsSent).ConfigureAwait(false);

            // Act
            var thingySnapshot = await LoadSnapshotAsync(thingyId).ConfigureAwait(false);

            // Assert
            thingySnapshot.Should().NotBeNull();
            thingySnapshot.PingsReceived.Count.Should().Be(pingsSent);
        }

        [Test]
        public async Task LoadedAggregateHasCorrectVersionsWhenSnapshotIsApplied()
        {
            // Arrange
            var thingyId = ThingyId.New;
            const int pingsSent = ThingyAggregate.SnapshotEveryVersion + 1;
            await PublishPingCommandsAsync(thingyId, pingsSent).ConfigureAwait(false);

            // Act
            var thingyAggregate = await AggregateStore.LoadAsync<ThingyAggregate, ThingyId>(
                thingyId,
                CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            thingyAggregate.Version.Should().Be(pingsSent);
            thingyAggregate.SnapshotVersion.GetValueOrDefault().Should().Be(ThingyAggregate.SnapshotEveryVersion);
        }

        protected async Task<ThingySnapshot> LoadSnapshotAsync(ThingyId thingyId)
        {
            var snapshotContainer = await SnapshotStore.LoadSnapshotAsync<ThingyAggregate, ThingyId, ThingySnapshot>(
                thingyId,
                CancellationToken.None)
                .ConfigureAwait(false);
            return (ThingySnapshot)snapshotContainer?.Snapshot;
        }
    }
}