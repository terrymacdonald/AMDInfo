using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using DisplayMagicianShared;
using NLog.Config;
using DisplayMagicianShared.AMD;
using System.Collections.Generic;
using DisplayMagicianShared.Windows;
using System.Linq;

namespace AMDInfo
{
    class Program
    {
        public struct AMDINFO_DISPLAY_CONFIG
        {
            public AMD_DISPLAY_CONFIG AMDConfig;
            public WINDOWS_DISPLAY_CONFIG WindowsConfig;
        }

        static AMDINFO_DISPLAY_CONFIG myDisplayConfig = new AMDINFO_DISPLAY_CONFIG();

        static void Main(string[] args)
        {

            // Prepare NLog for logging
            var config = new NLog.Config.LoggingConfiguration();

            // Targets where to log to: File and Console
            //string date = DateTime.Now.ToString("yyyyMMdd.HHmmss");
            string AppLogFilename = Path.Combine($"AMDInfo.log");

            // Rules for mapping loggers to targets          
            NLog.LogLevel logLevel = NLog.LogLevel.Trace;

            // Create the log file target
            var logfile = new NLog.Targets.FileTarget("logfile")
            {
                FileName = AppLogFilename,
                DeleteOldFileOnStartup = true
            };

            // Create a logging rule to use the log file target
            var loggingRule = new LoggingRule("LogToFile");
            loggingRule.EnableLoggingForLevels(logLevel, NLog.LogLevel.Fatal);
            loggingRule.Targets.Add(logfile);
            loggingRule.LoggerNamePattern = "*";
            config.LoggingRules.Add(loggingRule);

            // Apply config           
            NLog.LogManager.Configuration = config;

            // Start the Log file
            SharedLogger.logger.Info($"AMDInfo/Main: Starting AMDInfo v1.7.8");


            Console.WriteLine($"\nAMDInfo v1.7.8");
            Console.WriteLine($"==============");
            Console.WriteLine($"By Terry MacDonald 2022\n");

            // First check that we have an AMD Video Card in this PC
            List<string> videoCardVendors = WinLibrary.GetLibrary().GetCurrentPCIVideoCardVendors();
            if (!AMDLibrary.GetLibrary().PCIVendorIDs.All(value => videoCardVendors.Contains(value)))
            {
                SharedLogger.logger.Error($"NVIDIAInfo/Main: There are no AMD Video Cards enabled within this computer. AMDInfo requires at least one AMD Video Card to work. Please use DisplayMagician instead.");
                Console.WriteLine($"ERROR - There are no AMD Video Cards enabled within this computer. AMDInfo requires at least one AMD Video Card to work.");
                Console.WriteLine($"        Please use DisplayMagician instead. See https://displaymagician.littlebitbig.com for more information.");
                Console.WriteLine();
                Environment.Exit(1);
            }

            // Update the configuration
            AMDLibrary amdLibrary = AMDLibrary.GetLibrary();
            WinLibrary winLibrary = WinLibrary.GetLibrary();

            if (args.Length > 0)
            {
                if (args[0] == "save")
                {
                    SharedLogger.logger.Debug($"AMDInfo/Main: Attempting to save the display settings to {args[1]} as save command was provided");
                    if (args.Length != 2)
                    {
                        Console.WriteLine($"ERROR - You need to provide a filename in which to save display settings");
                        SharedLogger.logger.Error($"AMDInfo/Main: ERROR - You need to provide a filename in which to save display settings");
                        Environment.Exit(1);
                    }
                    saveToFile(args[1]);
                    if (!File.Exists(args[1]))
                    {
                        Console.WriteLine($"ERROR - Couldn't save settings to the file {args[1]}");
                        SharedLogger.logger.Error($"AMDInfo/Main: ERROR - Couldn't save settings to the file {args[1]}");
                        Environment.Exit(1);
                    }
                }
                else if (args[0] == "load")
                {
                    SharedLogger.logger.Debug($"AMDInfo/Main: Attempting to use the display settings in {args[1]} as load command was provided");
                    if (args.Length != 2)
                    {
                        Console.WriteLine($"ERROR - You need to provide a filename from which to load display settings");
                        SharedLogger.logger.Error($"AMDInfo/Main: ERROR - You need to provide a filename from which to load display settings");
                        Environment.Exit(1);
                    }
                    if (!File.Exists(args[1]))
                    {
                        Console.WriteLine($"ERROR - Couldn't find the file {args[1]} to load settings from it");
                        SharedLogger.logger.Error($"AMDInfo/Main: ERROR - Couldn't find the file {args[1]} to load settings from it");
                        Environment.Exit(1);
                    }
                    loadFromFile(args[1]);
                }
                else if (args[0] == "possible")
                {
                    SharedLogger.logger.Debug($"AMDInfo/Main: showing if the {args[1]} is a valid display cofig file as possible command was provided");
                    if (args.Length != 2)
                    {
                        Console.WriteLine($"ERROR - You need to provide a filename from which we will check if the display settings are possible");
                        SharedLogger.logger.Error($"AMDInfo/Main: ERROR - You need to provide a filename from which we will check if the display settings are possible");
                        Environment.Exit(1);
                    }
                    if (!File.Exists(args[1]))
                    {
                        Console.WriteLine($"ERROR - Couldn't find the file {args[1]} to check the settings from it");
                        SharedLogger.logger.Error($"AMDInfo/Main: ERROR - Couldn't find the file {args[1]} to check the settings from it");
                        Environment.Exit(1);
                    }
                    possibleFromFile(args[1]);
                }
                else if (args[0] == "equal")
                {
                    SharedLogger.logger.Debug($"AMDInfo/Main: The equal command was provided");
                    if (args.Length == 3)
                    {
                        if (!File.Exists(args[1]))
                        {
                            Console.WriteLine($"ERROR - Couldn't find the file {args[1]} to check the settings from it\n");
                            SharedLogger.logger.Error($"AMDInfo/Main: ERROR - Couldn't find the file {args[1]} to check the settings from it");
                            Environment.Exit(1);
                        }
                        if (!File.Exists(args[2]))
                        {
                            Console.WriteLine($"ERROR - Couldn't find the file {args[2]} to check the settings from it\n");
                            SharedLogger.logger.Error($"AMDInfo/Main: ERROR - Couldn't find the file {args[2]} to check the settings from it");
                            Environment.Exit(1);
                        }
                        equalFromFiles(args[1], args[2]);
                    }
                    else if (args.Length == 2)
                    {
                        if (!File.Exists(args[1]))
                        {
                            Console.WriteLine($"ERROR - Couldn't find the file {args[1]} to check the settings from it\n");
                            SharedLogger.logger.Error($"AMDInfo/Main: ERROR - Couldn't find the file {args[1]} to check the settings from it");
                            Environment.Exit(1);
                        }
                        equalFromFiles(args[1]);
                    }
                    else
                    {
                        Console.WriteLine($"ERROR - You need to provide two filenames in order for us to see if they are equal.");
                        Console.WriteLine($"        Equal means they are exactly the same.");
                        SharedLogger.logger.Error($"AMDInfo/Main: ERROR - You need to provide two filenames in order for us to see if they are equal.");
                        Environment.Exit(1);
                    }
                }
                else if (args[0] == "currentids")
                {
                    SharedLogger.logger.Debug($"AMDInfo/Main: showing currently connected display ids as currentids command was provided");
                    Console.WriteLine("The current display identifiers are:");
                    SharedLogger.logger.Info($"AMDInfo/Main: The current display identifiers are:");
                    foreach (string displayId in amdLibrary.CurrentDisplayIdentifiers)
                    {
                        Console.WriteLine(@displayId);
                        SharedLogger.logger.Info($@"{displayId}");
                    }
                }
                else if (args[0] == "allids")
                {
                    SharedLogger.logger.Debug($"AMDInfo/Main: showing all display ids as allids command was provided");
                    Console.WriteLine("All connected display identifiers are:");
                    SharedLogger.logger.Info($"AMDInfo/Main: All connected display identifiers are:");
                    foreach (string displayId in amdLibrary.GetAllConnectedDisplayIdentifiers())
                    {
                        Console.WriteLine(@displayId);
                        SharedLogger.logger.Info($@"{displayId}");
                    }
                }
                else if (args[0] == "print")
                {
                    SharedLogger.logger.Debug($"AMDInfo/Main: printing display info as print command was provided");
                    Console.WriteLine(amdLibrary.PrintActiveConfig());
                }
                else if (args[0] == "help" || args[0] == "--help" || args[0] == "-h" || args[0] == "/?" || args[0] == "-?")
                {
                    SharedLogger.logger.Debug($"AMDInfo/Main: Showing help as help command was provided");
                    showHelp();
                    Environment.Exit(1);
                }
                else
                {
                    SharedLogger.logger.Debug($"AMDInfo/Main: Showing help as an invalid command was provided");
                    showHelp();
                    Console.WriteLine("*** ERROR - Invalid command line parameter provided! ***\n");
                    Environment.Exit(1);
                }
            }
            else
            {
                SharedLogger.logger.Debug($"AMDInfo/Main: Showing help as no command was provided");
                showHelp();
                Environment.Exit(1);
            }
            Console.WriteLine();
            Environment.Exit(0);
        }

