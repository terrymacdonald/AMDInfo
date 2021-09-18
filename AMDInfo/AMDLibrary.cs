using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using DisplayMagicianShared;
using System.ComponentModel;
using DisplayMagicianShared.Windows;

namespace DisplayMagicianShared.AMD
{
    [StructLayout(LayoutKind.Sequential)]
    public struct AMD_ADAPTER_CONFIG : IEquatable<AMD_ADAPTER_CONFIG>
    {
        public int AdapterDeviceNumber;
        public int AdapterBusNumber;
        public int AdapterIndex;
        public bool IsPrimaryAdapter;
        public string DisplayName;
        public int OSDisplayIndex;

        public override bool Equals(object obj) => obj is AMD_ADAPTER_CONFIG other && this.Equals(other);

        public bool Equals(AMD_ADAPTER_CONFIG other)
        => AdapterIndex == other.AdapterIndex &&
           AdapterBusNumber == other.AdapterBusNumber &&
           AdapterDeviceNumber == other.AdapterDeviceNumber &&
           IsPrimaryAdapter == other.IsPrimaryAdapter &&
           DisplayName == other.DisplayName &&
           OSDisplayIndex == other.OSDisplayIndex;

        public override int GetHashCode()
        {
            return (AdapterIndex, AdapterBusNumber, AdapterDeviceNumber, IsPrimaryAdapter, DisplayName, OSDisplayIndex).GetHashCode();
        }

        public static bool operator ==(AMD_ADAPTER_CONFIG lhs, AMD_ADAPTER_CONFIG rhs) => lhs.Equals(rhs);

