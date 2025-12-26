using System.Reflection;
using System.Runtime.Loader;
using ProtoBuf;
using ProtoBuf.Meta;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Runtime.InteropServices;

namespace GrabProtobuff;

class Program
{
    private static string? GenerateProtobuff(string baseDir)
    {
        // 1. Setup paths based on your 'ls' output
        var dllPath = Path.Combine(baseDir, "VintagestoryLib.dll");
        var libDir = Path.Combine(baseDir, "Lib");

        // 2. Attach a resolver to find dependencies in the 'Lib' folder
        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            // Check root folder first
            var path = Path.Combine(baseDir, name.Name + ".dll");
            if (File.Exists(path))
            {
                return context.LoadFromAssemblyPath(path);
            }

            // Then check the Lib folder
            path = Path.Combine(libDir, name.Name + ".dll");
            if (File.Exists(path)) {
                return context.LoadFromAssemblyPath(path);
            }

            return null;
        };

        try
        {
            // 3. Load the assembly for execution (needed for protobuf-net reflection)
            var assembly = Assembly.LoadFrom(dllPath);
            var model = RuntimeTypeModel.Create();

            // 4. Find the types
            var protoTypes = assembly.GetTypes()
                .Where(t => t.GetCustomAttributes(typeof(ProtoContractAttribute), true).Any());

            foreach (var type in protoTypes)
            {
                model.Add(type, true);
            }

            // 5. Generate Schema
            var options = new SchemaGenerationOptions
            {
                Syntax = ProtoSyntax.Proto3,
                Package = "vintagestory"
            };

            var schema = model.GetSchema(options);
            return schema;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            if (ex.InnerException != null) Console.WriteLine($"Inner: {ex.InnerException.Message}");
            return null;
        }
    }

    private static string GetDefaultLocation(ArgumentResult arg)
    {
        string[] potentialPaths =
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vintagestory"),
            "/Applications/Vintage Story.app/",
        };

        foreach (var path in potentialPaths)
        {
            // We check for the directory AND the critical DLL to ensure it's a valid install
            if (Directory.Exists(path) && File.Exists(Path.Combine(path, "VintagestoryLib.dll")))
            {
                return path;
            }
        }
        
        return string.Empty;
    }

    private static string GetDefaultProtoOutput(ArgumentResult arg)
    {
        return "./vintagestory.proto";
    }
    
    private static async Task<int> Main(string[] args)
    {
        // 1. Define your Option
        var vintageStoryLocationOption = new Option<string>("--input")
        {
            DefaultValueFactory = GetDefaultLocation
        };
        var protoOutputOption = new Option<string>("--output")
        {
            DefaultValueFactory = GetDefaultProtoOutput
        };
        
        // 2. Define the RootCommand
        var rootCommand = new RootCommand("Grabs Protobuff definitions from VintageStory.");
        rootCommand.Options.Add(vintageStoryLocationOption);
        rootCommand.Options.Add(protoOutputOption);

        // 3. Use SetAction instead of SetHandler
        rootCommand.SetAction(parseResult =>
        {
            // Retrieve the value directly from the parseResult
            var vintageStoryLocation = parseResult.GetValue(vintageStoryLocationOption);
            if (vintageStoryLocation == null)
            {
                return;
            }
            var result = GenerateProtobuff(vintageStoryLocation);
            var outputLocation = parseResult.GetValue(protoOutputOption);
            if (outputLocation == null)
            {
                Console.WriteLine(result);
                return;
            }
            outputLocation = Path.GetFullPath(outputLocation);
            File.WriteAllText(outputLocation, result);
            Console.WriteLine($"Wrote to: {outputLocation}");
        });

        // 4. Use the new Parse/Invoke pattern
        return await rootCommand.Parse(args).InvokeAsync();
    }
    
}