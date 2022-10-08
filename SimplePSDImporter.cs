#if TOOLS
using Godot;
using Godot.Collections;
using System;
using System.IO;
using System.Collections.Generic;

/*
	EditorImportPlugin that reads and imports PSD files.

	A PSD file is a binary image format encoded in Big Endian.
	It contains multiple sections, most of which is skipped past.

	This implementation only supports 8-bits-per-channel,
	and requires "Maximize Compatibility" to be checked when saving.
	When "Maximize Compatibility" is checked, a flattened image is 
	saved along with the separate layers. This is what we use to 
	get the image data.
*/
public partial class SimplePSDImporter : EditorImportPlugin {
	// Plugin overrides
	public override string _GetImporterName() => "kebabskal.psdimporter";
	public override string _GetVisibleName() => "PSD File";
	public override string[] _GetRecognizedExtensions() => new[] { "psd" };
	public override string _GetSaveExtension() => "res";
	public override string _GetResourceType() => "CompressedTexture2D";
	public override long _GetImportOrder() => 1;
	public override double _GetPriority() => 1;
	public override long _GetPresetCount() => 0;
	public override string _GetPresetName(long presetIndex) => "";
	public override bool _GetOptionVisibility(string path, StringName optionName, Dictionary options) => true;
	public override Array<Dictionary> _GetImportOptions(string path, long presetIndex) {
		var presets = new Array<Dictionary>();

		var generateMipMaps = new Dictionary();
		generateMipMaps.Add("name", "generate_mip_maps");
		generateMipMaps.Add("default_value", true);

		var premultiplyAlpha = new Dictionary();
		premultiplyAlpha.Add("name", "premultiply_alpha");
		premultiplyAlpha.Add("default_value", true);

		presets.Add(generateMipMaps);
		presets.Add(premultiplyAlpha);

		return presets;
	}

	// The main import method
	public override long _Import(string sourceFile, string savePath, Dictionary options, Array<string> platformVariants, Array<string> genFiles) {

		GD.Print($"Import {sourceFile}");

		// Read image from PSD file
		var realPath = ProjectSettings.GlobalizePath(sourceFile);
		var image = ReadPSD(
			realPath,
			options.ContainsKey("generate_mip_maps") && (bool)options["generate_mip_maps"],
			options.ContainsKey("premultiply_alpha") && (bool)options["premultiply_alpha"]
		);

		// Create image resource
		var imageResource = ImageTexture.CreateFromImage(image);
		var result = ResourceSaver.Save(
			imageResource,
			$"{savePath}.{_GetSaveExtension()}",
			ResourceSaver.SaverFlags.Compress
		);

		if (result != Error.Ok)
			GD.PrintErr(result);

		return (long)result;
	}

	Image ReadPSD(string sourcePath, bool mipMaps = false, bool premultiplyAlpha = true) {
		// File IO
		var fileBytes = File.ReadAllBytes(sourcePath);
		MemoryStream fileStream = new MemoryStream(fileBytes);
		var reader = new BinaryReaderBigEndian(fileStream);

		// Read header
		var signature = new string(reader.ReadChars(4));
		var version = reader.ReadInt16();
		var empty = reader.ReadBytes(6);
		var channels = reader.ReadUInt16();
		var height = (int)reader.ReadUInt32();
		var width = (int)reader.ReadUInt32();
		var depth = reader.ReadUInt16();
		var colorMode = reader.ReadUInt16();

		// Header error handling
		if (signature != "8BPS")
			throw new Exception($"Not a PSD file ({signature})");

		if (version != 1)
			throw new Exception($"Unsupported version {version}");

		if (depth != 8)
			throw new Exception($"Unsupported bit depth {depth}");

		// Color mode - Unused
		var colorModeLength = (int)reader.ReadUInt32();
		var colorModeData = reader.ReadBytes(colorModeLength);

		// Image resources - Unused
		var imageResourcesLength = (int)reader.ReadUInt32();
		var imageResourcesData = reader.ReadBytes(imageResourcesLength);

		// Layers - Unused
		var layerSectionLength = (int)reader.ReadUInt32();
		var layerData = reader.ReadBytes(layerSectionLength);

		// Parse image data
		var compression = reader.ReadUInt16();
		var uncompressedBytes = new List<byte>();

		switch (compression) {
			case (0):
				// Uncompressed - Used for very low resolution PSDs
				while (true) {
					try {
						uncompressedBytes.Add(reader.ReadByte());
					} catch (Exception) {
						break;
					}
				}
				break;

			case (1):
				// RLE Compression

				// Skip scanline lengths
				reader.ReadBytes(height * channels * 2);

				while (true) {
					// TODO: Investigate if there's a better way to 
					// find the end than catching EndOfStreamException
					try {
						// Read a one byte instruction and convert it to be in the -128..127 range
						var n = (int)reader.ReadByte();
						if (n > 127) n = n - 256;

						// Act on instructions
						if (n == -128) {

							// Skip

						} else if (n < 0) {

							// Repeat
							var byteCount = -n + 1;
							var value = reader.ReadByte();
							for (int i = 0; i < byteCount; i++)
								uncompressedBytes.Add(value);

						} else if (n >= 0 && n <= 127) {

							// Read data
							var byteCount = 1 + n;
							var bytes = reader.ReadBytes(byteCount);
							uncompressedBytes.AddRange(bytes);

						}
					} catch (EndOfStreamException) {
						break;
					}
				}
				break;

			default:
				throw new Exception($"Unsupported compression format {compression}");
		}

		// In PSD files each channel is stored separately. 
		// We need to reorder bytes from these "bitplanes" to interleaved RGB(A)
		var data = new byte[width * height * channels];
		var bitplaneStride = width * height;

		if (premultiplyAlpha && channels == 4) {
			// Multiply color values with alpha to combat white fringing on 
			// transparent pixels on top of dark backgrounds
			for (int i = 0; i < data.Length - (width * channels); i += channels) {
				var x = i / channels;
				var a = data[i + 3] = uncompressedBytes[x + bitplaneStride * 3]; // A
				data[i + 0] = (byte)((uncompressedBytes[x + bitplaneStride * 0] * a) / 256); // R
				data[i + 1] = (byte)((uncompressedBytes[x + bitplaneStride * 1] * a) / 256); // G
				data[i + 2] = (byte)((uncompressedBytes[x + bitplaneStride * 2] * a) / 256); // B
			}
		} else {
			for (int i = 0; i < data.Length - (width * channels); i += channels) {
				var x = i / channels;
				data[i + 0] = uncompressedBytes[x + bitplaneStride * 0]; // R
				data[i + 1] = uncompressedBytes[x + bitplaneStride * 1]; // G
				data[i + 2] = uncompressedBytes[x + bitplaneStride * 2]; // B
				if (channels == 4)
					data[i + 3] = uncompressedBytes[x + bitplaneStride * 3]; // A
			}
		}

		// Create the image
		var image = new Image();
		image.CreateFromData(
			width,
			height,
			// We do not have mipmaps yet...
			false,
			// Pick the correct pixel format
			channels == 4 ? Image.Format.Rgba8 : Image.Format.Rgb8,
			data
		);

		// Optionally generate mipmaps
		if (mipMaps)
			image.GenerateMipmaps();

		return image;
	}
}
#endif
