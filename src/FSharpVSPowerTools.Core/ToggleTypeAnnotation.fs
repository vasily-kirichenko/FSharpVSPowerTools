module FSharpVSPowerTools.CodeGeneration.ToggleTypeAnnotation

open FSharpVSPowerTools
open FSharpVSPowerTools.AsyncMaybe
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.Range

let getArgumentExpr pos parsedInput = 
    let inline getIfPosInRange range f =
        if rangeContainsPos range pos then f()
        else None

    let rec walkImplFileInput (ParsedImplFileInput(_, _, _, _, _, moduleOrNamespaceList, _)) = 
        List.tryPick walkSynModuleOrNamespace moduleOrNamespaceList

    and walkSynModuleOrNamespace (SynModuleOrNamespace(_, _, decls, _, _, _, range)) =
        getIfPosInRange range (fun () ->
            List.tryPick walkSynModuleDecl decls
        )

    and walkSynModuleDecl (decl: SynModuleDecl) =
        getIfPosInRange decl.Range (fun () ->
            match decl with
            | SynModuleDecl.Exception(ExceptionDefn(_, synMembers, _), _) -> List.tryPick walkSynMemberDefn synMembers
            | SynModuleDecl.Let(_, bindings, _) -> List.tryPick walkBinding bindings
            | SynModuleDecl.ModuleAbbrev _ -> None
            | SynModuleDecl.NamespaceFragment(fragment) -> walkSynModuleOrNamespace fragment
            | SynModuleDecl.NestedModule(_, modules, _, _) -> List.tryPick walkSynModuleDecl modules
            | SynModuleDecl.Types(typeDefs, _) -> List.tryPick walkSynTypeDefn typeDefs
            | SynModuleDecl.DoExpr (_, expr, _) -> walkExpr expr
            | SynModuleDecl.Attributes _
            | SynModuleDecl.HashDirective _
            | SynModuleDecl.Open _ -> None
        )

    and walkSynTypeDefn (TypeDefn(_, representation, members, range)) = 
        getIfPosInRange range (fun () ->
            walkSynTypeDefnRepr representation
            |> Option.orElse (List.tryPick walkSynMemberDefn members)        
        )

    and walkSynTypeDefnRepr(typeDefnRepr: SynTypeDefnRepr) = 
        getIfPosInRange typeDefnRepr.Range (fun () ->
            match typeDefnRepr with
            | SynTypeDefnRepr.ObjectModel(_, members, _) -> List.tryPick walkSynMemberDefn members
            | SynTypeDefnRepr.Simple _ -> None
        )

    and walkSynMemberDefn (memberDefn: SynMemberDefn) =
        getIfPosInRange memberDefn.Range (fun () ->
            match memberDefn with
            | SynMemberDefn.AbstractSlot _ -> None
            | SynMemberDefn.AutoProperty(_, _, _, _, _, _, _, _, expr, _, _) -> walkExpr expr
            | SynMemberDefn.Interface(_, members, _) -> Option.bind (List.tryPick walkSynMemberDefn) members
            | SynMemberDefn.Member(binding, _) -> walkBinding binding
            | SynMemberDefn.NestedType(typeDef, _, _) -> walkSynTypeDefn typeDef
            | SynMemberDefn.ValField _ -> None
            | SynMemberDefn.LetBindings(bindings, _, _, _) -> List.tryPick walkBinding bindings
            | SynMemberDefn.Open _
            | SynMemberDefn.ImplicitInherit _
            | SynMemberDefn.Inherit _
            | SynMemberDefn.ImplicitCtor _ -> None
        )

    and walkBinding (Binding (_, _, _, _, _, _, _, _, retTy, expr, _, _) as binding) =
        getIfPosInRange binding.RangeOfBindingAndRhs (fun () -> walkExpr expr)

    and walkExpr expr =
        getIfPosInRange expr.Range (fun () ->
            match expr with
            | SynExpr.Quote (synExpr1, _, synExpr2, _, _) -> List.tryPick walkExpr [synExpr1; synExpr2]
            | SynExpr.Const _ -> None
            | SynExpr.Typed (synExpr, _, _) -> walkExpr synExpr
            | SynExpr.Paren(synExpr, _, _, _)
            | SynExpr.New(_, _, synExpr, _)
            | SynExpr.ArrayOrListOfSeqExpr(_, synExpr, _)
            | SynExpr.CompExpr(_, _, synExpr, _)
            | SynExpr.Lambda(_, _, _, synExpr, _)
            | SynExpr.Lazy(synExpr, _)
            | SynExpr.Do(synExpr, _)
            | SynExpr.Assert(synExpr, _) -> walkExpr synExpr

            | SynExpr.Tuple(synExprList, _, _)
            | SynExpr.ArrayOrList(_, synExprList, _) -> List.tryPick walkExpr synExprList

            | SynExpr.Record _ -> None

            | SynExpr.ObjExpr(_, _, binds, ifaces, _, _) -> 
                List.tryPick walkBinding binds
                |> Option.orElse (List.tryPick walkSynInterfaceImpl ifaces)
            | SynExpr.While(_, synExpr1, synExpr2, _) -> List.tryPick walkExpr [synExpr1; synExpr2]
            | SynExpr.ForEach(_, _, _, _, synExpr1, synExpr2, _) -> List.tryPick walkExpr [synExpr1; synExpr2]
            | SynExpr.For(_, _, synExpr1, _, synExpr2, synExpr3, _) -> List.tryPick walkExpr [synExpr1; synExpr2; synExpr3]
            | SynExpr.MatchLambda(_, _, synMatchClauseList, _, _) -> 
                synMatchClauseList |> List.tryPick (fun (Clause(_, _, e, _, _)) -> walkExpr e)
            | SynExpr.Match(_, synExpr, synMatchClauseList, _, _) ->
                walkExpr synExpr
                |> Option.orElse (synMatchClauseList |> List.tryPick (fun (Clause(_, _, e, _, _)) -> walkExpr e))

            | SynExpr.App(_, _, synExpr1, synExpr2, _) -> List.tryPick walkExpr [synExpr1; synExpr2]
            | SynExpr.TypeApp(synExpr, _, _, _, _, _, _) -> walkExpr synExpr
            | SynExpr.LetOrUse(_, _, synBindingList, synExpr, _) -> 
                Option.orElse (List.tryPick walkBinding synBindingList) (walkExpr synExpr)
            | SynExpr.TryWith(synExpr, _, _, _, _, _, _) -> walkExpr synExpr
            | SynExpr.TryFinally(synExpr1, synExpr2, _, _, _) -> List.tryPick walkExpr [synExpr1; synExpr2]
            | SynExpr.Sequential(_, _, synExpr1, synExpr2, _) -> List.tryPick walkExpr [synExpr1; synExpr2]
            | SynExpr.IfThenElse(synExpr1, synExpr2, synExprOpt, _, _, _, _) -> 
                match synExprOpt with
                | Some synExpr3 ->
                    List.tryPick walkExpr [synExpr1; synExpr2; synExpr3]
                | None ->
                    List.tryPick walkExpr [synExpr1; synExpr2]

            | SynExpr.Ident _
            | SynExpr.LongIdent _ -> None
            | SynExpr.LongIdentSet (_, synExpr, _) -> walkExpr synExpr
            | SynExpr.DotGet(synExpr, _, _, _) -> walkExpr synExpr
            | SynExpr.DotSet(synExpr1, _, synExpr2, _) -> List.tryPick walkExpr [synExpr1; synExpr2]
            | SynExpr.DotIndexedGet(synExpr, IndexerArgList synExprList, _, _) -> 
                Option.orElse (walkExpr synExpr) (List.tryPick walkExpr synExprList) 

            | SynExpr.DotIndexedSet(synExpr1, IndexerArgList synExprList, synExpr2, _, _, _) -> 
                [ yield synExpr1
                  yield! synExprList
                  yield synExpr2 ]
                |> List.tryPick walkExpr

            | SynExpr.JoinIn(synExpr1, _, synExpr2, _) ->
                List.tryPick walkExpr [synExpr1; synExpr2]
            | SynExpr.NamedIndexedPropertySet(_, synExpr1, synExpr2, _) ->
                List.tryPick walkExpr [synExpr1; synExpr2]

            | SynExpr.DotNamedIndexedPropertySet(synExpr1, _, synExpr2, synExpr3, _) ->  
                List.tryPick walkExpr [synExpr1; synExpr2; synExpr3]

            | SynExpr.TypeTest(synExpr, _, _)
            | SynExpr.Upcast(synExpr, _, _)
            | SynExpr.Downcast(synExpr, _, _) -> walkExpr synExpr
            | SynExpr.InferredUpcast(synExpr, _)
            | SynExpr.InferredDowncast(synExpr, _) -> walkExpr synExpr
            | SynExpr.AddressOf(_, synExpr, _, _) -> walkExpr synExpr
            | SynExpr.TraitCall(_, _, synExpr, _) -> walkExpr synExpr
            | SynExpr.Null _
            | SynExpr.ImplicitZero _ -> None
            | SynExpr.YieldOrReturn(_, synExpr, _)
            | SynExpr.YieldOrReturnFrom(_, synExpr, _) 
            | SynExpr.DoBang(synExpr, _) -> walkExpr synExpr
            | SynExpr.LetOrUseBang(_, _, _, _, synExpr1, synExpr2, _) -> List.tryPick walkExpr [synExpr1; synExpr2]
            | SynExpr.LibraryOnlyILAssembly _
            | SynExpr.LibraryOnlyStaticOptimization _ 
            | SynExpr.LibraryOnlyUnionCaseFieldGet _
            | SynExpr.LibraryOnlyUnionCaseFieldSet _
            | SynExpr.ArbitraryAfterError _ -> None
            | SynExpr.FromParseError(synExpr, _)
            | SynExpr.DiscardAfterMissingQualificationAfterDot(synExpr, _) -> 
                walkExpr synExpr
        )

    and walkSynInterfaceImpl (InterfaceImpl(_synType, synBindings, _range)) =
        List.tryPick walkBinding synBindings

    match parsedInput with
    | ParsedInput.SigFile _input -> None
    | ParsedInput.ImplFile input -> walkImplFileInput input

let getArgumentFromPos (codeGenService: ICodeGenerationService<'Project, 'Pos, 'Range>) project (pos: 'Pos) document =
    asyncMaybe {
        let! parseResults = codeGenService.ParseFileInProject(document, project) |> liftAsync
        let pos = codeGenService.ExtractFSharpPos(pos)
        return! parseResults.ParseTree |> Option.bind (getArgumentExpr pos) |> liftMaybe
    }