        public static bool operator !=(AMD_ADAPTER_CONFIG lhs, AMD_ADAPTER_CONFIG rhs) => !(lhs == rhs);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AMD_SLSMAP_CONFIG : IEquatable<AMD_SLSMAP_CONFIG>
    {
        public ADL_SLS_MAP SLSMap;
        public List<ADL_SLS_TARGET> SLSTargets;
        public List<ADL_SLS_MODE> NativeModes;
        public List<ADL_SLS_OFFSET> NativeModeOffsets;
        public List<ADL_BEZEL_TRANSIENT_MODE> BezelModes;
        public List<ADL_BEZEL_TRANSIENT_MODE> TransientModes;
        public List<ADL_SLS_OFFSET> SLSOffsets;

        public override bool Equals(object obj) => obj is AMD_SLS_CONFIG other && this.Equals(other);

        public bool Equals(AMD_SLSMAP_CONFIG other)
        => SLSMap == other.SLSMap &&
           SLSTargets.SequenceEqual(other.SLSTargets) &&
           NativeModes.SequenceEqual(other.NativeModes) &&
           NativeModeOffsets.SequenceEqual(other.NativeModeOffsets) &&
           BezelModes.SequenceEqual(other.BezelModes) &&
           TransientModes.SequenceEqual(other.TransientModes) &&
           SLSOffsets.SequenceEqual(other.SLSOffsets);

        public override int GetHashCode()
        {
            return (SLSMap, SLSTargets, NativeModes, NativeModeOffsets, BezelModes, TransientModes, SLSOffsets).GetHashCode();
        }
        public static bool operator ==(AMD_SLSMAP_CONFIG lhs, AMD_SLSMAP_CONFIG rhs) => lhs.Equals(rhs);

        public static bool operator !=(AMD_SLSMAP_CONFIG lhs, AMD_SLSMAP_CONFIG rhs) => !(lhs == rhs);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AMD_SLS_CONFIG : IEquatable<AMD_SLS_CONFIG>
    {
        public bool IsSlsEnabled;
        public List<AMD_SLSMAP_CONFIG> SLSMapConfigs;
        public List<ADL_MODE> SLSEnabledDisplayTargets;

        public override bool Equals(object obj) => obj is AMD_SLS_CONFIG other && this.Equals(other);

        public bool Equals(AMD_SLS_CONFIG other)
        => IsSlsEnabled == other.IsSlsEnabled &&
           SLSMapConfigs.SequenceEqual(other.SLSMapConfigs) &&
           SLSEnabledDisplayTargets.SequenceEqual(other.SLSEnabledDisplayTargets);

        public override int GetHashCode()
        {
            return (IsSlsEnabled, SLSMapConfigs, SLSEnabledDisplayTargets).GetHashCode();
        }
        public static bool operator ==(AMD_SLS_CONFIG lhs, AMD_SLS_CONFIG rhs) => lhs.Equals(rhs);

        public static bool operator !=(AMD_SLS_CONFIG lhs, AMD_SLS_CONFIG rhs) => !(lhs == rhs);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AMD_HDR_CONFIG : IEquatable<AMD_HDR_CONFIG>
    {
        public int AdapterIndex;
        public bool HDRSupported;
        public bool HDREnabled;

        public override bool Equals(object obj) => obj is AMD_HDR_CONFIG other && this.Equals(other);
        public bool Equals(AMD_HDR_CONFIG other)
        => AdapterIndex == other.AdapterIndex && 
           HDRSupported == other.HDRSupported &&
           HDREnabled == other.HDREnabled;

        public override int GetHashCode()
        {
            return (AdapterIndex, HDRSupported, HDREnabled).GetHashCode();
        }
        public static bool operator ==(AMD_HDR_CONFIG lhs, AMD_HDR_CONFIG rhs) => lhs.Equals(rhs);

        public static bool operator !=(AMD_HDR_CONFIG lhs, AMD_HDR_CONFIG rhs) => !(lhs == rhs);
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct AMD_DISPLAY_CONFIG : IEquatable<AMD_DISPLAY_CONFIG>
    {
        public List<AMD_ADAPTER_CONFIG> AdapterConfigs;
        public AMD_SLS_CONFIG SlsConfig;
        public List<ADL_DISPLAY_MAP> DisplayMaps;
        public List<ADL_DISPLAY_TARGET> DisplayTargets;
        public Dictionary<int,AMD_HDR_CONFIG> HdrConfigs;
        public List<string> DisplayIdentifiers;
        public override bool Equals(object obj) => obj is AMD_DISPLAY_CONFIG other && this.Equals(other);

        public bool Equals(AMD_DISPLAY_CONFIG other)
        => AdapterConfigs.SequenceEqual(other.AdapterConfigs) &&
           SlsConfig.Equals(other.SlsConfig) &&
           DisplayMaps.SequenceEqual(other.DisplayMaps) &&
           DisplayTargets.SequenceEqual(other.DisplayTargets) &&
           HdrConfigs.SequenceEqual(other.HdrConfigs) &&
           DisplayIdentifiers.SequenceEqual(other.DisplayIdentifiers);

        public override int GetHashCode()
        {
            return (AdapterConfigs, SlsConfig, DisplayMaps, DisplayTargets, DisplayIdentifiers).GetHashCode();
        }

        public static bool operator ==(AMD_DISPLAY_CONFIG lhs, AMD_DISPLAY_CONFIG rhs) => lhs.Equals(rhs);

        public static bool operator !=(AMD_DISPLAY_CONFIG lhs, AMD_DISPLAY_CONFIG rhs) => !(lhs == rhs);
    }

    class AMDLibrary : IDisposable
    {

        // Static members are 'eagerly initialized', that is, 
        // immediately when class is loaded for the first time.
        // .NET guarantees thread safety for static initialization
        private static AMDLibrary _instance = new AMDLibrary();

        private static WinLibrary _winLibrary = new WinLibrary();

        private bool _initialised = false;

        // To detect redundant calls
        private bool _disposed = false;

        // Instantiate a SafeHandle instance.
        private SafeHandle _safeHandle = new SafeFileHandle(IntPtr.Zero, true);
        private IntPtr _adlContextHandle = IntPtr.Zero;

        static AMDLibrary() { }
        public AMDLibrary()
        {

            try
            {
                SharedLogger.logger.Trace($"AMDLibrary/AMDLibrary: Attempting to load the AMD ADL DLL {ADLImport.ATI_ADL_DLL}");
                // Attempt to prelink all of the NVAPI functions
                Marshal.PrelinkAll(typeof(ADLImport));

                SharedLogger.logger.Trace("AMDLibrary/AMDLibrary: Intialising AMD ADL2 library interface");
                // Second parameter is 1 so that we only the get connected adapters in use now

                // We set the environment variable as a workaround so that ADL2_Display_SLSMapConfigX2_Get works :(
                // This is a weird thing that AMD even set in their own code! WTF! Who programmed that as a feature?
                Environment.SetEnvironmentVariable("ADL_4KWORKAROUND_CANCEL", "TRUE");

                try
                {
                    ADL_STATUS ADLRet;
                    ADLRet = ADLImport.ADL2_Main_Control_Create(ADLImport.ADL_Main_Memory_Alloc, ADLImport.ADL_TRUE, out _adlContextHandle);
                    if (ADLRet == ADL_STATUS.ADL_OK)
                    {
                        _initialised = true;
                        SharedLogger.logger.Trace($"AMDLibrary/AMDLibrary: AMD ADL2 library was initialised successfully");
                    }
                    else
                    {
                        SharedLogger.logger.Trace($"AMDLibrary/AMDLibrary: Error intialising AMD ADL2 library. ADL2_Main_Control_Create() returned error code {ADLRet}");
                    }
                }
                catch (Exception ex)
                {
                    SharedLogger.logger.Trace(ex, $"AMDLibrary/AMDLibrary: Exception intialising AMD ADL2 library. ADL2_Main_Control_Create() caused an exception.");
                }

                _winLibrary = WinLibrary.GetLibrary();
            }
            catch(DllNotFoundException ex)
            {
                // If we get here then the AMD ADL DLL wasn't found. We can't continue to use it, so we log the error and exit
                SharedLogger.logger.Info(ex, $"AMDLibrary/AMDLibrary: Exception trying to load the AMD ADL DLL {ADLImport.ATI_ADL_DLL}. This generally means you don't have the AMD ADL driver installed.");
            }            

        }

        ~AMDLibrary()
        {
            SharedLogger.logger.Trace("AMDLibrary/~AMDLibrary: Destroying AMD ADL2 library interface");
            // If the ADL2 library was initialised, then we need to free it up.
            if (_initialised)
            {
                try
                {
                    ADLImport.ADL2_Main_Control_Destroy(_adlContextHandle);
                    SharedLogger.logger.Trace($"AMDLibrary/AMDLibrary: AMD ADL2 library was destroyed successfully");
                }
                catch(Exception ex)
                {
                    SharedLogger.logger.Trace(ex, $"AMDLibrary/AMDLibrary: Exception destroying AMD ADL2 library. ADL2_Main_Control_Destroy() caused an exception.");
                }
                
            }
        }

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose() => Dispose(true);

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {

                //ADLImport.ADL_Main_Control_Destroy();

                // Dispose managed state (managed objects).
                _safeHandle?.Dispose();
            }

            _disposed = true;
        }


        public bool IsInstalled
        {
            get
            {
                return _initialised;
            }
        }

        public List<string> PCIVendorIDs
        {
            get
            {
                // A list of all the matching PCI Vendor IDs are per https://www.pcilookup.com/?ven=amd&dev=&action=submit
                return new List<string>() { "1002" };
            }
        }

        public static AMDLibrary GetLibrary()
        {
            return _instance;
        }
        
       

        public AMD_DISPLAY_CONFIG GetActiveConfig()
        {
            SharedLogger.logger.Trace($"AMDLibrary/GetActiveConfig: Getting the currently active config");
            bool allDisplays = true;
            return GetAMDDisplayConfig(allDisplays);
        }

        private AMD_DISPLAY_CONFIG GetAMDDisplayConfig(bool allDisplays = false)
        {
            AMD_DISPLAY_CONFIG myDisplayConfig = new AMD_DISPLAY_CONFIG();
            myDisplayConfig.AdapterConfigs = new List<AMD_ADAPTER_CONFIG>();

            // We set up the default for this display config as SLS disabled
            // (We will change this later if it turns out we're using SLS)
            myDisplayConfig.SlsConfig.IsSlsEnabled = false;
            myDisplayConfig.SlsConfig.SLSEnabledDisplayTargets = new List<ADL_MODE>();

            if (_initialised)
            {

                // Get the Adapter info for ALL adapter and put it in the AdapterBuffer
                SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: Running ADL2_Adapter_AdapterInfoX4_Get to get the information about all AMD Adapters.");
                int numAdaptersInfo = 0;
                IntPtr adapterInfoBuffer = IntPtr.Zero;
                ADL_STATUS ADLRet = ADLImport.ADL2_Adapter_AdapterInfoX4_Get(_adlContextHandle, -1, out numAdaptersInfo, out adapterInfoBuffer);
                if (ADLRet == ADL_STATUS.ADL_OK)
                {
                    SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: ADL2_Adapter_AdapterInfoX4_Get returned information about all AMD Adapters.");
                }
                else
                {
                    SharedLogger.logger.Error($"AMDLibrary/GetAMDDisplayConfig: ERROR - ADL2_Adapter_AdapterInfoX4_Get returned ADL_STATUS {ADLRet} when trying to get the adapter info about all AMD Adapters. Trying to skip this adapter so something at least works.");
                    return myDisplayConfig;
                }

                ADL_ADAPTER_INFOX2[] adapterArray = new ADL_ADAPTER_INFOX2[numAdaptersInfo];
                if (numAdaptersInfo > 0)
                {
                    IntPtr currentDisplayTargetBuffer = adapterInfoBuffer;
                    for (int i = 0; i < numAdaptersInfo; i++)
                    {
                        // build a structure in the array slot
                        adapterArray[i] = new ADL_ADAPTER_INFOX2();
                        // fill the array slot structure with the data from the buffer
                        adapterArray[i] = (ADL_ADAPTER_INFOX2)Marshal.PtrToStructure(currentDisplayTargetBuffer, typeof(ADL_ADAPTER_INFOX2));
                        // destroy the bit of memory we no longer need
                        //Marshal.DestroyStructure(currentDisplayTargetBuffer, typeof(ADL_ADAPTER_INFOX2));
                        // advance the buffer forwards to the next object
                        currentDisplayTargetBuffer = (IntPtr)((long)currentDisplayTargetBuffer + Marshal.SizeOf(adapterArray[i]));
                    }
                    // Free the memory used by the buffer                        
                    Marshal.FreeCoTaskMem(adapterInfoBuffer);
                }

                // Now go through each adapter and get the information we need from it
                for (int adapterIndex = 0; adapterIndex < numAdaptersInfo; adapterIndex++)
                {
                    // Skip this adapter if it isn't active
                    ADL_ADAPTER_INFOX2 oneAdapter = adapterArray[adapterIndex]; // There is always just one as we asked for a specific one!
                    if (oneAdapter.Exist != ADLImport.ADL_TRUE)
                    {
                        SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: AMD Adapter #{oneAdapter.AdapterIndex.ToString()} doesn't exist at present so skipping detection for this adapter.");
                        continue;
                    }

                    // Only skip non-present displays if we want all displays information
                    if (oneAdapter.Present != ADLImport.ADL_TRUE)
                    {
                        SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: AMD Adapter #{oneAdapter.AdapterIndex.ToString()} isn't enabled at present so skipping detection for this adapter.");
                        continue;
                    }

                    // Check if the adapter is active
                    // Skip this adapter if it isn't active
                    int adapterActiveStatus = ADLImport.ADL_FALSE;
                    ADLRet = ADLImport.ADL2_Adapter_Active_Get(_adlContextHandle, adapterIndex, out adapterActiveStatus);
                    if (ADLRet == ADL_STATUS.ADL_OK)
                    {
                        if (adapterActiveStatus == ADLImport.ADL_TRUE)
                        {
                            SharedLogger.logger.Trace($"AMDLibrary/GetSomeDisplayIdentifiers: ADL2_Adapter_Active_Get returned ADL_TRUE - AMD Adapter #{adapterIndex} is active! We can continue.");
                        }
                        else
                        {
                            SharedLogger.logger.Trace($"AMDLibrary/GetSomeDisplayIdentifiers: ADL2_Adapter_Active_Get returned ADL_FALSE - AMD Adapter #{adapterIndex} is NOT active, so skipping.");
                            continue;
                        }
                    }
                    else
                    {
                        SharedLogger.logger.Warn($"AMDLibrary/GetSomeDisplayIdentifiers: WARNING - ADL2_Adapter_Active_Get returned ADL_STATUS {ADLRet} when trying to see if AMD Adapter #{adapterIndex} is active. Trying to skip this adapter so something at least works.");
                        continue;
                    }
                    
                    // Go grab the DisplayMaps and DisplayTargets as that is useful infor for creating screens
                    int numDisplayTargets = 0;
                    int numDisplayMaps = 0;
                    IntPtr displayTargetBuffer = IntPtr.Zero;
                    IntPtr displayMapBuffer = IntPtr.Zero;
                    ADLRet = ADLImport.ADL2_Display_DisplayMapConfig_Get(_adlContextHandle, adapterIndex, out numDisplayMaps, out displayMapBuffer, out numDisplayTargets, out displayTargetBuffer, ADLImport.ADL_DISPLAY_DISPLAYMAP_OPTION_GPUINFO);
                    if (ADLRet == ADL_STATUS.ADL_OK)
                    {
                        SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: ADL2_Display_DisplayMapConfig_Get returned information about all displaytargets connected to AMD adapter {adapterIndex}.");
                    }
                    else
                    {
                        SharedLogger.logger.Error($"AMDLibrary/GetAMDDisplayConfig: ERROR - ADL2_Display_DisplayMapConfig_Get returned ADL_STATUS {ADLRet} when trying to get the display target info from AMD adapter {adapterIndex} in the computer.");
                        throw new AMDLibraryException($"ADL2_Display_DisplayMapConfig_Get returned ADL_STATUS {ADLRet} when trying to get the display target info from AMD adapter {adapterIndex} in the computer");
                    }

                    ADL_DISPLAY_MAP[] displayMapArray = { };
                    if (numDisplayMaps > 0)
                    {

                        IntPtr currentDisplayMapBuffer = displayMapBuffer;
                        displayMapArray = new ADL_DISPLAY_MAP[numDisplayMaps];
                        for (int i = 0; i < numDisplayMaps; i++)
                        {
                            // build a structure in the array slot
                            displayMapArray[i] = new ADL_DISPLAY_MAP();
                            // fill the array slot structure with the data from the buffer
                            displayMapArray[i] = (ADL_DISPLAY_MAP)Marshal.PtrToStructure(currentDisplayMapBuffer, typeof(ADL_DISPLAY_MAP));
                            // destroy the bit of memory we no longer need
                            Marshal.DestroyStructure(currentDisplayMapBuffer, typeof(ADL_DISPLAY_MAP));
                            // advance the buffer forwards to the next object
                            currentDisplayMapBuffer = (IntPtr)((long)currentDisplayMapBuffer + Marshal.SizeOf(displayMapArray[i]));
                        }
                        // Free the memory used by the buffer                        
                        Marshal.FreeCoTaskMem(displayMapBuffer);
                        // Save the item
                        myDisplayConfig.DisplayMaps = displayMapArray.ToList<ADL_DISPLAY_MAP>();

                    }

                    ADL_DISPLAY_TARGET[] displayTargetArray = { };
                    if (numDisplayTargets > 0)
                    {
                        IntPtr currentDisplayTargetBuffer = displayTargetBuffer;
                        //displayTargetArray = new ADL_DISPLAY_TARGET[numDisplayTargets];
                        displayTargetArray = new ADL_DISPLAY_TARGET[numDisplayTargets];
                        for (int i = 0; i < numDisplayTargets; i++)
                        {
                            // build a structure in the array slot
                            displayTargetArray[i] = new ADL_DISPLAY_TARGET();
                            //displayTargetArray[i] = new ADL_DISPLAY_TARGET();
                            // fill the array slot structure with the data from the buffer
                            displayTargetArray[i] = (ADL_DISPLAY_TARGET)Marshal.PtrToStructure(currentDisplayTargetBuffer, typeof(ADL_DISPLAY_TARGET));
                            //displayTargetArray[i] = (ADL_DISPLAY_TARGET)Marshal.PtrToStructure(currentDisplayTargetBuffer, typeof(ADL_DISPLAY_TARGET));
                            // destroy the bit of memory we no longer need
                            Marshal.DestroyStructure(currentDisplayTargetBuffer, typeof(ADL_DISPLAY_TARGET));
                            // advance the buffer forwards to the next object
                            currentDisplayTargetBuffer = (IntPtr)((long)currentDisplayTargetBuffer + Marshal.SizeOf(displayTargetArray[i]));
                            //currentDisplayTargetBuffer = (IntPtr)((long)currentDisplayTargetBuffer + Marshal.SizeOf(displayTargetArray[i]));

                        }
                        // Free the memory used by the buffer                        
                        Marshal.FreeCoTaskMem(displayTargetBuffer);
                        // Save the item                            
                        //savedAdapterConfig.DisplayTargets = new ADL_DISPLAY_TARGET[numDisplayTargets];
                        myDisplayConfig.DisplayTargets = displayTargetArray.ToList<ADL_DISPLAY_TARGET>();
                    }

                    // Loop through all the displayTargets currently in use
                    foreach (var displayTarget in displayTargetArray)
                    {
                        if (displayTarget.DisplayID.DisplayLogicalAdapterIndex == oneAdapter.AdapterIndex)
                        {
                            // we only want to record the adapters that are currently in use as displayTargets
                            AMD_ADAPTER_CONFIG savedAdapterConfig = new AMD_ADAPTER_CONFIG();
                            savedAdapterConfig.AdapterBusNumber = oneAdapter.BusNumber;
                            savedAdapterConfig.AdapterDeviceNumber = oneAdapter.DeviceNumber;
                            savedAdapterConfig.AdapterIndex = oneAdapter.AdapterIndex;
                            savedAdapterConfig.DisplayName = oneAdapter.DisplayName;
                            savedAdapterConfig.OSDisplayIndex = oneAdapter.OSDisplayIndex;

                            // Save the AMD Adapter Config
                            if (!myDisplayConfig.AdapterConfigs.Contains(savedAdapterConfig))
                            {
                                // Save the new adapter config only if we haven't already
                                myDisplayConfig.AdapterConfigs.Add(savedAdapterConfig);
                            }

                        }
                    }

                    // Prep the SLSMapConfig list
                    myDisplayConfig.SlsConfig.SLSMapConfigs = new List<AMD_SLSMAP_CONFIG>();

                    // If there are more than 1 display targets then eyefinity is possible
                    if (numDisplayTargets > 1)
                    {
                        // Check if SLS is enabled for this adapter!
                        int matchingSLSMapIndex = -1;
                        ADLRet = ADLImport.ADL2_Display_SLSMapIndex_Get(_adlContextHandle, oneAdapter.AdapterIndex, numDisplayTargets, displayTargetArray, out matchingSLSMapIndex);
                        if (ADLRet == ADL_STATUS.ADL_OK && matchingSLSMapIndex != -1)
                        {
                            // We have a matching SLS index!
                            SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: AMD Adapter #{oneAdapter.AdapterIndex.ToString()} has one or more SLS Maps that could be used with this display configuration! Eyefinity (SLS) could be enabled.");                            

                            AMD_SLSMAP_CONFIG mySLSMapConfig = new AMD_SLSMAP_CONFIG();

                            // We want to get the SLSMapConfig for this matching SLS Map to see if it is actually in use
                            int numSLSTargets = 0;
                            IntPtr slsTargetBuffer = IntPtr.Zero;
                            int numNativeMode = 0;
                            IntPtr nativeModeBuffer = IntPtr.Zero;
                            int numNativeModeOffsets = 0;
                            IntPtr nativeModeOffsetsBuffer = IntPtr.Zero;
                            int numBezelMode = 0;
                            IntPtr bezelModeBuffer = IntPtr.Zero;
                            int numTransientMode = 0;
                            IntPtr transientModeBuffer = IntPtr.Zero;
                            int numSLSOffset = 0;
                            IntPtr slsOffsetBuffer = IntPtr.Zero;
                            ADL_SLS_MAP slsMap = new ADL_SLS_MAP();
                            ADLRet = ADLImport.ADL2_Display_SLSMapConfigX2_Get(
                                                                            _adlContextHandle,
                                                                                oneAdapter.AdapterIndex,
                                                                                matchingSLSMapIndex,
                                                                                ref slsMap,
                                                                                out numSLSTargets,
                                                                                out slsTargetBuffer,
                                                                                out numNativeMode,
                                                                                out nativeModeBuffer,
                                                                                out numNativeModeOffsets,
                                                                                out nativeModeOffsetsBuffer,
                                                                                out numBezelMode,
                                                                                out bezelModeBuffer,
                                                                                out numTransientMode,
                                                                                out transientModeBuffer,
                                                                                out numSLSOffset,
                                                                                out slsOffsetBuffer,
                                                                                ADLImport.ADL_DISPLAY_SLSGRID_CAP_OPTION_RELATIVETO_CURRENTANGLE);
                            if (ADLRet == ADL_STATUS.ADL_OK)
                            {
                                SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: ADL2_Display_SLSMapConfigX2_Get returned information about the SLS Info connected to AMD adapter {adapterIndex}.");
                            }
                            else
                            {
                                SharedLogger.logger.Error($"AMDLibrary/GetAMDDisplayConfig: ERROR - ADL2_Display_SLSMapConfigX2_Get returned ADL_STATUS {ADLRet} when trying to get the SLS Info from AMD adapter {adapterIndex} in the computer.");
                                continue;
                            }

                            // First check that the number of grid entries is equal to the number
                            // of display targets associated with this adapter & SLS surface.
                            if (numDisplayTargets != (slsMap.Grid.SLSGridColumn * slsMap.Grid.SLSGridRow))
                            {
                                //Number of display targets returned is not equal to the SLS grid size, so SLS can't be enabled fo this display
                                //myDisplayConfig.SlsConfig.IsSlsEnabled = false; // This is already set to false at the start!
                                break;
                            }

                            // Add the slsMap to the config we want to store
                            mySLSMapConfig.SLSMap = slsMap;

                            // Process the slsTargetBuffer
                            ADL_SLS_TARGET[] slsTargetArray = new ADL_SLS_TARGET[numSLSTargets];
                            if (numSLSTargets > 0)
                            {
                                IntPtr currentSLSTargetBuffer = slsTargetBuffer;
                                for (int i = 0; i < numSLSTargets; i++)
                                {
                                    // build a structure in the array slot
                                    slsTargetArray[i] = new ADL_SLS_TARGET();
                                    // fill the array slot structure with the data from the buffer
                                    slsTargetArray[i] = (ADL_SLS_TARGET)Marshal.PtrToStructure(currentSLSTargetBuffer, typeof(ADL_SLS_TARGET));
                                    // destroy the bit of memory we no longer need
                                    //Marshal.DestroyStructure(currentDisplayTargetBuffer, typeof(ADL_ADAPTER_INFOX2));
                                    // advance the buffer forwards to the next object
                                    currentSLSTargetBuffer = (IntPtr)((long)currentSLSTargetBuffer + Marshal.SizeOf(slsTargetArray[i]));
                                }
                                // Free the memory used by the buffer                        
                                Marshal.FreeCoTaskMem(slsTargetBuffer);

                                // Add the slsTarget to the config we want to store
                                mySLSMapConfig.SLSTargets = slsTargetArray.ToList();

                            }
                            else
                            {
                                // Add the slsTarget to the config we want to store
                                mySLSMapConfig.SLSTargets = new List<ADL_SLS_TARGET>();
                            }

                            // Process the nativeModeBuffer
                            ADL_SLS_MODE[] nativeModeArray = new ADL_SLS_MODE[numNativeMode];
                            if (numNativeMode > 0)
                            {
                                IntPtr currentNativeModeBuffer = nativeModeBuffer;
                                for (int i = 0; i < numNativeMode; i++)
                                {
                                    // build a structure in the array slot
                                    nativeModeArray[i] = new ADL_SLS_MODE();
                                    // fill the array slot structure with the data from the buffer
                                    nativeModeArray[i] = (ADL_SLS_MODE)Marshal.PtrToStructure(currentNativeModeBuffer, typeof(ADL_SLS_MODE));
                                    // destroy the bit of memory we no longer need
                                    //Marshal.DestroyStructure(currentDisplayTargetBuffer, typeof(ADL_ADAPTER_INFOX2));
                                    // advance the buffer forwards to the next object
                                    currentNativeModeBuffer = (IntPtr)((long)currentNativeModeBuffer + Marshal.SizeOf(nativeModeArray[i]));
                                }
                                // Free the memory used by the buffer                        
                                Marshal.FreeCoTaskMem(nativeModeBuffer);

                                // Add the nativeMode to the config we want to store
                                mySLSMapConfig.NativeModes = nativeModeArray.ToList();

                            }
                            else
                            {
                                // Add the slsTarget to the config we want to store
                                mySLSMapConfig.NativeModes = new List<ADL_SLS_MODE>();
                            }

                            // Process the nativeModeOffsetsBuffer
                            ADL_SLS_OFFSET[] nativeModeOffsetArray = new ADL_SLS_OFFSET[numNativeModeOffsets];
                            if (numNativeModeOffsets > 0)
                            {
                                IntPtr currentNativeModeOffsetsBuffer = nativeModeOffsetsBuffer;
                                for (int i = 0; i < numNativeModeOffsets; i++)
                                {
                                    // build a structure in the array slot
                                    nativeModeOffsetArray[i] = new ADL_SLS_OFFSET();
                                    // fill the array slot structure with the data from the buffer
                                    nativeModeOffsetArray[i] = (ADL_SLS_OFFSET)Marshal.PtrToStructure(currentNativeModeOffsetsBuffer, typeof(ADL_SLS_OFFSET));
                                    // destroy the bit of memory we no longer need
                                    //Marshal.DestroyStructure(currentDisplayTargetBuffer, typeof(ADL_ADAPTER_INFOX2));
                                    // advance the buffer forwards to the next object
                                    currentNativeModeOffsetsBuffer = (IntPtr)((long)currentNativeModeOffsetsBuffer + Marshal.SizeOf(nativeModeOffsetArray[i]));
                                }
                                // Free the memory used by the buffer                        
                                Marshal.FreeCoTaskMem(nativeModeOffsetsBuffer);

                                // Add the nativeModeOffsets to the config we want to store
                                mySLSMapConfig.NativeModeOffsets = nativeModeOffsetArray.ToList();

                            }
                            else
                            {
                                // Add the empty list to the config we want to store
                                mySLSMapConfig.NativeModeOffsets = new List<ADL_SLS_OFFSET>();
                            }

                            // Process the bezelModeBuffer
                            ADL_BEZEL_TRANSIENT_MODE[] bezelModeArray = new ADL_BEZEL_TRANSIENT_MODE[numBezelMode];
                            if (numBezelMode > 0)
                            {
                                IntPtr currentBezelModeBuffer = bezelModeBuffer;
                                for (int i = 0; i < numBezelMode; i++)
                                {
                                    // build a structure in the array slot
                                    bezelModeArray[i] = new ADL_BEZEL_TRANSIENT_MODE();
                                    // fill the array slot structure with the data from the buffer
                                    bezelModeArray[i] = (ADL_BEZEL_TRANSIENT_MODE)Marshal.PtrToStructure(currentBezelModeBuffer, typeof(ADL_BEZEL_TRANSIENT_MODE));
                                    // destroy the bit of memory we no longer need
                                    //Marshal.DestroyStructure(currentDisplayTargetBuffer, typeof(ADL_ADAPTER_INFOX2));
                                    // advance the buffer forwards to the next object
                                    currentBezelModeBuffer = (IntPtr)((long)currentBezelModeBuffer + Marshal.SizeOf(bezelModeArray[i]));
                                }
                                // Free the memory used by the buffer                        
                                Marshal.FreeCoTaskMem(bezelModeBuffer);

                                // Add the bezelModes to the config we want to store
                                mySLSMapConfig.BezelModes = bezelModeArray.ToList();

                            }
                            else
                            {
                                // Add the slsTarget to the config we want to store
                                mySLSMapConfig.BezelModes = new List<ADL_BEZEL_TRANSIENT_MODE>();
                            }

                            // Process the transientModeBuffer
                            ADL_BEZEL_TRANSIENT_MODE[] transientModeArray = new ADL_BEZEL_TRANSIENT_MODE[numTransientMode];
                            if (numTransientMode > 0)
                            {
                                IntPtr currentTransientModeBuffer = transientModeBuffer;
                                for (int i = 0; i < numTransientMode; i++)
                                {
                                    // build a structure in the array slot
                                    transientModeArray[i] = new ADL_BEZEL_TRANSIENT_MODE();
                                    // fill the array slot structure with the data from the buffer
                                    transientModeArray[i] = (ADL_BEZEL_TRANSIENT_MODE)Marshal.PtrToStructure(currentTransientModeBuffer, typeof(ADL_BEZEL_TRANSIENT_MODE));
                                    // destroy the bit of memory we no longer need
                                    //Marshal.DestroyStructure(currentDisplayTargetBuffer, typeof(ADL_ADAPTER_INFOX2));
                                    // advance the buffer forwards to the next object
                                    currentTransientModeBuffer = (IntPtr)((long)currentTransientModeBuffer + Marshal.SizeOf(transientModeArray[i]));
                                }
                                // Free the memory used by the buffer                        
                                Marshal.FreeCoTaskMem(transientModeBuffer);

                                // Add the transientModes to the config we want to store
                                mySLSMapConfig.TransientModes = transientModeArray.ToList();
                            }
                            else
                            {
                                // Add the slsTarget to the config we want to store
                                mySLSMapConfig.TransientModes = new List<ADL_BEZEL_TRANSIENT_MODE>();
                            }

                            // Process the slsOffsetBuffer
                            ADL_SLS_OFFSET[] slsOffsetArray = new ADL_SLS_OFFSET[numSLSOffset];
                            if (numSLSOffset > 0)
                            {
                                IntPtr currentSLSOffsetBuffer = slsOffsetBuffer;
                                for (int i = 0; i < numSLSOffset; i++)
                                {
                                    // build a structure in the array slot
                                    slsOffsetArray[i] = new ADL_SLS_OFFSET();
                                    // fill the array slot structure with the data from the buffer
                                    slsOffsetArray[i] = (ADL_SLS_OFFSET)Marshal.PtrToStructure(currentSLSOffsetBuffer, typeof(ADL_SLS_OFFSET));
                                    // destroy the bit of memory we no longer need
                                    //Marshal.DestroyStructure(currentDisplayTargetBuffer, typeof(ADL_ADAPTER_INFOX2));
                                    // advance the buffer forwards to the next object
                                    currentSLSOffsetBuffer = (IntPtr)((long)currentSLSOffsetBuffer + Marshal.SizeOf(slsOffsetArray[i]));
                                }
                                // Free the memory used by the buffer                        
                                Marshal.FreeCoTaskMem(slsOffsetBuffer);

                                // Add the slsOffsets to the config we want to store
                                mySLSMapConfig.SLSOffsets = slsOffsetArray.ToList();

                            }
                            else
                            {
                                // Add the slsTarget to the config we want to store
                                mySLSMapConfig.SLSOffsets = new List<ADL_SLS_OFFSET>();
                            }                           

                            // Now we try to calculate whether SLS is enabled
                            // NFI why they don't just add a ADL2_Display_SLSMapConfig_GetState function to make this easy for ppl :(
                            // NVIDIA make it easy, why can't you AMD?

                            // Logic cribbed from https://github.com/elitak/amd-adl-sdk/blob/master/Sample/Eyefinity/ati_eyefinity.c
                            // Go through each display Target
                            foreach (var displayTarget in displayTargetArray)
                            {
                                // Get the current Display Modes for this adapter/display combination
                                int numDisplayModes;
                                IntPtr displayModeBuffer;
                                ADLRet = ADLImport.ADL2_Display_Modes_Get(
                                                                            _adlContextHandle,
                                                                                oneAdapter.AdapterIndex,
                                                                                displayTarget.DisplayID.DisplayLogicalIndex,
                                                                                out numDisplayModes,
                                                                                out displayModeBuffer);
                                if (ADLRet == ADL_STATUS.ADL_OK)
                                {
                                    SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: ADL2_Display_Modes_Get returned information about the display modes used by display #{displayTarget.DisplayID.DisplayLogicalAdapterIndex} connected to AMD adapter {adapterIndex}.");
                                }
                                else
                                {
                                    SharedLogger.logger.Error($"AMDLibrary/GetAMDDisplayConfig: ERROR - ADL2_Display_Modes_Get returned ADL_STATUS {ADLRet} when trying to get the display modes from AMD adapter {adapterIndex} in the computer.");
                                    continue;
                                }

                                ADL_MODE[] displayModeArray = new ADL_MODE[numDisplayModes];
                                if (numDisplayModes > 0)
                                {
                                    IntPtr currentDisplayModeBuffer = displayModeBuffer;
                                    for (int i = 0; i < numDisplayModes; i++)
                                    {
                                        // build a structure in the array slot
                                        displayModeArray[i] = new ADL_MODE();
                                        // fill the array slot structure with the data from the buffer
                                        displayModeArray[i] = (ADL_MODE)Marshal.PtrToStructure(currentDisplayModeBuffer, typeof(ADL_MODE));
                                        // destroy the bit of memory we no longer need
                                        //Marshal.DestroyStructure(currentDisplayTargetBuffer, typeof(ADL_ADAPTER_INFOX2));
                                        // advance the buffer forwards to the next object
                                        currentDisplayModeBuffer = (IntPtr)((long)currentDisplayModeBuffer + Marshal.SizeOf(displayModeArray[i]));
                                    }
                                    // Free the memory used by the buffer                        
                                    Marshal.FreeCoTaskMem(displayModeBuffer);

                                    // Add the slsOffsets to the config we want to store
                                    //mySLSMapConfig.SLSOffsets = displayModeArray.ToList();

                                }

                                // If Eyefinity is enabled for this adapter, then the display mode of an
                                // attached display target will match one of the SLS display modes reported by
                                // ADL_Display_SLSMapConfig_Get(). The match will either be with "native" SLS 
                                // modes (which are not bezel-compensated), or with "bezel" SLS modes which are.
                                // 
                                // So, simply compare current display mode against all the ones listed for the
                                // SLS native or bezel-compensated modes: if there is a match, then the mode
                                // currently used by this adapter is an Eyefinity/SLS mode, and Eyefinity is enabled.
                                // First check the native SLS mode list
                                // Process the slsOffsetBuffer
                                bool isSlsEnabled = false;
                                bool isBezelCompensatedDisplay = false;
                                foreach (var displayMode in displayModeArray)
                                {
                                    foreach (var nativeMode in nativeModeArray)
                                    {
                                        if (nativeMode.DisplayMode.XRes == displayMode.XRes && nativeMode.DisplayMode.YRes == displayMode.YRes)
                                        {
                                            isSlsEnabled = true;
                                            break;
                                        }

                                    }

                                    // If no match was found, check the bezel-compensated SLS mode list
                                    if (!isSlsEnabled)
                                    {
                                        foreach (var bezelMode in bezelModeArray)
                                        {
                                            if (bezelMode.DisplayMode.XRes == displayMode.XRes && bezelMode.DisplayMode.YRes == displayMode.YRes)
                                            {
                                                isSlsEnabled = true;
                                                isBezelCompensatedDisplay = true;
                                                break;
                                            }
                                        }
                                    }

                                    // Now we check which slot we need to put this display into
                                    if (isSlsEnabled)
                                    {
                                        // SLS is enabled for this display
                                        if (!myDisplayConfig.SlsConfig.SLSEnabledDisplayTargets.Contains(displayMode))
                                        {
                                            myDisplayConfig.SlsConfig.SLSEnabledDisplayTargets.Add(displayMode);
                                        }
                                        // we also update the main IsSLSEnabled so that it is indicated at the top level too

                                        myDisplayConfig.SlsConfig.IsSlsEnabled = true;
                                        SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: AMD Adapter #{oneAdapter.AdapterIndex.ToString()} has a matching SLS grid set! Eyefinity (SLS) is enabled. Setting IsSlsEnabled to true");

                                    }
                                }

                            }

                            // Only Add the mySLSMapConfig to the displayConfig if SLS is enabled
                            if (myDisplayConfig.SlsConfig.IsSlsEnabled)
                            {
                                myDisplayConfig.SlsConfig.SLSMapConfigs.Add(mySLSMapConfig);
                            }

                        }
                        else
                        {
                            // If we get here then there there was no active SLSGrid, meaning Eyefinity is disabled!
                            SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: AMD Adapter #{oneAdapter.AdapterIndex.ToString()} has no active SLS grids set! Eyefinity (SLS) hasn't even been setup yet. Keeping the default IsSlsEnabled value of false.");
                        }
                    }                                        
                    else
                    {
                        // If we get here then there are less than two displays connected. Eyefinity cannot be enabled in this case!
                        SharedLogger.logger.Error($"AMDLibrary/GetAMDDisplayConfig: There are less than two displays connected to this adapter so Eyefinity cannot be enabled.");
                    }


                    myDisplayConfig.HdrConfigs = new Dictionary<int, AMD_HDR_CONFIG>();
                    // Now we need to get all the displays connected to this adapter so that we can get their HDR state
                    foreach (var displayTarget in displayTargetArray)
                    {
                        // Go through each display and see if HDR is supported
                        int supported = 0;
                        int enabled = 0;
                        ADLRet = ADLImport.ADL2_Display_HDRState_Get(_adlContextHandle, adapterIndex, displayTarget.DisplayID, out supported, out enabled);
                        if (ADLRet == ADL_STATUS.ADL_OK)
                        {
                            if (supported > 0 && enabled > 0)
                            {
                                SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: ADL2_Display_HDRState_Get says that display {displayTarget.DisplayID.DisplayLogicalIndex} on adapter {adapterIndex} supports HDR and HDR is enabled.");
                            }
                            else if (supported > 0 && enabled == 0)
                            {
                                SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: ADL2_Display_HDRState_Get says that display {displayTarget.DisplayID.DisplayLogicalIndex} on adapter {adapterIndex} supports HDR and HDR is NOT enabled.");
                            }
                            else 
                            {
                                SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: ADL2_Display_HDRState_Get says that display {displayTarget.DisplayID.DisplayLogicalIndex} on adapter {adapterIndex} does NOT support HDR.");
                            }                            
                        }
                        else
                        {
                            SharedLogger.logger.Error($"AMDLibrary/GetAMDDisplayConfig: ERROR - ADL2_Display_HDRState_Get returned ADL_STATUS {ADLRet} when trying to get the display target info from AMD adapter {adapterIndex} in the computer.");
                            throw new AMDLibraryException($"ADL2_Display_HDRState_Get returned ADL_STATUS {ADLRet} when trying to get the display target info from AMD adapter {adapterIndex} in the computer");
                        }

                        AMD_HDR_CONFIG hdrConfig = new AMD_HDR_CONFIG();
                        hdrConfig.AdapterIndex = displayTarget.DisplayID.DisplayPhysicalAdapterIndex;
                        hdrConfig.HDREnabled = enabled > 0 ? true : false;
                        hdrConfig.HDRSupported = supported > 0 ? true : false;

                        // Now add this to the HDR config list.                        
                        if (!myDisplayConfig.HdrConfigs.ContainsKey(displayTarget.DisplayID.DisplayLogicalIndex))
                        {
                            // Save the new display config only if we haven't already
                            myDisplayConfig.HdrConfigs.Add(displayTarget.DisplayID.DisplayLogicalIndex, hdrConfig);
                        }
                    }

                    // Add the AMD Display Identifiers
                    myDisplayConfig.DisplayIdentifiers = GetCurrentDisplayIdentifiers();
                }                
            }
            else
            {
                SharedLogger.logger.Error($"AMDLibrary/GetAMDDisplayConfig: ERROR - Tried to run GetAMDDisplayConfig but the AMD ADL library isn't initialised!");
                throw new AMDLibraryException($"Tried to run GetAMDDisplayConfig but the AMD ADL library isn't initialised!");
            }
            
            // Return the configuration
            return myDisplayConfig;
        }


        public string PrintActiveConfig()
        {
            string stringToReturn = "";

            // Get the size of the largest Active Paths and Modes arrays
            int pathCount = 0;
            int modeCount = 0;
            WIN32STATUS err = CCDImport.GetDisplayConfigBufferSizes(QDC.QDC_ONLY_ACTIVE_PATHS, out pathCount, out modeCount);
            if (err != WIN32STATUS.ERROR_SUCCESS)
            {
                SharedLogger.logger.Error($"AMDLibrary/PrintActiveConfig: ERROR - GetDisplayConfigBufferSizes returned WIN32STATUS {err} when trying to get the maximum path and mode sizes");
                throw new AMDLibraryException($"GetDisplayConfigBufferSizes returned WIN32STATUS {err} when trying to get the maximum path and mode sizes");
            }

            SharedLogger.logger.Trace($"AMDLibrary/PrintActiveConfig: Getting the current Display Config path and mode arrays");
            var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
            err = CCDImport.QueryDisplayConfig(QDC.QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);
            if (err == WIN32STATUS.ERROR_INSUFFICIENT_BUFFER)
            {
                SharedLogger.logger.Warn($"AMDLibrary/PrintActiveConfig: The displays were modified between GetDisplayConfigBufferSizes and QueryDisplayConfig so we need to get the buffer sizes again.");
                SharedLogger.logger.Trace($"AMDLibrary/PrintActiveConfig: Getting the size of the largest Active Paths and Modes arrays");
                // Screen changed in between GetDisplayConfigBufferSizes and QueryDisplayConfig, so we need to get buffer sizes again
                // as per https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-querydisplayconfig 
                err = CCDImport.GetDisplayConfigBufferSizes(QDC.QDC_ONLY_ACTIVE_PATHS, out pathCount, out modeCount);
                if (err != WIN32STATUS.ERROR_SUCCESS)
                {
                    SharedLogger.logger.Error($"AMDLibrary/PrintActiveConfig: ERROR - GetDisplayConfigBufferSizes returned WIN32STATUS {err} when trying to get the maximum path and mode sizes again");
                    throw new AMDLibraryException($"GetDisplayConfigBufferSizes returned WIN32STATUS {err} when trying to get the maximum path and mode sizes again");
                }
                SharedLogger.logger.Trace($"AMDLibrary/PrintActiveConfig: Getting the current Display Config path and mode arrays");
                paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
                modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
                err = CCDImport.QueryDisplayConfig(QDC.QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);
                if (err == WIN32STATUS.ERROR_INSUFFICIENT_BUFFER)
                {
                    SharedLogger.logger.Error($"AMDLibrary/PrintActiveConfig: ERROR - The displays were still modified between GetDisplayConfigBufferSizes and QueryDisplayConfig, even though we tried twice. Something is wrong.");
                    throw new AMDLibraryException($"The displays were still modified between GetDisplayConfigBufferSizes and QueryDisplayConfig, even though we tried twice. Something is wrong.");
                }
                else if (err != WIN32STATUS.ERROR_SUCCESS)
                {
                    SharedLogger.logger.Error($"AMDLibrary/PrintActiveConfig: ERROR - QueryDisplayConfig returned WIN32STATUS {err} when trying to query all available displays again");
                    throw new AMDLibraryException($"QueryDisplayConfig returned WIN32STATUS {err} when trying to query all available displays again.");
                }
            }
            else if (err != WIN32STATUS.ERROR_SUCCESS)
            {
                SharedLogger.logger.Error($"AMDLibrary/PrintActiveConfig: ERROR - QueryDisplayConfig returned WIN32STATUS {err} when trying to query all available displays");
                throw new AMDLibraryException($"QueryDisplayConfig returned WIN32STATUS {err} when trying to query all available displays.");
            }

            foreach (var path in paths)
            {
                stringToReturn += $"----++++==== Path ====++++----\n";

                // get display source name
                var sourceInfo = new DISPLAYCONFIG_SOURCE_DEVICE_NAME();
                sourceInfo.Header.Type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
                sourceInfo.Header.Size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DEVICE_NAME>();
                sourceInfo.Header.AdapterId = path.SourceInfo.AdapterId;
                sourceInfo.Header.Id = path.SourceInfo.Id;
                err = CCDImport.DisplayConfigGetDeviceInfo(ref sourceInfo);
                if (err == WIN32STATUS.ERROR_SUCCESS)
                {
                    SharedLogger.logger.Trace($"AMDLibrary/PrintActiveConfig: Found Display Source {sourceInfo.ViewGdiDeviceName} for source {path.SourceInfo.Id}.");
                    stringToReturn += $"****** Interrogating Display Source {path.SourceInfo.Id} *******\n";
                    stringToReturn += $"Found Display Source {sourceInfo.ViewGdiDeviceName}\n";
                    stringToReturn += $"\n";
                }
                else
                {
                    SharedLogger.logger.Warn($"AMDLibrary/PrintActiveConfig: WARNING - DisplayConfigGetDeviceInfo returned WIN32STATUS {err} when trying to get the source info for source adapter #{path.SourceInfo.AdapterId}");
                }


                // get display target name
                var targetInfo = new DISPLAYCONFIG_TARGET_DEVICE_NAME();
                targetInfo.Header.Type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
                targetInfo.Header.Size = (uint)Marshal.SizeOf<DISPLAYCONFIG_TARGET_DEVICE_NAME>();
                targetInfo.Header.AdapterId = path.TargetInfo.AdapterId;
                targetInfo.Header.Id = path.TargetInfo.Id;
                err = CCDImport.DisplayConfigGetDeviceInfo(ref targetInfo);
                if (err == WIN32STATUS.ERROR_SUCCESS)
                {
                    SharedLogger.logger.Trace($"AMDLibrary/PrintActiveConfig: Connector Instance: {targetInfo.ConnectorInstance} for source {path.TargetInfo.Id}.");
                    SharedLogger.logger.Trace($"AMDLibrary/PrintActiveConfig: EDID Manufacturer ID: {targetInfo.EdidManufactureId} for source {path.TargetInfo.Id}.");
                    SharedLogger.logger.Trace($"AMDLibrary/PrintActiveConfig: EDID Product Code ID: {targetInfo.EdidProductCodeId} for source {path.TargetInfo.Id}.");
                    SharedLogger.logger.Trace($"AMDLibrary/PrintActiveConfig: Flags Friendly Name from EDID: {targetInfo.Flags.FriendlyNameFromEdid} for source {path.TargetInfo.Id}.");
                    SharedLogger.logger.Trace($"AMDLibrary/PrintActiveConfig: Flags Friendly Name Forced: {targetInfo.Flags.FriendlyNameForced} for source {path.TargetInfo.Id}.");
                    SharedLogger.logger.Trace($"AMDLibrary/PrintActiveConfig: Flags EDID ID is Valid: {targetInfo.Flags.EdidIdsValid} for source {path.TargetInfo.Id}.");
                    SharedLogger.logger.Trace($"AMDLibrary/PrintActiveConfig: Monitor Device Path: {targetInfo.MonitorDevicePath} for source {path.TargetInfo.Id}.");
                    SharedLogger.logger.Trace($"AMDLibrary/PrintActiveConfig: Monitor Friendly Device Name: {targetInfo.MonitorFriendlyDeviceName} for source {path.TargetInfo.Id}.");
                    SharedLogger.logger.Trace($"AMDLibrary/PrintActiveConfig: Output Technology: {targetInfo.OutputTechnology} for source {path.TargetInfo.Id}.");

                    stringToReturn += $"****** Interrogating Display Target {targetInfo.MonitorFriendlyDeviceName} *******\n";
                    stringToReturn += $" Connector Instance: {targetInfo.ConnectorInstance}\n";
                    stringToReturn += $" EDID Manufacturer ID: {targetInfo.EdidManufactureId}\n";
                    stringToReturn += $" EDID Product Code ID: {targetInfo.EdidProductCodeId}\n";
                    stringToReturn += $" Flags Friendly Name from EDID: {targetInfo.Flags.FriendlyNameFromEdid}\n";
                    stringToReturn += $" Flags Friendly Name Forced: {targetInfo.Flags.FriendlyNameForced}\n";
                    stringToReturn += $" Flags EDID ID is Valid: {targetInfo.Flags.EdidIdsValid}\n";
                    stringToReturn += $" Monitor Device Path: {targetInfo.MonitorDevicePath}\n";
                    stringToReturn += $" Monitor Friendly Device Name: {targetInfo.MonitorFriendlyDeviceName}\n";
                    stringToReturn += $" Output Technology: {targetInfo.OutputTechnology}\n";
                    stringToReturn += $"\n";
                }
                else
                {
                    SharedLogger.logger.Warn($"AMDLibrary/PrintActiveConfig: WARNING - DisplayConfigGetDeviceInfo returned WIN32STATUS {err} when trying to get the target info for display #{path.TargetInfo.Id}");
                }


                // get display adapter name
                var adapterInfo = new DISPLAYCONFIG_ADAPTER_NAME();
                adapterInfo.Header.Type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_ADAPTER_NAME;
                adapterInfo.Header.Size = (uint)Marshal.SizeOf<DISPLAYCONFIG_ADAPTER_NAME>();
                adapterInfo.Header.AdapterId = path.TargetInfo.AdapterId;
                adapterInfo.Header.Id = path.TargetInfo.Id;
                err = CCDImport.DisplayConfigGetDeviceInfo(ref adapterInfo);
                if (err == WIN32STATUS.ERROR_SUCCESS)
                {
                    SharedLogger.logger.Trace($"AMDLibrary/PrintActiveConfig: Found Adapter Device Path {adapterInfo.AdapterDevicePath} for source {path.TargetInfo.AdapterId}.");
                    stringToReturn += $"****** Interrogating Display Adapter {adapterInfo.AdapterDevicePath} *******\n";
                    stringToReturn += $" Display Adapter {adapterInfo.AdapterDevicePath}\n";
                    stringToReturn += $"\n";
                }
                else
                {
                    SharedLogger.logger.Warn($"AMDLibrary/GetWindowsDisplayConfig: WARNING - DisplayConfigGetDeviceInfo returned WIN32STATUS {err} when trying to get the adapter device path for target #{path.TargetInfo.AdapterId}");
                }

                // get display target preferred mode
                var targetPreferredInfo = new DISPLAYCONFIG_TARGET_PREFERRED_MODE();
                targetPreferredInfo.Header.Type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_PREFERRED_MODE;
                targetPreferredInfo.Header.Size = (uint)Marshal.SizeOf<DISPLAYCONFIG_TARGET_PREFERRED_MODE>();
                targetPreferredInfo.Header.AdapterId = path.TargetInfo.AdapterId;
                targetPreferredInfo.Header.Id = path.TargetInfo.Id;
                err = CCDImport.DisplayConfigGetDeviceInfo(ref targetPreferredInfo);
                if (err == WIN32STATUS.ERROR_SUCCESS)
                {
                    SharedLogger.logger.Trace($"AMDLibrary/GetWindowsDisplayConfig: Found Target Preferred Width {targetPreferredInfo.Width} for target {path.TargetInfo.Id}.");
                    SharedLogger.logger.Trace($"AMDLibrary/GetWindowsDisplayConfig: Found Target Preferred Height {targetPreferredInfo.Height} for target {path.TargetInfo.Id}.");
                    SharedLogger.logger.Trace($"AMDLibrary/GetWindowsDisplayConfig: Found Target Video Signal Info Active Size: ({targetPreferredInfo.TargetMode.TargetVideoSignalInfo.ActiveSize.Cx}x{targetPreferredInfo.TargetMode.TargetVideoSignalInfo.ActiveSize.Cy} for target {path.TargetInfo.Id}.");
                    SharedLogger.logger.Trace($"AMDLibrary/GetWindowsDisplayConfig: Found Target Video Signal Info Total Size: ({targetPreferredInfo.TargetMode.TargetVideoSignalInfo.TotalSize.Cx}x{targetPreferredInfo.TargetMode.TargetVideoSignalInfo.TotalSize.Cy} for target {path.TargetInfo.Id}.");
                    SharedLogger.logger.Trace($"AMDLibrary/GetWindowsDisplayConfig: Found Target Video Signal Info HSync Frequency: {targetPreferredInfo.TargetMode.TargetVideoSignalInfo.HSyncFreq} for target {path.TargetInfo.Id}.");
                    SharedLogger.logger.Trace($"AMDLibrary/GetWindowsDisplayConfig: Found Target Video Signal Info VSync Frequency: {targetPreferredInfo.TargetMode.TargetVideoSignalInfo.VSyncFreq} for target {path.TargetInfo.Id}.");
                    SharedLogger.logger.Trace($"AMDLibrary/GetWindowsDisplayConfig: Found Target Video Signal Info Pixel Rate: {targetPreferredInfo.TargetMode.TargetVideoSignalInfo.PixelRate} for target {path.TargetInfo.Id}.");
                    SharedLogger.logger.Trace($"AMDLibrary/GetWindowsDisplayConfig: Found Target Video Signal Info Scan Line Ordering: {targetPreferredInfo.TargetMode.TargetVideoSignalInfo.ScanLineOrdering} for target {path.TargetInfo.Id}.");
                    SharedLogger.logger.Trace($"AMDLibrary/GetWindowsDisplayConfig: Found Target Video Signal Info Video Standard: {targetPreferredInfo.TargetMode.TargetVideoSignalInfo.VideoStandard} for target {path.TargetInfo.Id}.");

                    stringToReturn += $"****** Interrogating Target Preferred Mode for Display {path.TargetInfo.Id} *******\n";
                    stringToReturn += $" Target Preferred Width {targetPreferredInfo.Width} for target {path.TargetInfo.Id}\n";
                    stringToReturn += $" Target Preferred Height {targetPreferredInfo.Height} for target {path.TargetInfo.Id}\n";
                    stringToReturn += $" Target Video Signal Info Active Size: ({targetPreferredInfo.TargetMode.TargetVideoSignalInfo.ActiveSize.Cx}x{targetPreferredInfo.TargetMode.TargetVideoSignalInfo.ActiveSize.Cy}\n";
                    stringToReturn += $" Target Video Signal Info Total Size: ({targetPreferredInfo.TargetMode.TargetVideoSignalInfo.TotalSize.Cx}x{targetPreferredInfo.TargetMode.TargetVideoSignalInfo.TotalSize.Cy}\n";
                    stringToReturn += $" Target Video Signal Info HSync Frequency: {targetPreferredInfo.TargetMode.TargetVideoSignalInfo.HSyncFreq}\n";
                    stringToReturn += $" Target Video Signal Info VSync Frequency: {targetPreferredInfo.TargetMode.TargetVideoSignalInfo.VSyncFreq}\n";
                    stringToReturn += $" Target Video Signal Info Pixel Rate: {targetPreferredInfo.TargetMode.TargetVideoSignalInfo.PixelRate}\n";
                    stringToReturn += $" Target Video Signal Info Scan Line Ordering: {targetPreferredInfo.TargetMode.TargetVideoSignalInfo.ScanLineOrdering}\n";
                    stringToReturn += $" Target Video Signal Info Video Standard: {targetPreferredInfo.TargetMode.TargetVideoSignalInfo.VideoStandard}\n";
                    stringToReturn += $"\n";
                }
                else
                {
                    SharedLogger.logger.Warn($"AMDLibrary/GetWindowsDisplayConfig: WARNING - DisplayConfigGetDeviceInfo returned WIN32STATUS {err} when trying to get the preferred target name for display #{path.TargetInfo.Id}");
                }

                // get display target base type
                var targetBaseTypeInfo = new DISPLAYCONFIG_TARGET_BASE_TYPE();
                targetBaseTypeInfo.Header.Type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_BASE_TYPE;
                targetBaseTypeInfo.Header.Size = (uint)Marshal.SizeOf<DISPLAYCONFIG_TARGET_BASE_TYPE>();
                targetBaseTypeInfo.Header.AdapterId = path.TargetInfo.AdapterId;
                targetBaseTypeInfo.Header.Id = path.TargetInfo.Id;
                err = CCDImport.DisplayConfigGetDeviceInfo(ref targetBaseTypeInfo);
                if (err == WIN32STATUS.ERROR_SUCCESS)
                {
                    SharedLogger.logger.Trace($"AMDLibrary/GetWindowsDisplayConfig: Found Virtual Resolution is Disabled: {targetBaseTypeInfo.BaseOutputTechnology} for target {path.TargetInfo.Id}.");

                    stringToReturn += $"****** Interrogating Target Base Type for Display {path.TargetInfo.Id} *******\n";
                    stringToReturn += $" Base Output Technology: {targetBaseTypeInfo.BaseOutputTechnology}\n";
                    stringToReturn += $"\n";
                }
                else
                {
                    SharedLogger.logger.Warn($"AMDLibrary/GetWindowsDisplayConfig: WARNING - DisplayConfigGetDeviceInfo returned WIN32STATUS {err} when trying to get the target base type for display #{path.TargetInfo.Id}");
                }

                // get display support virtual resolution
                var supportVirtResInfo = new DISPLAYCONFIG_SUPPORT_VIRTUAL_RESOLUTION();
                supportVirtResInfo.Header.Type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_SUPPORT_VIRTUAL_RESOLUTION;
                supportVirtResInfo.Header.Size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SUPPORT_VIRTUAL_RESOLUTION>();
                supportVirtResInfo.Header.AdapterId = path.TargetInfo.AdapterId;
                supportVirtResInfo.Header.Id = path.TargetInfo.Id;
                err = CCDImport.DisplayConfigGetDeviceInfo(ref supportVirtResInfo);
                if (err == WIN32STATUS.ERROR_SUCCESS)
                {
                    SharedLogger.logger.Trace($"AMDLibrary/GetWindowsDisplayConfig: Found Base Output Technology: {supportVirtResInfo.IsMonitorVirtualResolutionDisabled} for target {path.TargetInfo.Id}.");
                    stringToReturn += $"****** Interrogating Target Supporting virtual resolution for Display {path.TargetInfo.Id} *******\n";
                    stringToReturn += $" Virtual Resolution is Disabled: {supportVirtResInfo.IsMonitorVirtualResolutionDisabled}\n";
                    stringToReturn += $"\n";
                }
                else
                {
                    SharedLogger.logger.Warn($"AMDLibrary/GetWindowsDisplayConfig: WARNING - DisplayConfigGetDeviceInfo returned WIN32STATUS {err} when trying to find out the virtual resolution support for display #{path.TargetInfo.Id}");
                }

                //get advanced color info
                var colorInfo = new DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO();
                colorInfo.Header.Type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO;
                colorInfo.Header.Size = (uint)Marshal.SizeOf<DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO>();
                colorInfo.Header.AdapterId = path.TargetInfo.AdapterId;
                colorInfo.Header.Id = path.TargetInfo.Id;
                err = CCDImport.DisplayConfigGetDeviceInfo(ref colorInfo);
                if (err == WIN32STATUS.ERROR_SUCCESS)
                {
                    SharedLogger.logger.Trace($"AMDLibrary/GetWindowsDisplayConfig: Found Advanced Color Supported: {colorInfo.AdvancedColorSupported} for target {path.TargetInfo.Id}.");
                    SharedLogger.logger.Trace($"AMDLibrary/GetWindowsDisplayConfig: Found Advanced Color Enabled: {colorInfo.AdvancedColorEnabled} for target {path.TargetInfo.Id}.");
                    SharedLogger.logger.Trace($"AMDLibrary/GetWindowsDisplayConfig: Found Advanced Color Force Disabled: {colorInfo.AdvancedColorForceDisabled} for target {path.TargetInfo.Id}.");
                    SharedLogger.logger.Trace($"AMDLibrary/GetWindowsDisplayConfig: Found Bits per Color Channel: {colorInfo.BitsPerColorChannel} for target {path.TargetInfo.Id}.");
                    SharedLogger.logger.Trace($"AMDLibrary/GetWindowsDisplayConfig: Found Color Encoding: {colorInfo.ColorEncoding} for target {path.TargetInfo.Id}.");
                    SharedLogger.logger.Trace($"AMDLibrary/GetWindowsDisplayConfig: Found Wide Color Enforced: {colorInfo.WideColorEnforced} for target {path.TargetInfo.Id}.");

                    stringToReturn += $"****** Interrogating Advanced Color Info for Display {path.TargetInfo.Id} *******\n";
                    stringToReturn += $" Advanced Color Supported: {colorInfo.AdvancedColorSupported}\n";
                    stringToReturn += $" Advanced Color Enabled: {colorInfo.AdvancedColorEnabled}\n";
                    stringToReturn += $" Advanced Color Force Disabled: {colorInfo.AdvancedColorForceDisabled}\n";
                    stringToReturn += $" Bits per Color Channel: {colorInfo.BitsPerColorChannel}\n";
                    stringToReturn += $" Color Encoding: {colorInfo.ColorEncoding}\n";
                    stringToReturn += $" Wide Color Enforced: {colorInfo.WideColorEnforced}\n";
                    stringToReturn += $"\n";
                }
                else
                {
                    SharedLogger.logger.Warn($"AMDLibrary/GetWindowsDisplayConfig: WARNING - DisplayConfigGetDeviceInfo returned WIN32STATUS {err} when trying to find out the virtual resolution support for display #{path.TargetInfo.Id}");
                }

                // get SDR white levels
                var whiteLevelInfo = new DISPLAYCONFIG_SDR_WHITE_LEVEL();
                whiteLevelInfo.Header.Type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_SDR_WHITE_LEVEL;
                whiteLevelInfo.Header.Size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SDR_WHITE_LEVEL>();
                whiteLevelInfo.Header.AdapterId = path.TargetInfo.AdapterId;
                whiteLevelInfo.Header.Id = path.TargetInfo.Id;
                err = CCDImport.DisplayConfigGetDeviceInfo(ref whiteLevelInfo);
                if (err == WIN32STATUS.ERROR_SUCCESS)
                {
                    SharedLogger.logger.Trace($"AMDLibrary/GetWindowsDisplayConfig: Found SDR White Level: {whiteLevelInfo.SDRWhiteLevel} for target {path.TargetInfo.Id}.");

                    stringToReturn += $"****** Interrogating SDR Whilte Level for Display {path.TargetInfo.Id} *******\n";
                    stringToReturn += $" SDR White Level: {whiteLevelInfo.SDRWhiteLevel}\n";
                    stringToReturn += $"\n";
                }
                else
                {
                    SharedLogger.logger.Warn($"AMDLibrary/GetWindowsDisplayConfig: WARNING - DisplayConfigGetDeviceInfo returned WIN32STATUS {err} when trying to find out the SDL white level for display #{path.TargetInfo.Id}");
                }
            }
            return stringToReturn;
        }

        public bool SetActiveConfig(AMD_DISPLAY_CONFIG displayConfig)
        {

            if (_initialised)
            {
                // Set the initial state of the ADL_STATUS
                ADL_STATUS ADLRet = 0;

                // We want to get the current config
                AMD_DISPLAY_CONFIG currentDisplayConfig = GetAMDDisplayConfig();

                // set the display locations
                if (displayConfig.SlsConfig.IsSlsEnabled)
                {
                    // We need to change to an Eyefinity (SLS) profile, so we need to apply the new SLS Topologies
                    SharedLogger.logger.Trace($"AMDLibrary/SetActiveConfig: SLS is enabled in the new display configuration, so we need to set it");

                    foreach (AMD_SLSMAP_CONFIG slsMapConfig in displayConfig.SlsConfig.SLSMapConfigs)
                    {
                        // Turn on this SLS Map Config
                        ADLRet = ADLImport.ADL2_Display_SLSMapConfig_SetState(_adlContextHandle, slsMapConfig.SLSMap.AdapterIndex, slsMapConfig.SLSMap.SLSMapIndex, ADLImport.ADL_TRUE);
                        if (ADLRet == ADL_STATUS.ADL_OK)
                        {
                            SharedLogger.logger.Trace($"AMDLibrary/SetActiveConfig: ADL2_Display_SLSMapConfig_SetState successfully set the SLSMAP with index {slsMapConfig.SLSMap.SLSMapIndex} to TRUE for adapter { slsMapConfig.SLSMap.AdapterIndex}.");
                        }
                        else
                        {
                            SharedLogger.logger.Error($"AMDLibrary/SetActiveConfig: ERROR - ADL2_Display_SLSMapConfig_SetState returned ADL_STATUS {ADLRet} when trying to set the SLSMAP with index {slsMapConfig.SLSMap.SLSMapIndex} to TRUE for adapter { slsMapConfig.SLSMap.AdapterIndex}.");
                            return false;
                        }

                    }

                }
                else
                {
                    // We need to change to a plain, non-Eyefinity (SLS) profile, so we need to disable any SLS Topologies if they are being used
                    SharedLogger.logger.Trace($"AMDLibrary/SetActiveConfig: SLS is not used in the new display configuration, so we need to set it to disabled if it's configured currently");

                    if (currentDisplayConfig.SlsConfig.IsSlsEnabled)
                    {
                        // We need to disable the current Eyefinity (SLS) profile to turn it off
                        SharedLogger.logger.Trace($"AMDLibrary/SetActiveConfig: SLS is enabled in the current display configuration, so we need to turn it off");

                        foreach (AMD_SLSMAP_CONFIG slsMapConfig in currentDisplayConfig.SlsConfig.SLSMapConfigs)
                        {
                            // Turn on this SLS Map Config
                            ADLRet = ADLImport.ADL2_Display_SLSMapConfig_SetState(_adlContextHandle, slsMapConfig.SLSMap.AdapterIndex, slsMapConfig.SLSMap.SLSMapIndex, ADLImport.ADL_FALSE);
                            if (ADLRet == ADL_STATUS.ADL_OK)
                            {
                                SharedLogger.logger.Trace($"AMDLibrary/SetActiveConfig: ADL2_Display_SLSMapConfig_SetState successfully disabled the SLSMAP with index {slsMapConfig.SLSMap.SLSMapIndex} for adapter { slsMapConfig.SLSMap.AdapterIndex}.");
                            }
                            else
                            {
                                SharedLogger.logger.Error($"AMDLibrary/SetActiveConfig: ERROR - ADL2_Display_SLSMapConfig_SetState returned ADL_STATUS {ADLRet} when trying to set the SLSMAP with index {slsMapConfig.SLSMap.SLSMapIndex} to FALSE for adapter { slsMapConfig.SLSMap.AdapterIndex}.");
                                return false;
                            }
                        }
                    }

                }

                // We want to set the AMD HDR settings now
                // We got through each of the attached displays and set the HDR

                // Go through each of the HDR configs we have
                foreach (var hdrConfig in displayConfig.HdrConfigs)
                {
                    // Try and find the HDR config displays in the list of currently connected displays
                    foreach (var displayInfoItem in currentDisplayConfig.DisplayTargets)
                    {
                        // If we find the HDR config display in the list of currently connected displays then try to set the HDR setting we recorded earlier
                        if (hdrConfig.Key == displayInfoItem.DisplayID.DisplayLogicalIndex)
                        {
                            if (hdrConfig.Value.HDREnabled)
                            {
                                ADLRet = ADLImport.ADL2_Display_HDRState_Set(_adlContextHandle, hdrConfig.Value.AdapterIndex, displayInfoItem.DisplayID, 1);
                                if (ADLRet == ADL_STATUS.ADL_OK)
                                {
                                    SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: ADL2_Display_HDRState_Set was able to turn on HDR for display {displayInfoItem.DisplayID.DisplayLogicalIndex}.");
                                }
                                else
                                {
                                    SharedLogger.logger.Error($"AMDLibrary/GetAMDDisplayConfig: ADL2_Display_HDRState_Set was NOT able to turn on HDR for display {displayInfoItem.DisplayID.DisplayLogicalIndex}.");
                                }
                            }
                            else
                            {
                                ADLRet = ADLImport.ADL2_Display_HDRState_Set(_adlContextHandle, hdrConfig.Value.AdapterIndex, displayInfoItem.DisplayID, 0);
                                if (ADLRet == ADL_STATUS.ADL_OK)
                                {
                                    SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: ADL2_Display_HDRState_Set was able to turn off HDR for display {displayInfoItem.DisplayID.DisplayLogicalIndex}.");
                                }
                                else
                                {
                                    SharedLogger.logger.Error($"AMDLibrary/GetAMDDisplayConfig: ADL2_Display_HDRState_Set was NOT able to turn off HDR for display {displayInfoItem.DisplayID.DisplayLogicalIndex}.");
                                }
                            }                                
                            break;
                        }
                    }
                    
                }

            }
            else
            {
                SharedLogger.logger.Error($"AMDLibrary/SetActiveConfig: ERROR - Tried to run SetActiveConfig but the AMD ADL library isn't initialised!");
                throw new AMDLibraryException($"Tried to run SetActiveConfig but the AMD ADL library isn't initialised!");
            }

            return true;
        }

        public bool IsActiveConfig(AMD_DISPLAY_CONFIG displayConfig)
        {
            // Get the current windows display configs to compare to the one we loaded
            bool allDisplays = false;
            AMD_DISPLAY_CONFIG currentWindowsDisplayConfig = GetAMDDisplayConfig(allDisplays);

            // Check whether the display config is in use now
            SharedLogger.logger.Trace($"AMDLibrary/IsActiveConfig: Checking whether the display configuration is already being used.");
            if (displayConfig.Equals(currentWindowsDisplayConfig))
            {
                SharedLogger.logger.Trace($"AMDLibrary/IsActiveConfig: The display configuration is already being used (supplied displayConfig Equals currentWindowsDisplayConfig)");
                return true;
            }
            else
            {
                SharedLogger.logger.Trace($"AMDLibrary/IsActiveConfig: The display configuration is NOT currently in use (supplied displayConfig Equals currentWindowsDisplayConfig)");
                return false;
            }

        }

        public bool IsValidConfig(AMD_DISPLAY_CONFIG displayConfig)
        {
            // We want to check the AMD Eyefinity (SLS) config is valid
            SharedLogger.logger.Trace($"AMDLibrary/IsValidConfig: Testing whether the display configuration is valid");
            // 
            if (displayConfig.SlsConfig.IsSlsEnabled)
            {

                // ===================================================================================================================================
                // Important! ValidateDisplayGrids does not work at the moment. It errors when supplied with a Grid Topology that works in SetDisplaGrids
                // We therefore cannot use ValidateDisplayGrids to actually validate the config before it's use. We instead need to rely on SetDisplaGrids reporting an
                // error if it is unable to apply the requested configuration. While this works fine, it's not optimal.
                // TODO: Test ValidateDisplayGrids in a future NVIDIA driver release to see if they fixed it.
                // ===================================================================================================================================
                return true;

                /*// Figure out how many Mosaic Grid topoligies there are                    
                uint mosaicGridCount = 0;
                NVAPI_STATUS NVStatus = NVImport.NvAPI_Mosaic_EnumDisplayGrids(ref mosaicGridCount);
                if (NVStatus == NVAPI_STATUS.NVAPI_OK)
                {
                    SharedLogger.logger.Trace($"NVIDIALibrary/GetNVIDIADisplayConfig: NvAPI_Mosaic_GetCurrentTopo returned OK.");
                }
                // Get Current Mosaic Grid settings using the Grid topologies fnumbers we got before
                //NV_MOSAIC_GRID_TOPO_V2[] mosaicGridTopos = new NV_MOSAIC_GRID_TOPO_V2[mosaicGridCount];
                NV_MOSAIC_GRID_TOPO_V1[] mosaicGridTopos = new NV_MOSAIC_GRID_TOPO_V1[mosaicGridCount];
                NVStatus = NVImport.NvAPI_Mosaic_EnumDisplayGrids(ref mosaicGridTopos, ref mosaicGridCount);
                if (NVStatus == NVAPI_STATUS.NVAPI_OK)
                {
                    SharedLogger.logger.Trace($"NVIDIALibrary/GetNVIDIADisplayConfig: NvAPI_Mosaic_GetCurrentTopo returned OK.");
                }
                else if (NVStatus == NVAPI_STATUS.NVAPI_NOT_SUPPORTED)
                {
                    SharedLogger.logger.Warn($"NVIDIALibrary/GetNVIDIADisplayConfig: Mosaic is not supported with the existing hardware. NvAPI_Mosaic_GetCurrentTopo() returned error code {NVStatus}");
                }
                else if (NVStatus == NVAPI_STATUS.NVAPI_INVALID_ARGUMENT)
                {
                    SharedLogger.logger.Warn($"NVIDIALibrary/GetNVIDIADisplayConfig: One or more argumentss passed in are invalid. NvAPI_Mosaic_GetCurrentTopo() returned error code {NVStatus}");
                }
                else if (NVStatus == NVAPI_STATUS.NVAPI_API_NOT_INITIALIZED)
                {
                    SharedLogger.logger.Warn($"NVIDIALibrary/GetNVIDIADisplayConfig: The NvAPI API needs to be initialized first. NvAPI_Mosaic_GetCurrentTopo() returned error code {NVStatus}");
                }
                else if (NVStatus == NVAPI_STATUS.NVAPI_NO_IMPLEMENTATION)
                {
                    SharedLogger.logger.Warn($"NVIDIALibrary/GetNVIDIADisplayConfig: This entry point not available in this NVIDIA Driver. NvAPI_Mosaic_GetCurrentTopo() returned error code {NVStatus}");
                }
                else if (NVStatus == NVAPI_STATUS.NVAPI_ERROR)
                {
                    SharedLogger.logger.Warn($"NVIDIALibrary/GetNVIDIADisplayConfig: A miscellaneous error occurred. NvAPI_Mosaic_GetCurrentTopo() returned error code {NVStatus}");
                }
                else
                {
                    SharedLogger.logger.Trace($"NVIDIALibrary/GetNVIDIADisplayConfig: Some non standard error occurred while getting Mosaic Topology! NvAPI_Mosaic_GetCurrentTopo() returned error code {NVStatus}");
                }
                */

                /*NV_MOSAIC_SETDISPLAYTOPO_FLAGS setTopoFlags = NV_MOSAIC_SETDISPLAYTOPO_FLAGS.NONE;
                bool topoValid = false;
                NV_MOSAIC_DISPLAY_TOPO_STATUS_V1[] topoStatuses = new NV_MOSAIC_DISPLAY_TOPO_STATUS_V1[displayConfig.MosaicConfig.MosaicGridCount];
                NVAPI_STATUS NVStatus = NVImport.NvAPI_Mosaic_ValidateDisplayGrids(setTopoFlags, ref displayConfig.MosaicConfig.MosaicGridTopos, ref topoStatuses, displayConfig.MosaicConfig.MosaicGridCount);
                //NV_MOSAIC_DISPLAY_TOPO_STATUS_V1[] topoStatuses = new NV_MOSAIC_DISPLAY_TOPO_STATUS_V1[mosaicGridCount];
                //NVStatus = NVImport.NvAPI_Mosaic_ValidateDisplayGrids(setTopoFlags, ref mosaicGridTopos, ref topoStatuses, mosaicGridCount);
                if (NVStatus == NVAPI_STATUS.NVAPI_OK)
                {
                    SharedLogger.logger.Trace($"NVIDIALibrary/SetActiveConfig: NvAPI_Mosaic_GetCurrentTopo returned OK.");
                    for (int i = 0; i < topoStatuses.Length; i++)
                    {
                        // If there is an error then we need to log it!
                        // And make it not be used
                        if (topoStatuses[i].ErrorFlags == NV_MOSAIC_DISPLAYCAPS_PROBLEM_FLAGS.OK)
                        {
                            SharedLogger.logger.Trace($"NVIDIALibrary/SetActiveConfig: Congratulations! No error flags for GridTopology #{i}");
                            topoValid = true;
                        }
                        else if (topoStatuses[i].ErrorFlags == NV_MOSAIC_DISPLAYCAPS_PROBLEM_FLAGS.DISPLAY_ON_INVALID_GPU)
                        {
                            SharedLogger.logger.Error($"NVIDIALibrary/SetActiveConfig: Error with the GridTopology #{i}: Display is on an invalid GPU");
                        }
                        else if (topoStatuses[i].ErrorFlags == NV_MOSAIC_DISPLAYCAPS_PROBLEM_FLAGS.DISPLAY_ON_WRONG_CONNECTOR)
                        {
                            SharedLogger.logger.Error($"NVIDIALibrary/SetActiveConfig: Error with the GridTopology #{i}: Display is on the wrong connection. It was on a different connection when the display profile was saved.");
                        }
                        else if (topoStatuses[i].ErrorFlags == NV_MOSAIC_DISPLAYCAPS_PROBLEM_FLAGS.ECC_ENABLED)
                        {
                            SharedLogger.logger.Error($"NVIDIALibrary/SetActiveConfig: Error with the GridTopology #{i}: ECC has been enabled, and Mosaic/Surround doesn't work with ECC");
                        }
                        else if (topoStatuses[i].ErrorFlags == NV_MOSAIC_DISPLAYCAPS_PROBLEM_FLAGS.GPU_TOPOLOGY_NOT_SUPPORTED)
                        {
                            SharedLogger.logger.Error($"NVIDIALibrary/SetActiveConfig: Error with the GridTopology #{i}: This GPU topology is not supported.");
                        }
                        else if (topoStatuses[i].ErrorFlags == NV_MOSAIC_DISPLAYCAPS_PROBLEM_FLAGS.MISMATCHED_OUTPUT_TYPE)
                        {
                            SharedLogger.logger.Error($"NVIDIALibrary/SetActiveConfig: Error with the GridTopology #{i}: The output type has changed for the display. The display was connected through another output type when the display profile was saved.");
                        }
                        else if (topoStatuses[i].ErrorFlags == NV_MOSAIC_DISPLAYCAPS_PROBLEM_FLAGS.NOT_SUPPORTED)
                        {
                            SharedLogger.logger.Error($"NVIDIALibrary/SetActiveConfig: Error with the GridTopology #{i}: This Grid Topology is not supported on this video card.");
                        }
                        else if (topoStatuses[i].ErrorFlags == NV_MOSAIC_DISPLAYCAPS_PROBLEM_FLAGS.NO_COMMON_TIMINGS)
                        {
                            SharedLogger.logger.Error($"NVIDIALibrary/SetActiveConfig: Error with the GridTopology #{i}: Couldn't find common timings that suit all the displays in this Grid Topology.");
                        }
                        else if (topoStatuses[i].ErrorFlags == NV_MOSAIC_DISPLAYCAPS_PROBLEM_FLAGS.NO_DISPLAY_CONNECTED)
                        {
                            SharedLogger.logger.Error($"NVIDIALibrary/SetActiveConfig: Error with the GridTopology #{i}: No display connected.");
                        }
                        else if (topoStatuses[i].ErrorFlags == NV_MOSAIC_DISPLAYCAPS_PROBLEM_FLAGS.NO_EDID_AVAILABLE)
                        {
                            SharedLogger.logger.Error($"NVIDIALibrary/SetActiveConfig: Error with the GridTopology #{i}: Your display didn't provide any information when we attempted to query it. Your display either doesn't support support EDID querying or has it a fault. ");
                        }
                        else if (topoStatuses[i].ErrorFlags == NV_MOSAIC_DISPLAYCAPS_PROBLEM_FLAGS.NO_GPU_TOPOLOGY)
                        {
                            SharedLogger.logger.Error($"NVIDIALibrary/SetActiveConfig: Error with the GridTopology #{i}: There is no GPU topology provided.");
                        }
                        else if (topoStatuses[i].ErrorFlags == NV_MOSAIC_DISPLAYCAPS_PROBLEM_FLAGS.NO_SLI_BRIDGE)
                        {
                            SharedLogger.logger.Error($"NVIDIALibrary/SetActiveConfig: Error with the GridTopology #{i}: There is no SLI bridge, and there was one when the display profile was created.");
                        }
                        // And now we also check to see if there are any warnings we also need to log
                        if (topoStatuses[i].WarningFlags == NV_MOSAIC_DISPLAYTOPO_WARNING_FLAGS.NONE)
                        {
                            SharedLogger.logger.Trace($"NVIDIALibrary/SetActiveConfig: Congratulations! No warning flags for GridTopology #{i}");
                        }
                        else if (topoStatuses[i].WarningFlags == NV_MOSAIC_DISPLAYTOPO_WARNING_FLAGS.DISPLAY_POSITION)
                        {
                            SharedLogger.logger.Warn($"NVIDIALibrary/SetActiveConfig: Warning for the GridTopology #{i}: The display position has changed, and this may affect your display view.");
                        }
                        else if (topoStatuses[i].WarningFlags == NV_MOSAIC_DISPLAYTOPO_WARNING_FLAGS.DRIVER_RELOAD_REQUIRED)
                        {
                            SharedLogger.logger.Warn($"NVIDIALibrary/SetActiveConfig: Warning for the GridTopology #{i}: Your computer needs to be restarted before your NVIDIA device driver can use this Grid Topology.");
                        }
                    }
                }
                else if (NVStatus == NVAPI_STATUS.NVAPI_NOT_SUPPORTED)
                {
                    SharedLogger.logger.Warn($"NVIDIALibrary/SetActiveConfig: Mosaic is not supported with the existing hardware. NvAPI_Mosaic_ValidateDisplayGrids() returned error code {NVStatus}");
                }
                else if (NVStatus == NVAPI_STATUS.NVAPI_NO_ACTIVE_SLI_TOPOLOGY)
                {
                    SharedLogger.logger.Warn($"NVIDIALibrary/SetActiveConfig: No matching GPU topologies could be found. NvAPI_Mosaic_ValidateDisplayGrids() returned error code {NVStatus}");
                }
                else if (NVStatus == NVAPI_STATUS.NVAPI_TOPO_NOT_POSSIBLE)
                {
                    SharedLogger.logger.Warn($"NVIDIALibrary/SetActiveConfig: The topology passed in is not currently possible. NvAPI_Mosaic_ValidateDisplayGrids() returned error code {NVStatus}");
                }
                else if (NVStatus == NVAPI_STATUS.NVAPI_INVALID_ARGUMENT)
                {
                    SharedLogger.logger.Warn($"NVIDIALibrary/SetActiveConfig: One or more argumentss passed in are invalid. NvAPI_Mosaic_ValidateDisplayGrids() returned error code {NVStatus}");
                }
                else if (NVStatus == NVAPI_STATUS.NVAPI_API_NOT_INITIALIZED)
                {
                    SharedLogger.logger.Warn($"NVIDIALibrary/SetActiveConfig: The NvAPI API needs to be initialized first. NvAPI_Mosaic_ValidateDisplayGrids() returned error code {NVStatus}");
                }
                else if (NVStatus == NVAPI_STATUS.NVAPI_NO_IMPLEMENTATION)
                {
                    SharedLogger.logger.Warn($"NVIDIALibrary/SetActiveConfig: This entry point not available in this NVIDIA Driver. NvAPI_Mosaic_ValidateDisplayGrids() returned error code {NVStatus}");
                }
                else if (NVStatus == NVAPI_STATUS.NVAPI_INCOMPATIBLE_STRUCT_VERSION)
                {
                    SharedLogger.logger.Warn($"NVIDIALibrary/SetActiveConfig: The version of the structure passed in is not compatible with this entrypoint. NvAPI_Mosaic_ValidateDisplayGrids() returned error code {NVStatus}");
                }
                else if (NVStatus == NVAPI_STATUS.NVAPI_MODE_CHANGE_FAILED)
                {
                    SharedLogger.logger.Warn($"NVIDIALibrary/SetActiveConfig: There was an error changing the display mode. NvAPI_Mosaic_ValidateDisplayGrids() returned error code {NVStatus}");
                }
                else if (NVStatus == NVAPI_STATUS.NVAPI_ERROR)
                {
                    SharedLogger.logger.Warn($"NVIDIALibrary/SetActiveConfig: A miscellaneous error occurred. NvAPI_Mosaic_ValidateDisplayGrids() returned error code {NVStatus}");
                }
                else
                {
                    SharedLogger.logger.Trace($"NVIDIALibrary/SetActiveConfig: Some non standard error occurred while getting Mosaic Topology! NvAPI_Mosaic_ValidateDisplayGrids() returned error code {NVStatus}");
                }
                // Cancel the screen change if there was an error with anything above this.
                if (topoValid)
                {
                    // If there was an issue then we need to return false
                    // to indicate that the display profile can't be applied
                    SharedLogger.logger.Trace($"NVIDIALibrary/SetActiveConfig: The display settings are valid.");
                    return true;
                }
                else
                {
                    // If there was an issue then we need to return false
                    // to indicate that the display profile can't be applied
                    SharedLogger.logger.Trace($"NVIDIALibrary/SetActiveConfig: There was an error when validating the requested grid topology that prevents us from using the display settings provided. THe display setttings are NOT valid.");
                    return false;
                }*/
            }
            else
            {
                // Its not a Mosaic topology, so we just let it pass, as it's windows settings that matter.
                return true;
            }
        }

        public bool IsPossibleConfig(AMD_DISPLAY_CONFIG displayConfig)
        {
            /*// Get the all possible windows display configs
            AMD_DISPLAY_CONFIG allWindowsDisplayConfig = GetAMDDisplayConfig(QDC.QDC_ALL_PATHS);

            SharedLogger.logger.Trace("AMDLibrary/PatchAdapterIDs: Going through the list of adapters we stored in the config to make sure they still exist");
            // Firstly check that the Adapter Names are still currently available (i.e. the adapter hasn't been replaced).
            foreach (string savedAdapterName in displayConfig.displayAdapters.Values)
            {
                // If there is even one of the saved adapters that has changed, then it's no longer possible
                // to use this display config!
                if (!allWindowsDisplayConfig.displayAdapters.Values.Contains(savedAdapterName))
                {
                    SharedLogger.logger.Error($"AMDLibrary/PatchAdapterIDs: ERROR - Saved adapter {savedAdapterName} is not available right now! This display configuration won't work!");
                    return false;
                }
            }
            SharedLogger.logger.Trace($"AMDLibrary/PatchAdapterIDs: All teh adapters that the display configuration uses are still avilable to use now!");

            // Now we go through the Paths to update the LUIDs as per Soroush's suggestion
            SharedLogger.logger.Trace($"AMDLibrary/IsPossibleConfig: Attemptong to patch the saved display configuration's adapter IDs so that it will still work (these change at each boot)");
            PatchAdapterIDs(ref displayConfig, allWindowsDisplayConfig.displayAdapters);

            SharedLogger.logger.Trace($"AMDLibrary/IsPossibleConfig: Testing whether the display configuration is valid ");
            // Test whether a specified display configuration is supported on the computer                    
            uint myPathsCount = (uint)displayConfig.displayConfigPaths.Length;
            uint myModesCount = (uint)displayConfig.displayConfigModes.Length;
            WIN32STATUS err = CCDImport.SetDisplayConfig(myPathsCount, displayConfig.displayConfigPaths, myModesCount, displayConfig.displayConfigModes, SDC.DISPLAYMAGICIAN_VALIDATE);
            if (err == WIN32STATUS.ERROR_SUCCESS)
            {
                SharedLogger.logger.Trace($"AMDLibrary/IsPossibleConfig: SetDisplayConfig validated that the display configuration is valid and can be used!");
                return true;
            }
            else
            {
                SharedLogger.logger.Trace($"AMDLibrary/IsPossibleConfig: SetDisplayConfig confirmed that the display configuration is invalid and cannot be used!");
                return false;
            }*/
            return true;
        }

        public List<string> GetCurrentDisplayIdentifiers()
        {
            SharedLogger.logger.Trace($"AMDLibrary/GetCurrentDisplayIdentifiers: Getting the current display identifiers for the displays in use now");
            bool allDisplays = false;
            return GetSomeDisplayIdentifiers(allDisplays);
        }

        public List<string> GetAllConnectedDisplayIdentifiers()
        {
            SharedLogger.logger.Trace($"AMDLibrary/GetAllConnectedDisplayIdentifiers: Getting all the display identifiers that can possibly be used");
            bool allDisplays = true;
            return GetSomeDisplayIdentifiers(allDisplays);            
        }

        private List<string> GetSomeDisplayIdentifiers(bool allDisplays = false)
        {
            SharedLogger.logger.Debug($"AMDLibrary/GetSomeDisplayIdentifiers: Generating unique Display Identifiers");

            List<string> displayIdentifiers = new List<string>();

            if (_initialised)
            {
                // Get the number of AMD adapters that the OS knows about
                int numAdapters = 0;
                ADL_STATUS ADLRet = ADLImport.ADL2_Adapter_NumberOfAdapters_Get(_adlContextHandle, out numAdapters);
                if (ADLRet == ADL_STATUS.ADL_OK)
                {
                    SharedLogger.logger.Trace($"AMDLibrary/GetSomeDisplayIdentifiers: ADL2_Adapter_NumberOfAdapters_Get returned the number of AMD Adapters the OS knows about ({numAdapters}).");
                }
                else
                {
                    SharedLogger.logger.Error($"AMDLibrary/GetSomeDisplayIdentifiers: ERROR - ADL2_Adapter_NumberOfAdapters_Get returned ADL_STATUS {ADLRet} when trying to get number of AMD adapters in the computer.");
                    throw new AMDLibraryException($"GetSomeDisplayIdentifiers returned ADL_STATUS {ADLRet} when trying to get number of AMD adapters in the computer");
                }

                // Figure out primary adapter
                int primaryAdapterIndex = 0;
                ADLRet = ADLImport.ADL2_Adapter_Primary_Get(_adlContextHandle, out primaryAdapterIndex);
                if (ADLRet == ADL_STATUS.ADL_OK)
                {
                    SharedLogger.logger.Trace($"AMDLibrary/ADL2_Adapter_Primary_Get: The primary adapter has index {primaryAdapterIndex}.");
                }
                else
                {
                    SharedLogger.logger.Error($"AMDLibrary/GetSomeDisplayIdentifiers: ERROR - ADL2_Adapter_Primary_Get returned ADL_STATUS {ADLRet} when trying to get the primary adapter info from all the AMD adapters in the computer.");
                    throw new AMDLibraryException($"GetSomeDisplayIdentifiers returned ADL_STATUS {ADLRet} when trying to get the adapter info from all the AMD adapters in the computer");
                }

                // Now go through each adapter and get the information we need from it
                for (int adapterIndex = 0; adapterIndex < numAdapters; adapterIndex++)
                {
                    // Skip this adapter if it isn't active
                    int adapterActiveStatus = ADLImport.ADL_FALSE;
                    ADLRet = ADLImport.ADL2_Adapter_Active_Get(_adlContextHandle, adapterIndex, out adapterActiveStatus);
                    if (ADLRet == ADL_STATUS.ADL_OK)
                    {
                        if (adapterActiveStatus == ADLImport.ADL_TRUE)
                        {
                            SharedLogger.logger.Trace($"AMDLibrary/GetSomeDisplayIdentifiers: ADL2_Adapter_Active_Get returned ADL_TRUE - AMD Adapter #{adapterIndex} is active! We can continue.");
                        }
                        else
                        {
                            SharedLogger.logger.Trace($"AMDLibrary/GetSomeDisplayIdentifiers: ADL2_Adapter_Active_Get returned ADL_FALSE - AMD Adapter #{adapterIndex} is NOT active, so skipping.");
                            continue;
                        }
                    }
                    else
                    {
                        SharedLogger.logger.Warn($"AMDLibrary/GetSomeDisplayIdentifiers: WARNING - ADL2_Adapter_Active_Get returned ADL_STATUS {ADLRet} when trying to see if AMD Adapter #{adapterIndex} is active. Trying to skip this adapter so something at least works.");
                        continue;
                    }

                    // Get the Adapter info for this adapter and put it in the AdapterBuffer
                    SharedLogger.logger.Trace($"AMDLibrary/GetSomeDisplayIdentifiers: Running ADL2_Adapter_AdapterInfoX4_Get to get the information about AMD Adapter #{adapterIndex}.");
                    int numAdaptersInfo = 0;
                    IntPtr adapterInfoBuffer = IntPtr.Zero;
                    ADLRet = ADLImport.ADL2_Adapter_AdapterInfoX4_Get(_adlContextHandle, adapterIndex, out numAdaptersInfo, out adapterInfoBuffer);
                    if (ADLRet == ADL_STATUS.ADL_OK)
                    {
                        SharedLogger.logger.Trace($"AMDLibrary/GetSomeDisplayIdentifiers: ADL2_Adapter_AdapterInfoX4_Get returned information about AMD Adapter #{adapterIndex}.");
                    }
                    else
                    {
                        SharedLogger.logger.Error($"AMDLibrary/GetSomeDisplayIdentifiers: ERROR - ADL2_Adapter_AdapterInfoX4_Get returned ADL_STATUS {ADLRet} when trying to get the adapter info from AMD Adapter #{adapterIndex}. Trying to skip this adapter so something at least works.");
                        continue;
                    }

                    ADL_ADAPTER_INFOX2[] adapterArray = new ADL_ADAPTER_INFOX2[numAdaptersInfo];
                    if (numAdaptersInfo > 0)
                    {
                        IntPtr currentDisplayTargetBuffer = adapterInfoBuffer;
                        for (int i = 0; i < numAdaptersInfo; i++)
                        {
                            // build a structure in the array slot
                            adapterArray[i] = new ADL_ADAPTER_INFOX2();
                            // fill the array slot structure with the data from the buffer
                            adapterArray[i] = (ADL_ADAPTER_INFOX2)Marshal.PtrToStructure(currentDisplayTargetBuffer, typeof(ADL_ADAPTER_INFOX2));
                            // destroy the bit of memory we no longer need
                            //Marshal.DestroyStructure(currentDisplayTargetBuffer, typeof(ADL_ADAPTER_INFOX2));
                            // advance the buffer forwards to the next object
                            currentDisplayTargetBuffer = (IntPtr)((long)currentDisplayTargetBuffer + Marshal.SizeOf(adapterArray[i]));
                        }
                        // Free the memory used by the buffer                        
                        Marshal.FreeCoTaskMem(adapterInfoBuffer);
                    }

                    SharedLogger.logger.Trace($"AMDLibrary/GetSomeDisplayIdentifiers: Converted ADL2_Adapter_AdapterInfoX4_Get memory buffer into a {adapterArray.Length} long array about AMD Adapter #{adapterIndex}.");

                    AMD_ADAPTER_CONFIG savedAdapterConfig = new AMD_ADAPTER_CONFIG();
                    ADL_ADAPTER_INFOX2 oneAdapter = adapterArray[0];
                    if (oneAdapter.Exist != 1)
                    {
                        SharedLogger.logger.Trace($"AMDLibrary/GetSomeDisplayIdentifiers: AMD Adapter #{oneAdapter.AdapterIndex.ToString()} doesn't exist at present so skipping detection for this adapter.");
                        continue;
                    }

                    // Only skip non-present displays if we want all displays information
                    if (allDisplays && oneAdapter.Present != 1)
                    {
                        SharedLogger.logger.Trace($"AMDLibrary/GetSomeDisplayIdentifiers: AMD Adapter #{oneAdapter.AdapterIndex.ToString()} isn't enabled at present so skipping detection for this adapter.");
                        continue;
                    }

                    // Now we still try to get the information we need for the Display Identifiers
                    // Go grab the DisplayMaps and DisplayTargets as that is useful infor for creating screens
                    int numDisplayTargets = 0;
                    int numDisplayMaps = 0;
                    IntPtr displayTargetBuffer = IntPtr.Zero;
                    IntPtr displayMapBuffer = IntPtr.Zero;
                    ADLRet = ADLImport.ADL2_Display_DisplayMapConfig_Get(_adlContextHandle, adapterIndex, out numDisplayMaps, out displayMapBuffer, out numDisplayTargets, out displayTargetBuffer, ADLImport.ADL_DISPLAY_DISPLAYMAP_OPTION_GPUINFO);
                    if (ADLRet == ADL_STATUS.ADL_OK)
                    {
                        SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: ADL2_Display_DisplayMapConfig_Get returned information about all displaytargets connected to AMD adapter {adapterIndex}.");
                    }
                    else
                    {
                        SharedLogger.logger.Error($"AMDLibrary/GetAMDDisplayConfig: ERROR - ADL2_Display_DisplayMapConfig_Get returned ADL_STATUS {ADLRet} when trying to get the display target info from AMD adapter {adapterIndex} in the computer.");
                        continue;
                    }                   

                    ADL_DISPLAY_TARGET[] displayTargetArray = { };
                    if (numDisplayTargets > 0)
                    {
                        IntPtr currentDisplayTargetBuffer = displayTargetBuffer;
                        //displayTargetArray = new ADL_DISPLAY_TARGET[numDisplayTargets];
                        displayTargetArray = new ADL_DISPLAY_TARGET[numDisplayTargets];
                        for (int i = 0; i < numDisplayTargets; i++)
                        {
                            // build a structure in the array slot
                            displayTargetArray[i] = new ADL_DISPLAY_TARGET();
                            //displayTargetArray[i] = new ADL_DISPLAY_TARGET();
                            // fill the array slot structure with the data from the buffer
                            displayTargetArray[i] = (ADL_DISPLAY_TARGET)Marshal.PtrToStructure(currentDisplayTargetBuffer, typeof(ADL_DISPLAY_TARGET));
                            //displayTargetArray[i] = (ADL_DISPLAY_TARGET)Marshal.PtrToStructure(currentDisplayTargetBuffer, typeof(ADL_DISPLAY_TARGET));
                            // destroy the bit of memory we no longer need
                            Marshal.DestroyStructure(currentDisplayTargetBuffer, typeof(ADL_DISPLAY_TARGET));
                            // advance the buffer forwards to the next object
                            currentDisplayTargetBuffer = (IntPtr)((long)currentDisplayTargetBuffer + Marshal.SizeOf(displayTargetArray[i]));
                            //currentDisplayTargetBuffer = (IntPtr)((long)currentDisplayTargetBuffer + Marshal.SizeOf(displayTargetArray[i]));

                        }
                        // Free the memory used by the buffer                        
                        Marshal.FreeCoTaskMem(displayTargetBuffer);
                    }

                    int forceDetect = 0;
                    int numDisplays;
                    IntPtr displayInfoBuffer;                    
                    ADLRet = ADLImport.ADL2_Display_DisplayInfo_Get(_adlContextHandle, adapterIndex, out numDisplays, out displayInfoBuffer, forceDetect);
                    if (ADLRet == ADL_STATUS.ADL_OK)
                    {
                        SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: ADL2_Display_DisplayInfo_Get returned information about all displaytargets connected to AMD adapter {adapterIndex}.");
                    }
                    else if (ADLRet == ADL_STATUS.ADL_ERR_NULL_POINTER || ADLRet == ADL_STATUS.ADL_ERR_NOT_SUPPORTED)
                    {
                        SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: ADL2_Display_DisplayInfo_Get returned ADL_ERR_NULL_POINTER so skipping getting display info from this AMD adapter {adapterIndex}.");
                        continue;
                    }
                    else
                    {
                        SharedLogger.logger.Error($"AMDLibrary/GetAMDDisplayConfig: ERROR - ADL2_Display_DisplayInfo_Get returned ADL_STATUS {ADLRet} when trying to get the display target info from AMD adapter {adapterIndex} in the computer.");
                    }

                    ADL_DISPLAY_INFO[] displayInfoArray = { };
                    if (numDisplays > 0)
                    {
                        IntPtr currentDisplayInfoBuffer = displayInfoBuffer;
                        displayInfoArray = new ADL_DISPLAY_INFO[numDisplays];
                        for (int i = 0; i < numDisplays; i++)
                        {
                            // build a structure in the array slot
                            displayInfoArray[i] = new ADL_DISPLAY_INFO();
                            // fill the array slot structure with the data from the buffer
                            displayInfoArray[i] = (ADL_DISPLAY_INFO)Marshal.PtrToStructure(currentDisplayInfoBuffer, typeof(ADL_DISPLAY_INFO));
                            // destroy the bit of memory we no longer need
                            Marshal.DestroyStructure(currentDisplayInfoBuffer, typeof(ADL_DISPLAY_INFO));
                            // advance the buffer forwards to the next object
                            currentDisplayInfoBuffer = (IntPtr)((long)currentDisplayInfoBuffer + Marshal.SizeOf(displayInfoArray[i]));
                            //currentDisplayTargetBuffer = (IntPtr)((long)currentDisplayTargetBuffer + Marshal.SizeOf(displayTargetArray[i]));

                        }
                        // Free the memory used by the buffer                        
                        Marshal.FreeCoTaskMem(displayInfoBuffer);
                    }


                    // Now we need to get all the displays connected to this adapter so that we can get their HDR state
                    foreach (var displayInfoItem in displayInfoArray)
                    {

                        // Ignore the display if it isn't connected (note: we still need to see if it's actively mapped to windows!)
                        if (!displayInfoItem.DisplayConnectedSet)
                        {
                            continue;
                        }

                        // If the display is not mapped in windows then we only want to skip this display if all alldisplays is false
                        if (!displayInfoItem.DisplayMappedSet && !allDisplays)
                        {
                            continue;
                        }

                        // Create an array of all the important display info we need to create the display identifier
                        List<string> displayInfo = new List<string>();
                        displayInfo.Add("AMD");
                        try
                        {
                            displayInfo.Add(oneAdapter.DeviceNumber.ToString());
                        }
                        catch (Exception ex)
                        {
                            SharedLogger.logger.Warn(ex, $"AMDLibrary/GetSomeDisplayIdentifiers: Exception getting AMD Adapter Device Number from video card. Substituting with a # instead");
                            displayInfo.Add("#");
                        }
                        try
                        {
                            displayInfo.Add(oneAdapter.AdapterName);
                        }
                        catch (Exception ex)
                        {
                            SharedLogger.logger.Warn(ex, $"AMDLibrary/GetSomeDisplayIdentifiers: Exception getting AMD Adapter Name from video card. Substituting with a # instead");
                            displayInfo.Add("#");
                        }
                        try
                        {
                            displayInfo.Add(displayInfoItem.DisplayConnector.ToString("G"));
                        }
                        catch (Exception ex)
                        {
                            SharedLogger.logger.Warn(ex, $"AMDLibrary/GetSomeDisplayIdentifiers: Exception getting Display Connector from video card. Substituting with a # instead");
                            displayInfo.Add("#");
                        }

                        // Get some more Display Info (if we can!)
                        ADL_DDC_INFO2 ddcInfo = new ADL_DDC_INFO2();
                        ADLRet = ADLImport.ADL2_Display_DDCInfo2_Get(_adlContextHandle, adapterIndex, displayInfoItem.DisplayID.DisplayLogicalIndex, out ddcInfo);
                        if (ADLRet == ADL_STATUS.ADL_OK)
                        {                            
                            SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: ADL2_Display_DDCInfo2_Get returned information about DDC Information for display {displayInfoItem.DisplayID.DisplayLogicalIndex} connected to AMD adapter {adapterIndex}.");
                            if (ddcInfo.SupportsDDC == 1)
                            {
                                // The display supports DDC and returned some data!
                                SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: ADL2_Display_DDCInfo2_Get returned information about DDC Information for display {displayInfoItem.DisplayID.DisplayLogicalIndex} connected to AMD adapter {adapterIndex}.");

                                try
                                {
                                    displayInfo.Add(ddcInfo.ManufacturerID.ToString());
                                }
                                catch (Exception ex)
                                {
                                    SharedLogger.logger.Warn(ex, $"AMDLibrary/GetSomeDisplayIdentifiers: Exception getting AMD Display EDID Manufacturer Code from video card. Substituting with a # instead");
                                    displayInfo.Add("#");
                                }
                                try
                                {
                                    displayInfo.Add(ddcInfo.ProductID.ToString());
                                }
                                catch (Exception ex)
                                {
                                    SharedLogger.logger.Warn(ex, $"AMDLibrary/GetSomeDisplayIdentifiers: Exception getting AMD Display EDID Product Code from video card. Substituting with a # instead");
                                    displayInfo.Add("#");
                                }
                                try
                                {
                                    displayInfo.Add(ddcInfo.DisplayName.ToString());
                                }
                                catch (Exception ex)
                                {
                                    SharedLogger.logger.Warn(ex, $"AMDLibrary/GetSomeDisplayIdentifiers: Exception getting AMD Display Name from video card. Substituting with a # instead");
                                    displayInfo.Add("#");
                                }
                            }
                            else
                            {
                                // The display does NOT support DDC and nothing was returned! We need to find the information some other way!

                                try
                                {
                                    displayInfo.Add(displayInfoItem.DisplayManufacturerName.ToString());
                                }
                                catch (Exception ex)
                                {
                                    SharedLogger.logger.Warn(ex, $"AMDLibrary/GetSomeDisplayIdentifiers: Exception getting AMD Display Manufacturer Name 2 from video card. Substituting with a # instead");
                                    displayInfo.Add("#");
                                }
                                try
                                {
                                    displayInfo.Add(displayInfoItem.DisplayName.ToString());
                                }
                                catch (Exception ex)
                                {
                                    SharedLogger.logger.Warn(ex, $"AMDLibrary/GetSomeDisplayIdentifiers: Exception getting AMD Display Name 2 from video card. Substituting with a # instead");
                                    displayInfo.Add("#");
                                }
                            }
                        }
                        else
                        {
                            SharedLogger.logger.Error($"AMDLibrary/GetAMDDisplayConfig: ERROR - ADL2_Display_DDCInfo2_Get returned ADL_STATUS {ADLRet} when trying to get the display target info from AMD adapter {adapterIndex} in the computer.");

                            // ADL2_Display_DDCInfo2_Get had a problem and nothing was returned! We need to find the information some other way!

                            try
                            {
                                displayInfo.Add(displayInfoItem.DisplayManufacturerName.ToString());
                            }
                            catch (Exception ex)
                            {
                                SharedLogger.logger.Warn(ex, $"AMDLibrary/GetSomeDisplayIdentifiers: Exception getting AMD Display Manufacturer Name 2 from video card. Substituting with a # instead");
                                displayInfo.Add("#");
                            }
                            try
                            {
                                displayInfo.Add(displayInfoItem.DisplayName.ToString());
                            }
                            catch (Exception ex)
                            {
                                SharedLogger.logger.Warn(ex, $"AMDLibrary/GetSomeDisplayIdentifiers: Exception getting AMD Display Name 2 from video card. Substituting with a # instead");
                                displayInfo.Add("#");
                            }
                        }

                        

                        // Create a display identifier out of it
                        string displayIdentifier = String.Join("|", displayInfo);
                        // Add it to the list of display identifiers so we can return it
                        // but only add it if it doesn't already exist. Otherwise we get duplicates :/
                        if (!displayIdentifiers.Contains(displayIdentifier))
                        {
                            displayIdentifiers.Add(displayIdentifier);
                            SharedLogger.logger.Debug($"ProfileRepository/GenerateProfileDisplayIdentifiers: DisplayIdentifier: {displayIdentifier}");
                        }
                    }
                }
            }
            else
            {
                SharedLogger.logger.Error($"AMDLibrary/GetSomeDisplayIdentifiers: ERROR - Tried to run GetSomeDisplayIdentifiers but the AMD ADL library isn't initialised!");
                throw new AMDLibraryException($"Tried to run GetSomeDisplayIdentifiers but the AMD ADL library isn't initialised!");
            }


            // Sort the display identifiers
            displayIdentifiers.Sort();

            return displayIdentifiers;
        }

    }

    [global::System.Serializable]
    public class AMDLibraryException : Exception
    {
        public AMDLibraryException() { }
        public AMDLibraryException(string message) : base(message) { }
        public AMDLibraryException(string message, Exception inner) : base(message, inner) { }
        protected AMDLibraryException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
