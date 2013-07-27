﻿(*

Copyright 1998 University of Cambridge
Copyright 1998-2007 John Harrison
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
/// Boolean theory including (intuitionistic) defs of logical connectives.
module NHol.bool

open System
open FSharp.Compatibility.OCaml

open ExtCore.Control
open ExtCore.Control.Collections

open NHol
open lib
open fusion
open fusion.Hol_kernel
open basics
open nets
open printer
open preterm
open parser
open equal
#endif

(* ------------------------------------------------------------------------- *)
(* Set up parse status of basic and derived logical constants.               *)
(* ------------------------------------------------------------------------- *)

parse_as_prefix "~"
parse_as_binder "\\"
parse_as_binder "!"
parse_as_binder "?"
parse_as_binder "?!"
parse_as_infix("==>", (4, "right"))
parse_as_infix("\\/", (6, "right"))
parse_as_infix("/\\", (8, "right"))

(* ------------------------------------------------------------------------- *)
(* Set up more orthodox notation for equations and equivalence.              *)
(* ------------------------------------------------------------------------- *)

parse_as_infix("<=>", (2, "right"))
override_interface("<=>", parse_term @"(=):bool->bool->bool")
parse_as_infix("=", (12, "right"))

(* ------------------------------------------------------------------------- *)
(* Special syntax for Boolean equations (IFF).                               *)
(* ------------------------------------------------------------------------- *)

/// Tests if a term is an equation between Boolean terms (iff / logical equivalence).
let is_iff tm = 
    match tm with
    | Comb(Comb(Const("=", Tyapp("fun", [Tyapp("bool", []); _])), l), r) -> true
    | _ -> false

/// Term destructor for logical equivalence.
let dest_iff tm = 
    match tm with
    | Comb(Comb(Const("=", Tyapp("fun", [Tyapp("bool", []); _])), l), r) -> 
        Choice.result (l, r)
    | _ -> 
        Choice.failwith "dest_iff"

/// Constructs a logical equivalence (Boolean equation).
let mk_iff = 
    let eq_tm = parse_term @"(<=>)"
    fun (l, r) -> 
        mk_comb(eq_tm, l)
        |> Choice.bind (fun l' -> mk_comb(l', r))

(* ------------------------------------------------------------------------- *)
(* Rule allowing easy instantiation of polymorphic proformas.                *)
(* ------------------------------------------------------------------------- *)

/// Instantiate types and terms in a theorem.
let PINST tyin tmin = 
    let iterm_fn = INST(map (I ||>> (Choice.get << inst tyin)) tmin)
    let itype_fn = INST_TYPE tyin
    fun th -> 
        try 
            iterm_fn(itype_fn th)
        with
        | Failure _ as e ->
            Choice.nestedFailwith e "PINST"

(* ------------------------------------------------------------------------- *)
(* Useful derived deductive rule.                                            *)
(* ------------------------------------------------------------------------- *)

/// Eliminates a provable assumption from a theorem.
let PROVE_HYP ath bth = 
    choice {
        let! t = Choice.map concl ath
        let! ts = Choice.map hyp bth
        if exists (aconv t) ts then 
            return! EQ_MP (DEDUCT_ANTISYM_RULE ath bth) ath
        else 
            return! bth
    }

(* ------------------------------------------------------------------------- *)
(* Rules for T                                                               *)
(* ------------------------------------------------------------------------- *)

let T_DEF = new_basic_definition <| parse_term @"T = ((\p:bool. p) = (\p:bool. p))"

let TRUTH = EQ_MP (SYM T_DEF) (REFL(parse_term @"\p:bool. p"))

/// Eliminates equality with T.
let EQT_ELIM th = 
    EQ_MP (SYM th) TRUTH
    |> Choice.bindError (fun _ -> Choice.failwith "EQT_ELIM")