        static void showHelp()
        {
            Console.WriteLine($"AMDInfo is a little program to help test setting display layout and HDR settings in Windows 10 64-bit and later.\n");
            Console.WriteLine($"You need to have AMD Radeon Software Adrenalin 2020 Edition 21.2.1 or later installed and an AMD video card installed and active.\n");
            Console.WriteLine($"You can run it without any command line parameters, and it will print all the information it can find from the \nAMD and Windows Display CCD and GDI interfaces.\n");
            Console.WriteLine($"You can also run it with 'AMDInfo save myfilename.cfg' and it will save the current display configuration into\nthe myfilename.cfg file.\n");
            Console.WriteLine($"This is most useful when you subsequently use the 'AMDInfo load myfilename.cfg' command, as it will load the\ndisplay configuration from the myfilename.cfg file and make it live. In this way, you can make yourself a library\nof different cfg files with different display layouts, then use the AMDInfo load command to swap between them.\n\n");
            Console.WriteLine($"Valid commands:\n");
            Console.WriteLine($"\t'AMDInfo print' will print information about your current display setting.");
            Console.WriteLine($"\t'AMDInfo save myfilename.cfg' will save your current display setting to the myfilename.cfg file.");
            Console.WriteLine($"\t'AMDInfo load myfilename.cfg' will load and apply the display setting in the myfilename.cfg file.");
            Console.WriteLine($"\t'AMDInfo possible myfilename.cfg' will test the display setting in the myfilename.cfg file to see\n\t\tif it is possible.");
            Console.WriteLine($"\t'AMDInfo equal myfilename.cfg' will test if the display setting in the myfilename.cfg is equal to\n\t\tthe one in use.");
            Console.WriteLine($"\t'AMDInfo equal myfilename.cfg myother.cfg' will test if the display setting in the myfilename.cfg\n\t\tis equal to the one in myother.cfg.");
            Console.WriteLine($"\t'AMDInfo currentids' will display the display identifiers for all active displays.");
            Console.WriteLine($"\t'AMDInfo allids' will display the display identifiers for all displays that are active or can be \n\t\tmade active.");
            Console.WriteLine($"\nUse DisplayMagician to store display settings for each game you have. https://github.com/terrymacdonald/DisplayMagician\n");
        }

