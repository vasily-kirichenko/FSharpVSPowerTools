open FSharpVSPowerTools
open System
open System.IO
open System.Diagnostics
open Microsoft.FSharp.Compiler.SourceCodeServices

[<EntryPoint>]
let main argv = 
    let ls = LanguageService(fun _ -> ())    
    let opts() = 
        { ProjectFileName =
            @"L:\git\VisualFSharpPowerTools\src\FSharpVSPowerTools.Core\FSharpVSPowerTools.Core.fsproj"
          ProjectFileNames =
           [|@"C:\Users\kot\AppData\Local\Temp\.NETFramework,Version=v4.5.AssemblyAttributes.fs"
             @"L:\git\VisualFSharpPowerTools\src\FSharpVSPowerTools.Core\AssemblyInfo.fs"
             @"L:\git\VisualFSharpPowerTools\src\FSharpVSPowerTools.Core\Utils.fs"
             @"L:\git\VisualFSharpPowerTools\src\FSharpVSPowerTools.Core\CompilerLocationUtils.fs"
             @"L:\git\VisualFSharpPowerTools\src\FSharpVSPowerTools.Core\TypedAstUtils.fs"
             @"L:\git\VisualFSharpPowerTools\src\FSharpVSPowerTools.Core\Lexer.fs"
             @"L:\git\VisualFSharpPowerTools\src\FSharpVSPowerTools.Core\LanguageService.fs"
             @"L:\git\VisualFSharpPowerTools\src\FSharpVSPowerTools.Core\XmlDocParser.fs"
             @"L:\git\VisualFSharpPowerTools\src\FSharpVSPowerTools.Core\DepthParser.fs"
             @"L:\git\VisualFSharpPowerTools\src\FSharpVSPowerTools.Core\NavigableItemsCollector.fs"
             @"L:\git\VisualFSharpPowerTools\src\FSharpVSPowerTools.Core\SourceCodeClassifier.fs"
             @"L:\git\VisualFSharpPowerTools\src\FSharpVSPowerTools.Core\CodeGeneration.fs"
             @"L:\git\VisualFSharpPowerTools\src\FSharpVSPowerTools.Core\InterfaceStubGenerator.fs"
             @"L:\git\VisualFSharpPowerTools\src\FSharpVSPowerTools.Core\RecordStubGenerator.fs"
             @"L:\git\VisualFSharpPowerTools\src\FSharpVSPowerTools.Core\UnionMatchCaseGenerator.fs"
             @"L:\git\VisualFSharpPowerTools\src\FSharpVSPowerTools.Core\NavigateToIndex.fs"|]
          ProjectOptions =
           [|@"-o:obj\Debug\FSharpVSPowerTools.Core.dll"; "-g"; "--debug:full";
             "--noframework"; "--define:DEBUG"; "--define:TRACE";
             "--doc:bin\Debug\FSharpVSPowerTools.Core.XML"; "--optimize-"; "--tailcalls-";
             @"-r:L:\git\VisualFSharpPowerTools\packages\FSharp.Compiler.Service.0.0.50\lib\net40\FSharp.Compiler.Service.dll";
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\FSharp\.NETFramework\v4.0\4.3.0.0\FSharp.Core.dll";
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\mscorlib.dll";
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Core.dll";
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.dll";
             @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Numerics.dll";
             "--target:library"; "--nowarn:52"; "--warn:5"; "--warnaserror:76";
             "--vserrors"; "--validate-type-providers"; "--LCID:1033"; "--utf8output";
             "--fullpaths"; "--flaterrors"; "--subsystemversion:6.00"; "--highentropyva+";
             "--sqmsessionguid:7eb2c256-ecde-48c0-a6c6-0da5081726eb"; "--warnon:1182"|];
          ReferencedProjects = [||]
          IsIncompleteTypeCheckEnvironment = false
          UseScriptResolutionRules = false
          LoadTime = DateTime.Now
          UnresolvedReferences = None }

    let file = @"L:\git\VisualFSharpPowerTools\src\FSharpVSPowerTools.Core\RecordStubGenerator.fs"

    let work() =
        let sw = Stopwatch.StartNew()
        let uses = 
            ls.GetAllUsesOfAllSymbolsInFile (opts(), file, File.ReadAllText file, AllowStaleResults.No)
            |> Async.RunSynchronously
        sw.Stop()
        printfn "Got %d symbol uses in %O" uses.Length sw.Elapsed

    Trace.Listeners.Add(new ConsoleTraceListener()) |> ignore

    while true do
        match Console.ReadKey().Key with
        | ConsoleKey.W -> work()
        | ConsoleKey.C -> 
            ls.Checker.ClearLanguageServiceRootCachesAndCollectAndFinalizeAllTransients()
            GC.Collect()
            GC.Collect()
        | _ -> printfn "Wrong input. Press <W> to run another iteration or <C> to clear FCS + GC."

    0
