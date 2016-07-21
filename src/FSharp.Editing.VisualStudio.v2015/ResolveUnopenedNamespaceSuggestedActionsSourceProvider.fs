﻿namespace FSharp.Editing.VisualStudio.v2015

open System.ComponentModel.Composition
open Microsoft.VisualStudio.Language.Intellisense
open System
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Text.Editor
open Microsoft.VisualStudio.Text.Operations
open Microsoft.VisualStudio.Utilities
open FSharp.Editing
open FSharp.Editing.VisualStudio.ProjectSystem
open System.Threading.Tasks
open Microsoft.VisualStudio.Shell
open Microsoft.VisualStudio.Imaging.Interop
open FSharp.Editing.VisualStudio
open FSharp.Editing.VisualStudio.CodeGeneration

[<Export(typeof<ISuggestedActionsSourceProvider>)>]
[<Name "Resolve Unopened Namespaces Suggested Actions">]
[<ContentType "F#">]
[<TextViewRole(PredefinedTextViewRoles.Editable)>]
type ResolveUnopenedNamespaceSuggestedActionsSourceProvider [<ImportingConstructor>]
   (fsharpVsLanguageService: VSLanguageService,
    textDocumentFactoryService: ITextDocumentFactoryService,
    [<Import(typeof<SVsServiceProvider>)>]
    serviceProvider: IServiceProvider,
    undoHistoryRegistry: ITextUndoHistoryRegistry,
    projectFactory: ProjectFactory) =

    interface ISuggestedActionsSourceProvider with
        member __.CreateSuggestedActionsSource(textView: ITextView, buffer: ITextBuffer): ISuggestedActionsSource =
            if textView.TextBuffer <> buffer then null
            else
                let generalOptions = Setting.getGeneralOptions serviceProvider
                if generalOptions == null || not generalOptions.ResolveUnopenedNamespacesEnabled then null
                else
                    match textDocumentFactoryService.TryGetTextDocument(buffer) with
                    | true, doc -> 
                        let resolver =
                            new UnopenedNamespaceResolver(
                                doc, textView, undoHistoryRegistry.RegisterHistory(buffer), fsharpVsLanguageService, projectFactory)

                        new ResolveUnopenedNamespaceSuggestedActionsSource(resolver) :> _
                    | _ -> null

and ResolveUnopenedNamespaceSuggestedActionsSource (resolver: UnopenedNamespaceResolver) as self =
    let actionsChanged = Event<_,_>()
    do resolver.Updated.Add (fun _ -> actionsChanged.Trigger (self, EventArgs.Empty))
    interface ISuggestedActionsSource with
        member __.Dispose() = (resolver :> IDisposable).Dispose()
        member __.GetSuggestedActions (_requestedActionCategories, _range, _ct) =
            match resolver.Suggestions with
            | [] -> Seq.empty
            | suggestions ->
                seq { for span, groups in suggestions do
                        let actions =
                            [ for group in groups do
                                for s in group do
                                   yield { new ISuggestedAction with
                                             member __.DisplayText = s.Text
                                             member __.Dispose() = ()
                                             member __.GetActionSetsAsync _ct = Task.FromResult <| seq []
                                             member __.GetPreviewAsync _ct = Task.FromResult null
                                             member __.HasActionSets = false
                                             member __.HasPreview = false
                                             member __.IconAutomationText = null
                                             member __.IconMoniker =
                                                 if s.NeedsIcon then ImageMoniker(Guid=Guid "{ae27a6b0-e345-4288-96df-5eaf394ee369}", Id=90)
                                                 else Unchecked.defaultof<_>
                                             member __.InputGestureText = null
                                             member __.Invoke _ct = s.Invoke()
                                             member __.TryGetTelemetryId _telemetryId = false } ]
                        yield SuggestedActionSet(actions, applicableToSpan = Nullable span.Span) }

        member __.HasSuggestedActionsAsync (_requestedCategories, _range, _ct) =
            Task.FromResult (resolver.Suggestions |> List.isEmpty |> not)

        [<CLIEvent>]
        member __.SuggestedActionsChanged: IEvent<EventHandler<EventArgs>, EventArgs> = actionsChanged.Publish
        member __.TryGetTelemetryId telemetryId = telemetryId <- Guid.Empty; false