/// Introduces equality with T.
let EQT_INTRO = 
    let t = parse_term @"t:bool"
    let pth = 
        let th1 = DEDUCT_ANTISYM_RULE (ASSUME t) TRUTH
        let th2 = th1 |> Choice.map concl |> Choice.bind (fun tm -> EQT_ELIM(ASSUME tm))
        DEDUCT_ANTISYM_RULE th2 th1
    fun th -> 
        th
        |> Choice.bind (fun th' ->
            EQ_MP (INST [concl th', t] pth) th)

(* ------------------------------------------------------------------------- *)
(* Rules for /\                                                              *)
(* ------------------------------------------------------------------------- *)

let AND_DEF = new_basic_definition <| parse_term @"(/\) = \p q. (\f:bool->bool->bool. f p q) = (\f. f T T)"

/// Constructs a conjunction.
let mk_conj = mk_binary "/\\"

/// Constructs the conjunction of a list of terms.
let list_mk_conj = end_itlist(curry (Choice.get << mk_conj))

/// Introduces a conjunction.
let CONJ = 
    let f = parse_term @"f:bool->bool->bool"
    let p = parse_term @"p:bool"
    let q = parse_term @"q:bool"
    let pth() = 
        let pth = ASSUME p
        let qth = ASSUME q
        let th1 = MK_COMB(AP_TERM f (EQT_INTRO pth), EQT_INTRO qth)
        let th2 = ABS f th1
        let th3 = BETA_RULE(AP_THM (AP_THM AND_DEF p) q)
        EQ_MP (SYM th3) th2
    fun th1 th2 -> 
        let th =
            (th1, th2)
            ||> Choice.bind2 (fun th1 th2 ->
                    INST [concl th1, p; concl th2, q] <| pth())
        PROVE_HYP th2 (PROVE_HYP th1 th)

/// Extracts left conjunct of theorem.
let CONJUNCT1 = 
    let P = parse_term @"P:bool"
    let Q = parse_term @"Q:bool"
    let pth = 
        let th1 = CONV_RULE (RAND_CONV BETA_CONV) (AP_THM AND_DEF <| parse_term @"P:bool")
        let th2 = CONV_RULE (RAND_CONV BETA_CONV) (AP_THM th1 <| parse_term @"Q:bool")
        let th3 = EQ_MP th2 (ASSUME <| parse_term @"P /\ Q")
        EQT_ELIM(BETA_RULE(AP_THM th3 <| parse_term @"\(p:bool) (q:bool). p"))
    fun th ->
        choice {
            let! tm = Choice.map concl th
            let! l, r = dest_conj tm
            return! PROVE_HYP th (INST [l, P; r, Q] pth)
        }
        |> Choice.bindError (fun _ -> Choice.failwith "CONJUNCT1")

/// Extracts right conjunct of theorem.
let CONJUNCT2 = 
    let P = parse_term @"P:bool"
    let Q = parse_term @"Q:bool"
    let pth = 
        let th1 = CONV_RULE (RAND_CONV BETA_CONV) (AP_THM AND_DEF <| parse_term @"P:bool")
        let th2 = CONV_RULE (RAND_CONV BETA_CONV) (AP_THM th1 <| parse_term @"Q:bool")
        let th3 = EQ_MP th2 (ASSUME <| parse_term @"P /\ Q")
        EQT_ELIM(BETA_RULE(AP_THM th3 <| parse_term @"\(p:bool) (q:bool). q"))
    fun th -> 
        choice {
            let! tm = Choice.map concl th
            let! l, r = dest_conj tm
            return! PROVE_HYP th (INST [l, P; r, Q] pth)
        }
        |> Choice.bindError (fun _ -> Choice.failwith "CONJUNCT2")

/// Extracts both conjuncts of a conjunction.
let CONJ_PAIR th = 
    // TODO: this doesn't seem correct
    CONJUNCT1 th, CONJUNCT2 th

/// Recursively splits conjunctions into a list of conjuncts.
let CONJUNCTS = striplist (Some << CONJ_PAIR)

(* ------------------------------------------------------------------------- *)
(* Rules for ==>                                                             *)
(* ------------------------------------------------------------------------- *)

let IMP_DEF = new_basic_definition <| parse_term @"(==>) = \p q. p /\ q <=> p"

/// Constructs an implication.
let mk_imp = mk_binary "==>"

/// Implements the Modus Ponens inference rule.
let MP = 
    let p = parse_term @"p:bool"
    let q = parse_term @"q:bool"
    let pth() = 
        let th1 = BETA_RULE(AP_THM (AP_THM IMP_DEF p) q)
        let th2 = EQ_MP th1 (ASSUME <| parse_term @"p ==> q")
        CONJUNCT2(EQ_MP (SYM th2) (ASSUME <| parse_term @"p:bool"))
    fun ith th -> 
        choice {
            let! tm = Choice.map concl ith
            let! ant, con = dest_imp tm
            let! tm' = Choice.map concl th
            if aconv ant tm' then 
                return! PROVE_HYP th (PROVE_HYP ith (INST [ant, p; con, q] <| pth()))
            else 
                return! Choice.failwith "MP: theorems do not agree"
        }

/// Discharges an assumption.
let DISCH = 
    let p = parse_term @"p:bool"
    let q = parse_term @"q:bool"
    let pth() = SYM(BETA_RULE(AP_THM (AP_THM IMP_DEF p) q))
    fun a th -> 
        choice {
            let th1 = CONJ (ASSUME a) th
            let! tm1 = Choice.map concl th1
            let th2 = CONJUNCT1(ASSUME tm1)
            let th3 = DEDUCT_ANTISYM_RULE th1 th2
            let! tm = Choice.map concl th
            let th4 = INST [a, p; tm, q] <| pth()
            return! EQ_MP th4 th3
        }

/// Discharges all hypotheses of a theorem.
let rec DISCH_ALL th = 
    choice {
        let! th' = th
        match hyp th' with
        | t :: _ ->
            return! DISCH_ALL(DISCH t th) |> Choice.bindError (fun _ -> th)
        | _ -> 
            return! th
    }

/// Undischarges the antecedent of an implicative theorem.
let UNDISCH th = 
    MP th (ASSUME(Choice.get <| rand(Choice.get <| rator(concl <| Choice.get th))))
    |> Choice.mapError (fun _ -> Exception "UNDISCH")

/// Iteratively undischarges antecedents in a chain of implications.
let rec UNDISCH_ALL th = 
    Choice.map concl th
    |> Choice.bind (fun tm ->
        if is_imp tm then UNDISCH_ALL(UNDISCH th)
        else th)

/// Deduces equality of boolean terms from forward and backward implications.
let IMP_ANTISYM_RULE th1 th2 = DEDUCT_ANTISYM_RULE (UNDISCH th2) (UNDISCH th1)

/// Adds an assumption to a theorem.
let ADD_ASSUM tm th = MP (DISCH tm th) (ASSUME tm)

/// Derives forward and backward implication from equality of boolean terms.
let EQ_IMP_RULE = 
    let peq = parse_term @"p <=> q"
    let pq = dest_iff peq
    let pth1 p = DISCH peq (DISCH p (EQ_MP (ASSUME peq) (ASSUME p)))
    let pth2 q = DISCH peq (DISCH q (EQ_MP (SYM(ASSUME peq)) (ASSUME q)))
    fun th -> 
        // TODO: revise this
        match pq with
        | Success(p, q) ->
            match Choice.bind (dest_iff << concl) th with
            | Success (l, r) ->
                MP (INST [l, p; r, q] <| pth1 p) th, MP (INST [l, p; r, q] <| pth2 q) th
            | _ -> Choice.failwithPair "EQ_IMP_RULE"
        | _ -> Choice.failwithPair "EQ_IMP_RULE"

/// Implements the transitivity of implication.
let IMP_TRANS = 
    let pq = parse_term @"p ==> q"
    let qr = parse_term @"q ==> r"
    let p_imp_q = dest_imp pq
    let r = rand qr
    let pth p = itlist DISCH [pq; qr; p] (MP (ASSUME qr) (MP (ASSUME pq) (ASSUME p)))
    fun th1 th2 -> 
        choice {
            let! (p, q) = p_imp_q
            let! r = r
            let! tm1 = Choice.map concl th1
            let! tm2 = Choice.map concl th2
            let! x, y = dest_imp tm1
            let! y', z = dest_imp tm2
            if y <> y' then 
                return! Choice.failwith "IMP_TRANS"
            else 
                return! MP (MP (INST [x, p; y, q; z, r] <| pth p) th1) th2
        }

(* ------------------------------------------------------------------------- *)
(* Rules for !                                                               *)
(* ------------------------------------------------------------------------- *)

let FORALL_DEF = new_basic_definition <| parse_term @"(!) = \P:A->bool. P = \x. T"

/// Term constructor for universal quantification.
let mk_forall = mk_binder "!"

/// Iteratively constructs a universal quantification.
let list_mk_forall(vs, bod) = itlist (curry (Choice.get << mk_forall)) vs bod

/// Specializes the conclusion of a theorem.
let SPEC = 
    let P = parse_term @"P:A->bool"
    let x = parse_term @"x:A"
    let pth() = 
        let th1 = EQ_MP (AP_THM FORALL_DEF <| parse_term @"P:A->bool") (ASSUME <| parse_term @"(!)(P:A->bool)")
        let th2 = AP_THM(CONV_RULE BETA_CONV th1) <| parse_term @"x:A"
        let th3 = CONV_RULE (RAND_CONV BETA_CONV) th2
        DISCH_ALL(EQT_ELIM th3)
    fun tm th ->
        choice {
            let! tm' = Choice.map concl th
            let! abs = rand tm'
            let! ba = bndvar abs
            let! db = dest_var ba
            return! CONV_RULE BETA_CONV (MP (PINST [snd db, aty] [abs, P; tm, x] <| pth()) th)
        }
        |> Choice.bindError (fun _ -> Choice.failwith "SPEC")

/// Specializes zero or more variables in the conclusion of a theorem.
let SPECL tms th = 
    rev_itlist SPEC tms th
    |> Choice.bindError (fun _ -> Choice.failwith "SPEC")

/// Specializes the conclusion of a theorem, returning the chosen variant.
let SPEC_VAR th = 
    let bv = 
        let ts = Choice.map thm_frees th
        let t = Choice.bind (Choice.bind bndvar << rand << concl) th
        (ts, t) ||> Choice.bind2 variant
    bv, Choice.bind (fun bv -> SPEC bv th) bv

/// Specializes the conclusion of a theorem with its own quantified variables.
let rec SPEC_ALL th = 
    Choice.map concl th
    |> Choice.bind (fun tm ->
        if is_forall tm then SPEC_ALL(snd(SPEC_VAR th))
        else th)

/// Specializes a theorem, with type instantiation if necessary.
let ISPEC t th = 
    Choice.bind (dest_forall << concl) th
    |> Choice.bindError (fun e -> Choice.nestedFailwith e "ISPEC: input theorem not universally quantified")
    |> Choice.bind (fun (x, _) ->
        let tyins = 
            choice { 
                let! (_, ty) = dest_var x
                let! ty' = type_of t
                return! type_match ty ty' []
            }
        match tyins with
        | Success tyins ->
            SPEC t (INST_TYPE tyins th)
        | Error e ->
            Choice.nestedFailwith e "ISPEC can't type-Choice.get <| instantiate input theorem")
    |> Choice.bindError (fun _ -> Choice.failwith "ISPEC: type variable(s) free in assumptions")

/// Specializes a theorem zero or more times, with type instantiation if necessary.
let ISPECL tms th = 
        if tms = [] then th
        else 
            let avs = fst(chop_list (length tms) (fst(strip_forall(concl <| Choice.get th))))
            
            let tyins = 
                match Choice.List.map (Choice.map snd << dest_var) avs, Choice.List.map type_of tms with
                | Success avs, Success tms ->
                    Choice.List.fold (fun acc (x, y) -> type_match x y acc) [] (zip avs tms)
                | _ -> Choice.failwith "ISPECL.tyins"
            tyins
            |> Choice.bind (fun tyins -> SPECL tms (INST_TYPE tyins th))
            |> Choice.bindError (fun _ -> Choice.failwith "ISPECL")

/// Generalizes the conclusion of a theorem.
let GEN = 
    let pth() = SYM(CONV_RULE (RAND_CONV BETA_CONV) (AP_THM FORALL_DEF <| parse_term @"P:A->bool"))
    fun x th -> 
        choice {
            let! (_, ty) = dest_var x
            let qth = INST_TYPE [ty, aty] <| pth()
            let! ptm = Choice.bind (Choice.bind rand << rand << concl) qth        
            let th' = ABS x (EQT_INTRO th)
            let! phi = Choice.bind (lhand << concl) th'
            let rth = INST [phi, ptm] qth
            return! EQ_MP rth th'
        }

/// Generalizes zero or more variables in the conclusion of a theorem.
let GENL = itlist GEN

/// Generalizes the conclusion of a theorem over its own free variables.
let GEN_ALL th = 
    Choice.map dest_thm th
    |> Choice.bind (fun (asl, c) ->
        let vars = subtract (frees c) (freesl asl)
        GENL vars th)

(* ------------------------------------------------------------------------- *)
(* Rules for ?                                                               *)
(* ------------------------------------------------------------------------- *)

let EXISTS_DEF = new_basic_definition <| parse_term @"(?) = \P:A->bool. !q. (!x. P x ==> q) ==> q"

/// Term constructor for existential quantification.
let mk_exists = mk_binder "?"

/// Multiply existentially quantifies both sides of an equation using the given variables.
let list_mk_exists(vs, bod) = itlist (curry (Choice.get << mk_exists)) vs bod

/// Introduces existential quantification given a particular witness.
let EXISTS = 
    let P = parse_term @"P:A->bool"
    let x = parse_term @"x:A"
    let pth() = 
        let th1 = CONV_RULE (RAND_CONV BETA_CONV) (AP_THM EXISTS_DEF P)
        let th2 = SPEC (parse_term @"x:A") (ASSUME <| parse_term @"!x:A. P x ==> Q")
        let th3 = DISCH (parse_term @"!x:A. P x ==> Q") (MP th2 (ASSUME <| parse_term @"(P:A->bool) x"))
        EQ_MP (SYM th1) (GEN (parse_term @"Q:bool") th3)

    fun (etm, stm) th -> 
        choice {
            let! qf, abs = dest_comb etm
            let bth = Choice.bind BETA_CONV (mk_comb(abs, stm))
            let! sty = type_of stm
            let cth = PINST [sty, aty] [abs, P; stm, x] <| pth()
            return! PROVE_HYP (EQ_MP (SYM bth) th) cth
        }
        |> Choice.bindError (fun _ -> Choice.failwith "EXISTS")

/// Introduces an existential quantifier over a variable in a theorem.
let SIMPLE_EXISTS v th = 
    choice {
        let! tm = Choice.map concl th
        let! tm' = mk_exists(v, tm)
        return! EXISTS (tm', v) th
    }

/// Eliminates existential quantification using deduction from a particular witness.
let CHOOSE = 
    let P = parse_term @"P:A->bool"
    let Q = parse_term @"Q:bool"
    let pth() = 
        let th1 = CONV_RULE (RAND_CONV BETA_CONV) (AP_THM EXISTS_DEF P)
        let th2 = SPEC (parse_term @"Q:bool") (UNDISCH(fst(EQ_IMP_RULE th1)))
        DISCH_ALL(DISCH (parse_term @"(?) (P:A->bool)") (UNDISCH th2))
    fun (v, th1) th2 -> 
        choice {
            let! abs = Choice.bind (rand << concl) th1
            let! bv, bod = dest_abs abs
            let! cmb = mk_comb(abs, v)
            let! pat = vsubst [v, bv] bod
            let th3 = CONV_RULE BETA_CONV (ASSUME cmb)
            let th4 = GEN v (DISCH cmb (MP (DISCH pat th2) th3))
            let! (_, ty) = dest_var v
            let! tm2 = Choice.map concl th2
            let th5 = 
                PINST [ty, aty] [abs, P; tm2, Q] <| pth()
            return! MP (MP th5 th4) th1
        }
        |> Choice.bindError (fun _ -> Choice.failwith "CHOOSE")

/// Existentially quantifies a hypothesis of a theorem.
let SIMPLE_CHOOSE v th = 
    choice {
        let! ts = Choice.map hyp th
        let! t = mk_exists(v, hd ts)
        return! CHOOSE (v, ASSUME t) th
    }

(* ------------------------------------------------------------------------- *)
(* Rules for \/                                                              *)
(* ------------------------------------------------------------------------- *)

let OR_DEF = new_basic_definition <| parse_term @"(\/) = \p q. !r. (p ==> r) ==> (q ==> r) ==> r"

/// Constructs a disjunction.
let mk_disj = mk_binary "\\/"

/// Constructs the disjunction of a list of terms.
let list_mk_disj = end_itlist(curry (Choice.get << mk_disj))

/// Introduces a right disjunct into the conclusion of a theorem.
let DISJ1 = 
    let P = parse_term @"P:bool"
    let Q = parse_term @"Q:bool"
    let pth() = 
        let th1 = CONV_RULE (RAND_CONV BETA_CONV) (AP_THM OR_DEF <| parse_term @"P:bool")
        let th2 = CONV_RULE (RAND_CONV BETA_CONV) (AP_THM th1 <| parse_term @"Q:bool")
        let th3 = MP (ASSUME <| parse_term @"P ==> t") (ASSUME <| parse_term @"P:bool")
        let th4 = GEN (parse_term @"t:bool") (DISCH (parse_term @"P ==> t") (DISCH (parse_term @"Q ==> t") th3))
        EQ_MP (SYM th2) th4
    fun th tm -> 
        Choice.map concl th
        |> Choice.bind (fun tm' -> PROVE_HYP th (INST [tm', P; tm, Q] <| pth()))
        |> Choice.bindError (fun _ -> Choice.failwith "DISJ1")

/// Introduces a left disjunct into the conclusion of a theorem.
let DISJ2 = 
    let P = parse_term @"P:bool"
    let Q = parse_term @"Q:bool"
    let pth() = 
        let th1 = CONV_RULE (RAND_CONV BETA_CONV) (AP_THM OR_DEF <| parse_term @"P:bool")
        let th2 = CONV_RULE (RAND_CONV BETA_CONV) (AP_THM th1 <| parse_term @"Q:bool")
        let th3 = MP (ASSUME <| parse_term @"Q ==> t") (ASSUME <| parse_term @"Q:bool")
        let th4 = GEN (parse_term @"t:bool") (DISCH (parse_term @"P ==> t") (DISCH (parse_term @"Q ==> t") th3))
        EQ_MP (SYM th2) th4
    fun tm th -> 
        Choice.map concl th
        |> Choice.bind (fun tm' -> PROVE_HYP th (INST [tm, P; tm', Q] <| pth()))
        |> Choice.bindError (fun _ -> Choice.failwith "DISJ2")

