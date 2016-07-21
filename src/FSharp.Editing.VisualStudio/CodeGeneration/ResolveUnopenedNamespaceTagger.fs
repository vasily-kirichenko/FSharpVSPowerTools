namespace FSharp.Editing.VisualStudio.CodeGeneration

open Microsoft.FSharp.Compiler
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Text.Editor
open Microsoft.VisualStudio.Text.Tagging
open Microsoft.VisualStudio.Text.Operations
open Microsoft.VisualStudio.Language.Intellisense
open System
open FSharp.Editing
open FSharp.Editing.VisualStudio
open FSharp.Editing.VisualStudio.ProjectSystem
open FSharp.Editing.Features
open FSharp.Editing.AsyncMaybe

type ResolveUnopenedNamespaceSmartTag(actionSets) =
    inherit SmartTag(SmartTagType.Factoid, actionSets)

type UnopenedNamespaceResolver
    (
        doc: ITextDocument,
        view: ITextView, 
        textUndoHistory: ITextUndoHistory,
        vsLanguageService: VSLanguageService, 
        projectFactory: ProjectFactory
    ) as self =
    
    let buffer = view.TextBuffer
    let changed = Event<_>()
    let mutable suggestions: (SnapshotSpan * SuggestionGroup list) list = [] 
    
    let openNamespace (snapshotSpan: SnapshotSpan) (ctx: InsertContext) ns name = 
        use transaction = textUndoHistory.CreateTransaction(Resource.recordGenerationCommandName)
        // first, replace the symbol with (potentially) partially qualified name
        let snapshot = 
            if name <> "" then snapshotSpan.Snapshot.TextBuffer.Replace (snapshotSpan.Span, name) 
            else snapshotSpan.Snapshot
        
        let doc =
            { new IInsertContextDocument<ITextSnapshot> with
                  member __.Insert (snapshot, line, lineStr) = 
                      let pos = snapshot.GetLineFromLineNumber(line).Start.Position
                      snapshot.TextBuffer.Insert (pos, lineStr + Environment.NewLine)
                  member __.GetLineStr (snapshot, line) = snapshot.GetLineFromLineNumber(line).GetText() }
        
        InsertContext.insertOpenDeclaration snapshot doc ctx ns |> ignore
        transaction.Complete()

    let replaceFullyQualifiedSymbol (snapshotSpan: SnapshotSpan) qualifier = 
        use transaction = textUndoHistory.CreateTransaction(Resource.recordGenerationCommandName)
        snapshotSpan.Snapshot.TextBuffer.Replace (snapshotSpan.Span, qualifier) |> ignore
        transaction.Complete()

    let fixUnderscoresInMenuText (text: string) = text.Replace("_", "__")

    let openNamespaceAction snapshot ctx name ns multipleNames = 
        let displayText = "open " + ns + if multipleNames then " (" + name + ")" else ""

        { new ISuggestion with
            member __.Text = fixUnderscoresInMenuText displayText
            member __.Invoke() = openNamespace snapshot ctx ns name
            member __.NeedsIcon = true }

    let qualifiedSymbolAction snapshotSpan (fullName, qualifier) =
        { new ISuggestion with
            member __.Text = fixUnderscoresInMenuText fullName
            member __.Invoke() = replaceFullyQualifiedSymbol snapshotSpan qualifier
            member __.NeedsIcon = false }

    let getSuggestions (snapshotSpan: SnapshotSpan) (candidates: (Entity * InsertContext) list) : SuggestionGroup list =
        let openNamespaceActions = 
            candidates
            |> Seq.choose (fun (entity, ctx) -> entity.Namespace |> Option.map (fun ns -> ns, entity.Name, ctx))
            |> Seq.groupBy (fun (ns, _, _) -> ns)
            |> Seq.map (fun (ns, xs) -> 
                ns, 
                xs 
                |> Seq.map (fun (_, name, ctx) -> name, ctx) 
                |> Seq.distinctBy (fun (name, _) -> name)
                |> Seq.sortBy fst
                |> Seq.toArray)
            |> Seq.map (fun (ns, names) ->
                let multipleNames = names |> Array.length > 1
                names |> Seq.map (fun (name, ctx) -> ns, name, ctx, multipleNames))
            |> Seq.concat
            |> Seq.map (fun (ns, name, ctx, multipleNames) -> 
                openNamespaceAction snapshotSpan ctx name ns multipleNames)
            |> Seq.toList
            
        let qualifySymbolActions =
            candidates
            |> Seq.map (fun (entity, _) -> entity.FullRelativeName, entity.Qualifier)
            |> Seq.distinct
            |> Seq.sort
            |> Seq.map (qualifiedSymbolAction snapshotSpan)
            |> Seq.toList
            
        match openNamespaceActions, qualifySymbolActions with
        | [], [] -> []
        | _ -> [ openNamespaceActions; qualifySymbolActions ]

    let project() = projectFactory.CreateForDocument buffer doc.FilePath

    let updateAtCaretPosition (CallInUIContext callInUIContext) =
        async {
            let! result = asyncMaybe {
                let! point = buffer.GetSnapshotPoint view.Caret.Position
                let! project = project()
                let! checkResults = vsLanguageService.ParseAndCheckFileInProject(doc.FilePath, project)

                match checkResults.Errors with
                | [||] -> return! None
                | errors ->
                    Logging.logError <| fun _ -> sprintf "Parse errors: %+A" errors
                    let! parseTree = checkResults.ParseTree
                    let! entities = vsLanguageService.GetAllEntities (doc.FilePath, project)
                    let currentLine = point.Line + 1
                    return!
                        errors
                        |> Array.filter (fun e -> e.StartLineAlternate = currentLine && e.EndLineAlternate = currentLine)
                        |> Async.Array.map (fun e ->
                            asyncMaybe {
                                let line = e.StartLineAlternate - 1
                                let range = Range.make line e.StartColumn line e.EndColumn
                                let word = SnapshotSpan.MakeFromRange point.Snapshot range
                                let! entityKind = ParsedInput.getEntityKind parseTree (Range.mkPos e.StartLineAlternate e.StartColumn)
                                                                    
                                //entities |> Seq.map string |> fun es -> System.IO.File.WriteAllLines (@"l:\entities.txt", es)
                                
                                let isAttribute = entityKind = EntityKind.Attribute
                                let entities =
                                    entities |> List.filter (fun e ->
                                        match entityKind, e.Kind with
                                        | EntityKind.Attribute, EntityKind.Attribute 
                                        | EntityKind.Type, (EntityKind.Type | EntityKind.Attribute)
                                        | EntityKind.FunctionOrValue _, _ -> true 
                                        | EntityKind.Attribute, _
                                        | _, EntityKind.Module _
                                        | EntityKind.Module _, _
                                        | EntityKind.Type, _ -> false)
                                
                                let entities = 
                                    [ for e in entities do
                                         yield e.TopRequireQualifiedAccessParent, e.AutoOpenParent, e.Namespace, e.CleanedIdents
                                
                                         if isAttribute then
                                             let lastIdent = e.CleanedIdents.[e.CleanedIdents.Length - 1]
                                             if e.Kind = EntityKind.Attribute && lastIdent.EndsWith "Attribute" then
                                                 yield 
                                                     e.TopRequireQualifiedAccessParent, 
                                                     e.AutoOpenParent,
                                                     e.Namespace,
                                                     e.CleanedIdents 
                                                     |> Array.replace (e.CleanedIdents.Length - 1) (lastIdent.Substring(0, lastIdent.Length - 9)) ]
                                
                                debug "[ResolveUnopenedNamespaceSmartTagger] %d entities found" entities.Length
                                
                                let! idents = UntypedAstUtils.getLongIdentAt parseTree (Range.mkPos line range.End.Column)
                                let createEntity = ParsedInput.tryFindInsertionContext range.Start.Line parseTree idents
                                return word, entities |> Seq.collect createEntity |> Seq.toList |> getSuggestions word 
                            })
                        |> Async.map (Array.choose id >> Array.toList)
                        |> liftAsync
            } 
            suggestions <- result |> Option.getOrElse []
            do! callInUIContext <| fun _ -> changed.Trigger self
        }

    let docEventListener = new DocumentEventListener ([ViewChange.layoutEvent view; ViewChange.caretEvent view], 100us, updateAtCaretPosition)

    member __.Updated = changed.Publish
    member __.Suggestions = suggestions

    interface IDisposable with
        member __.Dispose() = dispose docEventListener

