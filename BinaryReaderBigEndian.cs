using System;
using System.IO;

/* 
	Simple wrapper around BinaryReader to support
	Big Endian for certain read operations
*/
class BinaryReaderBigEndian : BinaryReader {
	public BinaryReaderBigEndian(System.IO.Stream stream) : base(stream) { }

	new public Int16 ReadInt16() {
		var data = base.ReadBytes(2);
		Array.Reverse(data);
		return BitConverter.ToInt16(data, 0);
	}

	new public UInt16 ReadUInt16() {
		var data = base.ReadBytes(2);
		Array.Reverse(data);
		return BitConverter.ToUInt16(data, 0);
	}

	new public UInt32 ReadUInt32() {
		var data = base.ReadBytes(4);
		Array.Reverse(data);
		return BitConverter.ToUInt32(data, 0);
	}

	public string ReadPascalUnicode() {
		var length = (int)ReadUInt32();
		var data = ReadBytes(length * 2);
		return System.Text.Encoding.BigEndianUnicode.GetString(data);
	}
}