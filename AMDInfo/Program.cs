using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AMDInfo
{
    class Program
    {

        /*public struct ADVANCED_HDR_INFO_PER_PATH
        {
            public LUID AdapterId;
            public uint Id;
            public DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO AdvancedColorInfo;
            public DISPLAYCONFIG_SDR_WHITE_LEVEL SDRWhiteLevel;
        }

        public struct AMD_DISPLAY_CONFIG
        {
            public DISPLAYCONFIG_PATH_INFO[] displayConfigPaths;
            public DISPLAYCONFIG_MODE_INFO[] displayConfigModes;
            public ADVANCED_HDR_INFO_PER_PATH[] displayHDRStates;
        }*/

        //static AMD_DISPLAY_CONFIG myDisplayConfig = new AMD_DISPLAY_CONFIG();

        static void Main(string[] args)
        {
            Console.WriteLine($"AMDInfo v1.0.0");
            Console.WriteLine($"==============");
            Console.WriteLine($"By Terry MacDonald 2021\n");

            if (args.Length > 0)
            {
                if (args[0] == "save")
                {                    
                    //saveToFile(args[1]);
                    if (!File.Exists(args[1]))
                    {
                        Console.WriteLine($"ERROR - Couldn't save settings to the file {args[1]}");
                        Environment.Exit(1);
                    }
                }
                else if (args[0] == "load")
                {
                    if (!File.Exists(args[1]))
                    {
                        Console.WriteLine($"ERROR - Couldn't find the file {args[1]} to load settings from it");
                        Environment.Exit(1);
                    }
                    //loadFromFile(args[1]);
                }
                else if (args[0] == "help" || args[0] == "--help" || args[0] == "-h" || args[0] == "/?" || args[0] == "-?")
                {
                    Console.WriteLine($"AMDInfo is a little program to help test setting display layout and HDR settings using the AMD ADL driver.\n");
                    Console.WriteLine($"You can run it without any command line parameters, and it will print all the information it can find from the \nWindows Display CCD interface.\n");
                    Console.WriteLine($"You can also run it with 'CCDInfo save myfilename.cfg' and it will save the current display configuration into\nthe myfilename.cfg file.\n");
                    Console.WriteLine($"This is most useful when you subsequently use the 'CCDInfo load myfilename.cfg' command, as it will load the\ndisplay configuration from the myfilename.cfg file and make it live.");
                    Console.WriteLine($"In this way, you can make yourself a library of different cfg files with different display layouts, then use\nthe CCDInfo load command to swap between them.");
                    Environment.Exit(1);
                }
            }

            // Set up some variables
            ADL_STATUS ADLRet;
            IntPtr _adlContextHandle = IntPtr.Zero;
            bool _initialised = false;

            try
            {
                // Second parameter is 1: Get only the present adapters
                ADLRet = ADLImport.ADL2_Main_Control_Create(ADLImport.ADL_Main_Memory_Alloc, (int)1, out _adlContextHandle);
                if (ADLRet == ADL_STATUS.ADL_OK)
                {
                    _initialised = true;
                    Console.WriteLine($"AMDLibrary/AMDLibrary: ADL2 library was initialised successfully");
                }
                else
                {
                    Console.WriteLine($"AMDLibrary/AMDLibrary: Error intialising ADL2 library. ADL2_Main_Control_Create() returned error code {ADLRet}");
                    Environment.Exit(1);
                }

                if (_initialised)
                {

                    int[] gpuBusIndexes = new int[4];
                    int numAdapters = 0;
                    int busNumber;
                    bool gpuFound = false;
                    IntPtr adapterInfoBuffer = IntPtr.Zero;
                    //ADLImport.ADL2_Adapter_NumberOfAdapters_Get(_adlContextHandle, ref NumberOfAdapters);
                    ADLRet = ADLImport.ADL2_Adapter_AdapterInfoX3_Get(_adlContextHandle, ADLImport.ADL_ADAPTER_INDEX_ALL, out numAdapters, out adapterInfoBuffer);
                    if (ADLRet == ADL_STATUS.ADL_OK)
                    {
                        Console.WriteLine($"AMDLibrary/AMDLibrary: ADL2_Adapter_AdapterInfoX3_Get worked!");
                    }
                    else
                    {
                        Console.WriteLine($"AMDLibrary/AMDLibrary: ADL2_Adapter_AdapterInfoX3_Get() returned error code {ADLRet}");
                        Environment.Exit(1);
                    }

                    ADL_ADAPTER_INFO oneAdapter = new ADL_ADAPTER_INFO();
                    List<ADL_ADAPTER_INFO> adapterInfoList = new List<ADL_ADAPTER_INFO>();
                    for (int adapter = 0; adapter < numAdapters; adapter++)
                    {
                        oneAdapter = (ADL_ADAPTER_INFO)Marshal.PtrToStructure(new IntPtr(adapterInfoBuffer.ToInt64() + (adapter * Marshal.SizeOf(oneAdapter))), oneAdapter.GetType());
                        adapterInfoList.Add(oneAdapter);

                        if (oneAdapter.Exist != 1 || oneAdapter.Present != 1)
                        {
                            Console.WriteLine($"AMDLibrary/AMDLibrary: The Adapter {oneAdapter.DisplayName} does not exist or is not present.");
                            continue;
                        }

                        Console.WriteLine($"AMDLibrary/AMDLibrary: The Adapter {oneAdapter.DisplayName} exists and is present.");


                        // Get whiether this adapter is active
                        int isActive = ADLImport.ADL_FALSE;
                        ADLRet = ADLImport.ADL2_Adapter_Active_Get(_adlContextHandle, oneAdapter.AdapterIndex, ref isActive);
                        if (ADLRet == ADL_STATUS.ADL_OK)
                        {
                            Console.WriteLine($"AMDLibrary/AMDLibrary: ADL2_Adapter_Active_Get on adapter {oneAdapter.AdapterIndex} worked!");
                        }
                        else
                        {
                            Console.WriteLine($"AMDLibrary/AMDLibrary: ADL2_Adapter_Active_Get() returned error code {ADLRet}");
                            Environment.Exit(1);
                        }

                        if (isActive != ADLImport.ADL_TRUE)
                        {
                            Console.WriteLine($"AMDLibrary/AMDLibrary: Adapter {oneAdapter.AdapterIndex} is NOT active! Skipping");
                            continue;
                        }

                        if (isActive != ADLImport.ADL_TRUE)
                        {
                            Console.WriteLine($"AMDLibrary/AMDLibrary: Adapter {oneAdapter.AdapterIndex} is NOT active! Skipping");
                            continue;
                        }

                        if (oneAdapter.AdapterIndex < 0)
                        {
                            Console.WriteLine($"AMDLibrary/AMDLibrary: Adapter {oneAdapter.AdapterIndex} has an index less than 0. Skipping");
                            continue;
                        }

                        Console.WriteLine($"AMDLibrary/AMDLibrary: Adapter {oneAdapter.AdapterIndex} really exists!");

                        IntPtr DisplayBuffer = IntPtr.Zero;
                        int numDisplays = 0;
                        // Force the display detection and get the Display Info. Use 0 as last parameter to NOT force detection
                        ADLRet = ADLImport.ADL2_Display_DisplayInfo_Get(_adlContextHandle, oneAdapter.AdapterIndex, ref numDisplays, out DisplayBuffer, 1);
                        if (ADLRet != ADL_STATUS.ADL_OK)
                        {
                            Console.WriteLine($"AMDLibrary/AMDLibrary: ADL2_Display_DisplayInfo_Get() returned an error code {ADLRet}");
                            continue;
                        }
                        else
                        {
                            Console.WriteLine($"AMDLibrary/AMDLibrary: ADL2_Display_DisplayInfo_Get on adapter {oneAdapter.AdapterIndex} worked!");
                        }

                        for (int displayLoop = 0; displayLoop < numDisplays; displayLoop++)
                        {
                            ADL_DISPLAY_INFO oneDisplayInfo = new ADL_DISPLAY_INFO();
                            oneDisplayInfo = (ADL_DISPLAY_INFO)Marshal.PtrToStructure(new IntPtr(DisplayBuffer.ToInt64() + (displayLoop * Marshal.SizeOf(oneDisplayInfo))), oneDisplayInfo.GetType());

                            // Is the display mapped to this adapter? If not we skip it!
                            if (oneDisplayInfo.DisplayID.DisplayLogicalAdapterIndex != oneAdapter.AdapterIndex)
                            {
                                Console.WriteLine($"AMDLibrary/SetActiveProfile: AMD Adapter #{oneAdapter.AdapterIndex.ToString()} ({oneAdapter.AdapterName}) AdapterID display ID#{oneDisplayInfo.DisplayID.DisplayLogicalIndex} is not a real display as its DisplayID.DisplayLogicalAdapterIndex is -1");
                                continue;
                            }

                            Console.WriteLine($"AMDLibrary/SetActiveProfile: AMD Adapter #{oneAdapter.AdapterIndex.ToString()} ({oneAdapter.AdapterName}) AdapterID display ID#{oneDisplayInfo.DisplayID.DisplayLogicalIndex} is a real display");

                            // At this point we know we have a real display, so we can begin to interrogate it.

                        }


                        Console.WriteLine($"AMDLibrary/GenerateProfileDisplayIdentifiers: Number Of Adapters: {numAdapters.ToString()} ");

                    }

                    // Destroy the context handle and memory
                    ADLImport.ADL2_Main_Control_Destroy(_adlContextHandle);
                }
                else
                {
                    Console.WriteLine($"ERROR - Couldn't initialise the AMD ADL library.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR - Exception while trying to access ADL2_Main_Control_Create");
            }
            

        }

        /*static void saveToFile(string filename)
        {
            Console.WriteLine($"ProfileRepository/SaveProfiles: Attempting to save the profiles repository to the {filename}.");

            // Get the size of the largest Active Paths and Modes arrays
            WIN32STATUS err = CCDImport.GetDisplayConfigBufferSizes(QDC.QDC_ONLY_ACTIVE_PATHS, out var pathCount, out var modeCount);
            if (err != WIN32STATUS.ERROR_SUCCESS)
                throw new Win32Exception((int)err);

            // Get the Active Paths and Modes in use now
            var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
            err = CCDImport.QueryDisplayConfig(QDC.QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);
            if (err != WIN32STATUS.ERROR_SUCCESS)
                throw new Win32Exception((int)err);

            // Now cycle through the paths and grab the HDR state information
            var hdrInfos = new ADVANCED_HDR_INFO_PER_PATH[pathCount];
            int hdrInfoCount = 0;
            foreach (var path in paths)
            {
                // Get advanced HDR info
                var colorInfo = new DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO();
                colorInfo.Header.Type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO;
                colorInfo.Header.Size = Marshal.SizeOf<DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO>();
                colorInfo.Header.AdapterId = path.TargetInfo.AdapterId;
                colorInfo.Header.Id = path.TargetInfo.Id;
                err = CCDImport.DisplayConfigGetDeviceInfo(ref colorInfo);
                if (err != WIN32STATUS.ERROR_SUCCESS)
                    throw new Win32Exception((int)err);

                // get SDR white levels
                var whiteLevelInfo = new DISPLAYCONFIG_SDR_WHITE_LEVEL();
                whiteLevelInfo.Header.Type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_SDR_WHITE_LEVEL;
                whiteLevelInfo.Header.Size = Marshal.SizeOf<DISPLAYCONFIG_SDR_WHITE_LEVEL>();
                whiteLevelInfo.Header.AdapterId = path.TargetInfo.AdapterId;
                whiteLevelInfo.Header.Id = path.TargetInfo.Id;
                err = CCDImport.DisplayConfigGetDeviceInfo(ref whiteLevelInfo);
                if (err != WIN32STATUS.ERROR_SUCCESS)
                    throw new Win32Exception((int)err);


                hdrInfos[hdrInfoCount] = new ADVANCED_HDR_INFO_PER_PATH();
                hdrInfos[hdrInfoCount].AdapterId = path.TargetInfo.AdapterId;
                hdrInfos[hdrInfoCount].Id = path.TargetInfo.Id;
                hdrInfos[hdrInfoCount].AdvancedColorInfo = colorInfo;
                hdrInfos[hdrInfoCount].SDRWhiteLevel = whiteLevelInfo;
                hdrInfoCount++;
            }


            // Store the active paths and modes in our display config object
            myDisplayConfig.displayConfigPaths = paths;
            myDisplayConfig.displayConfigModes = modes;
            myDisplayConfig.displayHDRStates = hdrInfos;


            // Save the object to file!
            try
            {
                Console.WriteLine($"ProfileRepository/SaveProfiles: Converting the objects to JSON format.");

                var json = JsonConvert.SerializeObject(myDisplayConfig, Formatting.Indented, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Include,
                    DefaultValueHandling = DefaultValueHandling.Populate,
                    TypeNameHandling = TypeNameHandling.Auto

                });


                if (!string.IsNullOrWhiteSpace(json))
                {
                    Console.WriteLine($"ProfileRepository/SaveProfiles: Saving the profile repository to the {filename}.");

                    File.WriteAllText(filename, json, Encoding.Unicode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ProfileRepository/SaveProfiles: Unable to save the profile repository to the {filename}.");
            }
        }

        static void loadFromFile(string filename)
        {
            string json = "";
            try
            {
                json = File.ReadAllText(filename, Encoding.Unicode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ProfileRepository/LoadProfiles: Tried to read the JSON file {filename} to memory but File.ReadAllTextthrew an exception.");
            }

            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    myDisplayConfig = JsonConvert.DeserializeObject<WINDOWS_DISPLAY_CONFIG>(json, new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore,
                        NullValueHandling = NullValueHandling.Ignore,
                        DefaultValueHandling = DefaultValueHandling.Include,
                        TypeNameHandling = TypeNameHandling.Auto,
                        ObjectCreationHandling = ObjectCreationHandling.Replace
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ProfileRepository/LoadProfiles: Tried to parse the JSON in the {filename} but the JsonConvert threw an exception.");
                }


                // Get the size of the largest Active Paths and Modes arrays
                WIN32STATUS err = CCDImport.GetDisplayConfigBufferSizes(QDC.QDC_ALL_PATHS, out var pathCount, out var modeCount);
                if (err != WIN32STATUS.ERROR_SUCCESS)
                    throw new Win32Exception((int)err);
               
                // Now we want to validate the config is ok
                if (myDisplayConfig.displayConfigPaths.Length <= pathCount - 1 &&
                    myDisplayConfig.displayConfigModes.Length <= pathCount - 1)
                {
                    uint myPathsCount = (uint)myDisplayConfig.displayConfigPaths.Length;
                    uint myModesCount = (uint)myDisplayConfig.displayConfigModes.Length;

                    Console.WriteLine($"ProfileRepository/LoadProfiles: Testing whether the display configuration is valid.");
                    // Test whether a specified display configuration is supported on the computer                    
                    err = CCDImport.SetDisplayConfig(myPathsCount, myDisplayConfig.displayConfigPaths, myModesCount, myDisplayConfig.displayConfigModes, SDC.TEST_IF_VALID_DISPLAYCONFIG);
                    if (err != WIN32STATUS.ERROR_SUCCESS)
                        throw new Win32Exception((int)err);

                    Console.WriteLine($"ProfileRepository/LoadProfiles: Yay! The display configuration is valid!");
                    // Now set the specified display configuration for this computer                    
                    err = CCDImport.SetDisplayConfig(myPathsCount, myDisplayConfig.displayConfigPaths, myModesCount, myDisplayConfig.displayConfigModes, SDC.SET_DISPLAYCONFIG_AND_SAVE);
                    if (err != WIN32STATUS.ERROR_SUCCESS)
                        throw new Win32Exception((int)err);

                    Console.WriteLine($"ProfileRepository/LoadProfiles: The display configuration has been successfully applied");

                    foreach (ADVANCED_HDR_INFO_PER_PATH myHDRstate in myDisplayConfig.displayHDRStates)
                    {
                        // Get advanced HDR info
                        var colorInfo = new DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO();
                        colorInfo.Header.Type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO;
                        colorInfo.Header.Size = Marshal.SizeOf<DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO>();
                        colorInfo.Header.AdapterId = myHDRstate.AdapterId;
                        colorInfo.Header.Id = myHDRstate.Id;
                        err = CCDImport.DisplayConfigGetDeviceInfo(ref colorInfo);
                        if (err != WIN32STATUS.ERROR_SUCCESS)
                            throw new Win32Exception((int)err);


                        if (myHDRstate.AdvancedColorInfo.AdvancedColorSupported && colorInfo.AdvancedColorEnabled != myHDRstate.AdvancedColorInfo.AdvancedColorEnabled)
                        {
                            var setColorState = new DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE();
                            setColorState.Header.Type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_SET_ADVANCED_COLOR_STATE;
                            setColorState.Header.Size = Marshal.SizeOf<DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE>();
                            setColorState.Header.AdapterId = myHDRstate.AdapterId;
                            setColorState.EnableAdvancedColor = myHDRstate.AdvancedColorInfo.AdvancedColorEnabled;

                            err = CCDImport.DisplayConfigSetDeviceInfo(ref setColorState);
                            if (err != WIN32STATUS.ERROR_SUCCESS)
                                throw new Win32Exception((int)err);

                            Console.WriteLine($"ProfileRepository/LoadProfiles: HDR successfully set on Display {myHDRstate.Id}");
                        }
                        else
                        {
                            Console.WriteLine($"ProfileRepository/LoadProfiles: Skipping setting HDR on Display {myHDRstate.Id} as it does not support HDR");
                        }

                    }
                }
                else
                {
                    Console.WriteLine($"ProfileRepository/LoadProfiles: The number of Display Paths or Display Modes is higher than the max count allowed ({pathCount} for Paths and {modeCount} for Modes).");
                }

            }
            else
            {
                Console.WriteLine($"ProfileRepository/LoadProfiles: The {filename} profile JSON file exists but is empty! So we're going to treat it as if it didn't exist.");
            }
        }*/
    }
}