        static void saveToFile(string filename)
        {
            SharedLogger.logger.Trace($"AMDInfo/saveToFile: Attempting to save the current display configuration to the {filename}.");

            SharedLogger.logger.Trace($"AMDInfo/saveToFile: Getting the current Active Config");
            // Get references to the libraries used
            AMDLibrary amdLibrary = AMDLibrary.GetLibrary();
            WinLibrary winLibrary = WinLibrary.GetLibrary();
            // Get the current configuration
            myDisplayConfig.AMDConfig = amdLibrary.ActiveDisplayConfig;
            myDisplayConfig.WindowsConfig = winLibrary.ActiveDisplayConfig;

            SharedLogger.logger.Trace($"AMDInfo/saveToFile: Attempting to convert the current Active Config objects to JSON format");
            // Save the object to file!
            try
            {
                SharedLogger.logger.Trace($"AMDInfo/saveToFile: Attempting to convert the current Active Config objects to JSON format");

                var json = JsonConvert.SerializeObject(myDisplayConfig, Formatting.Indented, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Include,
                    DefaultValueHandling = DefaultValueHandling.Populate,
                    TypeNameHandling = TypeNameHandling.Auto

                });


                if (!string.IsNullOrWhiteSpace(json))
                {
                    SharedLogger.logger.Error($"AMDInfo/saveToFile: Saving the display settings to {filename}.");

                    File.WriteAllText(filename, json, Encoding.Unicode);

                    SharedLogger.logger.Error($"AMDInfo/saveToFile: Display settings successfully saved to {filename}.");
                    Console.WriteLine($"Display settings successfully saved to {filename}.");
                }
                else
                {
                    SharedLogger.logger.Error($"AMDInfo/saveToFile: The JSON string is empty after attempting to convert the current Active Config objects to JSON format");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AMDInfo/saveToFile: ERROR - Unable to save the profile repository to the {filename}.");
                SharedLogger.logger.Error(ex, $"AMDInfo/saveToFile: Saving the display settings to the {filename}.");
            }
        }