/// Eliminates disjunction by cases.
let DISJ_CASES = 
    let P = parse_term @"P:bool"
    let Q = parse_term @"Q:bool"
    let R = parse_term @"R:bool"
    let pth() = 
        let th1 = CONV_RULE (RAND_CONV BETA_CONV) (AP_THM OR_DEF <| parse_term @"P:bool")
        let th2 = CONV_RULE (RAND_CONV BETA_CONV) (AP_THM th1 <| parse_term @"Q:bool")
        let th3 = SPEC (parse_term @"R:bool") (EQ_MP th2 (ASSUME <| parse_term @"P \/ Q"))
        UNDISCH(UNDISCH th3)
    fun th0 th1 th2 -> 
        choice {
            let! c1 = Choice.map concl th1
            let! c2 = Choice.map concl th2
            if not(aconv c1 c2) then 
                return! Choice.failwith "DISJ_CASES"
            else 
                let! l, r = Choice.bind (dest_disj << concl) th0
                let th = INST [l, P; r, Q; c1, R] <| pth()
                return! PROVE_HYP (DISCH r th2) (PROVE_HYP (DISCH l th1) (PROVE_HYP th0 th))                        
        }
        |> Choice.bindError (fun _ -> Choice.failwith "DISJ_CASES")

/// Disjoins hypotheses of two theorems with same conclusion.
let SIMPLE_DISJ_CASES th1 th2 = 
    choice {
        let! tl1 = Choice.map hyp th1
        let! tl2 = Choice.map hyp th2
        let! tm = mk_disj(hd tl1, hd tl2)
        return! DISJ_CASES (ASSUME tm) th1 th2
    }

