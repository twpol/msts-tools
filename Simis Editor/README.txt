Simis Editor
************

Website: http://jgrmsts.codeplex.com/
License: Microsoft Public License.

Getting Started
===============

1. Install Microsoft .NET Framework 3.5 SP1.
2. Extract all Simis Editor files to a location of your choice.
3. Run "Simis Editor.exe".

What Simis Editor Current Supports
==================================

* Opening most supported Microsoft Train Simulator game files.
   * Activity (.act)
   * Path (.pat)
   * Service (.srv)
   * Shape (.s)
   * Sound Management (.sms)
   * Terrain (.t)
   * Traffic Pattern (.trf)
   * World (.w)
   * World Sound (.ws)
* Displaying file contents as a tree, with blocks and values (some values have names, most don't).
* Editing of the values by selecting the block or value and using the property grid on the right.
* Saving files in three different formats: Unicode text, binary, and compressed binary.

What Simis Editor Doesn't Support (Yet)
=======================================

* Creating new files.
* Opening other similar game files. More will be supported in future releases.
* Editing the tree structure of files.
* Edit menu functions: undo, redo, cut, copy, paste, delete.

Version History
===============

--- 0.2 --- XXXXXXXXX 2009 ---
* Writing support for all three formats (Unicode text, binary, and compressed binary).
* Enabled File>Save and File>Save As.
* BNFs for Terrain (.t) and Word (.w) updated to avoid :buffer type (which can't be saved).
* Reader now supports Unicode text files containing "hh:mm:ss" (parsed as 3 :uint values).
* Help>Test Simis Files now tests writing in addition to reading.
* GUI update to get native theming on Windows Vista and 7.
* Update checking now only checks once per day.
* Drag-drop a file onto the editor now opens the file.

--- 0.1 --- 15th June 2009 ---
* Reading support for Activity (.act), Path (.pat), Service (.srv), Shape (.s), Sound Management (.sms), Terrain (.t), Traffic Pattern (.trf), World (.w), World Sound (.ws) files.