        static void loadFromFile(string filename)
        {
            string json = "";
            try
            {
                SharedLogger.logger.Trace($"AMDInfo/loadFromFile: Attempting to load the display configuration from {filename} to use it.");
                json = File.ReadAllText(filename, Encoding.Unicode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AMDInfo/loadFromFile: ERROR - Tried to read the JSON file {filename} to memory but File.ReadAllTextthrew an exception.");
                SharedLogger.logger.Error(ex, $"AMDInfo/loadFromFile: Tried to read the JSON file {filename} to memory but File.ReadAllTextthrew an exception.");
            }

            if (!string.IsNullOrWhiteSpace(json))
            {
                SharedLogger.logger.Trace($"AMDInfo/loadFromFile: Contents exist within {filename} so trying to read them as JSON.");
                try
                {
                    myDisplayConfig = JsonConvert.DeserializeObject<AMDINFO_DISPLAY_CONFIG>(json, new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore,
                        NullValueHandling = NullValueHandling.Ignore,
                        DefaultValueHandling = DefaultValueHandling.Include,
                        TypeNameHandling = TypeNameHandling.Auto,
                        ObjectCreationHandling = ObjectCreationHandling.Replace
                    });
                    SharedLogger.logger.Trace($"AMDInfo/loadFromFile: Successfully parsed {filename} as JSON.");

                    // We have to patch the adapter IDs after we load a display config because Windows changes them after every reboot :(
                    WinLibrary.GetLibrary().PatchWindowsDisplayConfig(ref myDisplayConfig.WindowsConfig);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"AMDInfo/loadFromFile: ERROR - Tried to parse the JSON in the {filename} but the JsonConvert threw an exception.");
                    SharedLogger.logger.Error(ex, $"AMDInfo/loadFromFile: Tried to parse the JSON in the {filename} but the JsonConvert threw an exception.");
                }

                // Get references to the libraries used
                AMDLibrary amdLibrary = AMDLibrary.GetLibrary();
                WinLibrary winLibrary = WinLibrary.GetLibrary();

                if (!amdLibrary.IsActiveConfig(myDisplayConfig.AMDConfig) && !winLibrary.IsActiveConfig(myDisplayConfig.WindowsConfig))
                {
                    if (amdLibrary.IsPossibleConfig(myDisplayConfig.AMDConfig))
                    {
                        SharedLogger.logger.Trace($"AMDInfo/loadFromFile: The AMD display settings within {filename} are possible to use right now, so we'll use attempt to use them.");
                        Console.WriteLine($"Attempting to apply AMD display config from {filename}");
                        bool itWorkedforAMD = amdLibrary.SetActiveConfig(myDisplayConfig.AMDConfig);

                        if (itWorkedforAMD)
                        {
                            SharedLogger.logger.Trace($"AMDInfo/loadFromFile: The AMD display settings within {filename} were successfully applied.");
                            // Lets update the screens so Windows knows whats happening
                            // NVIDIA makes such large changes to the available screens in windows, we need to do this.
                            winLibrary.UpdateActiveConfig();

                            // Then let's try to also apply the windows changes
                            // Note: we are unable to check if the Windows CCD display config is possible, as it won't match if either the current display config is a Eyefinity config,
                            // or if the display config we want to change to is a Eyefinity config. So we just have to assume that it will work!
                            bool itWorkedforWindows = winLibrary.SetActiveConfig(myDisplayConfig.WindowsConfig);
                            if (itWorkedforWindows)
                            {
                                bool itWorkedforAMDColor = amdLibrary.SetActiveConfigOverride(myDisplayConfig.AMDConfig);

                                if (itWorkedforAMDColor)
                                {
                                    SharedLogger.logger.Trace($"AMDInfo/loadFromFile: The AMD display settings that override windows within {filename} were successfully applied.");
                                    Console.WriteLine($"AMDInfo Display config successfully applied");
                                }
                                else
                                {
                                    SharedLogger.logger.Trace($"AMDInfo/loadFromFile: The AMD display settings that override windows within {filename} were NOT applied correctly.");
                                    Console.WriteLine($"ERROR - AMDInfo AMD Override settings were not applied correctly.");
                                }
                            }
                            else
                            {
                                SharedLogger.logger.Trace($"AMDInfo/loadFromFile: The Windows CCD display settings within {filename} were NOT applied correctly.");
                                Console.WriteLine($"ERROR - AMDInfo Windows CCD settings were not applied correctly.");
                            }

                        }
                        else
                        {
                            SharedLogger.logger.Trace($"AMDInfo/loadFromFile: The AMD display settings within {filename} were NOT applied correctly.");
                            Console.WriteLine($"ERROR - AMDInfo AMD display settings were not applied correctly.");
                        }

                    }
                    else
                    {
                        Console.WriteLine($"AMDInfo/loadFromFile: ERROR - Cannot apply the AMD display config in {filename} as it is not currently possible to use it.");
                        SharedLogger.logger.Error($"AMDInfo/loadFromFile: ERROR - Cannot apply the AMD display config in {filename} as it is not currently possible to use it.");
                    }
                }
                else
                {
                    Console.WriteLine($"The display settings in {filename} are already installed. No need to install them again. Exiting.");
                    SharedLogger.logger.Info($"AMDInfo/loadFromFile: The display settings in {filename} are already installed. No need to install them again. Exiting.");
                }



            }
            else
            {
                Console.WriteLine($"AMDInfo/loadFromFile: ERROR - The {filename} profile JSON file exists but is empty! So we're going to treat it as if it didn't exist.");
                SharedLogger.logger.Error($"AMDInfo/loadFromFile: The {filename} profile JSON file exists but is empty! So we're going to treat it as if it didn't exist.");
            }
        }

