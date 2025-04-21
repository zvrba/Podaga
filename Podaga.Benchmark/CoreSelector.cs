using System;
using Windows.Win32.System.SystemInformation;

using Windows.Win32;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;

namespace Podaga.Benchmark;

internal static class CoreSelector
{
    public static readonly nuint SelectedCoreMask;

    unsafe static CoreSelector() {
        uint size = 0;
        PInvoke.GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore, (SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX*)0, ref size);
        if (Marshal.GetLastWin32Error() != (int)Windows.Win32.Foundation.WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
            throw new InvalidOperationException("GetLogicalProcessorInformationEx");

        var buffer = new SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX[size / sizeof(SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX)];
        fixed (SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX* p = &buffer[0]) {
            if (!PInvoke.GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore, p, ref size))
                throw new InvalidOperationException("GetLogicalProcessorInformationEx");
        }

        // Find max efficiency class.
        var imax = 0;
        var emax = 0;
        for (var i = 0; i < buffer.Length; ++i) {
            var p = buffer[i].Anonymous.Processor;
            if (p.EfficiencyClass >= emax && p.GroupCount > 0) {
                emax = p.EfficiencyClass;
                imax = i;
            }
        }

#if true    // Use only single core.
        var m = buffer[imax].Anonymous.Processor.GroupMask[0].Mask;
        while (true) {
            var m1 = m & (m - 1);
            if (m1 == 0)
                break;
            m = m1;
        }
        SelectedCoreMask = m;
#else
        SelectedCoreMask = buffer[imax].Anonymous.Processor.GroupMask[0].Mask;
#endif

        if (SelectedCoreMask == 0)
            throw new NotImplementedException("No suitable core affinity could be calculated.");
    }


    public unsafe static void SetAffinity() {
#if false
        if (!PInvoke.SetProcessAffinityMask(Process.GetCurrentProcess().SafeHandle, SelectedCoreMask))
            throw new InvalidOperationException("SetProcessAffinityMask");
#endif
        if (PInvoke.SetThreadAffinityMask(PInvoke.GetCurrentThread(), SelectedCoreMask) == 0)
            throw new InvalidOperationException("SetThreadAffinityMask");
    }
}
