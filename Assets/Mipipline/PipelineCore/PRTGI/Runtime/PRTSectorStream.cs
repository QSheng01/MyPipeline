using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Illusion.Rendering.PRTGI
{
    // Loads only requested transport files. The index/metadata stay on the ScriptableObject.
    internal sealed class PRTSectorStream : IDisposable
    {
        private sealed class Entry
        {
            internal PRTSectorData data;
            internal Task<PRTSectorData> loading;
            internal UnityWebRequest webRequest;
            internal UnityWebRequestAsyncOperation webOperation;
            internal long used, bytes;
        }
        private readonly PRTProbeVolumeAsset _asset;
        private readonly Dictionary<int, Entry> _entries = new();
        private readonly HashSet<int> _failed = new();
        private long _clock;
        private bool _disposed;
        internal long CachedBytes { get; private set; }
        internal int PendingCount { get; private set; }
        internal string LastError { get; private set; }

        internal PRTSectorStream(PRTProbeVolumeAsset asset) => _asset = asset;

        internal PRTSectorData Request(int index)
        {
            if (_disposed) return null;
            if (!_asset.IsStreamed) return _asset.ReadSector(index);
            if (_failed.Contains(index)) return null;
            if (!_entries.TryGetValue(index, out var entry))
            {
                if (PendingCount >= 4) return null;
                string path = _asset.SectorPath(index);
                entry = new Entry { bytes = _asset.SectorRawBytes(index), used = ++_clock };
                // The worker only reads immutable bytes and constructs value structs. Unity objects
                // and GPU resources remain on the main thread.
                if (path.Contains("://"))
                {
                    entry.webRequest = UnityWebRequest.Get(path);
                    entry.webOperation = entry.webRequest.SendWebRequest();
                }
                else entry.loading = Task.Run(() => _asset.DecodeSector(index, System.IO.File.ReadAllBytes(path)));
                _entries.Add(index, entry);
                PendingCount++;
                return null;
            }
            entry.used = ++_clock;
            if (entry.data != null) return entry.data;
            if (entry.webOperation != null)
            {
                if (!entry.webOperation.isDone) return null;
                if (entry.webRequest.result != UnityWebRequest.Result.Success)
                {
                    LastError = $"PRT sector {index}: {entry.webRequest.error}";
                    entry.webRequest.Dispose();
                    _entries.Remove(index);
                    _failed.Add(index);
                    PendingCount--;
                    return null;
                }
                byte[] bytes = entry.webRequest.downloadHandler.data;
                entry.webRequest.Dispose();
                entry.webRequest = null;
                entry.webOperation = null;
#if UNITY_WEBGL && !UNITY_EDITOR
                try { entry.data = _asset.DecodeSector(index, bytes); }
                catch (Exception exception)
                {
                    LastError = $"PRT sector {index}: {exception.Message}";
                    _entries.Remove(index); _failed.Add(index); PendingCount--; return null;
                }
                PendingCount--;
                CachedBytes += entry.bytes;
                return entry.data;
#else
                entry.loading = Task.Run(() => _asset.DecodeSector(index, bytes));
#endif
            }
            if (!entry.loading.IsCompleted) return null;
            PendingCount--;
            if (entry.loading.IsFaulted || entry.loading.IsCanceled)
            {
                LastError = $"PRT sector {index}: {entry.loading.Exception?.GetBaseException().Message ?? "cancelled"}";
                _entries.Remove(index);
                _failed.Add(index);
                return null;
            }
            entry.data = entry.loading.Result;
            entry.loading = null;
            CachedBytes += entry.bytes;
            return entry.data;
        }

        internal void Trim(long budget, int protectedSector)
        {
            if (!_asset.IsStreamed) return;
            budget = Math.Max(1, budget);
            while (CachedBytes > budget)
            {
                int victim = -1;
                long oldest = long.MaxValue;
                foreach (var pair in _entries)
                    if (pair.Key != protectedSector && pair.Value.data != null && pair.Value.used < oldest)
                    { victim = pair.Key; oldest = pair.Value.used; }
                if (victim < 0) break;
                CachedBytes -= _entries[victim].bytes;
                _entries.Remove(victim);
            }
        }

        public void Dispose()
        {
            _disposed = true;
            foreach (var entry in _entries.Values) entry.webRequest?.Dispose();
            _entries.Clear();
            _failed.Clear();
            CachedBytes = 0;
        }
    }
}
