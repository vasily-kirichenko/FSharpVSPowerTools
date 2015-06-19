﻿namespace FSharpVSPowerTools.Tests

open System
open System.IO
open FSharpVSPowerTools
open FSharpVSPowerTools.ProjectSystem
open Microsoft.VisualStudio.Text
open NUnit.Framework
open Microsoft.VisualStudio.Text.Classification

type ClassificationSpan =
    { Classification: string
      Span: int * int * int * int }

type SyntaxConstructClassifierHelper() =    
    inherit VsTestBase()
    
    let classifierProvider = new SyntaxConstructClassifierProvider(
                                    shellEventListener = base.ShellEventListener,
                                    serviceProvider = base.ServiceProvider, 
                                    classificationColorManager = null,
                                    projectFactory = base.ProjectFactory,
                                    fsharpVsLanguageService = base.VsLanguageService,
                                    classificationRegistry = base.ClassificationTypeRegistryService,
                                    textDocumentFactoryService = base.DocumentFactoryService)

    member __.GetClassifier(buffer) = classifierProvider.GetClassifier(buffer)

    member __.ClassificationSpansOf(buffer: ITextBuffer, classifier: IClassifier) =
        classifier.GetClassificationSpans(SnapshotSpan(buffer.CurrentSnapshot, 0, buffer.CurrentSnapshot.Length))
        |> Seq.sortBy (fun span -> span.Span.Start.Position)
        |> Seq.map (fun span ->
            let snapshot = span.Span.Snapshot
            // Use 1-based position for intuitive comparison
            let lineStart = snapshot.GetLineNumberFromPosition(span.Span.Start.Position) + 1 
            let lineEnd = snapshot.GetLineNumberFromPosition(span.Span.End.Position) + 1
            let startLine = snapshot.GetLineFromPosition(span.Span.Start.Position)
            let endLine = snapshot.GetLineFromPosition(span.Span.End.Position)
            let colStart = span.Span.Start.Position - startLine.Start.Position + 1
            let colEnd = span.Span.End.Position - endLine.Start.Position + 1
            
            { Classification = span.ClassificationType.Classification;
              Span = (lineStart, colStart, lineEnd, colEnd - 1) } )

    interface IDisposable with
        member __.Dispose() = 
            classifierProvider.Dispose()

module SyntaxConstructClassifierTests =
#if APPVEYOR
    let timeout = 60000<ms>
#else
    let timeout = 10000<ms>
