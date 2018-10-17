# Profiler Analyser

## What is this
The profile analyser tool augments the standard Unity Profiler. It provides multi frame analysis of the profiling data.

Features:
* Multi frame analysis of a single scan
  * Each marker is shown with its median, min, max values over all frames, including histogram and box and whisker plots to view the distribution
  * Various filtering options are available to limit the markers displayed by thread, call depth and name substrings.
  * Data can be sorted for each of the displayed values.
* Comparison of two multi frame profile scans
  * Each marker is shown with the difference in values between the scans, including with a visualisation to help quickly identify the key differences.
  * Supports comparison of scans from two different unity versions

## How to run
Add the ProfileAnalyser folder to your unity project, as a child of an Editor folder.
The 'Profile Analyser' tool is opened via the menu item below the 'Window' Menu in the Unity menu bar.

## Capturing data
Use the standard Unity profiler to record some profiling data from your application.
In the ProfileAnalyser 'Grab' the profiler data for the range of frames you are interested in.
The single frame analysis will instantly appear.
This capture can be saved as a .pdata file.

## Comparing two captures
Two .pdata files can be loaded into the 'Compare' tab via the 'Load left' and 'Load right' buttons.
Once two captures are loaded the comparison results will instantly appear.

## More information
See [Introduction PDF](profile-analyser-introduction.pdf)

