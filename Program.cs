using System;
using System.IO;
using McMaster.Extensions.CommandLineUtils;

namespace gui_converter
{
    internal static class Program
    {
        private static void ExecuteCommand(string filePath, bool quiet)
        {
            if (quiet)
            {
                Console.SetOut(TextWriter.Null);
                Console.SetError(TextWriter.Null);
            }

            GuiConverter.ProcessFile(filePath);
        }
        
        private static int Main(string[] args)
        {
            var app = new CommandLineApplication();
            app.HelpOption();

            var argFilePath = app.Argument
            (
                "filepath",
                "Path to EXE file."
            );

            var optQuiet = app.Option
            (
                "-q|--quiet", 
                "Do not print to console.", 
                CommandOptionType.NoValue
            );
            
            app.OnExecute
            (
                () =>
                {
                    var filePath = argFilePath.Value;
                    var quiet = optQuiet.HasValue();

                    if (filePath != null)
                    {
                        var inputFile = new FileInfo(filePath);

                        if (inputFile.Exists)
                        {
                            ExecuteCommand(inputFile.FullName, quiet);
                            return 0;
                        }

                        Console.Error.WriteLine("Could not find specified input file: " + filePath);
                        return -3;
                    }
                    
                    Console.Error.WriteLine("No input file was specified.");
                    return -2;
                }
            );

            try
            {
                var returnCode = app.Execute(args);

                if (returnCode != 0)
                {
                    app.ShowHelp();
                }

                return returnCode;
            }
            catch (UnrecognizedCommandParsingException)
            {
                app.ShowHelp();
                return -1;
            }
        }
    }
}
