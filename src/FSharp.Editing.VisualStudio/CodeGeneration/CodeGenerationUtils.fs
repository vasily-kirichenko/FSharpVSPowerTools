[<AutoOpen>]
module FSharp.Editing.VisualStudio.CodeGeneration.Utils

open Microsoft.VisualStudio.Text

type ISuggestion =
    abstract Text: string
    abstract Invoke: unit -> unit
    abstract NeedsIcon: bool

type SuggestionGroup = ISuggestion list