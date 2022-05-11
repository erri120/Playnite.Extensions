# Changelog

## 2.5.3

- Game Uninstallation will now display a global cancelable progress

## 2.5.2

- fix dependency issue

## 2.5.1

- more logging in the Assembly Loader
- updated dependencies

## 2.5.0

- Better handling of missing Assemblies
- Add "Uninstall and Remove" option (see [README](README.md#gamemanagement))

## 2.4.0

- Add option to enable/disable the default F95zone icon for Games
- Add "Series" field for DLsite

## 2.3.0

- New Generic Plugin: GameManagement, see [README](README.md#GameManagement) for more information
- Fixed parsing of floating point numbers

## 2.2.0

- You can now map multiple input properties to the same Playnite property
- The Version field of every Plugin Assembly now changes with each release
- All Plugins and Metadata Providers will now provide Playnite with a list of supported fields

## 2.1.0

- Add Fanza Metadata Provider
- Fixed locale issue with DLsite

## 2.0.3

- Option changed from "Completed" games to _finished_ games, these include games that have the "Completed" or "Abandoned" F95zone label
- Stop tracking deleted games

## 2.0.2

- Added option to look for updates for games that have the F95zone "Completed" label (disabled by default to reduce server load)

## 2.0.1

- DLsite Metadata Provider is now URL instead of Id based
- Fixed `ArgumentNullException` when accessing Links of Games without Links (F95zone Updater)
- Fixed `ArgumentNullException` when the user selected no item from the search (DLsite and F95zone)

## 2.0.0

Complete rework, removed everything and started anew. This release only has the F95zone and DLsite Metadata Plugin available. Both now have lots of settings and a search function you can play with.

## 1.7.2

- Updated ExtensionUpdater notifications to be more helpful
- ExtensionUpdater can now find Extensions in `%appdata%\Playnite\Extensions`

## 1.7.1

- Fixed culture difference when parsing doubles (`4.50` can not be parsed because the culture expects `4,50`)

## 1.7.0

- Updated Playnite SDK to 5.5.0 (for Playnite 8.x)
- Added Age Ratings Metadata Field to DLSite, F95Zone and Jastusa Metadata Plugins
- Added Platforms Metadata Field to VNDB Metadata Plugin

## 1.6.1

- Fixed Playnite crash when an `HttpException` occurred

## 1.6.0

- Added Screenshot Plugin

## 1.5.0

- Added Jastusa Metadata Provider

## 1.4.0

- Reworked F95Zone Metadata Provider with better Descriptions, Community Ratings and more stability changes.
- Lots of fixes and backend changes

## 1.3.0

- Added JPN to ENG translation option for DLSite genres

## 1.2.0

- Added VNDB Metadata Provider
- Fixed F95Zone NullException for some links
- Updated Icons for all Extensions
- Image selection for all Extensions

## 1.1.0

- Added F95Zone Metadata Provider

## 1.0.0

- First release with support for DLSite
