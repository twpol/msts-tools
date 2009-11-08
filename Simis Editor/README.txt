Simis Editor v0.2
*****************

Website: http://jgrmsts.codeplex.com/
License: Microsoft Public License.

Getting Started
===============

1. Make sure you have the Microsoft .NET 3.5 SP1 Framework installed.
2. Extract all files to an empty location of your choice.
3. Run "SimisEditor.exe".

What Works
==========

* File > Open... - opening supported file types:
   * Activity (.act)
   * Activity Save (.asv)    [New] [Uses :buffer type]
   * Environment (.env)      [New]
   * Hazard (.haz)           [New]
   * IOM (.iom)              [New]
   * Material Palette (.pal) [New]
   * Path (.pat)
   * Service (.srv)
   * Shape (.s)              [Updated]
   * Shape Detail (.sd)      [New]
   * Sound Management (.sms)
   * Terrain (.t)            [Updated] [Uses :buffer type]
   * Traffic Pattern (.trf)
   * World (.w)              [Updated]
   * World Sound (.ws)
   * Note: Formats which use the :buffer type can be loaded but are not completely parsed and can not be saved correctly.
* File > Save, File > Save As... - saving files in Unicode text, binary or compressed binary. [New]
* Help > Reload Simis Resources - reloads all files from the "Resources" subdirectory (useful for testing). [New]
* All blocks and values in a file can be seen in the main tree view. 
* Values can be edited by selecting the block or value in the tree and using the Property Grid on the right. [New]
* Indirect file opening - dropping a supported file on "Simis Editor.exe" or the application window will open it. [New]


What Doesn't Work (Yet)
=======================

* Creating new files.
* Opening other similar game files. More will be supported in future releases.
* Editing the tree structure of files.
* Edit menu functions: undo, redo, cut, copy, paste, delete.

Version History
===============

--- 0.2 --- 8th November 2009 ---
* Writing support for all three formats (Unicode text, binary, and compressed binary).
* Enabled File>Save and File>Save As.
* BNFs for Activity Save (.asv), Environment (.env), Hazard (.haz), IOM (.iom), Material Palette (.pal) and Shape Detail (.sd) added.
* BNF for Terrain (.t) updated to use less of :buffer type.
* BNF for World (.w) updated to not use :buffer type.
* Reader now supports Unicode text files containing "hh:mm:ss" (parsed as 3 :uint values).
* Help>Test Simis Files replaced with "SimisFile.exe".
* GUI update to get native theming on Windows Vista and 7.
* Update checking now only checks once per day.
* Drag-drop a file onto the editor now opens the file.
* "SimisFile.exe" added with functions:
   * Testing for reading and writing files.
   * Normalization of files.

--- 0.1 --- 15th June 2009 ---
* Reading support for Activity (.act), Path (.pat), Service (.srv), Shape (.s), Sound Management (.sms), Terrain (.t), Traffic Pattern (.trf), World (.w), World Sound (.ws) files.
* Terrain (.t) and World (.w) use :buffer type.
