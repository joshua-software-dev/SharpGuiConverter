using System;
using System.IO;
using System.Runtime.InteropServices;

namespace gui_converter
{
    internal class GuiUtility : IDisposable
    {
        public enum SubSystemType : ushort
        {
            ImageSubsystemWindowsGui = 2,
            ImageSubsystemWindowsCui = 3
        }

        [StructLayout(LayoutKind.Explicit)]
        private readonly struct ImageDosHeader
        {
            [FieldOffset(60)]
            public readonly uint e_lfanew;
        }

        [StructLayout(LayoutKind.Explicit)]
        public readonly struct ImageOptionalHeader
        {
            [FieldOffset(68)]
            public readonly ushort Subsystem;
        }

        public GuiUtility(string filePath)
        {
            Stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);
            var reader = new BinaryReader(Stream);
            var dosHeader = FromBinaryReader<ImageDosHeader>(reader);

            // Seek the new PE Header and skip NtHeadersSignature (4 bytes) & IMAGE_FILE_HEADER struct (20bytes).
            Stream.Seek(dosHeader.e_lfanew + 4 + 20, SeekOrigin.Begin);

            MainHeaderOffset = Stream.Position;
            OptionalHeader = FromBinaryReader<ImageOptionalHeader>(reader);
        }

        /// <summary>
        /// Reads in a block from a file and converts it to the struct
        /// type specified by the template parameter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static T FromBinaryReader<T>(BinaryReader reader)
        {
            // Read in a byte array
            var bytes = reader.ReadBytes(Marshal.SizeOf<T>());

            // Pin the managed memory while, copy it out the data, then unpin it
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var theStructure = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            handle.Free();

            return theStructure;
        }

        public void Dispose()
        {
            Stream?.Dispose();
        }

        /// <summary>
        /// Gets the optional header
        /// </summary>
        public ImageOptionalHeader OptionalHeader { get; }

        /// <summary>
        /// Gets the PE file stream for R/W functions.
        /// </summary> 
        public FileStream Stream { get; }

        public long MainHeaderOffset { get; }
    }
}