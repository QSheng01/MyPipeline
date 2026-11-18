using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Illusion.Rendering.PRTGI
{
    internal interface IPRTRelightRuntime : IDisposable
    {
        Texture Coefficients { get; }
        PRTCascadeLayout Layout { get; }
        string Backend { get; }
        long TotalSectorUpdates { get; }
        int NearSector { get; }
        int BackgroundSector { get; }
        int UpdatedSectorsThisFrame { get; }
        long ResidentBytes { get; }
        long CachedCpuSectorBytes { get; }
        int PendingCpuSectors { get; }
        long PeakResidentBytes { get; }
        int FrameUploadBytes { get; }
        int UploadingSectors { get; }
        int EvictingSectors { get; }
        int PublishedCellsThisFrame { get; }
        string ResidencyPressure { get; }
        void Tick(Camera camera, CommandBuffer cmd, ref RenderingData renderingData);
    }
}
