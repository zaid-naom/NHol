﻿(*

Copyright 1998 University of Cambridge
Copyright 1998-2007 John Harrison
Copyright 2013 Jack Pappas, Eric Taucher

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

#if USE
#else
/// Basic equality reasoning including conversionals.
module NHol.equal

open System
open FSharp.Compatibility.OCaml

open ExtCore.Control
open ExtCore.Control.Collections

open NHol
open system
open lib
open fusion
open fusion.Hol_kernel
open basics
open nets
open printer
open preterm
open parser
#endif

infof "Entering equal.fs"

(* ------------------------------------------------------------------------- *)
(* Type abbreviation for conversions.                                        *)
(* ------------------------------------------------------------------------- *)

type conv = term -> Protected<thm0>

(* ------------------------------------------------------------------------- *)
(* A bit more syntax.                                                        *)
(* ------------------------------------------------------------------------- *)

/// Take left-hand argument of a binary operator.
let lhand : term -> Protected<term> =
    Choice.bind rand << rator

/// Returns the left-hand side of an equation.
let lhs : term -> Protected<term> =
    Choice.map fst << dest_eq

/// Returns the right-hand side of an equation.
let rhs : term -> Protected<term> =
    Choice.map snd << dest_eq

(* ------------------------------------------------------------------------- *)
(* Similar to variant, but even avoids constants, and ignores types.         *)
(* ------------------------------------------------------------------------- *)

/// Rename variable to avoid specifed names and constant names.
let mk_primed_var : _ -> _ -> Protected<term> =
    let rec svariant avoid s = 
        if mem s avoid || (Choice.isResult <| get_const_type s && not(is_hidden s)) then 
            svariant avoid (s + "'")
        else s
    fun avoid v -> 
        dest_var v
        |> Choice.map (fun (s, ty) ->
            let s' = svariant (mapfilter (Choice.toOption << Choice.map fst << dest_var) avoid) s
            mk_var(s', ty))

(* ------------------------------------------------------------------------- *)
(* General case of beta-conversion.                                          *)
(* ------------------------------------------------------------------------- *)

/// General case of beta-conversion.
let BETA_CONV tm : Protected<thm0> =
    logEntryExitProtected "BETA_CONV" <| fun () ->
    BETA tm
    |> Choice.bindError (function
        | Failure _ ->
            choice { 
                let! f, arg = dest_comb tm
                let! v = bndvar f
                let! tm' = mk_comb(f, v)
                return! INST [arg, v] (BETA tm')
            }
        | e -> Choice.error e)
    |> Choice.mapError (fun e -> nestedFailure e "BETA_CONV: Not a beta-redex")

(* ------------------------------------------------------------------------- *)
(* A few very basic derived equality rules.                                  *)
(* ------------------------------------------------------------------------- *)

/// Applies a function to both sides of an equational theorem.
let AP_TERM tm th : Protected<thm0> =
    logEntryExitProtected "AP_TERM" <| fun () ->
    choice {
        let! th = th
        let! th1 = REFL tm
        return! MK_COMB (Choice.result th1, Choice.result th)
    }
    |> Choice.mapError (fun e -> nestedFailure e "AP_TERM")

/// Proves equality of equal functions applied to a term.
let AP_THM th tm : Protected<thm0> =
    logEntryExitProtected "AP_THM" <| fun () -> 
    choice {
        let! th = th
        let! th1 = REFL tm
        return! MK_COMB (Choice.result th, Choice.result th1)
    }
    |> Choice.mapError (fun e -> nestedFailure e "AP_THM")

/// Swaps left-hand and right-hand sides of an equation.
let SYM (th : Protected<thm0>) : Protected<thm0> =
    logEntryExitProtected "SYM" <| fun () ->
    choice {
        let! th = th
        let tm = concl th
        let! l, _ = dest_eq tm
        let! lth = REFL l
        let! tm' = rator tm
        let! tm'' = rator tm'
        let! th0 = AP_TERM tm'' (Choice.result th)
        let! th1 = MK_COMB(Choice.result th0, Choice.result lth)
        return! EQ_MP (Choice.result th1) (Choice.result lth)
    }

/// Proves equality of lpha-equivalent terms.
let ALPHA tm1 tm2 : Protected<thm0> =
    logEntryExitProtected "ALPHA" <| fun () ->
    choice {
        let! th1 = REFL tm1
        let! th2 = REFL tm2
        return! TRANS (Choice.result th1) (Choice.result th2)
    }
    |> Choice.mapError (fun e -> nestedFailure e "ALPHA")

/// Renames the bound variable of a lambda-abstraction.
let ALPHA_CONV v tm : Protected<thm0> =
    logEntryExitProtected "ALPHA_CONV" <| fun () ->
    choice {
        let! tm1 = alpha v tm
        return! ALPHA tm tm1
    }

/// Renames the bound variable of an abstraction or binder.
let GEN_ALPHA_CONV v tm =
    logEntryExitProtected "GEN_ALPHA_CONV" <| fun () ->
    choice {
        if is_abs tm then 
            return! ALPHA_CONV v tm
        else
            let! (b, abs) = dest_comb tm
            let! th1 = ALPHA_CONV v abs
            return! AP_TERM b (Choice.result th1)
    }

/// Compose equational theorems with binary operator.
let MK_BINOP op (lth, rth) =
    logEntryExitProtected "MK_BINOP" <| fun () ->
    choice {
        let! lth = lth
        let! rth = rth
        let! th1 = AP_TERM op (Choice.result lth)
        return! MK_COMB(Choice.result th1, Choice.result rth)
    }

(* ------------------------------------------------------------------------- *)
(* Terminal conversion combinators.                                          *)
(* ------------------------------------------------------------------------- *)

/// Conversion that always fails.
let NO_CONV : conv =
    fun _ -> Choice.failwith "NO_CONV"

/// Conversion that always succeeds and leaves a term unchanged.
let ALL_CONV : conv = REFL

(* ------------------------------------------------------------------------- *)
(* Combinators for sequencing, trying, repeating etc. conversions.           *)
(* ------------------------------------------------------------------------- *)

/// Applies two conversions in sequence.
let THENC (conv1 : conv) (conv2 : conv) : conv =
    fun t ->
    logEntryExitProtected "THENC" <| fun () ->
    choice {
        let! th1 = conv1 t
        let! t' = rand <| concl th1
        let! th2 = conv2 t'
        return!
            let th1 = Choice.result th1 in
            TRANS th1 (Choice.result th2)
    }

/// Applies the first of two conversions that succeeds.
let ORELSEC (conv1 : conv) (conv2 : conv) : conv =
    fun t ->
    logEntryExitProtected "ORELSEC" <| fun () ->
        conv1 t
        |> Choice.bindError (function Failure _ -> conv2 t | e -> Choice.error e)

/// Apply the first of the conversions in a given list that succeeds.
let FIRST_CONV : conv list -> conv =
    end_itlist ORELSEC

/// Applies in sequence all the conversions in a given list of conversions.
let EVERY_CONV : conv list -> conv =
    fun l -> itlist THENC l ALL_CONV

/// Repeatedly apply a conversion (zero or more times) until it fails.
let REPEATC : conv -> conv =
    let rec REPEATC conv t =
        (ORELSEC (THENC conv (REPEATC conv)) ALL_CONV) t
    REPEATC

/// Makes a conversion fail if applying it leaves a term unchanged.
let CHANGED_CONV (conv : conv) : conv =
    fun tm ->
        choice {
            let! th = conv tm
            let! l, r = dest_eq <| concl th
            if aconv l r then
                return! Choice.failwith "CHANGED_CONV"
            else
                return th
        }

/// Attempts to apply a conversion; applies identity conversion in case of failure.
let TRY_CONV conv = ORELSEC conv ALL_CONV

(* ------------------------------------------------------------------------- *)
(* Subterm conversions.                                                      *)
(* ------------------------------------------------------------------------- *)

/// Applies a conversion to the operator of an application.
let RATOR_CONV (conv : conv) : conv =
    fun tm ->
    choice {
    let! l, r = dest_comb tm
    let! conv_l = conv l
    return!
        let conv_l = Choice.result conv_l in
        AP_THM conv_l r
    }

/// Applies a conversion to the operand of an application.
let RAND_CONV (conv : conv) : conv =
    fun tm ->
    choice {
    let! l, r = dest_comb tm
    let! conv_r = conv r
    return!
        let conv_r = Choice.result conv_r in
        AP_TERM l conv_r
    }

/// Apply a conversion to left-hand argument of binary operator.
let LAND_CONV = RATOR_CONV << RAND_CONV

/// Applies two conversions to the two sides of an application.
let COMB2_CONV (lconv : conv) (rconv : conv) : conv =
    fun tm ->
    choice {
    let! l, r = dest_comb tm
    let! lconv_l = lconv l
    let! rconv_r = rconv r
    return!
        let lconv_l = Choice.result lconv_l in
        let rconv_r = Choice.result rconv_r in
        MK_COMB (lconv_l, rconv_r)
    }

/// Applies a conversion to the two sides of an application.
let COMB_CONV = W COMB2_CONV

/// Applies a conversion to the body of an abstraction.
let ABS_CONV (conv : conv) : conv = 
    fun tm ->
        choice {
        let! v, bod = dest_abs tm
        let! th = conv bod
        return!
            ABS v (Choice.result th)
            |> Choice.bindError (function 
                | Failure _ ->
                    choice {
                    let! tv = type_of v
                    let gv = genvar tv
                    let! gbod = vsubst [gv, v] bod
                    let! gth = ABS gv (conv gbod)
                    let gtm = concl gth
                    let! l, r = dest_eq gtm
                    let! v' = variant (frees gtm) v
                    let! l' = alpha v' l
                    let! r' = alpha v' r
                    let! tm' = mk_eq(l', r')
                    let! th1 = ALPHA gtm tm'
                    return! EQ_MP (Choice.result th1) (Choice.result gth)
                    }
                | e -> Choice.error e)
        }

/// Applies conversion to the body of a binder.
let BINDER_CONV (conv : conv) tm =
    if is_abs tm then
        ABS_CONV conv tm
    else
        RAND_CONV (ABS_CONV conv) tm

/// Applies a conversion to the top-level subterms of a term.
let SUB_CONV (conv : conv) tm = 
    match tm with
    | Comb(_, _) ->
        COMB_CONV conv tm
    | Abs(_, _) ->
        ABS_CONV conv tm
    | _ ->
        REFL tm

/// Applies a conversion to both arguments of a binary operator.
let BINOP_CONV (conv : conv) tm : Protected<thm0> =
    choice {
    let! lop, r = dest_comb tm
    let! op, l = dest_comb lop
    let! conv_l = conv l
    let! conv_r = conv r
    return!
        let conv_l = Choice.result conv_l in
        let conv_r = Choice.result conv_r in
        MK_COMB(AP_TERM op conv_l, conv_r)
    }

(* ------------------------------------------------------------------------- *)
(* Depth conversions; internal use of a failure-propagating 'Boultonized'    *)
(* version to avoid a great deal of rebuilding of terms.                     *)
(* ------------------------------------------------------------------------- *)

let rec private THENQC (conv1 : conv) (conv2 : conv) tm : Protected<thm0> = 
    choice { 
        let! th1 = conv1 tm
        return! 
            choice { 
                let! tm = rand <| concl th1
                let! th2 = conv2 tm
                return! TRANS (Choice.result th1) (Choice.result th2)
            }
            |> Choice.bindError (function Failure _ -> Choice.result th1 | e -> Choice.error e)
    }
    |> Choice.bindError (function Failure _ -> conv2 tm | e -> Choice.error e)

and private THENCQC (conv1 : conv) (conv2 : conv) tm : Protected<thm0> =
    let th1 = conv1 tm
    choice { 
        let! th1 = th1
        let! tm = rand <| concl th1
        let! th2 = conv2 tm
        return! TRANS (Choice.result th1) (Choice.result th2)
    }
    |> Choice.bindError (function Failure _ -> th1 | e -> Choice.error e)

and private COMB_QCONV (conv : conv) tm : Protected<thm0> = 
    choice {
        let! (l, r) = dest_comb tm
        return!
            choice {
                let! th1 = conv l
                return!
                    choice {
                        let! th2 = conv r
                        return! MK_COMB(Choice.result th1, Choice.result th2)
                    }
                    |> Choice.bindError (function Failure _ -> AP_THM (Choice.result th1) r | e -> Choice.error e)
            }
            |> Choice.bindError (function 
                | Failure _ -> 
                    choice {
                        let! r' = conv r
                        return! AP_TERM l (Choice.result r')
                    }
                | e -> Choice.error e)
    }

let rec private REPEATQC (conv : conv) tm = 
    THENCQC conv (REPEATQC conv) tm

let private SUB_QCONV (conv : conv) tm = 
    if is_abs tm then ABS_CONV conv tm
    else COMB_QCONV conv tm

let rec private ONCE_DEPTH_QCONV (conv : conv) tm = 
    (ORELSEC conv (SUB_QCONV(ONCE_DEPTH_QCONV conv))) tm

and private DEPTH_QCONV (conv : conv) tm = 
    THENQC (SUB_QCONV(DEPTH_QCONV conv)) (REPEATQC conv) tm

and private REDEPTH_QCONV (conv : conv) tm = 
    THENQC (SUB_QCONV(REDEPTH_QCONV conv)) (THENCQC conv (REDEPTH_QCONV conv)) tm

and private TOP_DEPTH_QCONV (conv : conv) tm = 
    THENQC (REPEATQC conv) (THENCQC (SUB_QCONV(TOP_DEPTH_QCONV conv)) (THENCQC conv (TOP_DEPTH_QCONV conv))) tm

and private TOP_SWEEP_QCONV (conv : conv) tm = 
    THENQC (REPEATQC conv) (SUB_QCONV(TOP_SWEEP_QCONV conv)) tm

/// Applies a conversion once to the first suitable sub-term(s) encountered in top-down order.
let ONCE_DEPTH_CONV (c : conv) : conv = TRY_CONV (ONCE_DEPTH_QCONV c)

/// Applies a conversion repeatedly to all the sub-terms of a term, in bottom-up order.
let DEPTH_CONV (c : conv) : conv = TRY_CONV (DEPTH_QCONV c)

/// Applies a conversion bottom-up to all subterms, retraversing changed ones.
let REDEPTH_CONV (c : conv) : conv = TRY_CONV (REDEPTH_QCONV c)

/// Applies a conversion top-down to all subterms, retraversing changed ones.
let TOP_DEPTH_CONV (c : conv) : conv = TRY_CONV (TOP_DEPTH_QCONV c)

/// Repeatedly applies a conversion top-down at all levels,
/// but after descending to subterms, does not return to higher ones.
let TOP_SWEEP_CONV (c : conv) : conv = TRY_CONV (TOP_SWEEP_QCONV c)

(* ------------------------------------------------------------------------- *)
(* Apply at leaves of op-tree; NB any failures at leaves cause failure.      *)
(* ------------------------------------------------------------------------- *)

/// Applied a conversion to the leaves of a tree of binary operator expressions.
let rec DEPTH_BINOP_CONV op (conv : conv) tm : Protected<thm0> = 
    match tm with
    | Comb(Comb(op', _), _) when op' = op ->
        choice {
        let! l, r = dest_binop op tm
        let! lth = DEPTH_BINOP_CONV op conv l
        let! rth = DEPTH_BINOP_CONV op conv r
        return!
            let lth = Choice.result lth in
            let rth = Choice.result rth in
            MK_COMB(AP_TERM op' lth, rth)
        }
    | _ -> conv tm

(* ------------------------------------------------------------------------- *)
(* Follow a path.                                                            *)
(* ------------------------------------------------------------------------- *)

/// Follow a path.
let PATH_CONV = 
    let rec path_conv s cnv = 
        match s with
        | [] -> cnv
        | "l" :: t ->
            RATOR_CONV(path_conv t cnv)
        | "r" :: t ->
            RAND_CONV(path_conv t cnv)
        | _ :: t ->
            ABS_CONV(path_conv t cnv)
    
    fun s cnv ->
        path_conv (explode s) cnv

(* ------------------------------------------------------------------------- *)
(* Follow a pattern                                                          *)
(* ------------------------------------------------------------------------- *)

/// Follow a pattern.
let PAT_CONV = 
    let rec PCONV xs pat conv = 
        if mem pat xs then conv
        elif not(exists (fun x -> free_in x pat |> Choice.get) xs) then ALL_CONV
        elif is_comb pat then
            let rat = rator pat
            let ran = rand pat
            fun tm -> 
                choice {
                    let! rat = rat
                    let! ran = ran
                    return! COMB2_CONV (PCONV xs rat conv) (PCONV xs ran conv) tm
                }
        else
            let tm' = body pat 
            fun tm -> 
                choice {
                    let! tm' = tm'
                    return! ABS_CONV(PCONV xs tm' conv) tm
                }
    fun pat -> 
        let xs, pbod = strip_abs pat
        PCONV xs pbod

(* ------------------------------------------------------------------------- *)
(* Symmetry conversion.                                                      *)
(* ------------------------------------------------------------------------- *)

/// Symmetry conversion.
let SYM_CONV tm : Protected<thm0> = 
    choice {
        let! th1' = ASSUME tm
        let! th1 = SYM(Choice.result th1')
        let tm' = concl th1
        let! th2' = ASSUME tm'
        let! th2 = SYM(Choice.result th2')
        return! DEDUCT_ANTISYM_RULE (Choice.result th2) (Choice.result th1)
    }
    |> Choice.mapError (fun e -> nestedFailure e "SYM_CONV")

(* ------------------------------------------------------------------------- *)
(* Conversion to a rule.                                                     *)
(* ------------------------------------------------------------------------- *)

/// Conversion to a rule.
let CONV_RULE (conv : conv) (th : Protected<thm0>) : Protected<thm0> =
    choice {
        let! th = th
        let tm = concl th
        let! th1 = conv tm
        return! EQ_MP (Choice.result th1) (Choice.result th)
    }

(* ------------------------------------------------------------------------- *)
(* Substitution conversion.                                                  *)
(* ------------------------------------------------------------------------- *)

/// Substitution conversion.
let SUBS_CONV (ths : Protected<thm0> list) tm : Protected<thm0> = 
    choice {
        if List.isEmpty ths then
            return! REFL tm
        else
            let! ths' = Choice.List.map id ths
            let! lefts = Choice.List.map (lhand << concl) ths'
            let! gvs = Choice.List.map (Choice.map genvar << type_of) lefts
            let! pat = subst (zip gvs lefts) tm
            let! abs = list_mk_abs(gvs, pat)
            let! th0 = REFL abs
            let! th =
                Choice.List.fold (fun x y -> 
                    CONV_RULE (THENC (RAND_CONV BETA_CONV) (LAND_CONV BETA_CONV)) (MK_COMB(Choice.result x, Choice.result y))) 
                        th0 ths'
            let! tm' = rand <| concl th
            if tm' = tm then
                return! REFL tm
            else
                return th
    }
    |> Choice.mapError (fun e -> nestedFailure e "SUBS_CONV")

(* ------------------------------------------------------------------------- *)
(* Get a few rules.                                                          *)
(* ------------------------------------------------------------------------- *)

/// Beta-reduces all the beta-redexes in the conclusion of a theorem.
let BETA_RULE = CONV_RULE(REDEPTH_CONV BETA_CONV)

/// Reverses the first equation(s) encountered in a top-down search.
let GSYM = CONV_RULE(ONCE_DEPTH_CONV SYM_CONV)

/// Makes simple term substitutions in a theorem using a given list of theorems.
let SUBS ths = CONV_RULE(SUBS_CONV ths)

(* ------------------------------------------------------------------------- *)
(* A cacher for conversions.                                                 *)
(* ------------------------------------------------------------------------- *)

let private ALPHA_HACK (th : Protected<thm0>) : conv = 
    fun tm ->
        choice {
        let! th = th
        let! tm0 = lhand <| concl th
        if tm0 = tm then
            return th
        else
            let! th1 = ALPHA tm tm0
            return! TRANS (Choice.result th1) (Choice.result th)
        }

/// A cacher for conversions.
let CACHE_CONV (conv : conv) : conv =
    // NOTE : This is not thread-safe!
    let net = ref empty_net
    fun tm ->
        choice {
            let! fs = lookup tm !net
            return!
                tryfind (fun f -> Choice.toOption <| f tm) fs
                |> Option.toChoiceWithError "tryfind"
                |> Choice.bindError (function
                    | Failure _ ->
                        let th = conv tm
                        match enter [] (tm, ALPHA_HACK th) !net with
                        | Success n ->
                            net := n
                        | Error ex ->
                            // NOTE: currently do nothing in case of error
                            System.Diagnostics.Debug.WriteLine "An unhandled error occurred in CACHE_CONV."
                            System.Diagnostics.Debug.WriteLine ("Message: " + ex.Message)
                            ()
                        th
                    | e -> Choice.error e)
        }
