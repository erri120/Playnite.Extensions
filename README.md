# Extensions for Playnite

[![Build Status](https://dev.azure.com/erri120/Playnite.Extensions/_apis/build/status/erri120.Playnite.Extensions?branchName=master)](https://dev.azure.com/erri120/Playnite.Extensions/_build/latest?definitionId=7&branchName=master)

Collection of Extensions I created for [Playnite](https://github.com/JosefNemec/Playnite). Links to forum posts: [F95Zone Forum Post](https://f95zone.to/threads/dlsite-and-f95zone-extensions-for-playnite.56493/), [Playnite Forum Post](https://playnite.link/forum/thread-335.html).

- [Installation](#installation)
- [Metadata Providers](#metadata-providers)
  - [DLSite](#dlsite)
  - [F95Zone](#f95zone)
  - [VNDB](#vndb)
- [Jastusa](#jastusa)
- [Troubleshooting](#troubleshooting)

## Installation

1) Get the latest Release from the [Release Tab](https://github.com/erri120/Playnite.Extensions/releases/).
2) Copy the folder to your `Playnite/Extensions/` folder.

## Metadata Providers

### DLSite

**Website**: [ENG](https://www.dlsite.com/ecchi-eng/), [JPN](https://www.dlsite.com/maniax/)

**Supported Fields**:

- Name
- Description
- Developers
- Publishers
- Background Images
- Cover Image
- Genres
- Links
- Release Date

**Usage**:

Copy either the entire URL or just the ID (eg `RE234198` or `RJ173356`) into the Name field, click the _Download Metadata..._ button in the bottom left corner and select _DLSite_.

![how-to-dlsite-1](images/how-to-dlsite-1.png)

Change any fields you want afterwards and click the _Save_ button in the bottom right corner.

![how-to-dlsite-2](images/how-to-dlsite-2.png)

**JPN to ENG**:

Not every game on DLSite has a page in English. In this case you can end up having the same genres twice: in English and in Japanese. To circumvent this, I added an optional feature for converting JPN genres to ENG. They fortunately have an ID system meaning that eg ID `60` is the same in JPN as it is ENG, that being `女性視点` and `Woman's Viewpoint` respectively.

Loading Metadata for a game can take a bit longer if you have this feature installed because it has to connect to DLSite for every genre it doesn't know the translation of. The translation of course gets cached so the time it takes will decrease the more DLSite games you add.

### F95Zone

**Website**: [F95](https://www.f95zone.to)

**Supported Fields**:

- Name
- Description
- Background Images
- Cover Image
- Developers
- Publishers
- Genres
- Tags
- Links
- Release Date
- Community Score

**Usage**:

Copy the entire URL into the Name field, click the _Download Metadata..._ button in the bottom left corner and select _F95Zone_.

![how-to-f95-1](images/how-to-f95-1.png)

Change any fields you want afterwards and click the _Save_ button in the bottom right corner.

![how-to-f95-2](images/how-to-f95-2.png)

### VNDB

**Website**: [VNDB](https://vndb.org/)

**Supported Fields**:

- Name
- Description
- Cover Image
- Background Image
- Release Date
- Community Score
- Genres
- Links

**Usage**:

You can either use the ID (eg: `v11`), Link (eg: `https://vndb.org/v11`) or Name (eg: `Fate/Stay Night`) of the game in the Name field. Click the _Download Metadata..._ button in the bottom left corner and select _VNDB_.

![how-to-vndb-1](images/how-to-vndb-1.png)

You will get a list of search results if you used the Name of the game.

![how-to-vndb-2](images/how-to-vndb-2.png)

Change any fields you want afterwards and click the _Save_ button in the bottom right corner.

![how-to-vndb-3](images/how-to-vndb-3.png)

## Jastusa

**Website**: [Jastusa](https://jastusa.com)

**Supported Fields**:

- Name
- Description
- Developers
- Publishers
- Links
- Cover Image
- Background Images
- Release Date

**Usage**:

Copy the entire URL into the Name field, click the _Download Metadata..._ button in the bottom left corner and select _Jastusa_.

![how-to-jastusa-1](images/how-to-jastusa-1.png)

Change any fields you want afterwards and click the _Save_ button in the bottom right corner.

![how-to-jastusa-2](images/how-to-jastusa-2.png)

## Troubleshooting

If an extension is not working correctly, make sure you take a look at the `playnite.log` file in `%appdata%/Playnite`.
