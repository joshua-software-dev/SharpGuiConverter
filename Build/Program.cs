using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Xml;
using static Bullseye.Targets;
using static SimpleExec.Command;

namespace Build
{
    internal static class Program
    {
        private static OSPlatform? _platform; 
        
        private static OSPlatform Platform
        {
            get
            {
                if (_platform != null)
                {
                    return _platform.GetValueOrDefault();
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    _platform = OSPlatform.Linux;
                    return _platform.GetValueOrDefault();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    _platform = OSPlatform.OSX;
                    return _platform.GetValueOrDefault();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    _platform = OSPlatform.Windows;
                    return _platform.GetValueOrDefault();
                }
    
                throw new ArgumentException("Unsupported platform for compiling.");
            }
        }

        private static string _version;
        
        private static string Version
        {
            get
            {
                if (_version != null)
                {
                    return _version;
                }
                
                var xmlDoc = new XmlDocument();
                xmlDoc.Load("Version/Version.csproj");
                _version = xmlDoc.SelectSingleNode("//Project/PropertyGroup/Version")?.InnerText;

                return _version;
            }
        }
        
        private static void DeleteDirectoryIfExist(string directory)
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch (DirectoryNotFoundException) {}
        }
        
        private static void CleanOutputDirectories(IEnumerable<string> outputDirs)
        {
            foreach (var outputDir in outputDirs)
            {
                try
                {
                    if (!File.GetAttributes(outputDir).HasFlag(FileAttributes.Directory))
                    {
                        File.Delete(outputDir);
                    }
                }
                catch (FileNotFoundException) {}
                catch (DirectoryNotFoundException) {}
                
                DeleteDirectoryIfExist(outputDir);
            }
        }
        
        private static void RestoreSolution()
        {
            ProcessAsyncHelper
                .RunAsync(new ProcessStartInfo("dotnet", "restore"))
                .GetAwaiter()
                .GetResult();
        }
        
        private static void MakeOutputDirectories(IEnumerable<string> outputDirs)
        {
            foreach (var outputDir in outputDirs)
            {
                Directory.CreateDirectory(outputDir);
            }
        }

        private static void PublishNative()
        {
            var nativeBuildTargets = new List<string> {"CoreRt"};

            if (Platform == OSPlatform.Linux)
            {
                nativeBuildTargets.Add("CoreRtMusl");
            }

            var platforms = new Dictionary<OSPlatform, string>
            {
                {OSPlatform.Linux, "linux-x64"},
                {OSPlatform.OSX, "osx.10.10-x64"},
                {OSPlatform.Windows, "win-x64"}
            };

            foreach (var nativeTarget in nativeBuildTargets)
            {
                Run("dotnet", $"publish SharpGuiConverter -c {nativeTarget} -r {platforms[Platform]}");
            }
        }

        private static void Publish()
        {
            Run("dotnet", "publish SharpGuiConverter -r linux-x64 -c Contained");
            Run("dotnet", "publish SharpGuiConverter -r linux-x64 -c Dependent");
            Run("dotnet", "publish SharpGuiConverter -r osx.10.10-x64 -c Contained");
            Run("dotnet", "publish SharpGuiConverter -r osx.10.10-x64 -c Dependent");
            Run("dotnet", "publish SharpGuiConverter -r win-x64 -c Contained");
            Run("dotnet", "publish SharpGuiConverter -r win-x64 -c Dependent");
        }

