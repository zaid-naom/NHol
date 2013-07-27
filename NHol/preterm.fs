﻿(*

Copyright 1998 University of Cambridge
Copyright 1998-2007 John Harrison
Copyright 2012 Marco Maggesi
Copyright 2012 Vincent Aravantinos
Copyright 2013 Jack Pappas, Anh-Dung Phan, Eric Taucher

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

*)

#if INTERACTIVE
#else
/// Preterms and pretypes; typechecking; translation to types and terms.
module NHol.preterm

open FSharp.Compatibility.OCaml
open FSharp.Compatibility.OCaml.Num

open ExtCore.Control
open ExtCore.Control.Collections

open NHol
open lib
open fusion
open fusion.Hol_kernel
open basics
open nets
open printer
#endif

(* ------------------------------------------------------------------------- *)
(* Flag to say whether to treat varstruct "\const. bod" as variable.         *)
(* ------------------------------------------------------------------------- *)

/// Interpret a simple varstruct as a variable, even if there is a constant of that name.
let ignore_constant_varstruct = ref true

(* ------------------------------------------------------------------------- *)
(* Flags controlling the treatment of invented type variables in quotations. *)
(* It can be treated as an error, result in a warning, or neither of those.  *)
(* ------------------------------------------------------------------------- *)

/// Determined if user is warned about invented type variables.
let type_invention_warning = ref true

/// Determines if invented type variables are treated as an error.
let type_invention_error = ref false

(* ------------------------------------------------------------------------- *)
(* Implicit types or type schemes for non-constants.                         *)
(* ------------------------------------------------------------------------- *)

/// Restrict variables to a particular type or type scheme.
let the_implicit_types = ref([] : (string * hol_type) list)

(* ------------------------------------------------------------------------- *)
(* Overloading and interface mapping.                                        *)
(* ------------------------------------------------------------------------- *)

/// Makes a symbol overloadable within the specified type skeleton.
let make_overloadable s gty =
    match assoc s !the_overload_skeletons with
    | Some x ->
        if x = gty then Choice.result ()
        else Choice.failwith "make_overloadable: differs from existing skeleton"
    | None ->
        Choice.result (the_overload_skeletons := (s, gty) :: (!the_overload_skeletons))

/// Remove all overload/interface mappings for an identifier.
let remove_interface sym = 
    let ``interface`` = filter ((<>) sym << fst) (!the_interface)
    the_interface := ``interface``

/// Remove a specific overload/interface mapping for an identifier.
let reduce_interface(sym, tm) = 
    let namty = 
        dest_const tm
        |> Choice.mapError (fun _ -> dest_var tm)
    match namty with
    | Success namty ->        
        the_interface := filter ((<>)(sym, namty)) (!the_interface)
    | Error _ ->
        // NOTE: currently doing nothing if error case is supplied
        ()

/// Map identifier to specific underlying constant.
let override_interface(sym, tm) = 
    let namty = 
        dest_const tm
        |> Choice.mapError (fun _ -> dest_var tm)
    match namty with
    | Success namty ->
        let ``interface`` = filter ((<>) sym << fst) (!the_interface)
        the_interface := (sym, namty) :: ``interface``
    | Error _ ->
        // NOTE: currently doing nothing if error case is supplied
        ()

/// Overload a symbol so it may denote a particular underlying constant.
let overload_interface(sym, tm) = 
    let gty =
        match assoc sym (!the_overload_skeletons) with
        | Some x -> Choice.result x
        | None ->
            Choice.failwith("symbol \"" + sym + "\" is not overloadable")
    gty
    |> Choice.bind (fun gty ->
        match dest_const tm |> Choice.mapError (fun _ -> dest_var tm) with
        | Success ((name, ty) as namty) ->
            if not(Choice.isResult <| type_match gty ty []) then 
                Choice.failwith "Not an instance of type skeleton"
            else 
                let ``interface`` = filter ((<>)(sym, namty)) (!the_interface)
                Choice.result (the_interface := (sym, namty) :: ``interface``)
        // NOTE: currently doing nothing if error case is supplied
        | Error _ -> Choice.result ())

