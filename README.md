# Tactics Ogre PSP Datamining

Datamining related things for Tactics Ogre LUCT PSP edition.


## [imhex-patterns](./imhex-patterns)

Hex patterns and text encodings for using with the [ImHex Hex Editor](https://imhex.werwolv.net/).

To use it, add the imhex-patterns folder to `ImHex -> Help -> Settings -> Folders`.

Note: due to a bug, pattern auto-loading won't work on Windows until ImHex next release (>v1.24.3).


## [extracts](./extracts)

Data extraction scripts for my personal needs, it might not be very easy to prepare an environment for running it, but some parts maybe useful:

* [ExtractScreenplay.cs#L22](./extracts/ExtractScreenplay/ExtractScreenplay.cs#L22): inferred name of some global flags.
* [ExtractScreenplay.cs#L190](./extracts/ExtractScreenplay/ExtractScreenplay.cs#L190): screenplay invk/task instruction parameters.