(* ------------------------------------------------------------------------- *)
(* Rules for negation and falsity.                                           *)
(* ------------------------------------------------------------------------- *)

let F_DEF = new_basic_definition <| parse_term @"F = !p:bool. p"
let NOT_DEF = new_basic_definition <| parse_term @"(~) = \p. p ==> F"

/// Constructs a logical negation.
let mk_neg = 
    let neg_tm = parse_term @"(~)"
    fun tm -> 
        mk_comb(neg_tm, tm)
        |> Choice.bindError (fun e -> Choice.nestedFailwith e "mk_neg")

/// <summary>
/// Transforms <c>|- ~t</c> into <c>|- t ==> F</c>.
/// </summary>
let NOT_ELIM = 
    let P = parse_term @"P:bool"
    let pth() = CONV_RULE (RAND_CONV BETA_CONV) (AP_THM NOT_DEF P)
    fun th -> 
        Choice.bind (rand << concl) th
        |> Choice.bind (fun tm -> EQ_MP (INST [tm, P] <| pth()) th)
        |> Choice.bindError (fun _ -> Choice.failwith "NOT_ELIM")

/// <summary>
/// Transforms <c>|- t ==> F</c> into <c>|- ~t</c>.
/// </summary>
let NOT_INTRO = 
    let P = parse_term @"P:bool"
    let pth() = SYM(CONV_RULE (RAND_CONV BETA_CONV) (AP_THM NOT_DEF P))
    fun th -> 
        Choice.bind (Choice.bind rand << rator << concl) th
        |> Choice.bind (fun tm -> EQ_MP (INST [tm, P] <| pth()) th)
        |> Choice.bindError (fun _ -> Choice.failwith "NOT_INTRO")