        static void possibleFromFile(string filename)
        {
            // Get references to the libraries used
            AMDLibrary amdLibrary = AMDLibrary.GetLibrary();
            WinLibrary winLibrary = WinLibrary.GetLibrary();

            string json = "";
            try
            {
                SharedLogger.logger.Trace($"AMDInfo/possibleFromFile: Attempting to load the display configuration from {filename} to see if it's possible.");
                json = File.ReadAllText(filename, Encoding.Unicode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AMDInfo/possibleFromFile: ERROR - Tried to read the JSON file {filename} to memory but File.ReadAllTextthrew an exception.");
                SharedLogger.logger.Error(ex, $"AMDInfo/possibleFromFile: Tried to read the JSON file {filename} to memory but File.ReadAllTextthrew an exception.");
            }

            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    SharedLogger.logger.Trace($"AMDInfo/possibleFromFile: Contents exist within {filename} so trying to read them as JSON.");
                    myDisplayConfig = JsonConvert.DeserializeObject<AMDINFO_DISPLAY_CONFIG>(json, new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore,
                        NullValueHandling = NullValueHandling.Ignore,
                        DefaultValueHandling = DefaultValueHandling.Include,
                        TypeNameHandling = TypeNameHandling.Auto,
                        ObjectCreationHandling = ObjectCreationHandling.Replace
                    });
                    SharedLogger.logger.Trace($"AMDInfo/possibleFromFile: Successfully parsed {filename} as JSON.");

                    // We have to patch the adapter IDs after we load a display config because Windows changes them after every reboot :(
                    WinLibrary.GetLibrary().PatchWindowsDisplayConfig(ref myDisplayConfig.WindowsConfig);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"AMDInfo/possibleFromFile: ERROR - Tried to parse the JSON in the {filename} but the JsonConvert threw an exception.");
                    SharedLogger.logger.Error(ex, $"AMDInfo/possibleFromFile: Tried to parse the JSON in the {filename} but the JsonConvert threw an exception.");
                }

                if (amdLibrary.IsPossibleConfig(myDisplayConfig.AMDConfig) && winLibrary.IsPossibleConfig(myDisplayConfig.WindowsConfig))
                {
                    SharedLogger.logger.Trace($"AMDInfo/possibleFromFile: The AMD display settings in {filename} are able to be applied on this computer if you'd like to apply them.");
                    Console.WriteLine($"The AMD display settings in {filename} are able to be applied on this computer if you'd like to apply them.");
                    Console.WriteLine($"You can apply them with the command 'AMDInfo load {filename}'");                    
                }
                else
                {
                    SharedLogger.logger.Trace($"AMDInfo/possibleFromFile: The {filename} file contains a display setting that will NOT work on this computer right now.");
                    SharedLogger.logger.Trace($"AMDInfo/possibleFromFile: This may be because the required screens are turned off, or some other change has occurred on the PC.");
                    Console.WriteLine($"The {filename} file contains a display setting that will NOT work on this computer right now.");
                    Console.WriteLine($"This may be because the required screens are turned off, or some other change has occurred on the PC.");
                }

            }
            else
            {
                SharedLogger.logger.Error($"AMDInfo/possibleFromFile: The {filename} profile JSON file exists but is empty! So we're going to treat it as if it didn't exist.");
                Console.WriteLine($"AMDInfo/possibleFromFile: The {filename} profile JSON file exists but is empty! So we're going to treat it as if it didn't exist.");
            }
        }


        static void equalFromFiles(string filename, string otherFilename)
        {
            string json = "";
            string otherJson = "";
            AMDINFO_DISPLAY_CONFIG displayConfig = new AMDINFO_DISPLAY_CONFIG();
            AMDINFO_DISPLAY_CONFIG otherDisplayConfig = new AMDINFO_DISPLAY_CONFIG();
            SharedLogger.logger.Trace($"AMDInfo/equalFromFile: Attempting to compare the display configuration from {filename} and {otherFilename} to see if they are equal.");
            try
            {
                json = File.ReadAllText(filename, Encoding.Unicode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AMDInfo/equalFromFile: ERROR - Tried to read the JSON file {filename} to memory but File.ReadAllTextthrew an exception.");
                SharedLogger.logger.Error(ex, $"AMDInfo/equalFromFile: Tried to read the JSON file {filename} to memory but File.ReadAllTextthrew an exception.");
            }

            try
            {
                otherJson = File.ReadAllText(otherFilename, Encoding.Unicode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AMDInfo/equalFromFile: ERROR - Tried to read the JSON file {otherFilename} to memory but File.ReadAllTextthrew an exception.");
                SharedLogger.logger.Error(ex, $"AMDInfo/equalFromFile: Tried to read the JSON file {otherFilename} to memory but File.ReadAllTextthrew an exception.");
            }

            if (!string.IsNullOrWhiteSpace(json) && !string.IsNullOrWhiteSpace(otherJson))
            {
                try
                {
                    SharedLogger.logger.Trace($"AMDInfo/equalFromFile: Contents exist within {filename} so trying to read them as JSON.");
                    displayConfig = JsonConvert.DeserializeObject<AMDINFO_DISPLAY_CONFIG>(json, new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore,
                        NullValueHandling = NullValueHandling.Ignore,
                        DefaultValueHandling = DefaultValueHandling.Include,
                        TypeNameHandling = TypeNameHandling.Auto,
                        ObjectCreationHandling = ObjectCreationHandling.Replace
                    });
                    SharedLogger.logger.Trace($"AMDInfo/equalFromFile: Successfully parsed {filename} as JSON.");

                    // We have to patch the adapter IDs after we load a display config because Windows changes them after every reboot :(
                    WinLibrary.GetLibrary().PatchWindowsDisplayConfig(ref displayConfig.WindowsConfig);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"AMDInfo/equalFromFile: ERROR - Tried to parse the JSON in the {filename} but the JsonConvert threw an exception.");
                    SharedLogger.logger.Error(ex, $"AMDInfo/equalFromFile: Tried to parse the JSON in the {filename} but the JsonConvert threw an exception.");
                }
                try
                {
                    SharedLogger.logger.Trace($"AMDInfo/equalFromFile: Contents exist within {otherFilename} so trying to read them as JSON.");
                    otherDisplayConfig = JsonConvert.DeserializeObject<AMDINFO_DISPLAY_CONFIG>(otherJson, new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore,
                        NullValueHandling = NullValueHandling.Ignore,
                        DefaultValueHandling = DefaultValueHandling.Include,
                        TypeNameHandling = TypeNameHandling.Auto,
                        ObjectCreationHandling = ObjectCreationHandling.Replace
                    });
                    SharedLogger.logger.Trace($"AMDInfo/equalFromFile: Successfully parsed {filename} as JSON.");

                    // We have to patch the adapter IDs after we load a display config because Windows changes them after every reboot :(
                    WinLibrary.GetLibrary().PatchWindowsDisplayConfig(ref otherDisplayConfig.WindowsConfig);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"AMDInfo/equalFromFile: ERROR - Tried to parse the JSON in the {filename} but the JsonConvert threw an exception.");
                    SharedLogger.logger.Error(ex, $"AMDInfo/equalFromFile: Tried to parse the JSON in the {filename} but the JsonConvert threw an exception.");
                }

                if (displayConfig.WindowsConfig.Equals(otherDisplayConfig.WindowsConfig) && displayConfig.AMDConfig.Equals(otherDisplayConfig.AMDConfig))
                {
                    SharedLogger.logger.Trace($"AMDInfo/equalFromFile: The AMD display settings in {filename} and {otherFilename} are equal.");
                    Console.WriteLine($"The AMD display settings in {filename} and {otherFilename} are equal.");
                }
                else
                {
                    SharedLogger.logger.Trace($"AMDInfo/equalFromFile: The AMD display settings in {filename} and {otherFilename} are NOT equal.");
                    Console.WriteLine($"The AMD display settings in {filename} and {otherFilename} are NOT equal.");
                }

            }
            else
            {
                SharedLogger.logger.Error($"AMDInfo/equalFromFile: The {filename} or {otherFilename} JSON files exist but at least one of them is empty! Cannot continue.");
                Console.WriteLine($"AMDInfo/equalFromFile: The {filename} or {otherFilename} JSON files exist but at least one of them is empty! Cannot continue.");
            }
        }

        static void equalFromFiles(string filename)
        {
            string json = "";
            AMDINFO_DISPLAY_CONFIG displayConfig = new AMDINFO_DISPLAY_CONFIG();
            SharedLogger.logger.Trace($"AMDInfo/equalFromFile: Attempting to compare the display configuration from {filename} and the currently active display configuration to see if they are equal.");
            try
            {
                json = File.ReadAllText(filename, Encoding.Unicode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AMDInfo/equalFromFile: ERROR - Tried to read the JSON file {filename} to memory but File.ReadAllTextthrew an exception.");
                SharedLogger.logger.Error(ex, $"AMDInfo/equalFromFile: Tried to read the JSON file {filename} to memory but File.ReadAllTextthrew an exception.");
            }

            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    SharedLogger.logger.Trace($"NVIDIAInfo/equalFromFile: Contents exist within {filename} so trying to read them as JSON.");
                    displayConfig = JsonConvert.DeserializeObject<AMDINFO_DISPLAY_CONFIG>(json, new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore,
                        NullValueHandling = NullValueHandling.Ignore,
                        DefaultValueHandling = DefaultValueHandling.Include,
                        TypeNameHandling = TypeNameHandling.Auto,
                        ObjectCreationHandling = ObjectCreationHandling.Replace
                    });
                    SharedLogger.logger.Trace($"AMDInfo/equalFromFile: Successfully parsed {filename} as JSON.");

                    // We have to patch the adapter IDs after we load a display config because Windows changes them after every reboot :(
                    WinLibrary.GetLibrary().PatchWindowsDisplayConfig(ref displayConfig.WindowsConfig);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"AMDInfo/equalFromFile: ERROR - Tried to parse the JSON in the {filename} but the JsonConvert threw an exception.");
                    SharedLogger.logger.Error(ex, $"AMDInfo/equalFromFile: Tried to parse the JSON in the {filename} but the JsonConvert threw an exception.");
                    return;
                }
                if (displayConfig.WindowsConfig.Equals(WinLibrary.GetLibrary().GetActiveConfig()) && displayConfig.AMDConfig.Equals(AMDLibrary.GetLibrary().GetActiveConfig()))
                //if (displayConfig.NVIDIAConfig.Equals(NVIDIALibrary.GetLibrary().GetActiveConfig()))
                //if (displayConfig.WindowsConfig.Equals(WinLibrary.GetLibrary().GetActiveConfig()))
                {
                    SharedLogger.logger.Trace($"AMDInfo/equalFromFile: The AMD display settings in {filename} and the currently active display configuration are equal.");
                    Console.WriteLine($"The AMD display settings in {filename} and the currently active display configuration are equal.");
                }
                else
                {
                    SharedLogger.logger.Trace($"AMDInfo/equalFromFile: The NVIDIA display settings in {filename} and the currently active display configuration are NOT equal.");
                    Console.WriteLine($"The AMD display settings in {filename} and the currently active display configuration are NOT equal.");
                }

            }
            else
            {
                SharedLogger.logger.Error($"AMDInfo/equalFromFile: The {filename} JSON file exists but is empty! Cannot continue.");
                Console.WriteLine($"AMDInfo/equalFromFile: The {filename} JSON file exists but is empty! Cannot continue.");
            }
        }
    }
}
