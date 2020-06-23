using System;
using System.IO;
using System.Runtime.InteropServices;
using static gui_converter.GuiUtility;

namespace gui_converter
{
    public static class GuiConverter
    {
        private static void ConvertFile(GuiUtility peFile, long subSystemOffset)
        {
            var subSystemSetting = BitConverter.GetBytes((ushort)SubSystemType.ImageSubsystemWindowsGui);

            if (!BitConverter.IsLittleEndian)
                Array.Reverse(subSystemSetting);

            if (peFile.Stream.CanWrite)
            {
                peFile.Stream.Seek(subSystemOffset, SeekOrigin.Begin);
                peFile.Stream.Write(subSystemSetting, 0, subSystemSetting.Length);
                Console.WriteLine("Conversion Complete...");
            }
            else
            {
                Console.WriteLine("Can't write changes!");
                Console.WriteLine("Conversion Failed...");
            }
        }

        private static void AnalyzeFile(GuiUtility peFile)
        {
            var subSystemTypeValue = (SubSystemType)peFile.OptionalHeader.Subsystem;
            var subSystemOffset = peFile.MainHeaderOffset;
            subSystemOffset += Marshal.OffsetOf<ImageOptionalHeader>("Subsystem").ToInt32();

            switch (subSystemTypeValue)
            {
                case SubSystemType.ImageSubsystemWindowsGui:
                    Console.WriteLine("Executable file is already a Win32 App!");
                    return;
                case SubSystemType.ImageSubsystemWindowsCui:
                    Console.WriteLine("Console app detected...");
                    Console.WriteLine("Converting...");
                    ConvertFile(peFile, subSystemOffset);
                    return;
                default:
                    Console.WriteLine("Unsupported subsystem: " + Enum.GetName(typeof(SubSystemType), subSystemTypeValue));
                    return;
            }
        }
        
        public static void ProcessFile(string exeFilePath)
        {
            Console.WriteLine("Beginning analysis of: `" + exeFilePath + "`");

            using (var peFile = new GuiUtility(exeFilePath))
            {
                AnalyzeFile(peFile);
            }
        }
    }
}