/// Converts negation to equality with F.
let EQF_INTRO = 
    let P = parse_term @"P:bool"
    let pth() = 
        let th1 = NOT_ELIM(ASSUME <| parse_term @"~ P")
        let th2 = DISCH (parse_term @"F") (SPEC P (EQ_MP F_DEF (ASSUME <| parse_term @"F")))
        DISCH_ALL(IMP_ANTISYM_RULE th1 th2)
    fun th -> 
        Choice.bind (rand << concl) th
        |> Choice.bind (fun tm -> MP (INST [tm, P] <| pth()) th)
        |> Choice.bindError (fun _ -> Choice.failwith "EQF_INTRO")

/// Replaces equality with F by negation.
let EQF_ELIM = 
    let P = parse_term @"P:bool"
    let pth() = 
        let th1 = EQ_MP (ASSUME <| parse_term @"P = F") (ASSUME <| parse_term @"P:bool")
        let th2 = DISCH P (SPEC (parse_term @"F") (EQ_MP F_DEF th1))
        DISCH_ALL(NOT_INTRO th2)
    fun th -> 
        Choice.bind (Choice.bind rand << rator << concl) th
        |> Choice.bind (fun tm -> MP (INST [tm, P] <| pth()) th)
        |> Choice.bindError (fun _ -> Choice.failwith "EQF_ELIM")

