using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Arise.FileSyncer.Core;

namespace Arise.FileSyncer.Common
{
    public readonly struct ProgressStatus : ISyncProgress
    {
        /// <summary>
        /// The ID of the connection
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Indeterminate
        /// </summary>
        public bool Indeterminate { get; }

        /// <summary>
        /// The current state in byte
        /// </summary>
        public long Current { get; }

        /// <summary>
        /// The target of the progress in bytes
        /// </summary>
        public long Maximum { get; }

        /// <summary>
        /// Speed in byte/second
        /// </summary>
        public double Speed { get; }

        public ProgressStatus(Guid id, ISyncProgress progress, double speed)
        {
            Id = id;
            Indeterminate = progress.Indeterminate;
            Current = progress.Current;
            Maximum = progress.Maximum;
            Speed = speed;
        }

        public ProgressStatus(Guid id, bool indeterminate, long current, long maximum, double speed)
        {
            Id = id;
            Indeterminate = indeterminate;
            Current = current;
            Maximum = maximum;
            Speed = speed;
        }
    }

    public class ProgressUpdateEventArgs : EventArgs
    {
        public ICollection<ProgressStatus> Progresses;

        public ProgressUpdateEventArgs()
        {
            Progresses = Array.Empty<ProgressStatus>();
        }

        public ProgressUpdateEventArgs(ICollection<ProgressStatus> progresses)
        {
            Progresses = progresses;
        }
    }

    public class ProgressTracker : IDisposable
    {
        public event EventHandler<ProgressUpdateEventArgs>? ProgressUpdate;

        private readonly ConcurrentDictionary<Guid, ProgressArchive> progressArchive;
        private readonly SyncerPeer peer;
        private readonly Timer progressTimer;
        private readonly int avarageNum;
        private readonly double speedInterval;

        public ProgressTracker(SyncerPeer peer, int updateInterval = 1000, int avarageNum = 10)
        {
            Debug.Assert(avarageNum > 1, "Avarage number must be bigger than 1.");

            this.peer = peer ?? throw new ArgumentNullException(nameof(peer));
            this.avarageNum = avarageNum;
            speedInterval = updateInterval / 1000.0;

            progressArchive = new ConcurrentDictionary<Guid, ProgressArchive>(1, 0);

            peer.Connections.ConnectionAdded += Peer_ConnectionAdded;
            peer.Connections.ConnectionRemoved += Peer_ConnectionRemoved;

            progressTimer = new Timer(ProgressTimerCallback, null, updateInterval, updateInterval);
        }

        private void UpdateArchives()
        {
            foreach (var archive in progressArchive)
            {
                if (peer.Connections.TryGetConnection(archive.Key, out var connection))
                {
                    archive.Value.AddNext(connection.Progress);
                }
            }
        }

        private void ProgressTimerCallback(object? state)
        {
            UpdateArchives();

            var progresses = new List<ProgressStatus>(progressArchive.Count);
            foreach (var archiveKV in progressArchive)
            {
                if (archiveKV.Value.CalcStatus(archiveKV.Key, speedInterval, out var status))
                {
                    progresses.Add(status);
                }
            }

            OnProgressUpdate(progresses);
        }

        private void Peer_ConnectionAdded(object? sender, ConnectionEventArgs e)
        {
            progressArchive.TryAdd(e.Id, new ProgressArchive(avarageNum));
        }

        private void Peer_ConnectionRemoved(object? sender, ConnectionEventArgs e)
        {
            progressArchive.TryRemove(e.Id, out var _);
        }

        private void OnProgressUpdate(ICollection<ProgressStatus> progresses)
        {
            ProgressUpdate?.Invoke(this, new ProgressUpdateEventArgs(progresses));
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    progressTimer.Dispose();

                    try
                    {
                        peer.Connections.ConnectionAdded -= Peer_ConnectionAdded;
                        peer.Connections.ConnectionRemoved -= Peer_ConnectionRemoved;
                    }
                    catch { Log.Warning("Exception in ProgressTracker dispose"); }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        private class ProgressArchive
        {
            private int lastIndex;
            public int LastIndex
            {
                get => lastIndex;
                set => lastIndex = value % Archive.Length;
            }

            public Progress[] Archive { get; }

            public ProgressArchive(int length)
            {
                lastIndex = length - 1;
                Archive = new Progress[length];
            }

            public void AddNext(ISyncProgress progress)
            {
                LastIndex++;
                Archive[LastIndex] = new Progress(progress);
            }

            public bool CalcStatus(Guid id, double speedInterval, out ProgressStatus status)
            {
                Progress mostRecent = Archive[LastIndex]; // TODO array without elements set isnt null??
                if (mostRecent == null)
                {
                    status = new ProgressStatus();
                    return false;
                }

                Progress? lastProgress = null;
                long speedOverall = 0;
                long speedCount = 0;

                for (int i = 1; i < Archive.Length + 1; i++)
                {
                    var progress = Archive[(i + LastIndex) % Archive.Length];

                    if (progress != null && !progress.Indeterminate)
                    {
                        if (lastProgress != null)
                        {
                            speedOverall += progress.Current - lastProgress.Current;
                            speedCount++;
                        }

                        lastProgress = progress;
                    }
                }

                if (speedCount == 0)
                {
                    status = new ProgressStatus();
                    return false;
                }

                double avarage = (double)speedOverall / speedCount;
                status = new ProgressStatus(id, mostRecent, avarage / speedInterval);
                return true;
            }
        }

        private class Progress : ISyncProgress
        {
            public bool Indeterminate { get; set; }
            public long Current { get; set; }
            public long Maximum { get; set; }

            public Progress()
            {
                Indeterminate = true;
                Current = 0;
                Maximum = 0;
            }

            public Progress(ISyncProgress progress)
            {
                Indeterminate = progress.Indeterminate;
                Current = progress.Current;
                Maximum = progress.Maximum;
            }
        }
    }
}