type ResolveUnopenedNamespaceSmartTagger
    (
        buffer: ITextBuffer, 
        serviceProvider: IServiceProvider, 
        resolver: UnopenedNamespaceResolver
    ) as self =
    
    let tagsChanged = Event<_, _>()
    let openNamespaceIcon = ResourceProvider.getRefactoringIcon serviceProvider RefactoringIconKind.AddUsing
    do resolver.Updated.Add (fun _ -> buffer.TriggerTagsChanged self tagsChanged)

    interface ITagger<ResolveUnopenedNamespaceSmartTag> with
        member __.GetTags _ =
            protectOrDefault (fun _ ->
                seq {
                    match resolver.Suggestions with
                    | [] -> ()
                    | suggestions ->
                        for span, suggestions in suggestions do
                            let actions =
                                suggestions
                                |> List.map (fun xs ->
                                    xs 
                                    |> List.map (fun suggestion ->
                                        { new ISmartTagAction with
                                            member __.ActionSets = null
                                            member __.DisplayText = suggestion.Text
                                            member __.Icon = if suggestion.NeedsIcon then openNamespaceIcon else null
                                            member __.IsEnabled = true
                                            member __.Invoke() = suggestion.Invoke() })
                                    |> Seq.toReadOnlyCollection
                                    |> fun xs -> SmartTagActionSet xs)
                                |> Seq.toReadOnlyCollection
                            
                            yield TagSpan<_>(span, ResolveUnopenedNamespaceSmartTag actions) :> ITagSpan<_>
                })
                Seq.empty
             
        [<CLIEvent>]
        member __.TagsChanged = tagsChanged.Publish

    interface IDisposable with
        member __.Dispose() = (resolver :> IDisposable).Dispose()