/// Implements the intuitionistic contradiction rule.
let CONTR = 
    let P = parse_term @"P:bool"
    let f_tm = parse_term @"F"
    let pth() = SPEC P (EQ_MP F_DEF (ASSUME <| parse_term @"F"))
    fun tm th -> 
        Choice.map concl th
        |> Choice.bind (fun tm' ->
            if tm' <> f_tm then Choice.failwith "CONTR"
            else PROVE_HYP th (INST [tm, P] <| pth()))

(* ------------------------------------------------------------------------- *)
(* Rules for unique existence.                                               *)
(* ------------------------------------------------------------------------- *)

let EXISTS_UNIQUE_DEF = new_basic_definition <| parse_term @"(?!) = \P:A->bool. ((?) P) /\ (!x y. P x /\ P y ==> x = y)"

/// Term constructor for unique existence.
let mk_uexists = mk_binder "?!"

/// Deduces existence from unique existence.
let EXISTENCE = 
    let P = parse_term @"P:A->bool"
    let pth() = 
        let th1 = CONV_RULE (RAND_CONV BETA_CONV) (AP_THM EXISTS_UNIQUE_DEF P)
        let th2 = UNDISCH(fst(EQ_IMP_RULE th1))
        DISCH_ALL(CONJUNCT1 th2)
    fun th -> 
        choice {
            let! abs = Choice.bind (rand << concl) th
            let! tm = bndvar abs
            let! (_, ty) = dest_var tm
            return! MP (PINST [ty, aty] [abs, P] <| pth()) th
        }
        |> Choice.bindError (fun _ -> Choice.failwith "EXISTENCE")
