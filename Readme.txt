JGR MSTS Editors & Tools
************************

Website: http://jgrmsts.codeplex.com/
License: Microsoft Public License.


Getting Started
===============

1. Make sure you have the Microsoft .NET 3.5 SP1 Framework installed.
    * Download from http://www.microsoft.com/downloads/details.aspx?FamilyID=ab99342f-5d1a-413d-8319-81da479ab0d7.
    * Included with Windows 7 and later.
2. Extract all files to an empty location of your choice.
3. Run the tools.
    * Simis Editor v0.3
    * Simis File


Tools - Simis Editor v0.3
=========================

* What Works:
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
* What Doesn't Work (Yet):
  * Creating new files.
  * Opening other similar game files. More will be supported in future releases.
  * Editing the tree structure of files.
  * Edit menu functions: undo, redo, cut, copy, paste, delete.


Tools - Simis File
==================

Performs operations on individual or collections of Simis files.

  SIMISFILE /F[ORMATS]

  SIMISFILE /D[DUMP] [/V[ERBOSE]] [file ...]

  SIMISFILE /N[ORMALIZE] [file ...]

  SIMISFILE /T[EST] [/V[ERBOSE]] [file ...] [dir ...]

  /FORMATS  Displays a list of the supported Simis file formats.
  /DUMP     Reads all files and displays the resulting Simis tree for each.
  /NORMALIZE
            Normalizes the specified files for comparisons. For binary files,
            this uncompresses the contents only. For text files, whitespace,
            quoted strings and numbers are all sanitized. The normalized file
            is written to the current directory, with the '.normalized'
            extension added if the source file is also in the current
            directory. This will overwrite files in the current directory only.
  /TEST     Tests all the files specified and found in the directories
            specified against the reading and writing code. No files are
            changed. A report of read/write success by file type is produced.
  /VERBOSE  Produces more output. For /DUMP and /TEST, displays the individual
            failures encountered while reading or writing files.
  file      One or more Simis files to process.
  dir       One or more directories containing Simis files. These will be
            scanned recursively.


Version History
===============

--- ??? February 2010 ---
* Simis Editor v0.3
  * Enabled Edit>Undo and Edit>Redo.
  * Undoing/redoing back to last saved state is identified as saved (no prompt on exit, etc.).
  * Added save prompt when closing window.
  * Added and enabled Edit>Select All.
  * Enabled Edit>Cut, Edit>Copy, Edit>Paste, Edit>Delete.
* Simis File
  * Added /DUMP option which prints out the node tree.
  * /NORMALIZE can normalize ACE files.
* Libraries
  * BNFs for Route (.trk), Route Database (.rdb), Route Items (.rit), Route Markers (.mkr), Route REF??? (.ref), Track Database (.tdb), Track Items (.tit), Train (Consist) (.con), Train (Consist) Cab View (.cvf), Train (Consist) Engine (.eng), Train (Consist) Wagon (.wag) added.
  * Added new BNF types :word and :byte for exploratory work.
  * comment() and skip() blocks now count parentheses.
  * "-" and "/" no longer quoted in written files.
  * Added _skip() and _info() block skipping.
  * Many performance improvements to reading and writing files.
  * Added a string indexer for SimisTreeNodes to find first child of given type.
  * Added ToValue<T>() to SimisTreeNodes for reading values from nodes.
  * Added tracing for BNF and FSM code.
  * SimisFormat selection based on filename and header.
  * SimisTestableStream can normalize ACE files.
  * SimisTestableStream will better normalize numeric values in text files.
  * New library, Jgr.Msts, containing MSTS-specific classes utilising the other libraries and some coordinate system transformations.
  * More unit tests.

--- 8th November 2009 ---
* Simis Editor v0.2
  * Enabled File>Save and File>Save As.
  * Help>Test Simis Files replaced with "SimisFile.exe".
  * Updated to get native theming on Windows Vista and 7.
  * Update checking now only checks once per day.
  * Drag-drop a file onto the editor now opens the file.
* Simis File
   * Testing for reading and writing files.
   * Normalization of files.
* Libraries
  * Writing support for all three formats (Unicode text, binary, and compressed binary).
  * BNFs for Activity Save (.asv), Environment (.env), Hazard (.haz), IOM (.iom), Material Palette (.pal) and Shape Detail (.sd) added.
  * BNF for Terrain (.t) updated to use less of :buffer type.
  * BNF for World (.w) updated to not use :buffer type.
  * Reader now supports Unicode text files containing "hh:mm:ss" (parsed as 3 :uint values).

--- 15th June 2009 ---
* Simis Editor v0.1
* Libraries
  * Reading support for Activity (.act), Path (.pat), Service (.srv), Shape (.s), Sound Management (.sms), Terrain (.t), Traffic Pattern (.trf), World (.w), World Sound (.ws) files.
  * Terrain (.t) and World (.w) use :buffer type.
