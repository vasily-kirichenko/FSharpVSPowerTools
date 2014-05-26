namespace FSharpVSPowerTools.Refactoring

open System.Collections.Generic
open System.Windows
open System.Windows.Threading
open Microsoft.VisualStudio
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Text.Editor
open Microsoft.VisualStudio.Text.Tagging
open Microsoft.VisualStudio.Text.Operations
open Microsoft.VisualStudio.Language.Intellisense
open Microsoft.VisualStudio.OLE.Interop
open Microsoft.VisualStudio.Shell.Interop
open FSharpVSPowerTools
open FSharpVSPowerTools.CodeGeneration
open FSharpVSPowerTools.CodeGeneration.RecordStubGenerator
open FSharpVSPowerTools.AsyncMaybe
open FSharpVSPowerTools.ProjectSystem
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.Range
open Microsoft.FSharp.Compiler.SourceCodeServices
open System

type ToggleTypeAnnotationSmartTag(actionSets) =
    inherit SmartTag(SmartTagType.Factoid, actionSets)

type ToggleTypeAnnotationSmartTagger(view: ITextView, buffer: ITextBuffer, editorOptionsFactory: IEditorOptionsFactoryService,
                                     textUndoHistory: ITextUndoHistory, vsLanguageService: VSLanguageService,
                                     serviceProvider: IServiceProvider) as self =
    let tagsChanged = Event<_, _>()
    let mutable currentWord = None
    let mutable recordDefinition = None
    let [<Literal>] CommandName = "Toggle type annotation"
    let codeGenInfra: ICodeGenerationService<_, _, _> = upcast CodeGenerationService(vsLanguageService)

    let updateAtCaretPosition() =
        asyncMaybe {
            let! point = buffer.GetSnapshotPoint view.Caret.Position |> liftMaybe
            let dte = serviceProvider.GetService<EnvDTE.DTE, SDTE>()
            let! doc = dte.GetActiveDocument() |> liftMaybe
            let! project = ProjectProvider.createForDocument doc |> liftMaybe
            let vsDocument = VSDocument(doc, point.Snapshot)
            let! symbolRange, recordExpression, recordDefinition, insertionPos =
                tryGetRecordDefinitionFromPos codeGenInfra project point vsDocument
            let newWord = symbolRange

            // Recheck cursor position to ensure it's still in new word
            let! point = buffer.GetSnapshotPoint view.Caret.Position |> liftMaybe
            if point.InSpan newWord then
                return! Some(newWord, recordExpression, recordDefinition, insertionPos)
                        |> liftMaybe
            else
                return! liftMaybe None
        }
        |> Async.map (fun result -> 
            let changed =
                match recordDefinition, result, currentWord with
                | None, None, _ -> false
                | _, Some(newWord, _, _, _), Some oldWord -> newWord <> oldWord
                | _ -> true
            recordDefinition <- result
            if changed then
                let span = SnapshotSpan(buffer.CurrentSnapshot, 0, buffer.CurrentSnapshot.Length)
                tagsChanged.Trigger(self, SnapshotSpanEventArgs(span)))
        |> Async.StartImmediate

    let _ = DocumentEventsListener ([ViewChange.layoutEvent view; ViewChange.caretEvent view], 
                                    200us, updateAtCaretPosition)

    // Check whether the record has been fully defined
    let shouldGenerateRecordStub (recordExpr: RecordExpr) (entity: FSharpEntity) =
        let fieldCount = entity.FSharpFields.Count
        let writtenFieldCount = recordExpr.FieldExprList.Length
        fieldCount > 0 && writtenFieldCount < fieldCount

    let handleGenerateRecordStub (snapshot: ITextSnapshot) (recordExpr: RecordExpr) (insertionPos: _) entity = 
        let editorOptions = editorOptionsFactory.GetOptions(buffer)
        let indentSize = editorOptions.GetOptionValue((IndentSize()).Key)
        let fieldsWritten = recordExpr.FieldExprList

        use transaction = textUndoHistory.CreateTransaction(CommandName)

        let stub = RecordStubGenerator.formatRecord
                       insertionPos
                       indentSize
                       "failwith \"Uninitialized field\""
                       entity
                       fieldsWritten
        let currentLine = snapshot.GetLineFromLineNumber(insertionPos.Position.Line-1).Start.Position + insertionPos.Position.Column

        buffer.Insert(currentLine, stub) |> ignore

        transaction.Complete()

    let getSmartTagActions snapshot expression insertionPos entity =
        let actionSetList = ResizeArray<SmartTagActionSet>()
        let actionList = ResizeArray<ISmartTagAction>()

        actionList.Add 
            { new ISmartTagAction with
                member x.ActionSets = null
                member x.DisplayText = CommandName
                member x.Icon = null
                member x.IsEnabled = true
                member x.Invoke() = handleGenerateRecordStub snapshot expression insertionPos entity }

        let actionSet = SmartTagActionSet(actionList.AsReadOnly())
        actionSetList.Add(actionSet)
        actionSetList.AsReadOnly()

    interface ITagger<ToggleTypeAnnotationSmartTag> with
        member x.GetTags _ =
            seq {
                match recordDefinition with 
                | Some (word, expression, entity, insertionPos) when shouldGenerateRecordStub expression entity ->
                    let span = SnapshotSpan(buffer.CurrentSnapshot, word.Span)
                    let actions = getSmartTagActions word.Snapshot expression insertionPos entity
                    yield upcast TagSpan<_>(span, ToggleTypeAnnotationSmartTag actions)
                | _ -> ()
            }

        [<CLIEvent>]
        member x.TagsChanged = tagsChanged.Publish