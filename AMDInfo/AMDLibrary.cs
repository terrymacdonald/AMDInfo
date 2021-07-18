﻿using System;
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
        public ADL_DISPLAY_MAP[] DisplayMaps;
        public ADL_DISPLAY_TARGET[] DisplayTargets;
        public int SLSMapIndex;
        public bool IsSLSEnabled;
        public ADL_SLS_MAP[] SLSMap;

        public bool Equals(AMD_ADAPTER_CONFIG other)
        => AdapterIndex == other.AdapterIndex &&
           AdapterBusNumber == other.AdapterBusNumber &&
           AdapterDeviceNumber == other.AdapterDeviceNumber &&
           DisplayMaps.SequenceEqual(other.DisplayMaps) &&
           DisplayTargets.SequenceEqual(other.DisplayTargets);
        public override int GetHashCode()
        {
            return (AdapterIndex, AdapterBusNumber, AdapterDeviceNumber, DisplayMaps, DisplayTargets).GetHashCode();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AMD_DISPLAY_CONFIG : IEquatable<AMD_DISPLAY_CONFIG>
    {
        //public Dictionary<ulong, string> DisplayAdapters;
        public List<AMD_ADAPTER_CONFIG> AdapterConfigs;
        //public DISPLAYCONFIG_MODE_INFO[] DisplayConfigModes;
        //public ADVANCED_HDR_INFO_PER_PATH[] DisplayHDRStates;
        public WINDOWS_DISPLAY_CONFIG WindowsDisplayConfig;

        public bool Equals(AMD_DISPLAY_CONFIG other)
        => AdapterConfigs.SequenceEqual(other.AdapterConfigs)  && 
           //DisplayConfigPaths.SequenceEqual(other.DisplayConfigPaths) &&
           //DisplayConfigModes.SequenceEqual(other.DisplayConfigModes) &&
           //DisplayHDRStates.SequenceEqual(other.DisplayHDRStates) && 
           WindowsDisplayConfig.Equals(other.WindowsDisplayConfig);

        public override int GetHashCode()
        {
            return (AdapterConfigs, WindowsDisplayConfig).GetHashCode();
        }
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
            SharedLogger.logger.Trace("AMDLibrary/AMDLibrary: Intialising AMD ADL2 library interface");
            // Second parameter is 1 so that we only the get connected adapters in use now
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
            catch(Exception ex)
            {
                SharedLogger.logger.Trace(ex, $"AMDLibrary/AMDLibrary: Exception intialising AMD ADL2 library. ADL2_Main_Control_Create() caused an exception.");
            }

            _winLibrary = WinLibrary.GetLibrary();

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

            if (_initialised)
            {
                // Get the list of AMD adapters that the OS knows about
                // Note - this returns past and presenta adapters, so we need to filter for the active ones.               
                // Get the Adapter info and put it in the AdapterBuffer
                SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: Running ADL2_Adapter_AdapterInfoX4_Get to find all known AMD adapters.");
                //ADLRet = ADL.ADL2_Adapter_AdapterInfoX4_Get(_adlContextHandle, AdapterBuffer, size);
                int numAdapters = 0;
                IntPtr adapterBuffer = IntPtr.Zero;               
                ADL_STATUS ADLRet = ADLImport.ADL2_Adapter_AdapterInfoX4_Get(_adlContextHandle, ADLImport.ADL_ADAPTER_INDEX_ALL, out numAdapters, out adapterBuffer);
                if (ADLRet == ADL_STATUS.ADL_OK)
                {
                    SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: ADL2_Adapter_AdapterInfoX4_Get returned information about all adapters the OS knows about.");
                }
                else
                {
                    SharedLogger.logger.Error($"AMDLibrary/GetAMDDisplayConfig: ERROR - ADL2_Adapter_AdapterInfoX4_Get returned ADL_STATUS {ADLRet} when trying to get the adapter info from all the AMD adapters in the computer.");
                    throw new AMDLibraryException($"ADL2_Adapter_AdapterInfoX4_Get returned ADL_STATUS {ADLRet} when trying to get the adapter info from all the AMD adapters in the computer");
                }

                SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: Successfully run ADL2_Adapter_AdapterInfoX4_Get to find information about all known AMD adapters.");

                ADL_ADAPTER_INFOX2[] adapterArray = new ADL_ADAPTER_INFOX2[numAdapters];
                if (numAdapters > 0)
                {
                    IntPtr currentDisplayTargetBuffer = adapterBuffer;                    
                    for (int i = 0; i < numAdapters; i++)
                    {
                        // build a structure in the array slot
                        adapterArray[i] = new ADL_ADAPTER_INFOX2();
                        // fill the array slot structure with the data from the buffer
                        adapterArray[i] = (ADL_ADAPTER_INFOX2)Marshal.PtrToStructure(currentDisplayTargetBuffer, typeof(ADL_ADAPTER_INFOX2));
                        // destroy the bit of memory we no longer need
                        Marshal.DestroyStructure(currentDisplayTargetBuffer, typeof(ADL_ADAPTER_INFOX2));
                        // advance the buffer forwards to the next object
                        currentDisplayTargetBuffer = (IntPtr)((long)currentDisplayTargetBuffer + Marshal.SizeOf(adapterArray[i]));
                    }
                    // Free the memory used by the buffer                        
                    Marshal.FreeCoTaskMem(adapterBuffer);
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
                    SharedLogger.logger.Error($"AMDLibrary/ADL2_Adapter_Primary_Get: ERROR - ADL2_Adapter_Primary_Get returned ADL_STATUS {ADLRet} when trying to get the primary adapter info from all the AMD adapters in the computer.");
                    throw new AMDLibraryException($"ADL2_Adapter_Primary_Get returned ADL_STATUS {ADLRet} when trying to get the adapter info from all the AMD adapters in the computer");
                }

                // Now go through each adapter and get the information we need from it
                for (int adapterNum = 0; adapterNum < numAdapters; adapterNum++)
                {
                    AMD_ADAPTER_CONFIG savedAdapterConfig = new AMD_ADAPTER_CONFIG();
                    //oneAdapter = (ADL_ADAPTER_INFOX2)Marshal.PtrToStructure(new IntPtr(adapterBuffer.ToInt64() + (adapterNum * Marshal.SizeOf(oneAdapter))), oneAdapter.GetType());
                    ADL_ADAPTER_INFOX2 oneAdapter = adapterArray[adapterNum];

                    if (oneAdapter.Exist != 1)
                    {
                        SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: AMD Adapter #{oneAdapter.AdapterIndex.ToString()} doesn't exist at present so skipping detection for this adapter.");
                        continue;
                    }

                    // Only skip non-present displays if we want all displays information
                    if (allDisplays && oneAdapter.Present != 1)
                    {
                        SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: AMD Adapter #{oneAdapter.AdapterIndex.ToString()} isn't enabled at present so skipping detection for this adapter.");
                        continue;
                    }


                    savedAdapterConfig.AdapterBusNumber = oneAdapter.BusNumber;
                    savedAdapterConfig.AdapterDeviceNumber = oneAdapter.DeviceNumber;
                    savedAdapterConfig.AdapterIndex = oneAdapter.AdapterIndex;
                    if (oneAdapter.AdapterIndex == primaryAdapterIndex)
                    {
                        savedAdapterConfig.IsPrimaryAdapter = true;
                    }
                    else
                    {
                        savedAdapterConfig.IsPrimaryAdapter = false;
                    }


                    // Go grab the DisplayMaps and DisplayTargets
                    int numDisplayTargets = 0;
                    int numDisplayMaps = 0;
                    IntPtr displayTargetBuffer = IntPtr.Zero;
                    IntPtr displayMapBuffer = IntPtr.Zero;
                    ADL_DISPLAY_MAP[] displayMapArray;
                    ADL_DISPLAY_TARGET[] displayTargetArray;
                    ADLRet = ADLImport.ADL2_Display_DisplayMapConfig_Get(_adlContextHandle, adapterNum, out numDisplayMaps, out displayMapBuffer, out numDisplayTargets, out displayTargetBuffer, ADLImport.ADL_DISPLAY_DISPLAYMAP_OPTION_GPUINFO);
                    if (ADLRet == ADL_STATUS.ADL_OK)
                    {
                        SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: ADL2_Display_DisplayMapConfig_Get returned information about all displaytargets connected to AMD adapter {adapterNum}.");
                    }
                    else
                    {
                        SharedLogger.logger.Error($"AMDLibrary/GetAMDDisplayConfig: ERROR - ADL2_Display_DisplayMapConfig_Get returned ADL_STATUS {ADLRet} when trying to get the display target info from AMD adapter {adapterNum} in the computer.");
                        throw new AMDLibraryException($"ADL2_Display_DisplayMapConfig_Get returned ADL_STATUS {ADLRet} when trying to get the display target info from AMD adapter {adapterNum} in the computer");
                    }                    

                    if (numDisplayMaps > 0)
                    {
                        IntPtr currentDisplayMapBuffer = displayMapBuffer;
                        savedAdapterConfig.DisplayMaps = new ADL_DISPLAY_MAP[numDisplayMaps];
                        for (int i = 0; i < numDisplayMaps; i++)
                        {
                            // build a structure in the array slot
                            savedAdapterConfig.DisplayMaps[i] = new ADL_DISPLAY_MAP();
                            // fill the array slot structure with the data from the buffer
                            savedAdapterConfig.DisplayMaps[i] = (ADL_DISPLAY_MAP)Marshal.PtrToStructure(currentDisplayMapBuffer, typeof(ADL_DISPLAY_MAP));
                            // destroy the bit of memory we no longer need
                            Marshal.DestroyStructure(currentDisplayMapBuffer, typeof(ADL_DISPLAY_MAP));
                            // advance the buffer forwards to the next object
                            currentDisplayMapBuffer = (IntPtr)((long)currentDisplayMapBuffer + Marshal.SizeOf(savedAdapterConfig.DisplayMaps[i]));
                        }
                        // Free the memory used by the buffer                        
                        Marshal.FreeCoTaskMem(displayMapBuffer);
                        // Save the item
                        //savedAdapterConfig.DisplayMaps = displayMapArray;

                    }
                    if (numDisplayTargets > 0)
                    {
                        IntPtr currentDisplayTargetBuffer = displayTargetBuffer;
                        //displayTargetArray = new ADL_DISPLAY_TARGET[numDisplayTargets];
                        savedAdapterConfig.DisplayTargets = new ADL_DISPLAY_TARGET[numDisplayTargets];
                        for (int i = 0; i < numDisplayTargets; i++)
                        {
                            // build a structure in the array slot
                            savedAdapterConfig.DisplayTargets[i] = new ADL_DISPLAY_TARGET();
                            //displayTargetArray[i] = new ADL_DISPLAY_TARGET();
                            // fill the array slot structure with the data from the buffer
                            savedAdapterConfig.DisplayTargets[i] = (ADL_DISPLAY_TARGET)Marshal.PtrToStructure(currentDisplayTargetBuffer, typeof(ADL_DISPLAY_TARGET));
                            //displayTargetArray[i] = (ADL_DISPLAY_TARGET)Marshal.PtrToStructure(currentDisplayTargetBuffer, typeof(ADL_DISPLAY_TARGET));
                            // destroy the bit of memory we no longer need
                            Marshal.DestroyStructure(currentDisplayTargetBuffer, typeof(ADL_DISPLAY_TARGET));
                            // advance the buffer forwards to the next object
                            currentDisplayTargetBuffer = (IntPtr)((long)currentDisplayTargetBuffer + Marshal.SizeOf(savedAdapterConfig.DisplayTargets[i]));
                            //currentDisplayTargetBuffer = (IntPtr)((long)currentDisplayTargetBuffer + Marshal.SizeOf(displayTargetArray[i]));

                        }
                        // Free the memory used by the buffer                        
                        Marshal.FreeCoTaskMem(displayTargetBuffer);
                        // Save the item                            
                        //savedAdapterConfig.DisplayTargets = new ADL_DISPLAY_TARGET[numDisplayTargets];
                        //savedAdapterConfig.DisplayTargets = displayTargetArray;
                    }

                    // Only check for SLS if there is more than one displaytarget (screen)
                    if (numDisplayTargets > 1)
                    {
                        int SLSMapIndex = -1;
                        int numSLSMapIDs = -1;
                        IntPtr SLSMapIDBuffer = IntPtr.Zero;
                        ADL_DISPLAY_TARGET[] displayTargets = savedAdapterConfig.DisplayTargets;
                        ADLRet = ADLImport.ADL2_Display_SLSMapIndex_Get(_adlContextHandle, adapterNum, numDisplayTargets, displayTargets, out SLSMapIndex);
                        //ADLRet = ADLImport.ADL2_Display_SLSMapIndexList_Get(_adlContextHandle, adapterNum, ref numSLSMapIDs, out SLSMapIDBuffer, ADLImport.ADL_DISPLAY_SLSMAPINDEXLIST_OPTION_ACTIVE);
                        if (ADLRet == ADL_STATUS.ADL_OK)
                        {
                            SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: SLS (Eyfinity) is enabled on AMD adapter {adapterNum}.");

                            // Set the SLS Map Index if there is Eyefinity
                            savedAdapterConfig.IsSLSEnabled = true;
                            //savedAdapterConfig.SLSMapIndex = SLSMapIndex;

                            /*int[] SLSMapIDArray = new int[numSLSMapIDs];
                            if (numSLSMapIDs > 0)
                            {
                                IntPtr currentSLSMapsBuffer = SLSMapIDBuffer;
                                SLSMapIDArray = new int[numSLSMapIDs];
                                for (int i = 0; i < numSLSMapIDs; i++)
                                {
                                    // build a structure in the array slot
                                    SLSMapIDArray[i] = 0;
                                    // fill the array slot structure with the data from the buffer
                                    SLSMapIDArray[i] = (int)Marshal.PtrToStructure(currentSLSMapsBuffer, typeof(int));
                                    // destroy the bit of memory we no longer need
                                    Marshal.DestroyStructure(currentSLSMapsBuffer, typeof(ADL_SLS_MAP));
                                    // advance the buffer forwards to the next object
                                    currentSLSMapsBuffer = (IntPtr)((long)currentSLSMapsBuffer + Marshal.SizeOf(SLSMapIDArray[i]));
                                }
                                // Free the memory used by the buffer                        
                                Marshal.FreeCoTaskMem(SLSMapIDBuffer);
                            }
                            */
                            /*for (int slsMapIdx = 0; slsMapIdx < SLSMapIndex; slsMapIdx++)
                            {*/
                            //bool isActiveSLS = false;
                            // TODO Get the SLS Map Config X2??

                            int numSLSTargets = 0;
                            IntPtr SLSTargetBuffer = IntPtr.Zero;
                            int numNativeMode = 0;
                            IntPtr nativeModeBuffer = IntPtr.Zero;
                            int numNativeModeOffsets = 0;
                            IntPtr nativeModeOffsetsBuffer = IntPtr.Zero;
                            int numBezelMode = 0;
                            IntPtr bezelModeBuffer = IntPtr.Zero;
                            int numTransientMode = 0;
                            IntPtr TransientModeBuffer = IntPtr.Zero;
                            int numSLSOffset = 0;
                            IntPtr SLSOffsetBuffer = IntPtr.Zero;
                            //ADL2_Display_SLSMapConfigX2_Get(IntPtr ADLContextHandle, int adapterIndex, int SLSMapIndex, ref ADL_SLS_MAP[] SLSMap, ref int NumSLSTarget, out IntPtr SLSTargetArray, ref int lpNumNativeMode, out IntPtr NativeMode, ref int NumNativeModeOffsets, out IntPtr NativeModeOffsets, ref int NumBezelMode, out IntPtr BezelMode, ref int NumTransientMode, out IntPtr TransientMode, ref int NumSLSOffset, out IntPtr SLSOffset, int option);
                            IntPtr SLSMap = IntPtr.Zero;                            
                            //int SLSMapIndex = SLSMapIDArray[slsMapIdx];
                            //ADLRet = ADLImport.ADL2_Display_SLSMapConfigX2_Get(_adlContextHandle, adapterNum, SLSMapIndex, out SLSMap, out numSLSTargets, out SLSTargetBuffer, out numNativeMode, out nativeModeBuffer, out numNativeModeOffsets, 
                            //    out nativeModeOffsetsBuffer, out numBezelMode, out bezelModeBuffer, out numTransientMode, out TransientModeBuffer, out numSLSOffset, out SLSOffsetBuffer, ADLImport.ADL_DISPLAY_SLSGRID_CAP_OPTION_RELATIVETO_CURRENTANGLE);
                            ADLRet = ADLImport.ADL2_Display_SLSMapConfigX2_Get(_adlContextHandle, adapterNum, SLSMapIndex, out SLSMap, out numSLSTargets, out SLSTargetBuffer, out numNativeMode, out nativeModeBuffer, out numNativeModeOffsets,
                                out nativeModeOffsetsBuffer, out numBezelMode, out bezelModeBuffer, out numTransientMode, out TransientModeBuffer, out numSLSOffset, out SLSOffsetBuffer, ADLImport.ADL_DISPLAY_SLSMAPCONFIG_GET_OPTION_RELATIVETO_CURRENTANGLE);
                            if (ADLRet == ADL_STATUS.ADL_OK)
                            {
                                SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: ADL2_Display_SLSMapConfigX2_Get returned information about the SLS Info connected to AMD adapter {adapterNum}.");
                            }
                            else
                            {
                                SharedLogger.logger.Error($"AMDLibrary/GetAMDDisplayConfig: ERROR - ADL2_Display_SLSMapConfigX2_Get returned ADL_STATUS {ADLRet} when trying to get the SLS Info from AMD adapter {adapterNum} in the computer.");
                                //throw new AMDLibraryException($"ADL2_Display_DisplayMapConfig_Get returned ADL_STATUS {ADLRet} when trying to get the display target info from AMD adapter {adapterNum} in the computer");
                            }
                            //}
                        }
                        else
                        {
                            SharedLogger.logger.Error($"AMDLibrary/GetAMDDisplayConfig: SLS (Eyfinity) is NOT enabled on AMD adapter {adapterNum}.");
                            // Set the SLS Map Index if there is NOT Eyefinity
                            savedAdapterConfig.IsSLSEnabled = false;
                            savedAdapterConfig.SLSMapIndex = -1;

                        }
                    }

                    // We want to get the AMD HDR information and store it for later
                    //ADL2_Display_HDRState_Get(ADL_CONTEXT_HANDLE context, int iAdapterIndex, ADLDisplayID displayID, int * iSupport, int * iEnable)
                    // Save the AMD Adapter Config
                    if (!myDisplayConfig.AdapterConfigs.Contains(savedAdapterConfig))
                    {
                        // Save the new adapter config only if we haven't already
                        myDisplayConfig.AdapterConfigs.Add(savedAdapterConfig);
                    }                   

                }

                // We want to get the Windows CCD information and store it for later so that we record
                // display sizes, and screen positions and the like.
                myDisplayConfig.WindowsDisplayConfig = _winLibrary.GetActiveConfig();
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

                ADL_STATUS ADLRet = 0;
                // We want to get the current config
                //AMD_DISPLAY_CONFIG currentDisplayConfig = GetAMDDisplayConfig(QDC.QDC_ALL_PATHS);

                // We want to check the AMD Eyefinity (SLS) config is valid
                SharedLogger.logger.Trace($"AMDLibrary/SetActiveConfig: Testing whether the display configuration is valid");
                //ADL2_Display_SLSMapConfig_Valid(ADL_CONTEXT_HANDLE context, int iAdapterIndex, ADLSLSMap slsMap, int iNumDisplayTarget, ADLSLSTarget * lpSLSTarget, int * lpSupportedSLSLayoutImageMode, int * lpReasonForNotSupportSLS, int iOption)
                foreach(var adapter in displayConfig.AdapterConfigs)
                {
                    // set the display locations
                    if (adapter.IsSLSEnabled)
                    {
                        // DO the SLS based display setup
                        foreach (var slsMap in adapter.SLSMap)
                        {
                            // Check the SLS config is valid
                            int numDisplayTargets = 0;
                            int supportedSLSLayoutImageMode = 0;
                            int reasonForNotSupportingSLS = 0;
                            ADL_DISPLAY_TARGET[] displayTargetArray = { new ADL_DISPLAY_TARGET() };
                            IntPtr displayTargetBuffer = IntPtr.Zero;
                            int option = ADLImport.ADL_DISPLAY_SLSGRID_CAP_OPTION_RELATIVETO_LANDSCAPE;
                            ADLRet = ADLImport.ADL2_Display_SLSMapConfig_Valid(_adlContextHandle, adapter.AdapterIndex, slsMap, slsMap.NumSLSTarget, displayTargetArray, out supportedSLSLayoutImageMode, out reasonForNotSupportingSLS, option);
                            if (ADLRet == ADL_STATUS.ADL_OK)
                            {
                                SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: ADL2_Display_SLSMapConfig_Valid confirmed the SLS configuration is valid for AMD adapter {adapter.AdapterIndex}.");
                            }
                            else
                            {
                                SharedLogger.logger.Error($"AMDLibrary/GetAMDDisplayConfig: ERROR - ADL2_Display_SLSMapConfig_Valid returned ADL_STATUS {ADLRet} when trying to validate the SLS configuration for AMD adapter {adapter.AdapterIndex} in the computer.");
                                throw new AMDLibraryException($"ADL2_Display_SLSMapConfig_Valid returned ADL_STATUS {ADLRet}when trying to validate the SLS configuration for AMD adapter {adapter.AdapterIndex} in the computer");
                            }

                            if (numDisplayTargets > 0)
                            {
                                IntPtr currentDisplayTargetBuffer = displayTargetBuffer;
                                displayTargetArray = new ADL_DISPLAY_TARGET[numDisplayTargets];
                                for (int i = 0; i < numDisplayTargets; i++)
                                {
                                    // build a structure in the array slot
                                    displayTargetArray[i] = new ADL_DISPLAY_TARGET();
                                    // fill the array slot structure with the data from the buffer
                                    displayTargetArray[i] = (ADL_DISPLAY_TARGET)Marshal.PtrToStructure(currentDisplayTargetBuffer, typeof(ADL_DISPLAY_TARGET));
                                    // destroy the bit of memory we no longer need
                                    Marshal.DestroyStructure(currentDisplayTargetBuffer, typeof(ADL_DISPLAY_TARGET));
                                    // advance the buffer forwards to the next object
                                    currentDisplayTargetBuffer = (IntPtr)((long)currentDisplayTargetBuffer + Marshal.SizeOf(displayTargetArray[i]));
                                }
                                // Free the memory used by the buffer                        
                                Marshal.FreeCoTaskMem(displayTargetBuffer);
                            }
                        }
                        
                    }
                    else
                    {
                        // Do the non-SLS based display setup
                        AMD_ADAPTER_CONFIG amdAdapterConfig = adapter;
                        int numPossibleMapResult = 0;
                        IntPtr possibleMapResultBuffer = IntPtr.Zero;
                        ADLRet = ADLImport.ADL2_Display_DisplayMapConfig_Set(_adlContextHandle, amdAdapterConfig.AdapterIndex, amdAdapterConfig.DisplayMaps.Length, amdAdapterConfig.DisplayMaps, amdAdapterConfig.DisplayTargets.Length, amdAdapterConfig.DisplayTargets);
                        if (ADLRet == ADL_STATUS.ADL_OK)
                        {
                            SharedLogger.logger.Trace($"AMDLibrary/GetAMDDisplayConfig: ADL2_Display_DisplayMapConfig_Set returned information about all displaytargets connected to AMD adapter {amdAdapterConfig.AdapterIndex}.");
                        }
                        else
                        {
                            SharedLogger.logger.Error($"AMDLibrary/GetAMDDisplayConfig: ERROR - ADL2_Display_DisplayMapConfig_Get returned ADL_STATUS {ADLRet} when trying to get the display target info from AMD adapter {amdAdapterConfig.AdapterIndex} in the computer.");
                            throw new AMDLibraryException($"ADL2_Display_DisplayMapConfig_Get returned ADL_STATUS {ADLRet} when trying to get the display target info from AMD adapter {amdAdapterConfig.AdapterIndex} in the computer");
                        }
                    }


                }
                


                // We want to apply the AMD Eyefinity (SLS) config 
                //ADL2_Display_SLSMapConfig_SetState(ADL_CONTEXT_HANDLE  context,int     iAdapterIndex, int     iSLSMapIndex, int     iState)

                // We want to set the AMD HDR settings

                // We want to apply the Windows CCD layout info and HDR
            }
            else
            {
                SharedLogger.logger.Error($"AMDLibrary/SetActiveConfig: ERROR - Tried to run SetActiveConfig but the AMD ADL library isn't initialised!");
                throw new AMDLibraryException($"Tried to run SetActiveConfig but the AMD ADL library isn't initialised!");
            }


/*
            // Get the all possible windows display configs
            SharedLogger.logger.Trace($"AMDLibrary/SetActiveConfig: Generating a list of all the current display configs");
            WINDOWS_DISPLAY_CONFIG allWindowsDisplayConfig = GetWindowsDisplayConfig(QDC.QDC_ALL_PATHS);

            // Now we go through the Paths to update the LUIDs as per Soroush's suggestion
            SharedLogger.logger.Trace($"AMDLibrary/SetActiveConfig: Patching the adapter IDs to make the saved config valid");
            PatchAdapterIDs(ref displayConfig, allWindowsDisplayConfig.displayAdapters);

            SharedLogger.logger.Trace($"AMDLibrary/SetActiveConfig: Testing whether the display configuration is valid");
            // Test whether a specified display configuration is supported on the computer                    
            uint myPathsCount = (uint)displayConfig.displayConfigPaths.Length;
            uint myModesCount = (uint)displayConfig.displayConfigModes.Length;
            WIN32STATUS err = CCDImport.SetDisplayConfig(myPathsCount, displayConfig.displayConfigPaths, myModesCount, displayConfig.displayConfigModes, SDC.DISPLAYMAGICIAN_VALIDATE);
            if (err == WIN32STATUS.ERROR_SUCCESS)
            {
                SharedLogger.logger.Trace($"AMDLibrary/SetActiveConfig: Successfully validated that the display configuration supplied would work!");
            }
            else
            {
                SharedLogger.logger.Error($"AMDLibrary/SetActiveConfig: ERROR - SetDisplayConfig couldn't validate the display configuration supplied. This display configuration wouldn't work.");
                return false;
            }

            SharedLogger.logger.Trace($"AMDLibrary/SetActiveConfig: Yay! The display configuration is valid! Attempting to set the Display Config now");
            // Now set the specified display configuration for this computer                    
            err = CCDImport.SetDisplayConfig(myPathsCount, displayConfig.displayConfigPaths, myModesCount, displayConfig.displayConfigModes, SDC.DISPLAYMAGICIAN_SET);
            if (err == WIN32STATUS.ERROR_SUCCESS)
            {
                SharedLogger.logger.Trace($"AMDLibrary/SetActiveConfig: Successfully set the display configuration to the settings supplied!");
            }
            else
            {
                SharedLogger.logger.Error($"AMDLibrary/SetActiveConfig: ERROR - SetDisplayConfig couldn't set the display configuration using the settings supplied. Something is wrong.");
                throw new AMDLibraryException($"SetDisplayConfig couldn't set the display configuration using the settings supplied. Something is wrong.");
            }

            SharedLogger.logger.Trace($"AMDLibrary/SetActiveConfig: SUCCESS! The display configuration has been successfully applied");

            foreach (ADVANCED_HDR_INFO_PER_PATH myHDRstate in displayConfig.displayHDRStates)
            {
                SharedLogger.logger.Trace($"Trying to get information whether HDR color is in use now on Display {myHDRstate.Id}.");
                // Get advanced HDR info
                var colorInfo = new DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO();
                colorInfo.Header.Type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO;
                colorInfo.Header.Size = (uint)Marshal.SizeOf<DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO>();
                colorInfo.Header.AdapterId = myHDRstate.AdapterId;
                colorInfo.Header.Id = myHDRstate.Id;
                err = CCDImport.DisplayConfigGetDeviceInfo(ref colorInfo);
                if (err == WIN32STATUS.ERROR_SUCCESS)
                {
                    SharedLogger.logger.Trace($"AMDLibrary/SetActiveConfig: Advanced Color Info gathered from Display {myHDRstate.Id}");

                    if (myHDRstate.AdvancedColorInfo.AdvancedColorSupported && colorInfo.AdvancedColorEnabled != myHDRstate.AdvancedColorInfo.AdvancedColorEnabled)
                    {
                        SharedLogger.logger.Trace($"HDR is available for use on Display {myHDRstate.Id}, and we want it set to {myHDRstate.AdvancedColorInfo.AdvancedColorEnabled} but is currently {colorInfo.AdvancedColorEnabled}.");

                        var setColorState = new DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE();
                        setColorState.Header.Type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_SET_ADVANCED_COLOR_STATE;
                        setColorState.Header.Size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE>();
                        setColorState.Header.AdapterId = myHDRstate.AdapterId;
                        setColorState.Header.Id = myHDRstate.Id;
                        setColorState.EnableAdvancedColor = myHDRstate.AdvancedColorInfo.AdvancedColorEnabled;
                        err = CCDImport.DisplayConfigSetDeviceInfo(ref setColorState);
                        if (err == WIN32STATUS.ERROR_SUCCESS)
                        {
                            SharedLogger.logger.Trace($"AMDLibrary/SetActiveConfig: SUCCESS! Set HDR successfully to {myHDRstate.AdvancedColorInfo.AdvancedColorEnabled} on Display {myHDRstate.Id}");
                        }
                        else
                        {
                            SharedLogger.logger.Error($"AMDLibrary/SetActiveConfig: ERROR - DisplayConfigGetDeviceInfo returned WIN32STATUS {err} when trying to set the HDR settings for display #{myHDRstate.Id}");
                            return false;
                        }
                    }
                    else
                    {
                        SharedLogger.logger.Trace($"AMDLibrary/SetActiveConfig: Skipping setting HDR on Display {myHDRstate.Id} as it does not support HDR");
                    }
                }
                else
                {
                    SharedLogger.logger.Warn($"AMDLibrary/GetWindowsDisplayConfig: WARNING - DisplayConfigGetDeviceInfo returned WIN32STATUS {err} when trying to find out if HDR is supported for display #{myHDRstate.Id}");
                }

            }*/
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
                SharedLogger.logger.Trace($"AMDLibrary/IsActiveConfig: The display configuration is already being used (supplied displayConfig Equals currentWindowsDisplayConfig");
                return true;
            }
            else
            {
                SharedLogger.logger.Trace($"AMDLibrary/IsActiveConfig: The display configuration is NOT currently in use (supplied displayConfig Equals currentWindowsDisplayConfig");
                return false;
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
            SharedLogger.logger.Error($"AMDLibrary/GetCurrentDisplayIdentifiers: Getting the current display identifiers for the displays in use now");
            return GetSomeDisplayIdentifiers(QDC.QDC_ONLY_ACTIVE_PATHS);
        }

        public List<string> GetAllConnectedDisplayIdentifiers()
        {
            SharedLogger.logger.Error($"AMDLibrary/GetAllConnectedDisplayIdentifiers: Getting all the display identifiers that can possibly be used");
            return GetSomeDisplayIdentifiers(QDC.QDC_ALL_PATHS);
        }

        private List<string> GetSomeDisplayIdentifiers(QDC selector = QDC.QDC_ONLY_ACTIVE_PATHS)
        {
            SharedLogger.logger.Debug($"AMDLibrary/GetCurrentDisplayIdentifiers: Generating the unique Display Identifiers for the currently active configuration");

            List<string> displayIdentifiers = new List<string>();

            SharedLogger.logger.Trace($"AMDLibrary/GetCurrentDisplayIdentifiers: Testing whether the display configuration is valid (allowing tweaks).");
            // Get the size of the largest Active Paths and Modes arrays
            int pathCount = 0;
            int modeCount = 0;
            WIN32STATUS err = CCDImport.GetDisplayConfigBufferSizes(QDC.QDC_ONLY_ACTIVE_PATHS, out pathCount, out modeCount);
            if (err != WIN32STATUS.ERROR_SUCCESS)
            {
                SharedLogger.logger.Error($"AMDLibrary/PrintActiveConfig: ERROR - GetDisplayConfigBufferSizes returned WIN32STATUS {err} when trying to get the maximum path and mode sizes");
                throw new AMDLibraryException($"GetDisplayConfigBufferSizes returned WIN32STATUS {err} when trying to get the maximum path and mode sizes");
            }

            SharedLogger.logger.Trace($"AMDLibrary/GetSomeDisplayIdentifiers: Getting the current Display Config path and mode arrays");
            var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
            err = CCDImport.QueryDisplayConfig(QDC.QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);
            if (err == WIN32STATUS.ERROR_INSUFFICIENT_BUFFER)
            {
                SharedLogger.logger.Warn($"AMDLibrary/GetSomeDisplayIdentifiers: The displays were modified between GetDisplayConfigBufferSizes and QueryDisplayConfig so we need to get the buffer sizes again.");
                SharedLogger.logger.Trace($"AMDLibrary/GetSomeDisplayIdentifiers: Getting the size of the largest Active Paths and Modes arrays");
                // Screen changed in between GetDisplayConfigBufferSizes and QueryDisplayConfig, so we need to get buffer sizes again
                // as per https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-querydisplayconfig 
                err = CCDImport.GetDisplayConfigBufferSizes(QDC.QDC_ONLY_ACTIVE_PATHS, out pathCount, out modeCount);
                if (err != WIN32STATUS.ERROR_SUCCESS)
                {
                    SharedLogger.logger.Error($"AMDLibrary/GetSomeDisplayIdentifiers: ERROR - GetDisplayConfigBufferSizes returned WIN32STATUS {err} when trying to get the maximum path and mode sizes again");
                    throw new AMDLibraryException($"GetDisplayConfigBufferSizes returned WIN32STATUS {err} when trying to get the maximum path and mode sizes again");
                }
                SharedLogger.logger.Trace($"AMDLibrary/GetSomeDisplayIdentifiers: Getting the current Display Config path and mode arrays");
                paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
                modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
                err = CCDImport.QueryDisplayConfig(QDC.QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);
                if (err == WIN32STATUS.ERROR_INSUFFICIENT_BUFFER)
                {
                    SharedLogger.logger.Error($"AMDLibrary/GetSomeDisplayIdentifiers: ERROR - The displays were still modified between GetDisplayConfigBufferSizes and QueryDisplayConfig, even though we tried twice. Something is wrong.");
                    throw new AMDLibraryException($"The displays were still modified between GetDisplayConfigBufferSizes and QueryDisplayConfig, even though we tried twice. Something is wrong.");
                }
                else if (err != WIN32STATUS.ERROR_SUCCESS)
                {
                    SharedLogger.logger.Error($"AMDLibrary/GetSomeDisplayIdentifiers: ERROR - QueryDisplayConfig returned WIN32STATUS {err} when trying to query all available displays again");
                    throw new AMDLibraryException($"QueryDisplayConfig returned WIN32STATUS {err} when trying to query all available displays again.");
                }
            }
            else if (err != WIN32STATUS.ERROR_SUCCESS)
            {
                SharedLogger.logger.Error($"AMDLibrary/GetSomeDisplayIdentifiers: ERROR - QueryDisplayConfig returned WIN32STATUS {err} when trying to query all available displays");
                throw new AMDLibraryException($"QueryDisplayConfig returned WIN32STATUS {err} when trying to query all available displays.");
            }

            foreach (var path in paths)
            {
                if (path.TargetInfo.TargetAvailable == false)
                {
                    // We want to skip this one cause it's not valid
                    SharedLogger.logger.Trace($"AMDLibrary/GetSomeDisplayIdentifiers: Skipping path due to TargetAvailable not existing in display #{path.TargetInfo.Id}");
                    continue;
                }

                // get display source name
                var sourceInfo = new DISPLAYCONFIG_SOURCE_DEVICE_NAME();
                sourceInfo.Header.Type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
                sourceInfo.Header.Size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DEVICE_NAME>();
                sourceInfo.Header.AdapterId = path.SourceInfo.AdapterId;
                sourceInfo.Header.Id = path.SourceInfo.Id;
                err = CCDImport.DisplayConfigGetDeviceInfo(ref sourceInfo);
                if (err == WIN32STATUS.ERROR_SUCCESS)
                {
                    SharedLogger.logger.Trace($"AMDLibrary/GetSomeDisplayIdentifiers: Successfully got the source info from {path.SourceInfo.Id}.");
                }
                else
                {
                    SharedLogger.logger.Warn($"AMDLibrary/GetSomeDisplayIdentifiers: WARNING - DisplayConfigGetDeviceInfo returned WIN32STATUS {err} when trying to get the target info for display #{path.SourceInfo.Id}");
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
                    SharedLogger.logger.Trace($"AMDLibrary/GetSomeDisplayIdentifiers: Successfully got the target info from {path.TargetInfo.Id}.");
                }
                else
                {
                    SharedLogger.logger.Warn($"AMDLibrary/GetSomeDisplayIdentifiers: WARNING - DisplayConfigGetDeviceInfo returned WIN32STATUS {err} when trying to get the target info for display #{path.TargetInfo.Id}");
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
                    SharedLogger.logger.Trace($"AMDLibrary/GetSomeDisplayIdentifiers: Successfully got the display name info from {path.TargetInfo.Id}.");
                }
                else
                {
                    SharedLogger.logger.Warn($"AMDLibrary/GetSomeDisplayIdentifiers: WARNING - DisplayConfigGetDeviceInfo returned WIN32STATUS {err} when trying to get the target info for display #{path.TargetInfo.Id}");
                }

                // Create an array of all the important display info we need to record
                List<string> displayInfo = new List<string>();
                displayInfo.Add("WINAPI");
                try
                {
                    displayInfo.Add(adapterInfo.AdapterDevicePath.ToString());
                }
                catch (Exception ex)
                {
                    SharedLogger.logger.Warn(ex, $"AMDLibrary/GetSomeDisplayIdentifiers: Exception getting Windows Display Adapter Device Path from video card. Substituting with a # instead");
                    displayInfo.Add("#");
                }
                try
                {
                    displayInfo.Add(targetInfo.OutputTechnology.ToString());
                }
                catch (Exception ex)
                {
                    SharedLogger.logger.Warn(ex, $"AMDLibrary/GetSomeDisplayIdentifiers: Exception getting Windows Display Connector Instance from video card. Substituting with a # instead");
                    displayInfo.Add("#");
                }
                try
                {
                    displayInfo.Add(targetInfo.EdidManufactureId.ToString());
                }
                catch (Exception ex)
                {
                    SharedLogger.logger.Warn(ex, $"AMDLibrary/GetSomeDisplayIdentifiers: Exception getting Windows Display EDID Manufacturer Code from video card. Substituting with a # instead");
                    displayInfo.Add("#");
                }
                try
                {
                    displayInfo.Add(targetInfo.EdidProductCodeId.ToString());
                }
                catch (Exception ex)
                {
                    SharedLogger.logger.Warn(ex, $"AMDLibrary/GetSomeDisplayIdentifiers: Exception getting Windows Display EDID Product Code from video card. Substituting with a # instead");
                    displayInfo.Add("#");
                }
                try
                {
                    displayInfo.Add(targetInfo.MonitorFriendlyDeviceName.ToString());
                }
                catch (Exception ex)
                {
                    SharedLogger.logger.Warn(ex, $"AMDLibrary/GetSomeDisplayIdentifiers: Exception getting Windows Display Target Friendly name from video card. Substituting with a # instead");
                    displayInfo.Add("#");
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