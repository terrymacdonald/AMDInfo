# AMDInfo

AMDInfo is a test programme designed to exercise the NVAPI library that I developed for DisplayMagician. This little programme helps me validate that the library is working properly, and that it will work when added to the main DisplayMagician code.

AMDInfo records exactly how you setup your display settings, including AMD Eyefinity sccreens, display position, resolution, HDR settings, and even which screen is your main one, and then AMDInfo saves those settings to a file. It works using the AMD API and the Windows Display CCD interface to configure your display settings for you. You can set up your display settings exactly how you like them using AMD Setup and Windows Display Setup, and then use AMDInfo to save those settings to a file.

NOTE: AMDInfo doesn't handle NVIDIA Surround/Mosaic. Please see [NVIDIAInfo](https://github.com/terrymacdonald/NVIDIAInfo) for that!

Command line examples:

- Show what settings you currently are using: `AMDInfo print`
- Save the settings you currently are using to a file to use later: `AMDInfo save my-cool-settings.cfg`
- Load the settings you saved earlier and use them now: `AMDInfo load my-cool-settings.cfg`
- Show whether the display config file can be used: `AMDInfo possible my-cool-settings.cfg`


## To setup this software:

- Firstly, set up your display configuration using AMD settings and the Windows Display settings exactly as you want to use them (e.g. one single AMD Eyefinity window using 3 screens)
- Next, save the settings you currently are using to a file to use later, using a command like `AMDInfo save triple-surround-on.cfg`
- Next, change your display configuration using AMD settings and the Windows Display settings to another display configuration you'd like to have (e.g. 3 single screens without using AMD Surround)
- Next, save those settings to a different file to use later, using a command like `AMDInfo save triple-screen.cfg`

## To swap between different display setups:

Now that you've set up the different display configurations, you can swap between them using a command like this:

- To load the triple screen setup using AMD Eyefinity: `AMDInfo load triple-surround-on.cfg`
- To load the triple screen without AMD Eyefinity: `AMDInfo load triple-screen.cfg`

Feel free to use different config file names, and to set up what ever display configurations you like. Enjoy!

Note: This codebase is unlikely to be supported once DisplayMagician is working, but feel free to fork if you would like. Also feel free to send in suggestions for fixes to the C# AMD library interface. Any help is appreciated!