#endif
    
    let helper = new SyntaxConstructClassifierHelper()
    let mutable fileName = null 

    [<TestFixtureSetUp>]
    let fixtureSetUp() =
        TestUtilities.AssertListener.Initialize()
        DocumentEventListener.SkipTimerDelay <- true

    [<SetUp>]
    let setUp() = fileName <- getTempFileName ".fsx"

    [<Test>]
    let ``should return a syngle operator symbol if the code doesn't contain any other symbols``() = 
        let content = "let x = 0"
        let buffer = createMockTextBuffer content fileName
        helper.SetUpProjectAndCurrentDocument(VirtualProjectProvider(buffer, fileName), fileName)
        let classifier = helper.GetClassifier(buffer)
        testEvent classifier.ClassificationChanged "Timed out before classification changed" timeout
            (fun () -> 
            helper.ClassificationSpansOf(buffer, classifier) 
            |> Seq.toList
            |> assertEqual [ { Classification = "FSharp.Operator"; Span = (1, 7) => (1, 7) } ])

    [<Test>]
    let ``should be able to get classification spans for main categories``() = 
        let content = """
let moduleFunction x = x + 1
let genericClassOfInt = ResizeArray<int>()
let mutable mutableValue = 1
let (|ActivePattern|_|) x = Some x
let _ = <@ 1 = 1 @>
module Module1 =
    let x = ()
"""
        let buffer = createMockTextBuffer content fileName
        helper.SetUpProjectAndCurrentDocument(VirtualProjectProvider(buffer, fileName), fileName)
        let classifier = helper.GetClassifier(buffer)
        testEvent classifier.ClassificationChanged "Timed out before classification changed" timeout <| fun _ ->
        let actual = helper.ClassificationSpansOf(buffer, classifier) |> Seq.toList
        let expected =
            [ { Classification = "FSharp.Function"; Span = (2, 5) => (2, 18) }
              { Classification = "FSharp.Operator"; Span = (2, 22) => (2, 22) }
              { Classification = "FSharp.Operator"; Span = (2, 26) => (2, 26) }
              { Classification = "FSharp.Operator"; Span = (3, 23) => (3, 23) }
              { Classification = "FSharp.ReferenceType"; Span = (3, 25) => (3, 35) }
              { Classification = "FSharp.ValueType"; Span = (3, 37) => (3, 39) } 
              { Classification = "FSharp.MutableVar"; Span = (4, 13) => (4, 24) }
              { Classification = "FSharp.Operator"; Span = (4, 26) => (4, 26) }
              { Classification = "FSharp.PatternCase"; Span = (5, 7) => (5, 19) }
              { Classification = "FSharp.Operator"; Span = (5, 27) => (5, 27) } 
              { Classification = "FSharp.PatternCase"; Span = (5, 29) => (5, 32) }
              { Classification = "FSharp.Operator"; Span = (6, 7) => (6, 7) }
              { Classification = "FSharp.Quotation"; Span = (6, 9) => (6, 19) } 
              { Classification = "FSharp.Operator"; Span = (6, 14) => (6, 14) }
              { Classification = "FSharp.Module"; Span = (7, 8) => (7, 14) } 
              { Classification = "FSharp.Operator"; Span = (7, 16) => (7, 16) }
              { Classification = "FSharp.Operator"; Span = (8, 11) => (8, 11) }] 
        CollectionAssert.AreEquivalent(expected, actual)

    [<Test>]
    let ``should be able to get classification spans for unused items``() = 
        let content = """
open System
open System.Collections.Generic
let internal f() = ()
"""
        let buffer = createMockTextBuffer content fileName        
        // IsSymbolUsedForProject seems to require a file to exist on disks
        // If not, type checking fails with some weird errors
        File.WriteAllText(fileName, "")
        helper.SetUpProjectAndCurrentDocument(VirtualProjectProvider(buffer, fileName), fileName)
        let classifier = helper.GetClassifier(buffer)

        // first event is raised when "fast calculatable" spans (without Unused declarations and opens) are ready
        testEvent classifier.ClassificationChanged "Timed out before classification changed" timeout <| fun _ ->
            helper.ClassificationSpansOf(buffer, classifier)
            |> Seq.toList
            |> assertEqual
                [ { Classification = "FSharp.Function"; Span = (4, 14) => (4, 14) }
                  { Classification = "FSharp.Operator"; Span = (4, 18) => (4, 18) } ]

        // second event is raised when all spans, including Unused are ready
        testEvent classifier.ClassificationChanged "Timed out before classification changed" timeout <| fun _ ->
            let actual = helper.ClassificationSpansOf(buffer, classifier) |> Seq.toList
            let expected =
                [ { Classification = "FSharp.Unused"; Span = (2, 6) => (2, 11) }
                  { Classification = "FSharp.Unused"; Span = (3, 6) => (3, 31) }
                  { Classification = "FSharp.Unused"; Span = (4, 14) => (4, 14) }
                  {Classification = "FSharp.Operator"; Span = (4, 18) => (4, 18) } ]
            CollectionAssert.AreEquivalent(expected, actual)
        File.Delete(fileName)
        

    [<Test>]
    let ``should be able to get classification spans for provided types``() = 
        let content = """
module TypeProviderTests
open FSharp.Data
type Project = XmlProvider< "<root><value>\"1\"</value><value>\"3\"</value></root>">
let _ = Project.GetSample()
let _ = XmlProvider< "<root><value>\"1\"</value></root>">.GetSample()
let _ = XmlProvider< "<root><value>\"1\"</value></root>">.GetSample() |> ignore
"""
        let projectFileName = fullPathBasedOnSourceDir "../data/TypeProviderTests/TypeProviderTests.fsproj"
        let fileName = fullPathBasedOnSourceDir "../data/TypeProviderTests/TypeProviderTests.fs"
        let buffer = createMockTextBuffer content fileName        
        helper.SetUpProjectAndCurrentDocument(ExternalProjectProvider(projectFileName), fileName)
        let classifier = helper.GetClassifier(buffer)
        testEvent classifier.ClassificationChanged "Timed out before classification changed" timeout <| fun _ -> 
            let actual = helper.ClassificationSpansOf(buffer, classifier) |> Seq.toList
            let expected = 
                [ { Classification = "FSharp.Module"; Span = (2, 8, 2, 24) }
                  
                  { Classification = "FSharp.ReferenceType"; Span = (4, 6, 4, 12) }
                  { Classification = "FSharp.Operator"; Span = (4, 14) => (4, 14) }
                  { Classification = "FSharp.ReferenceType"; Span = (4, 16, 4, 26) }
                  { Classification = "FSharp.Escaped"; Span = (4, 43, 4, 44) }
                  { Classification = "FSharp.Escaped"; Span = (4, 46, 4, 47) }
                  { Classification = "FSharp.Escaped"; Span = (4, 63, 4, 64) }
                  { Classification = "FSharp.Escaped"; Span = (4, 66, 4, 67) }
                  
                  { Classification = "FSharp.Operator"; Span = (5, 7) => (5, 7) }
                  { Classification = "FSharp.ReferenceType"; Span = (5, 9, 5, 15) }
                  { Classification = "FSharp.Function"; Span = (5, 17, 5, 25) }

                  { Classification = "FSharp.Operator"; Span = (6, 7) => (6, 7) }
                  { Classification = "FSharp.ReferenceType"; Span = (6, 9, 6, 19) }
                  { Classification = "FSharp.Escaped"; Span = (6, 36, 6, 37) }
                  { Classification = "FSharp.Escaped"; Span = (6, 39, 6, 40) } 
                  { Classification = "FSharp.Function"; Span = (6, 59, 6, 67) }

                  { Classification = "FSharp.Operator"; Span = (7, 7) => (7, 7) }
                  { Classification = "FSharp.ReferenceType"; Span = (7, 9, 7, 19) }
                  { Classification = "FSharp.Escaped"; Span = (7, 36, 7, 37) }
                  { Classification = "FSharp.Escaped"; Span = (7, 39, 7, 40) } 
                  { Classification = "FSharp.Function"; Span = (7, 59, 7, 67) }
                  { Classification = "FSharp.Operator"; Span = (7, 71, 7, 72) } 
                  { Classification = "FSharp.Function"; Span = (7, 74, 7, 79) } ]
            CollectionAssert.AreEquivalent(expected, actual)
