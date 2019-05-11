using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Arise.FileSyncer.Core;
using System.Timers;

namespace Arise.FileSyncer.Common
{
    public class ProgressUpdate
    {
        public ISyncProgress Progress { get; }

        /// <summary>
        /// Speed in byte/second
        /// </summary>
        public double Speed { get; }

        public ProgressUpdate(ISyncProgress progress, double speed)
        {
            Progress = progress;
            Speed = speed;
        }
    }

    public class ProgressUpdateEventArgs : EventArgs
    {
        public Dictionary<Guid, ProgressUpdate> Progresses;

        public ProgressUpdateEventArgs()
        {
            Progresses = new Dictionary<Guid, ProgressUpdate>(0);
        }

        public ProgressUpdateEventArgs(Dictionary<Guid, ProgressUpdate> progressUpdates)
        {
            Progresses = progressUpdates;
        }
    }

    public class ProgressTracker : IDisposable
    {
        public event EventHandler<ProgressUpdateEventArgs> ProgressUpdate;

        private readonly ConcurrentDictionary<Guid, ProgressArchive> progressArchive;
        private readonly SyncerPeer peer;
        private readonly Timer progressTimer;
        private readonly int avarageNum;
        private readonly double speedInterval;

        public ProgressTracker(SyncerPeer peer, int updateInterval = 1000, int avarageNum = 10)
        {
            this.peer = peer;
            this.avarageNum = avarageNum;
            speedInterval = updateInterval / 1000.0;

            progressArchive = new ConcurrentDictionary<Guid, ProgressArchive>(1, 0);

            peer.ConnectionAdded += Peer_ConnectionAdded;
            peer.ConnectionRemoved += Peer_ConnectionRemoved;

            progressTimer = new Timer();
            progressTimer.Elapsed += ProgressTimer_Elapsed;
            progressTimer.Interval = updateInterval;
            progressTimer.AutoReset = true;
            progressTimer.Start();
        }

        private void UpdateArchive()
        {
            foreach (var archive in progressArchive)
            {
                if (peer.TryGetConnection(archive.Key, out var connection))
                {
                    archive.Value.AddNext(connection.Progress);
                }
            }
        }

        private void ProgressTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            UpdateArchive();

            var progresses = new Dictionary<Guid, ProgressUpdate>(progressArchive.Count);
            foreach (var archive in progressArchive)
            {
                var update = CalcUpdate(archive.Value);
                if (update != null)
                {
                    progresses.Add(archive.Key, update);
                }
            }

            OnProgressUpdate(progresses);
        }

        private ProgressUpdate CalcUpdate(ProgressArchive pa)
        {
            Progress mostRecent = pa.Archive[(pa.Index + pa.Archive.Length - 1) % pa.Archive.Length];
            Progress lastProgress = null;
            long speedOverall = 0;
            long speedNum = 0;

            for (int i = 0; i < pa.Archive.Length; i++)
            {
                int index = (i + pa.Index) % pa.Archive.Length;
                var progress = pa.Archive[index];

                if (progress != null && !progress.Indeterminate && progress.Maximum > 0)
                {
                    if (lastProgress != null)
                    {
                        speedOverall += progress.Current - lastProgress.Current;
                        speedNum++;
                    }

                    lastProgress = progress;
                }
            }

            if (speedNum == 0 || mostRecent == null) return null;

            double avarage = (double)speedOverall / speedNum;
            return new ProgressUpdate(mostRecent, avarage / speedInterval);
        }

        private void Peer_ConnectionAdded(object sender, ConnectionAddedEventArgs e)
        {
            progressArchive.TryAdd(e.Id, new ProgressArchive(avarageNum));
        }

        private void Peer_ConnectionRemoved(object sender, ConnectionRemovedEventArgs e)
        {
            progressArchive.TryRemove(e.Id, out var _);
        }

        private void OnProgressUpdate(Dictionary<Guid, ProgressUpdate> progresses)
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
                        peer.ConnectionAdded -= Peer_ConnectionAdded;
                        peer.ConnectionRemoved -= Peer_ConnectionRemoved;
                    }
                    catch { }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        private class ProgressArchive
        {
            public int Index {
                get => index;
                set => index = value % Archive.Length;
            }
            public Progress[] Archive { get; }

            private int index;

            public ProgressArchive(int length)
            {
                index = 0;
                Archive = new Progress[length];
            }

            public void AddNext(ISyncProgress progress)
            {
                Archive[Index++] = new Progress(progress);
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
