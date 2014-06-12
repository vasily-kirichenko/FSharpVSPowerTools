open FSharpVSPowerTools
open System
open System.IO
open System.Diagnostics
open Microsoft.FSharp.Compiler.SourceCodeServices

[<EntryPoint>]
let main _argv = 
    Environment.SetEnvironmentVariable("mFSharp_IncrementalTypeCheckCacheSize", "1")
    Environment.SetEnvironmentVariable("mFSharp_ParseFileInProjectCacheSize", "1")
    Environment.SetEnvironmentVariable("mFSharp_ProjectCacheSizeDefault", "1")

    let ls = LanguageService(fun _ -> ())    
    //let inRoot relativePath = Path.Combine (@"l:\github\FSharpVSPowerTools\", relativePath)
    let inRoot relativePath = Path.Combine (@"L:\git\VisualFSharpPowerTools", relativePath)
    let coreOpts() = 
        { ProjectFileName =
            inRoot @"src\FSharpVSPowerTools.Core\FSharpVSPowerTools.Core.fsproj"
          ProjectFileNames =
           [|inRoot @"src\FSharpVSPowerTools.Core\AssemblyInfo.fs"
             inRoot @"src\FSharpVSPowerTools.Core\Utils.fs"
             inRoot @"src\FSharpVSPowerTools.Core\CompilerLocationUtils.fs"
             inRoot @"src\FSharpVSPowerTools.Core\TypedAstUtils.fs"
             inRoot @"src\FSharpVSPowerTools.Core\Lexer.fs"
             inRoot @"src\FSharpVSPowerTools.Core\LanguageService.fs"
             inRoot @"src\FSharpVSPowerTools.Core\XmlDocParser.fs"
             inRoot @"src\FSharpVSPowerTools.Core\DepthParser.fs"
             inRoot @"src\FSharpVSPowerTools.Core\NavigableItemsCollector.fs"
             inRoot @"src\FSharpVSPowerTools.Core\SourceCodeClassifier.fs"
             inRoot @"src\FSharpVSPowerTools.Core\CodeGeneration.fs"
             inRoot @"src\FSharpVSPowerTools.Core\InterfaceStubGenerator.fs"
             inRoot @"src\FSharpVSPowerTools.Core\RecordStubGenerator.fs"
             inRoot @"src\FSharpVSPowerTools.Core\UnionMatchCaseGenerator.fs"
             inRoot @"src\FSharpVSPowerTools.Core\NavigateToIndex.fs"|]
          ProjectOptions =
           [|@"-o:obj\Debug\FSharpVSPowerTools.Core.dll"; "-g"; "--debug:full";
             "--noframework"; "--define:DEBUG"; "--define:TRACE";
             "--doc:bin\Debug\FSharpVSPowerTools.Core.XML"; "--optimize-"; "--tailcalls-";
             @"-r:" + inRoot @"packages\FSharp.Compiler.Service.0.0.52\lib\net40\FSharp.Compiler.Service.dll";
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

    let coreFile = inRoot @"src\FSharpVSPowerTools.Core\RecordStubGenerator.fs"
    
    let logicOpts() = 
        { ProjectFileName = inRoot "src\FSharpVSPowerTools.Logic\FSharpVSPowerTools.Logic.fsproj"
          ProjectFileNames =
            [|inRoot @"src\FSharpVSPowerTools.Logic\AssemblyInfo.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\Resource.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\VSUtils.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\Logger.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\VisualStudioVersion.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\ThemeManager.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\ProjectSystem.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\ProjectProvider.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\OpenDocumentsTracker.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\FileSystem.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\VSLanguageService.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\ActiveViewsRegistratorListener.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\NavigateToItem.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\CodeFormattingServices.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\CodeFormattingCommands.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\FormatDocumentCommand.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\FormatSelectionCommand.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\XmlDocFilter.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\DepthTagger.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\DepthAdornmentManager.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\SyntaxConstructClassifier.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\HighlightUsageTagger.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\RenameDialog.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\RenameCommandFilter.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\FolderNameDialog.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\MoveToFolderDialog.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\FolderMenuUI.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\FolderMenuCommands.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\CodeGenerationService.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\ImplementInterfaceSmartTagger.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\RecordStubGeneratorSmartTagger.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\UnionPatternMatchCaseGeneratorSmartTagger.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\LibraryNode.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\Library.fs"
              inRoot @"src\FSharpVSPowerTools.Logic\FindReferencesFilter.fs"|]
          ProjectOptions =
            [|"-o:obj\Debug\FSharpVSPowerTools.Logic.dll"; "-g"; "--debug:full";
              "--noframework"; "--define:TRACE"; "--define:DEBUG";
              "--doc:bin\Debug\FSharpVSPowerTools.Logic.XML"; "--optimize-";
              "--tailcalls-";
              @"-r:c:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\PublicAssemblies\EnvDTE.dll";
              @"-r:c:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\PublicAssemblies\EnvDTE80.dll";
              @"-r:" + inRoot @"packages\Fantomas.1.3.0\lib\FantomasLib.dll"
              @"-r:" + inRoot @"packages\FSharp.Compiler.Service.0.0.50\lib\net40\FSharp.Compiler.Service.dll";
              @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\FSharp\.NETFramework\v4.0\4.3.0.0\FSharp.Core.dll";
              @"-r:" + inRoot @"src\FSharpVSPowerTools.Core\bin\Debug\FSharpVSPowerTools.Core.dll";
              @"-r:" + inRoot @"packages\FsXaml.Wpf.0.9.3\lib\net45\FsXaml.Wpf.dll";
              @"-r:" + inRoot @"packages\FsXaml.Wpf.0.9.3\lib\net45\FsXaml.Wpf.TypeProvider.dll";
              @"-r:" + inRoot @"lib\vs2012\Microsoft.VisualStudio.ComponentModelHost.dll";
              @"-r:" + inRoot @"lib\vs2012\Microsoft.VisualStudio.CoreUtility.dll";
              @"-r:" + inRoot @"lib\vs2012\Microsoft.VisualStudio.Editor.dll";
              @"-r:" + inRoot @"lib\vs2012\Microsoft.VisualStudio.Language.Intellisense.dll";
              @"-r:" + inRoot @"lib\vs2012\Microsoft.VisualStudio.Language.NavigateTo.Interfaces.dll";
              @"-r:C:\Program Files (x86)\Microsoft Visual Studio 12.0\VSSDK\VisualStudioIntegration\Common\Assemblies\v2.0\Microsoft.VisualStudio.OLE.Interop.dll";
              @"-r:" + inRoot @"lib\vs2012\Microsoft.VisualStudio.Shell.11.0.dll";
              @"-r:C:\Program Files (x86)\Microsoft Visual Studio 12.0\VSSDK\VisualStudioIntegration\Common\Assemblies\v4.0\Microsoft.VisualStudio.Shell.Immutable.10.0.dll";
              @"-r:C:\Program Files (x86)\Microsoft Visual Studio 12.0\VSSDK\VisualStudioIntegration\Common\Assemblies\v2.0\Microsoft.VisualStudio.Shell.Interop.10.0.dll";
              @"-r:C:\Program Files (x86)\Microsoft Visual Studio 12.0\VSSDK\VisualStudioIntegration\Common\Assemblies\v2.0\Microsoft.VisualStudio.Shell.Interop.8.0.dll";
              @"-r:C:\Program Files (x86)\Microsoft Visual Studio 12.0\VSSDK\VisualStudioIntegration\Common\Assemblies\v2.0\Microsoft.VisualStudio.Shell.Interop.9.0.dll";
              @"-r:C:\Program Files (x86)\Microsoft Visual Studio 12.0\VSSDK\VisualStudioIntegration\Common\Assemblies\v2.0\Microsoft.VisualStudio.Shell.Interop.dll";
              @"-r:" + inRoot @"lib\vs2012\Microsoft.VisualStudio.Text.Data.dll";
              @"-r:" + inRoot @"lib\vs2012\Microsoft.VisualStudio.Text.Logic.dll";
              @"-r:" + inRoot @"lib\vs2012\Microsoft.VisualStudio.Text.UI.dll";
              @"-r:" + inRoot @"lib\vs2012\Microsoft.VisualStudio.Text.UI.Wpf.dll";
              @"-r:C:\Program Files (x86)\Microsoft Visual Studio 12.0\VSSDK\VisualStudioIntegration\Common\Assemblies\v2.0\Microsoft.VisualStudio.TextManager.Interop.8.0.dll";
              @"-r:C:\Program Files (x86)\Microsoft Visual Studio 12.0\VSSDK\VisualStudioIntegration\Common\Assemblies\v2.0\Microsoft.VisualStudio.TextManager.Interop.dll";
              @"-r:C:\Program Files (x86)\Microsoft Visual Studio 12.0\VSSDK\VisualStudioIntegration\Common\Assemblies\v4.0\Microsoft.VisualStudio.Threading.dll";
              @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\mscorlib.dll";
              @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\PresentationCore.dll";
              @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\PresentationFramework.dll";
              @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.ComponentModel.Composition.dll";
              @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Core.dll";
              @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Data.DataSetExtensions.dll";
              @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Data.dll";
              @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Design.dll";
              @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.dll";
              @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Drawing.dll";
              @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Windows.Forms.dll";
              @"-r:" + inRoot @"packages\Expression.Blend.Sdk.1.0.2\lib\net45\System.Windows.Interactivity.dll";
              @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Xaml.dll";
              @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Xml.dll";
              @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\UIAutomationTypes.dll";
              @"-r:c:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\PublicAssemblies\VSLangProj.dll";
              @"-r:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\WindowsBase.dll";
              @"--lib:" + inRoot @"src\FSharpVSPowerTools.Logic\..\..\lib\vs2012";
              "--target:library"; "--nowarn:52"; "--warn:5"; "--warnaserror:76";
              "--vserrors"; "--validate-type-providers"; "--LCID:1033"; "--utf8output";
              "--fullpaths"; "--flaterrors"; "--subsystemversion:6.00"; "--highentropyva+";
              "--sqmsessionguid:bbd55174-45ac-4eef-a556-38ffcbb22ec1"; "--warnon:1182"|];
          ReferencedProjects = 
              [| inRoot @"src\FSharpVSPowerTools.Core\bin\Debug\FSharpVSPowerTools.Core.dll", coreOpts() |]
          IsIncompleteTypeCheckEnvironment = false
          UseScriptResolutionRules = false
          LoadTime = DateTime.Now
          UnresolvedReferences = None }

    let logicFile = inRoot @"src\FSharpVSPowerTools.Logic\Library.fs"
    let cancelToken = ref None

    let work opts file =
        !cancelToken |> Option.iter (fun (x: System.Threading.CancellationTokenSource) -> x.Dispose())
        let token = new System.Threading.CancellationTokenSource()
        cancelToken := Some token
        let worker = 
            async {
                let sw = Stopwatch.StartNew()
                let! uses = ls.GetAllUsesOfAllSymbolsInFile (opts(), file, File.ReadAllText coreFile, AllowStaleResults.No)
                sw.Stop()
                printfn "Got %d symbol uses in %O" uses.Length sw.Elapsed
                GC.Collect()
                GC.Collect()
            }
        Async.Start (worker, token.Token)

    Trace.Listeners.Add(new ConsoleTraceListener()) |> ignore

    while true do
        match Console.ReadKey().Key with
        | ConsoleKey.W -> work logicOpts logicFile
        | ConsoleKey.C -> 
            ls.Checker.ClearLanguageServiceRootCachesAndCollectAndFinalizeAllTransients()
            GC.Collect()
            GC.Collect()
        | ConsoleKey.S ->
            !cancelToken |> Option.iter (fun x -> 
                x.Cancel()
                printfn "Token cancelled."
                x.Dispose()
                cancelToken := None)
            cancelToken := None
        | _ -> printfn "Wrong input. Press <W> to run another iteration, <C> to clear FCS + GC or <S> to cancel running iteration."

    0
    