/// Give overloaded constants involving a given type priority in operator overloading.
let prioritize_overload ty = 
    do_list (fun (s, gty) -> 
        try 
            let _, (n, t) = 
                Option.get <| find (fun (s', (n, t)) -> s' = s && mem ty (map fst (Choice.get <| type_match gty t []))) (!the_interface)
            Choice.get <| overload_interface(s, mk_var(n, t))
        with
        | Failure _ -> ()) (!the_overload_skeletons)

(* ------------------------------------------------------------------------- *)
(* Type abbreviations.                                                       *)
(* ------------------------------------------------------------------------- *)

// new_type_abbrev: Sets up a new type abbreviation.
// remove_type_abbrev: Removes use of name as a type abbreviation.
// type_abbrevs: Lists all current type abbreviations.
let new_type_abbrev, remove_type_abbrev, type_abbrevs = 
    let the_type_abbreviations = ref([] : (string * hol_type) list)
    let remove_type_abbrev s = the_type_abbreviations := filter (fun (s', _) -> s' <> s) (!the_type_abbreviations)
    let new_type_abbrev(s, ty) = 
        (remove_type_abbrev s
         the_type_abbreviations := merge (<) [s, ty] (!the_type_abbreviations))
    let type_abbrevs() = !the_type_abbreviations
    new_type_abbrev, remove_type_abbrev, type_abbrevs

(* ------------------------------------------------------------------------- *)
(* Handle constant hiding.                                                   *)
(* ------------------------------------------------------------------------- *)

// hide_constant: Restores recognition of a constant by the quotation parser.
// unhide_constant: Disables recognition of a constant by the quotation parser.
// is_hidden: Determines whether a constant is hidden.
let hide_constant, unhide_constant, is_hidden = 
    let hcs = ref([] : string list)
    let hide_constant c = hcs := union [c] (!hcs)
    let unhide_constant c = hcs := subtract (!hcs) [c]
    let is_hidden c = mem c (!hcs)
    hide_constant, unhide_constant, is_hidden

(* ------------------------------------------------------------------------- *)
(* The type of pretypes.                                                     *)
(* ------------------------------------------------------------------------- *)

type pretype = 
    | Utv of string                     (* User type variable         *)
    | Ptycon of string * pretype list   (* Type constructor           *)
    | Stv of int                        (* System type variable       *)

(* ------------------------------------------------------------------------- *)
(* Dummy pretype for the parser to stick in before a proper typing pass.     *)
(* ------------------------------------------------------------------------- *)

/// Dummy pretype.
let dpty = Ptycon("", [])

(* ------------------------------------------------------------------------- *)
(* Convert type to pretype.                                                  *)
(* ------------------------------------------------------------------------- *)

/// Converts a type into a pretype.
let rec pretype_of_type ty = 
    choice {
        let! con, args = dest_type ty
        let! ps = Choice.List.map pretype_of_type args
        return Ptycon(con, ps)
    }
    |> Choice.bindError (fun _ -> dest_vartype ty |> Choice.map Utv)

(* ------------------------------------------------------------------------- *)
(* Preterm syntax.                                                           *)
(* ------------------------------------------------------------------------- *)

type preterm = 
    | Varp of string * pretype      (* Variable           - v      *)
    | Constp of string * pretype    (* Constant           - c      *)
    | Combp of preterm * preterm    (* Combination        - f x    *)
    | Absp of preterm * preterm     (* Lambda-abstraction - \x. t  *)
    | Typing of preterm * pretype   (* Type constraint    - t : ty *)

(* ------------------------------------------------------------------------- *)
(* Convert term to preterm.                                                  *)
(* ------------------------------------------------------------------------- *)

/// Converts a term into a preterm.
let rec preterm_of_term tm = 
    choice {
        let! n, ty = dest_var tm
        let! pt = pretype_of_type ty
        return Varp(n, pt)
    }
    |> Choice.bindError (fun _ ->
        choice {
            let! n, ty = dest_const tm
            let! pt = pretype_of_type ty
            return Constp(n, pt)
        })
    |> Choice.bindError (fun _ ->
        choice {
            let! v, bod = dest_abs tm
            let! pb = preterm_of_term bod
            let! pv = preterm_of_term v
            return Absp(pv, pb)
        })
    |> Choice.bindError (fun _ ->
        choice {
            let! l, r = dest_comb tm
            let! l' = preterm_of_term l
            let! r' = preterm_of_term r  
            return Combp(l', r')
        })

(* ------------------------------------------------------------------------- *)
(* Main pretype->type, preterm->term and retypechecking functions.           *)
(* ------------------------------------------------------------------------- *)

// type_of_pretype: Converts a pretype to a type.
// term_of_preterm: Converts a preterm into a term.
// retypecheck: Typecheck a term, iterating over possible overload resolutions.
let type_of_pretype, term_of_preterm, retypecheck = 
    let tyv_num = ref 0
    let new_type_var() = 
        let n = !tyv_num
        (tyv_num := n + 1
         Stv(n))
    let pmk_cv(s, pty) =
        try
            Choice.get <| get_const_type s |> ignore
            Constp(s, pty)
        with _ ->
            Varp(s, pty)
    let pmk_numeral = 
        let num_pty = Ptycon("num", [])
        let NUMERAL = Constp("NUMERAL", Ptycon("fun", [num_pty; num_pty]))
        let BIT0 = Constp("BIT0", Ptycon("fun", [num_pty; num_pty]))
        let BIT1 = Constp("BIT1", Ptycon("fun", [num_pty; num_pty]))
        let t_0 = Constp("_0", num_pty)
        let rec pmk_numeral(n) = 
            if n =/ num_0 then t_0
            else 
                let m = quo_num n (num_2)
                let b = mod_num n (num_2)
                let op = 
                    if b =/ num_0 then BIT0
                    else BIT1
                Combp(op, pmk_numeral(m))
        fun n -> Combp(NUMERAL, pmk_numeral n)

    (* ----------------------------------------------------------------------- *)
    (* Pretype substitution for a pretype resulting from translation of type.  *)
    (* ----------------------------------------------------------------------- *)

    let rec pretype_subst th ty = 
        match ty with
        | Ptycon(tycon, args) -> Ptycon(tycon, map (pretype_subst th) args)
        | Utv v -> rev_assocd ty th ty
        | _ -> failwith "pretype_subst: Unexpected form of pretype"

    (* ----------------------------------------------------------------------- *)
    (* Convert type to pretype with new Stvs for all type variables.           *)
    (* ----------------------------------------------------------------------- *)

    let pretype_instance ty = 
        let gty = Choice.get <| pretype_of_type ty
        let tyvs = map (Choice.get << pretype_of_type) (tyvars ty)
        let subs = map (fun tv -> new_type_var(), tv) tyvs
        pretype_subst subs gty

    (* ----------------------------------------------------------------------- *)
    (* Get a new instance of a constant's generic type modulo interface.       *)
    (* ----------------------------------------------------------------------- *)

    let get_generic_type cname = 
        match filter ((=) cname << fst) (!the_interface) with
        | [_, (c, ty)] -> Choice.result ty
        | _ :: _ :: _ ->
            assoc cname (!the_overload_skeletons)
            |> Option.toChoiceWithError "find"
        | [] -> get_const_type cname

    (* ----------------------------------------------------------------------- *)
    (* Get the implicit generic type of a variable.                            *)
    (* ----------------------------------------------------------------------- *)

    let get_var_type vname =
        assoc vname !the_implicit_types
        |> Option.toChoiceWithError "find"

    (* ----------------------------------------------------------------------- *)
    (* Unravel unifications and apply them to a type.                          *)
    (* ----------------------------------------------------------------------- *)

    let solve env pty = 
        let rec solve env pty = 
            match pty with
            | Ptycon(f, args) -> Ptycon(f, map (solve env) args)
            | Stv(i) -> 
                if defined env i then solve env (apply env i)
                else pty
            | _ -> pty
        Choice.attempt (fun () -> solve env pty)

    (* ----------------------------------------------------------------------- *)
    (* Functions for display of preterms and pretypes, by converting them      *)
    (* to terms and types then re-using standard printing functions.           *)
    (* ----------------------------------------------------------------------- *)

    let free_stvs = 
        let rec free_stvs = 
            function 
            | Stv n -> [n]
            | Utv _ -> []
            | Ptycon(_, args) -> flat(map free_stvs args)
        setify << free_stvs

    let string_of_pretype stvs = 
        let rec type_of_pretype' ns = 
            function 
            | Stv n -> 
                mk_vartype(if mem n ns then "?" + string n else "_")
            | Utv v -> mk_vartype v
            | Ptycon(con, args) -> Choice.get <| mk_type(con, map (type_of_pretype' ns) args)
        fun pt ->
            Choice.attempt (fun () -> string_of_type (type_of_pretype' stvs pt))

    let string_of_preterm = 
        let rec untyped_t_of_pt = 
            function 
            | Varp(s, pty) -> mk_var(s, aty)
            | Constp(s, pty) -> Choice.get <| mk_mconst(s, Choice.get <| get_const_type s)
            | Combp(l, r) -> Choice.get <| mk_comb(untyped_t_of_pt l, untyped_t_of_pt r)
            | Absp(v, bod) -> Choice.get <| mk_gabs(untyped_t_of_pt v, untyped_t_of_pt bod)
            | Typing(ptm, pty) -> untyped_t_of_pt ptm
        fun pt ->
            Choice.attempt (fun () -> string_of_term (untyped_t_of_pt pt))

    let string_of_ty_error env po = 
        let string_of_ty_error env = 
            function 
            | None -> "Choice.get <| unify: types cannot be unified " + "(you should not see this message, please report)"
            | Some(t, ty1, ty2) -> 
                let ty1 = Choice.get <| solve env ty1
                let ty2 = Choice.get <| solve env ty2
                let sty1 = Choice.get <| string_of_pretype (free_stvs ty2) ty1
                let sty2 = Choice.get <| string_of_pretype (free_stvs ty1) ty2
                let default_msg s = " " + s + " cannot have type " + sty1 + " and " + sty2 + " simultaneously"
                match t with
                | Constp(s, _) -> 
                    " " + s + " has type " + string_of_type(Choice.get <| get_const_type s) + ", " + "it cannot be used with type " + sty2
                | Varp(s, _) -> default_msg s
                | t -> default_msg(Choice.get <| string_of_preterm t)
        Choice.attempt (fun () -> string_of_ty_error env po)

    (* ----------------------------------------------------------------------- *)
    (* Unification of types                                                    *)
    (* ----------------------------------------------------------------------- *)

    let rec istrivial ptm env x = 
        function 
        | Stv y as t -> y = x || defined env y && istrivial ptm env x (apply env y)
        | Ptycon(f, args) as t when exists (istrivial ptm env x) args -> failwith(Choice.get <| string_of_ty_error env ptm)
        | (Ptycon _ | Utv _) -> false

    let unify ptm env ty1 ty2 = 
        let unify ptm env ty1 ty2 = 
            let rec unify env = 
                function 
                | [] -> env
                | (ty1, ty2, _) :: oth when ty1 = ty2 -> unify env oth
                | (Ptycon(f, fargs), Ptycon(g, gargs), ptm) :: oth -> 
                    if f = g && length fargs = length gargs then unify env (map2 (fun x y -> x, y, ptm) fargs gargs @ oth)
                    else failwith(Choice.get <| string_of_ty_error env ptm)
                | (Stv x, t, ptm) :: oth -> 
                    if defined env x then unify env ((apply env x, t, ptm) :: oth)
                    else 
                        unify (if istrivial ptm env x t then env
                               else (x |-> t) env) oth
                | (t, Stv x, ptm) :: oth -> unify env ((Stv x, t, ptm) :: oth)
                | (_, _, ptm) :: oth -> failwith(Choice.get <| string_of_ty_error env ptm)
            unify env [ty1, ty2, match ptm with
                                 | None -> None
                                 | Some t -> Some(t, ty1, ty2)]

        Choice.attempt (fun () -> unify ptm env ty1 ty2)

    (* ----------------------------------------------------------------------- *)
    (* Attempt to attach a given type to a term, performing unifications.      *)
    (* ----------------------------------------------------------------------- *)

    let typify ty (ptm, venv, uenv) =
        let rec typify ty (ptm, venv, uenv) =
            //printfn "typify --> %A:%A:%A:%A" ty ptm venv uenv 
            match ptm with
            | Varp(s, _) when (Option.isSome <| assoc s venv) -> 
                let ty' =
                    assoc s venv
                    |> Option.getOrFailWith "find"
                Varp(s, ty'), [], Choice.get <| unify (Some ptm) uenv ty' ty
            | Varp(s, _) when Choice.isResult <| num_of_string s -> 
                let t = pmk_numeral(Choice.get <| num_of_string s)
                let ty' = Ptycon("num", [])
                t, [], Choice.get <| unify (Some ptm) uenv ty' ty
            | Varp(s, _) -> 
                warn (s <> "" && isnum s) "Non-numeral begins with a digit"
                if not(is_hidden s) && Choice.isResult <| get_generic_type s then 
                    let pty = pretype_instance(Choice.get <| get_generic_type s)
                    let ptm = Constp(s, pty)
                    ptm, [], Choice.get <| unify (Some ptm) uenv pty ty
                else 
                    let ptm = Varp(s, ty)
                    if not(Choice.isResult <| get_var_type s) then ptm, [s, ty], uenv
                    else 
                        let pty = pretype_instance(Choice.get <| get_var_type s)
                        ptm, [s, ty], Choice.get <| unify (Some ptm) uenv pty ty
            | Combp(f, x) -> 
                let ty'' = new_type_var()
                let ty' = Ptycon("fun", [ty''; ty])
                let f', venv1, uenv1 = typify ty' (f, venv, uenv)
                let x', venv2, uenv2 = typify ty'' (x, venv1 @ venv, uenv1)
                Combp(f', x'), (venv1 @ venv2), uenv2
            | Typing(tm, pty) -> typify ty (tm, venv, Choice.get <| unify (Some tm) uenv ty pty)
            | Absp(v, bod) -> 
                let ty', ty'' = 
                    match ty with
                    | Ptycon("fun", [ty'; ty'']) -> ty', ty''
                    | _ -> new_type_var(), new_type_var()
                let ty''' = Ptycon("fun", [ty'; ty''])
                let uenv0 = Choice.get <| unify (Some ptm) uenv ty''' ty
                let v', venv1, uenv1 = 
                    let v', venv1, uenv1 = typify ty' (v, [], uenv0)
                    match v' with
                    | Constp(s, _) when !ignore_constant_varstruct -> Varp(s, ty'), [s, ty'], uenv0
                    | _ -> v', venv1, uenv1
                let bod', venv2, uenv2 = typify ty'' (bod, venv1 @ venv, uenv1)
                Absp(v', bod'), venv2, uenv2
            | _ -> failwith "typify: unexpected constant at this stage"
        Choice.attempt (fun () -> typify ty (ptm, venv, uenv))

    (* ----------------------------------------------------------------------- *)
    (* Further specialize type constraints by resolving overloadings.          *)
    (* ----------------------------------------------------------------------- *)

    let resolve_interface ptm cont env = 
        let rec resolve_interface ptm cont env = 
            match ptm with
            | Combp(f, x) -> resolve_interface f (resolve_interface x cont) env
            | Absp(v, bod) -> resolve_interface v (resolve_interface bod cont) env
            | Varp(_, _) -> cont env
            | Constp(s, ty) -> 
                let maps = filter (fun (s', _) -> s' = s) (!the_interface)
                if maps = [] then cont env
                else 
                    tryfind (fun (_, (_, ty')) -> 
                            let ty' = pretype_instance ty'
                            Some <| cont(Choice.get <| unify (Some ptm) env ty' ty)) maps
                    |> Option.getOrFailWith "tryfind"
            | _ -> failwith "resolve_interface: Unhandled case."
        Choice.attempt (fun () -> resolve_interface ptm cont env)

    (* ----------------------------------------------------------------------- *)
    (* Hence apply throughout a preterm.                                       *)
    (* ----------------------------------------------------------------------- *)

    let solve_preterm env ptm = 
        let rec solve_preterm env ptm = 
            match ptm with
            | Varp(s, ty) -> Varp(s, Choice.get <| solve env ty)
            | Combp(f, x) -> Combp(solve_preterm env f, solve_preterm env x)
            | Absp(v, bod) -> Absp(solve_preterm env v, solve_preterm env bod)
            | Constp(s, ty) -> 
                let tys = Choice.get <| solve env ty
                try 
                    let _, (c', _) =
                        Option.get <| find (fun (s', (c', ty')) -> s = s' && Choice.isResult <| unify None env (pretype_instance ty') ty) 
                            (!the_interface)
                    pmk_cv(c', tys)
                with
                | Failure _ -> Constp(s, tys)
            | _ -> failwith "solve_preterm: Unhandled case."
        Choice.attempt (fun () -> solve_preterm env ptm)

    (* ----------------------------------------------------------------------- *)
    (* Flag to indicate that Stvs were translated to real type variables.      *)
    (* ----------------------------------------------------------------------- *)

    let stvs_translated = ref false

    (* ----------------------------------------------------------------------- *)
    (* Pretype <-> type conversion; -> flags system type variable translation. *)
    (* ----------------------------------------------------------------------- *)

    let type_of_pretype ty =
        let rec type_of_pretype ty = 
            match ty with
            | Stv n -> 
                stvs_translated := true
                let s = "?" + (string n)
                mk_vartype(s)
            | Utv(v) -> mk_vartype(v)
            | Ptycon(con, args) -> Choice.get <| mk_type(con, map type_of_pretype args)
        Choice.attempt (fun () -> type_of_pretype ty)

    (* ----------------------------------------------------------------------- *)
    (* Maps preterms to terms.                                                 *)
    (* ----------------------------------------------------------------------- *)

    let term_of_preterm = 
        let rec term_of_preterm ptm = 
            match ptm with
            | Varp(s, pty) -> mk_var(s, Choice.get <| type_of_pretype pty)
            | Constp(s, pty) -> Choice.get <| mk_mconst(s, Choice.get <| type_of_pretype pty)
            | Combp(l, r) -> Choice.get <| mk_comb(term_of_preterm l, term_of_preterm r)
            | Absp(v, bod) -> Choice.get <| mk_gabs(term_of_preterm v, term_of_preterm bod)
            | Typing(ptm, pty) -> term_of_preterm ptm
        let report_type_invention() = 
            if !stvs_translated then 
                if !type_invention_error then failwith "typechecking error (cannot infer type of variables)"
                else warn !type_invention_warning "inventing type variables"
        fun ptm -> 
            Choice.attempt (fun () ->
                stvs_translated := false
                let tm = term_of_preterm ptm
                report_type_invention()
                tm)

    (* ----------------------------------------------------------------------- *)
    (* Overall typechecker: initial typecheck plus overload resolution pass.   *)
    (* ----------------------------------------------------------------------- *)

    let retypecheck venv ptm = 
        let retypecheck venv ptm = 
            let ty = new_type_var()
            let ptm', _, env = 
                try 
                    Choice.get <| typify ty (ptm, venv, undefined)
                with
                | Failure msg as e ->
                    let msg = "typechecking error (initial type assignment): " + msg
                    nestedFailwith e msg

            let env' = 
                try 
                    Choice.get <| resolve_interface ptm' id env
                with
                | Failure _ as e ->
                    nestedFailwith e "typechecking error (overload resolution)"
            let ptm'' = solve_preterm env' ptm'
            ptm''
        // TODO: recheck this
        Choice.attemptNested <| fun () ->
            retypecheck venv ptm

    type_of_pretype, term_of_preterm, retypecheck