        private static void CreateArchiveFromFile(string inputFile, string outputZipPath)
        {
            if (!File.Exists(inputFile))
            {
                Console.Error.WriteLine("Warning: File not found, could not zip: " + inputFile);
                return;
            }

            using (var zip = ZipFile.Open(outputZipPath, ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(inputFile, Path.GetFileName(inputFile), CompressionLevel.Optimal);
            }
        }
        
        private static void ZipOutput()
        {
            CreateArchiveFromFile("SharpGuiConverter/bin/Contained/netcoreapp3.1/linux-x64/publish/SharpGuiConverter", $"release_binaries/SharpGuiConverter-Linux-core-contained-{Version}.zip");
            CreateArchiveFromFile("SharpGuiConverter/bin/Dependent/netcoreapp3.1/linux-x64/publish/SharpGuiConverter", $"release_binaries/SharpGuiConverter-Linux-core-dependent-{Version}.zip");
            CreateArchiveFromFile("SharpGuiConverter/bin/Contained/netcoreapp3.1/osx.10.10-x64/publish/SharpGuiConverter", $"release_binaries/SharpGuiConverter-MacOs-core-contained-{Version}.zip");
            CreateArchiveFromFile("SharpGuiConverter/bin/Dependent/netcoreapp3.1/osx.10.10-x64/publish/SharpGuiConverter", $"release_binaries/SharpGuiConverter-MacOs-core-dependent-{Version}.zip");
            CreateArchiveFromFile("SharpGuiConverter/bin/Contained/netcoreapp3.1/win-x64/publish/SharpGuiConverter.exe", $"release_binaries/SharpGuiConverter-Win64-core-contained-{Version}.zip");
            CreateArchiveFromFile("SharpGuiConverter/bin/Dependent/netcoreapp3.1/win-x64/publish/SharpGuiConverter.exe", $"release_binaries/SharpGuiConverter-Win64-core-dependent-{Version}.zip");

            CreateArchiveFromFile("SharpGuiConverter/bin/CoreRt/netcoreapp3.1/linux-x64/native/SharpGuiConverter", $"release_binaries/SharpGuiConverter-Linux-native-dynamic-{Version}.zip");
            CreateArchiveFromFile("SharpGuiConverter/bin/CoreRt/netcoreapp3.1/osx.10.10-x64/native/SharpGuiConverter", $"release_binaries/SharpGuiConverter-MacOs-native-dynamic-{Version}.zip");
            CreateArchiveFromFile("SharpGuiConverter/bin/CoreRt/netcoreapp3.1/win-x64/native/SharpGuiConverter.exe", $"release_binaries/SharpGuiConverter-Win64-native-dynamic-{Version}.zip");

            CreateArchiveFromFile("SharpGuiConverter/bin/CoreRtMusl/netcoreapp3.1/linux-x64/native/SharpGuiConverter", $"release_binaries/SharpGuiConverter-Linux-native-static-{Version}.zip");
        }
        
        private static void Main(string[] args)
        {
            string[] outputDirs = {"release_binaries"};

            Target(
                "clean-output-dirs",
                () => CleanOutputDirectories(outputDirs)
            );
            
            Target(
                "restore",
                RestoreSolution
            );
            
            Target(
                "clean",
                DependsOn("clean-output-dirs"),
                () =>
                {
                    DeleteDirectoryIfExist("Build/bin/");
                    DeleteDirectoryIfExist("Build/obj/");
                    DeleteDirectoryIfExist("SharpGuiConverter/bin/");
                    DeleteDirectoryIfExist("SharpGuiConverter/obj/");
                }
            );
            
            Target(
                "make-output-dirs", 
                DependsOn("clean-output-dirs"), 
                () => MakeOutputDirectories(outputDirs)
            );

            Target(
                "publish-core-only",
                DependsOn("make-output-dirs", "restore"),
                Publish
            );
            
            Target(
                "publish-native-only",
                DependsOn("make-output-dirs", "restore"),
                PublishNative
            );
            
            Target(
                "publish",
                dependsOn: DependsOn("publish-core-only", "publish-native-only"),
                null
            );

            Target("zip-output", ZipOutput);

            Target("core-only", dependsOn: DependsOn("publish-core-only", "zip-output"), null);
            
            Target("native", dependsOn: DependsOn("publish-native-only", "zip-output"), action: null);

            Target("default", dependsOn: DependsOn("publish", "zip-output"), null);
            
            RunTargetsAndExit(args);
        }
    }
}