namespace FSharpPowerTools.Core
open FSharpVSPowerTools
module Symbols = 
    open System.IO
    
    open Microsoft.FSharp.Compiler.SourceCodeServices
    
    let filterSymbolUsesDuplicates (uses: FSharpSymbolUse []) =
        uses
        |> Seq.map (fun symbolUse -> (symbolUse.FileName, symbolUse))
        |> Seq.groupBy (fst >> Path.GetFullPathSafe)
        |> Seq.collect (fun (_, symbolUses) -> 
            symbolUses 
            |> Seq.map snd 
            |> Seq.distinctBy (fun s -> s.RangeAlternate))
        |> Seq.toArray
type GetCheckResults = FileName -> Async<ParseAndCheckResults option>
module HighlightUsageInFile =
    open FSharpVSPowerTools
    open Microsoft.FSharp.Compiler.SourceCodeServices
    
    [<NoComparisonAttribute(* due to FSharpSymbol *)>]
    type HighlightUsageInFileResult =
        | UsageInFile of FSharpSymbol * string * FSharpSymbolUse array

    let findUsageInFile (currentLine: CurrentLine) (symbol: Symbol) (getCheckResults: GetCheckResults) = 
        asyncMaybe {
            let! parseAndCheckResults = getCheckResults currentLine.File
            let! _ = parseAndCheckResults.GetSymbolUseAtLocation (currentLine.EndLine+1, symbol.RightColumn, currentLine.Line, [symbol.Text])
            let! (symbol, ident, refs) = parseAndCheckResults.GetUsesOfSymbolInFileAtLocation (currentLine.EndLine, symbol.RightColumn, currentLine.Line, symbol.Text)
            return UsageInFile (symbol, ident, Symbols.filterSymbolUsesDuplicates refs)
        }