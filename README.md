![Godot SimplePSDImporter Logo](https://raw.githubusercontent.com/kebabskal/godot-simplepsd/main/Logo/SimplePSDImporter-Logo.png)

# Godot-SimplePSD

Simple cross-platform PSD Importer for Godot written in C\#.

Imports PSD files for use as textures and sprites in Godot 4.

Tested on Windows and MacOS.

## Features

- Enables you to use PSD files directly in Godot.
- Easy to use. Just drop your PSD files directly into your project folder.
- Supports Uncompressed and RLE-Compressed PSD files.
- Works on Windows and Mac.
- No native libraries used.
- Lean codebase if you want to customize or contribute.
- Alpha channel support.
- Option to Premultiply Alpha.

## Known limitations:

- Requires Mono build of Godot 4.
- Only supports 8-bits-per-channel.
- Requires "Maximize Compatibility" (on by default) to be checked when saving.
- Doesn't export layers separately.
- Few import options.

## Future improvements

- Support for higher bit depths.
- Export of individual layers.
- Export of PSD metadata such as Slices.

## Credits

- Written by Hannes Rahm (@kebabskal) live on twitch. With lots of help from the galaxy-brained chat.
