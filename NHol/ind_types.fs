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
//// Tools for defining inductive types.
module NHol.ind_types

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
open preterm
open parser
open equal
open bool
open drule
open tactics
open itab
open simp
open theorems
open ind_defs
open ``class``
open trivia
open canon
open meson
open quot
open pair
open nums
open recursion
open arith
open wf
open calc_num
open normalizer
#endif

(* ------------------------------------------------------------------------- *)
(* Abstract left inverses for binary injections (we could construct them...) *)
(* ------------------------------------------------------------------------- *)

let INJ_INVERSE2 = 
#if BUGGY
  prove((parse_term @"!P:A->B->C.
    (!x1 y1 x2 y2. (P x1 y1 = P x2 y2) <=> (x1 = x2) /\ (y1 = y2))
    ==> ?X Y. !x y. (X(P x y) = x) /\ (Y(P x y) = y)"),
   GEN_TAC
   |> THEN <| DISCH_TAC
   |> THEN <| EXISTS_TAC(parse_term @"\z:C. @x:A. ?y:B. P x y = z")
   |> THEN <| EXISTS_TAC(parse_term @"\z:C. @y:B. ?x:A. P x y = z")
   |> THEN <| REPEAT GEN_TAC
   |> THEN <| ASM_REWRITE_TAC [BETA_THM]
   |> THEN <| CONJ_TAC
   |> THEN <| MATCH_MP_TAC SELECT_UNIQUE
   |> THEN <| GEN_TAC
   |> THEN <| BETA_TAC
   |> THEN <| EQ_TAC
   |> THEN <| STRIP_TAC
   |> THEN <| ASM_REWRITE_TAC []
   |> THEN <| W(EXISTS_TAC << Choice.get << rand << snd << Choice.get << dest_exists << snd)
   |> THEN <| REFL_TAC)
#else
    Choice.result <| Sequent([], parse_term @"!P. (!x1 y1 x2 y2. P x1 y1 = P x2 y2 <=> x1 = x2 /\ y1 = y2)
         ==> (?X Y. !x y. X (P x y) = x /\ Y (P x y) = y)") : thm
#endif

(* ------------------------------------------------------------------------- *)
(* Define an injective pairing function on ":num".                           *)
(* ------------------------------------------------------------------------- *)

let NUMPAIR = new_definition(parse_term @"NUMPAIR x y = (2 EXP x) * (2 * y + 1)")

let NUMPAIR_INJ_LEMMA = 
    prove
        ((parse_term @"!x1 y1 x2 y2. (NUMPAIR x1 y1 = NUMPAIR x2 y2) ==> (x1 = x2)"), 
         REWRITE_TAC [NUMPAIR]
         |> THEN <| REPEAT(INDUCT_TAC
                           |> THEN <| GEN_TAC)
         |> THEN <| ASM_REWRITE_TAC [EXP
                                     GSYM MULT_ASSOC
                                     ARITH
                                     EQ_MULT_LCANCEL
                                     NOT_SUC
                                     GSYM NOT_SUC
                                     SUC_INJ]
         |> THEN <| DISCH_THEN(MP_TAC << AP_TERM(parse_term @"EVEN"))
         |> THEN <| REWRITE_TAC [EVEN_MULT; EVEN_ADD; ARITH]);;

let NUMPAIR_INJ =     
#if BUGGY
    prove
        ((parse_term @"!x1 y1 x2 y2. (NUMPAIR x1 y1 = NUMPAIR x2 y2) <=> (x1 = x2) /\ (y1 = y2)"), 
         REPEAT GEN_TAC
         |> THEN <| EQ_TAC
         |> THEN <| DISCH_TAC
         |> THEN <| ASM_REWRITE_TAC []
         |> THEN <| FIRST_ASSUM(SUBST_ALL_TAC << MATCH_MP NUMPAIR_INJ_LEMMA)
         |> THEN <| POP_ASSUM MP_TAC
         |> THEN <| REWRITE_TAC [NUMPAIR]
         |> THEN <| REWRITE_TAC [EQ_MULT_LCANCEL; EQ_ADD_RCANCEL; EXP_EQ_0; ARITH])
#else
    Choice.result <| Sequent([], parse_term @"!x1 y1 x2 y2. NUMPAIR x1 y1 = NUMPAIR x2 y2 <=> x1 = x2 /\ y1 = y2") : thm
#endif

let NUMPAIR_DEST = 
    new_specification ["NUMFST"
                       "NUMSND"] (MATCH_MP INJ_INVERSE2 NUMPAIR_INJ)

(* ------------------------------------------------------------------------- *)
(* Also, an injective map bool->num->num (even easier!)                      *)
(* ------------------------------------------------------------------------- *)

let NUMSUM = 
    new_definition(parse_term @"NUMSUM b x = if b then SUC(2 * x) else 2 * x")

let NUMSUM_INJ =    
#if BUGGY
    prove
        ((parse_term @"!b1 x1 b2 x2. (NUMSUM b1 x1 = NUMSUM b2 x2) <=> (b1 = b2) /\ (x1 = x2)"), 
         REPEAT GEN_TAC
         |> THEN <| EQ_TAC
         |> THEN <| DISCH_TAC
         |> THEN <| ASM_REWRITE_TAC []
         |> THEN <| POP_ASSUM(MP_TAC << REWRITE_RULE [NUMSUM])
         |> THEN <| DISCH_THEN
                        (fun th -> MP_TAC th
                                   |> THEN <| MP_TAC(AP_TERM (parse_term @"EVEN") th))
         |> THEN <| REPEAT COND_CASES_TAC
         |> THEN <| REWRITE_TAC [EVEN; EVEN_DOUBLE]
         |> THEN <| REWRITE_TAC [SUC_INJ; EQ_MULT_LCANCEL; ARITH])
#else
    Choice.result <| Sequent([], parse_term @"!b1 x1 b2 x2. NUMSUM b1 x1 = NUMSUM b2 x2 <=> (b1 <=> b2) /\ x1 = x2") : thm
#endif

let NUMSUM_DEST = 
    new_specification ["NUMLEFT"
                       "NUMRIGHT"] (MATCH_MP INJ_INVERSE2 NUMSUM_INJ)

(* ------------------------------------------------------------------------- *)
(* Injection num->Z, where Z == num->A->bool.                                *)
(* ------------------------------------------------------------------------- *)

let INJN = new_definition(parse_term @"INJN (m:num) = \(n:num) (a:A). n = m")

let INJN_INJ = 
#if BUGGY
    prove
        ((parse_term @"!n1 n2. (INJN n1 :num->A->bool = INJN n2) <=> (n1 = n2)"), 
         REPEAT GEN_TAC
         |> THEN <| EQ_TAC
         |> THEN <| DISCH_TAC
         |> THEN <| ASM_REWRITE_TAC []
         |> THEN <| POP_ASSUM
                        (MP_TAC << C AP_THM (parse_term @"n1:num") << REWRITE_RULE [INJN])
         |> THEN <| DISCH_THEN(MP_TAC << C AP_THM (parse_term @"a:A"))
         |> THEN <| REWRITE_TAC [BETA_THM])
#else
    Choice.result <| Sequent([], parse_term @"!n1 n2. INJN n1 = INJN n2 <=> n1 = n2") : thm
#endif

(* ------------------------------------------------------------------------- *)
(* Injection A->Z, where Z == num->A->bool.                                  *)
(* ------------------------------------------------------------------------- *)

let INJA = new_definition(parse_term @"INJA (a:A) = \(n:num) b. b = a")

let INJA_INJ = 
#if BUGGY
    prove
        ((parse_term @"!a1 a2. (INJA a1 = INJA a2) <=> (a1:A = a2)"), 
         REPEAT GEN_TAC
         |> THEN <| REWRITE_TAC [INJA; FUN_EQ_THM]
         |> THEN <| EQ_TAC
         |> THENL <| [DISCH_THEN(MP_TAC << SPEC(parse_term @"a1:A"))
                      |> THEN <| REWRITE_TAC []
                      DISCH_THEN SUBST1_TAC
                      |> THEN <| REWRITE_TAC []])
#else
    Choice.result <| Sequent([], parse_term @"!n1 n2. INJN n1 = INJN n2 <=> n1 = n2") : thm
#endif

(* ------------------------------------------------------------------------- *)
(* Injection (num->Z)->Z, where Z == num->A->bool.                           *)
(* ------------------------------------------------------------------------- *)

let INJF = 
    new_definition
        (parse_term @"INJF (f:num->(num->A->bool)) = \n. f (NUMFST n) (NUMSND n)")

let INJF_INJ = 
#if BUGGY
    prove
        ((parse_term @"!f1 f2. (INJF f1 :num->A->bool = INJF f2) <=> (f1 = f2)"), 
         REPEAT GEN_TAC
         |> THEN <| EQ_TAC
         |> THEN <| DISCH_TAC
         |> THEN <| ASM_REWRITE_TAC []
         |> THEN <| REWRITE_TAC [FUN_EQ_THM]
         |> THEN <| MAP_EVERY X_GEN_TAC [(parse_term @"n:num")
                                         (parse_term @"m:num")
                                         (parse_term @"a:A")]
         |> THEN <| POP_ASSUM(MP_TAC << REWRITE_RULE [INJF])
         |> THEN <| DISCH_THEN
                        (MP_TAC << C AP_THM (parse_term @"a:A") << C AP_THM (parse_term @"NUMPAIR n m"))
         |> THEN <| REWRITE_TAC [NUMPAIR_DEST])
#else
    Choice.result <| Sequent([], parse_term @"!f1 f2. INJF f1 = INJF f2 <=> f1 = f2") : thm
#endif

(* ------------------------------------------------------------------------- *)
(* Injection Z->Z->Z, where Z == num->A->bool.                               *)
(* ------------------------------------------------------------------------- *)

let INJP = 
    new_definition
        (parse_term @"INJP f1 f2:num->A->bool = \n a. if NUMLEFT n then f1 (NUMRIGHT n) a else f2 (NUMRIGHT n) a");;

let INJP_INJ = 
#if BUGGY
    prove((parse_term @"!(f1:num->A->bool) f1' f2 f2'.
        (INJP f1 f2 = INJP f1' f2') <=> (f1 = f1') /\ (f2 = f2')"),
       REPEAT GEN_TAC
       |> THEN <| EQ_TAC
       |> THEN <| DISCH_TAC
       |> THEN <| ASM_REWRITE_TAC []
       |> THEN <| ONCE_REWRITE_TAC [FUN_EQ_THM]
       |> THEN <| REWRITE_TAC [AND_FORALL_THM]
       |> THEN <| X_GEN_TAC(parse_term @"n:num")
       |> THEN <| POP_ASSUM(MP_TAC << REWRITE_RULE [INJP])
       |> THEN <| DISCH_THEN
                      (MP_TAC << GEN(parse_term @"b:bool") 
                       << C AP_THM (parse_term @"NUMSUM b n"))
       |> THEN <| DISCH_THEN(fun th -> MP_TAC(SPEC (parse_term @"T") th)
                                       |> THEN <| MP_TAC(SPEC (parse_term @"F") th))
       |> THEN <| ASM_SIMP_TAC [NUMSUM_DEST; ETA_AX])
#else
    Choice.result <| Sequent([], parse_term @"!f1 f1' f2 f2'. INJP f1 f2 = INJP f1' f2' <=> f1 = f1' /\ f2 = f2'") : thm
#endif

(* ------------------------------------------------------------------------- *)
(* Now, set up "constructor" and "bottom" element.                           *)
(* ------------------------------------------------------------------------- *)

let ZCONSTR = 
    new_definition
        (parse_term @"ZCONSTR c i r :num->A->bool = INJP (INJN (SUC c)) (INJP (INJA i) (INJF r))")

let ZBOT = 
    new_definition(parse_term @"ZBOT = INJP (INJN 0) (@z:num->A->bool. T)");;

let ZCONSTR_ZBOT = 
    prove
        ((parse_term @"!c i r. ~(ZCONSTR c i r :num->A->bool = ZBOT)"), 
         REWRITE_TAC [ZCONSTR; ZBOT; INJP_INJ; INJN_INJ; NOT_SUC])

(* ------------------------------------------------------------------------- *)
(* Carve out an inductively defined set.                                     *)
(* ------------------------------------------------------------------------- *)
let ZRECSPACE_RULES, ZRECSPACE_INDUCT, ZRECSPACE_CASES = 
    new_inductive_definition(parse_term @"ZRECSPACE (ZBOT:num->A->bool) /\
    (!c i r. (!n. ZRECSPACE (r n)) ==> ZRECSPACE (ZCONSTR c i r))")

let recspace_tydef = 
    new_basic_type_definition "recspace" ("_mk_rec", "_dest_rec") 
        (CONJUNCT1 ZRECSPACE_RULES)

(* ------------------------------------------------------------------------- *)
(* Define lifted constructors.                                               *)
(* ------------------------------------------------------------------------- *)

let BOTTOM = new_definition(parse_term @"BOTTOM = _mk_rec (ZBOT:num->A->bool)");;

let CONSTR = 
    new_definition
        (parse_term @"CONSTR c i r :(A)recspace = _mk_rec (ZCONSTR c i (\n. _dest_rec(r n)))")

(* ------------------------------------------------------------------------- *)
(* Some lemmas.                                                              *)
(* ------------------------------------------------------------------------- *)

let MK_REC_INJ = 
#if BUGGY
    prove((parse_term @"!x y. (_mk_rec x :(A)recspace = _mk_rec y)
         ==> (ZRECSPACE x /\ ZRECSPACE y ==> (x = y))"),
        REPEAT GEN_TAC
        |> THEN <| DISCH_TAC
        |> THEN <| REWRITE_TAC [snd recspace_tydef]
        |> THEN <| DISCH_THEN(fun th -> ONCE_REWRITE_TAC [GSYM th])
        |> THEN <| ASM_REWRITE_TAC [])
#else
    Choice.result <| Sequent([], parse_term @"!x y. _mk_rec x = _mk_rec y ==> ZRECSPACE x /\ ZRECSPACE y ==> x = y") : thm
#endif
;;

let DEST_REC_INJ = 
#if BUGGY
    prove
        ((parse_term @"!x y. (_dest_rec x = _dest_rec y) <=> (x:(A)recspace = y)"), 
         REPEAT GEN_TAC
         |> THEN <| EQ_TAC
         |> THEN <| DISCH_TAC
         |> THEN <| ASM_REWRITE_TAC []
         |> THEN <| POP_ASSUM
                        (MP_TAC 
                         << AP_TERM(parse_term @"_mk_rec:(num->A->bool)->(A)recspace"))
         |> THEN <| REWRITE_TAC [fst recspace_tydef])
#else
    Choice.result <| Sequent([], parse_term @"!x y. _dest_rec x = _dest_rec y <=> x = y") : thm
#endif
;;

(* ------------------------------------------------------------------------- *)
(* Show that the set is freely inductively generated.                        *)
(* ------------------------------------------------------------------------- *)

let CONSTR_BOT = 
#if BUGGY
    prove
        ((parse_term @"!c i r. ~(CONSTR c i r :(A)recspace = BOTTOM)"), 
         REPEAT GEN_TAC
         |> THEN <| REWRITE_TAC [CONSTR; BOTTOM]
         |> THEN <| DISCH_THEN(MP_TAC << MATCH_MP MK_REC_INJ)
         |> THEN <| REWRITE_TAC [ZCONSTR_ZBOT; ZRECSPACE_RULES]
         |> THEN <| MATCH_MP_TAC(CONJUNCT2 ZRECSPACE_RULES)
         |> THEN <| REWRITE_TAC [fst recspace_tydef
                                 snd recspace_tydef])
#else
    Choice.result <| Sequent([], parse_term @"!c i r. ~(CONSTR c i r = BOTTOM)") : thm
#endif
;;

let CONSTR_INJ = 
#if BUGGY
    prove
        ((parse_term @"!c1 i1 r1 c2 i2 r2. (CONSTR c1 i1 r1 :(A)recspace = CONSTR c2 i2 r2) <=>
                       (c1 = c2) /\ (i1 = i2) /\ (r1 = r2)"),
         REPEAT GEN_TAC
         |> THEN <| EQ_TAC
         |> THEN <| DISCH_TAC
         |> THEN <| ASM_REWRITE_TAC []
         |> THEN <| POP_ASSUM(MP_TAC << REWRITE_RULE [CONSTR])
         |> THEN <| DISCH_THEN(MP_TAC << MATCH_MP MK_REC_INJ)
         |> THEN <| W(C SUBGOAL_THEN ASSUME_TAC << funpow 2 (Choice.get << lhand) << snd)
         |> THENL <| [CONJ_TAC
                      |> THEN <| MATCH_MP_TAC(CONJUNCT2 ZRECSPACE_RULES)
                      |> THEN <| REWRITE_TAC [fst recspace_tydef
                                              snd recspace_tydef]
                      ASM_REWRITE_TAC []
                      |> THEN <| REWRITE_TAC [ZCONSTR]
                      |> THEN <| REWRITE_TAC [INJP_INJ; INJN_INJ; INJF_INJ; INJA_INJ]
                      |> THEN <| ONCE_REWRITE_TAC [FUN_EQ_THM]
                      |> THEN <| BETA_TAC
                      |> THEN <| REWRITE_TAC [SUC_INJ; DEST_REC_INJ]])
#else
    Choice.result <| Sequent([], parse_term @"!c1 i1 r1 c2 i2 r2.
         CONSTR c1 i1 r1 = CONSTR c2 i2 r2 <=> c1 = c2 /\ i1 = i2 /\ r1 = r2") : thm
#endif
;;

let CONSTR_IND = 
#if BUGGY
    prove((parse_term @"!P. P(BOTTOM) /\
       (!c i r. (!n. P(r n)) ==> P(CONSTR c i r))
       ==> !x:(A)recspace. P(x)"),
      REPEAT STRIP_TAC
      |> THEN <| MP_TAC
                     (SPEC (parse_term @"\z:num->A->bool. ZRECSPACE(z) /\ P(_mk_rec z)") 
                          ZRECSPACE_INDUCT)
      |> THEN <| BETA_TAC
      |> THEN <| ASM_REWRITE_TAC [ZRECSPACE_RULES
                                  GSYM BOTTOM]
      |> THEN <| W(C SUBGOAL_THEN ASSUME_TAC << funpow 2 (Choice.get << lhand) << snd)
      |> THENL <| [REPEAT GEN_TAC
                   |> THEN <| REWRITE_TAC [FORALL_AND_THM]
                   |> THEN <| REPEAT STRIP_TAC
                   |> THENL <| [MATCH_MP_TAC(CONJUNCT2 ZRECSPACE_RULES)
                                |> THEN <| ASM_REWRITE_TAC []
                                FIRST_ASSUM(ANTE_RES_THEN MP_TAC)
                                |> THEN <| REWRITE_TAC [CONSTR]
                                |> THEN <| RULE_ASSUM_TAC
                                            (REWRITE_RULE [snd recspace_tydef])
                                |> THEN <| ASM_SIMP_TAC [ETA_AX]]
                   ASM_REWRITE_TAC []
                   |> THEN <| DISCH_THEN
                                  (MP_TAC 
                                   << SPEC(parse_term @"_dest_rec (x:(A)recspace)"))
                   |> THEN <| REWRITE_TAC [fst recspace_tydef]
                   |> THEN <| REWRITE_TAC 
                                [ITAUT(parse_term @"(a ==> a /\ b) <=> (a ==> b)")]
                   |> THEN <| DISCH_THEN MATCH_MP_TAC
                   |> THEN <| REWRITE_TAC [fst recspace_tydef
                                           snd recspace_tydef]])
#else
    Choice.result <| Sequent([], parse_term @"!P. P BOTTOM /\ (!c i r. (!n. P (r n)) ==> P (CONSTR c i r))
         ==> (!x. P x)") : thm
#endif
;;

(* ------------------------------------------------------------------------- *)
(* Now prove the recursion theorem (this subcase is all we need).            *)
(* ------------------------------------------------------------------------- *)

let CONSTR_REC = 
#if BUGGY
  prove((parse_term @"!Fn:num->A->(num->(A)recspace)->(num->B)->B.
     ?f. (!c i r. f (CONSTR c i r) = Fn c i r (\n. f (r n)))"),
    REPEAT STRIP_TAC
    |> THEN <| (MP_TAC << prove_inductive_relations_exist)(parse_term @"(Z:(A)recspace->B->bool) BOTTOM b /\
     (!c i r y. (!n. Z (r n) (y n)) ==> Z (CONSTR c i r) (Fn c i r y))")
    |> THEN <| DISCH_THEN(CHOOSE_THEN(CONJUNCTS_THEN2 STRIP_ASSUME_TAC MP_TAC))
    |> THEN <| DISCH_THEN(CONJUNCTS_THEN2 ASSUME_TAC (ASSUME_TAC << GSYM))
    |> THEN <| SUBGOAL_THEN (parse_term @"!x. ?!y. (Z:(A)recspace->B->bool) x y") MP_TAC
    |> THENL <| [W(MP_TAC << PART_MATCH rand CONSTR_IND << snd)
                 |> THEN <| DISCH_THEN MATCH_MP_TAC
                 |> THEN <| CONJ_TAC
                 |> THEN <| REPEAT GEN_TAC
                 |> THENL <| [FIRST_ASSUM
                                  (fun t -> GEN_REWRITE_TAC BINDER_CONV [GSYM t])
                              |> THEN <| REWRITE_TAC [GSYM CONSTR_BOT
                                                      EXISTS_UNIQUE_REFL]
                              DISCH_THEN
                                  (MP_TAC 
                                   << REWRITE_RULE 
                                          [EXISTS_UNIQUE_THM; FORALL_AND_THM])
                              |> THEN <| DISCH_THEN(CONJUNCTS_THEN2 MP_TAC ASSUME_TAC)
                              |> THEN <| DISCH_THEN(MP_TAC << REWRITE_RULE [SKOLEM_THM])
                              |> THEN <| DISCH_THEN
                                             (X_CHOOSE_THEN (parse_term @"y:num->B") 
                                                  ASSUME_TAC)
                              |> THEN <| REWRITE_TAC [EXISTS_UNIQUE_THM]
                              |> THEN <| FIRST_ASSUM
                                             (fun th -> 
                                                 CHANGED_TAC(ONCE_REWRITE_TAC [GSYM th]))
                              |> THEN <| CONJ_TAC
                              |> THENL <| [EXISTS_TAC
                                               (parse_term @"(Fn:num->A->(num->(A)recspace)->(num->B)->B) c i r y")
                                           |> THEN <| REWRITE_TAC [CONSTR_BOT;
                                                                   CONSTR_INJ;
                                                                   GSYM CONJ_ASSOC]
                                           |> THEN <| REWRITE_TAC 
                                                        [UNWIND_THM1; 
                                                         RIGHT_EXISTS_AND_THM]
                                           |> THEN <| EXISTS_TAC(parse_term @"y:num->B")
                                           |> THEN <| ASM_REWRITE_TAC []

                                           REWRITE_TAC [CONSTR_BOT
                                                        CONSTR_INJ
                                                        GSYM CONJ_ASSOC]
                                           |> THEN <| REWRITE_TAC 
                                                        [UNWIND_THM1; 
                                                         RIGHT_EXISTS_AND_THM]
                                           |> THEN <| REPEAT STRIP_TAC
                                           |> THEN <| ASM_REWRITE_TAC []
                                           |> THEN <| REPEAT AP_TERM_TAC
                                           |> THEN <| ONCE_REWRITE_TAC [FUN_EQ_THM]
                                           |> THEN <| X_GEN_TAC(parse_term @"w:num")
                                           |> THEN <| FIRST_ASSUM MATCH_MP_TAC
                                           |> THEN <| EXISTS_TAC(parse_term @"w:num")
                                           |> THEN <| ASM_REWRITE_TAC []]]
                 REWRITE_TAC [UNIQUE_SKOLEM_ALT]
                 |> THEN <| DISCH_THEN
                                (X_CHOOSE_THEN (parse_term @"fn:(A)recspace->B") 
                                     (ASSUME_TAC << GSYM))
                 |> THEN <| EXISTS_TAC(parse_term @"fn:(A)recspace->B")
                 |> THEN <| ASM_REWRITE_TAC []
                 |> THEN <| REPEAT GEN_TAC
                 |> THEN <| FIRST_ASSUM MATCH_MP_TAC
                 |> THEN <| GEN_TAC
                 |> THEN <| FIRST_ASSUM(fun th -> GEN_REWRITE_TAC I [GSYM th])
                 |> THEN <| REWRITE_TAC [BETA_THM]])
#else
    Choice.result <| Sequent([], parse_term @"!Fn. ?f. !c i r. f (CONSTR c i r) = Fn c i r (\n. f (r n))") : thm
#endif

(* ------------------------------------------------------------------------- *)
(* The following is useful for coding up functions casewise.                 *)
(* ------------------------------------------------------------------------- *)

let FCONS = 
    new_recursive_definition num_RECURSION 
        (parse_term @"(!a f. FCONS (a:A) f 0 = a) /\ (!a f n. FCONS (a:A) f (SUC n) = f n)");;

// Some error occurs 
let FCONS_UNDO = 
#if BUGGY
    prove
        ((parse_term @"!f:num->A. f = FCONS (f 0) (f << SUC)"), 
         GEN_TAC
         |> THEN <| REWRITE_TAC [FUN_EQ_THM]
         |> THEN <| INDUCT_TAC
         |> THEN <| REWRITE_TAC [FCONS; o_THM])
#else
    Choice.result <| Sequent([],parse_term @"!f:num->A. f = FCONS (f 0) (f << SUC)") : thm
#endif

let FNIL = new_definition(parse_term @"FNIL (n:num) = @x:A. T");;

(* ------------------------------------------------------------------------- *)
(* The initial mutual type definition function, with a type-restricted       *)
(* recursion theorem.                                                        *)
(* ------------------------------------------------------------------------- *)

let define_type_raw_001 = 

    (* ----------------------------------------------------------------------- *)
    (* Handy utility to produce "SUC << SUC << SUC ..." form of numeral.       *)
    (* ----------------------------------------------------------------------- *)

    let sucivate = 
        let zero = (parse_term @"0")
        let suc = (parse_term @"SUC")
        fun n -> 
            Choice.funpow n (curry mk_comb suc) zero

    (* ----------------------------------------------------------------------- *)
    (* Eliminate local "definitions" in hyps.                                  *)
    (* ----------------------------------------------------------------------- *)

    let SCRUB_EQUATION eq (th, insts) = 
        choice {
            (*HA*)
            let! eq' = Choice.List.fold (fun acc x -> subst x acc) eq (map (fun t -> [t]) insts)
            let! l, r = dest_eq eq'
            return (MP (INST [r, l] (DISCH eq' th)) (REFL r), (r, l) :: insts)
        }

    (* ----------------------------------------------------------------------- *)
    (* Proves existence of model (inductively); use pseudo-constructors.       *)
    (*                                                                         *)
    (* Returns suitable definitions of constructors in terms of CONSTR, and    *)
    (* the rule and induction theorems from the inductive relation package.    *)
    (* ----------------------------------------------------------------------- *)

    let justify_inductive_type_model = 
        let t_tm = (parse_term @"T")
        let n_tm = (parse_term @"n:num")
        let beps_tm = (parse_term @"@x:bool. T")

        let rec munion s1 s2 = 
            if s1 = []
            then s2
            else 
                let h1 = hd s1
                let s1' = tl s1
                match remove (fun h2 -> h2 = h1) s2 with
                | Some (_, s2') ->
                    h1 :: (munion s1' s2')
                | None -> h1 :: (munion s1' s2)

        fun def -> 
            choice {
                let newtys, rights = unzip def
                let tyargls = itlist ((@) << map snd) rights []
                let alltys = itlist (munion << C subtract newtys) tyargls []
                let! epstms = Choice.List.map (fun ty -> mk_select(mk_var("v", ty), t_tm)) alltys

                let pty = 
                    // NOTE: review the order of arguments
                    Choice.List.reduceBack (fun ty1 ty2 -> mk_type("prod", [ty1; ty2])) alltys
                    |> Choice.fill bool_ty

                let! recty = mk_type("recspace", [pty])
                let! constr = mk_const("CONSTR", [pty, aty])
                let! fcons = mk_const("FCONS", [recty, aty])
                let! bot = mk_const("BOTTOM", [pty, aty])
                let! bottail = mk_abs(n_tm, bot)

                let mk_constructor n (cname, cargs) = 
                    choice {
                        let ttys = 
                            map (fun ty -> 
                                if mem ty newtys then recty
                                else ty) cargs

                        let! args = make_args "a" [] ttys
                        let! rargs, iargs = Choice.List.partition (fun t -> type_of t |> Choice.map (fun ty -> ty = recty)) args

                        let rec mk_injector epstms alltys iargs = 
                            choice {
                                if alltys = [] then 
                                    return []
                                else 
                                    let ty = hd alltys
                                    return! 
                                        choice { 
                                            let! a, iargs' = 
                                                remove (fun t -> Choice.get <| type_of t = ty) iargs
                                                |> Option.toChoiceWithError "remove"
                                            let! tms = (mk_injector (tl epstms) (tl alltys) iargs')
                                            return a :: tms
                                        }
                                        |> Choice.bindError (fun _ -> 
                                                choice {
                                                    let! tms = (mk_injector (tl epstms) (tl alltys) iargs)
                                                    return (hd epstms) :: tms
                                                })
                            }

                        let! iarg = 
                            choice { 
                                let! tms = mk_injector epstms alltys iargs
                                return! Choice.List.reduceBack (curry mk_pair) tms
                            }
                            |> Choice.bindError (fun _ -> Choice.result beps_tm)

                        let! rarg = Choice.List.fold (fun acc x -> mk_binop fcons x acc) bottail rargs
                        let! tms = Choice.List.map type_of args
                        let! conty = Choice.List.fold (fun acc ty -> mk_fun_ty ty acc) recty tms 

                        let condef = 
                            list_mk_comb(constr, [Choice.get <| sucivate n; 
                                                  iarg; 
                                                  rarg])

                        return! mk_eq(mk_var(cname, conty), list_mk_abs(args, condef))
                    }

                let rec mk_constructors n rights = 
                    choice {
                        if rights = [] then 
                            return []
                        else 
                            let! tms = mk_constructors (n + 1) (tl rights)
                            let! tm = mk_constructor n (hd rights)
                            return tm :: tms
                    }

                let! condefs = mk_constructors 0 (itlist (@) rights [])
                let conths = map ASSUME condefs
                let! predty = mk_fun_ty recty bool_ty

                let edefs = itlist (fun (x, l) acc -> map (fun t -> x, t) l @ acc) def []
                let idefs = map2 (fun (r, (_, atys)) def -> (r, atys), def) edefs condefs

                let mk_rule((r, a), condef) = 
                    choice {
                        let! left, right = dest_eq condef
                        let args, bod = strip_abs right
                        let lapp = list_mk_comb(left, args)
                    
                        let! conds = 
                            Choice.List.foldBack2 (fun arg argty sofar -> 
                                choice {
                                    if mem argty newtys then
                                        let! ty1 = dest_vartype argty
                                        let! tm1 = mk_comb(mk_var(ty1, predty), arg)
                                        return tm1 :: sofar
                                    else 
                                        return sofar
                                }) args a []
                    
                        let! tm1 = dest_vartype r
                        let! conc = mk_comb(mk_var(tm1, predty), lapp)
                    
                        let! rule = 
                            if conds = [] then Choice.result conc
                            else mk_imp(list_mk_conj conds, conc)
                    
                        return list_mk_forall(args, rule)
                    }

                let! tms1 = Choice.List.map mk_rule idefs
                let rules = list_mk_conj tms1
                let th0 = derive_nonschematic_inductive_relations rules
                let th1 = prove_monotonicity_hyps th0
                let th2a, th2bc = CONJ_PAIR th1
                let th2b = CONJUNCT1 th2bc
                return conths, th2a, th2b
            }

    (* ----------------------------------------------------------------------- *)
    (* Shows that the predicates defined by the rules are all nonempty.        *)
    (* (This could be done much more efficiently/cleverly, but it's OK.)       *)
    (* ----------------------------------------------------------------------- *)

    let prove_model_inhabitation rth = 
        choice {
            let srules = map SPEC_ALL (CONJUNCTS rth)
            let! imps, bases = Choice.List.partition (Choice.map (is_imp << concl)) srules
            let! concs1 = Choice.List.map (Choice.map concl) bases
            let! concs2 = Choice.List.map (Choice.bind rand << Choice.map concl) imps
            let concs = concs1 @ concs2
            let preds = setify(map (repeat(Choice.toOption << rator)) concs)
        
            let rec exhaust_inhabitations ths sofar = 
                choice {
                    let! ss = Choice.List.map (Choice.map (fst << strip_comb << concl)) sofar
                    let dunnit = setify ss
                    let! useful = Choice.List.filter (fun th -> 
                                    choice {
                                        let! th = th
                                        let! tm = rand (concl th)
                                        return not(mem (fst(strip_comb tm)) dunnit)
                                    }) ths

                    if useful = [] then 
                        return sofar
                    else 
                        let follow_horn thm = 
                            choice {
                                let! tm = Choice.bind (lhand << concl) thm
                                let preds = map (fst << strip_comb) (conjuncts tm)
                                let! asms = 
                                    Choice.List.map (fun p -> 
                                        Choice.List.tryFind (fun th -> 
                                                    choice {
                                                        let! th = th
                                                        return fst(strip_comb(concl th)) = p
                                                    }) sofar
                                        |> Choice.bind (Option.toChoiceWithError "find")) preds
                                return! MATCH_MP thm (end_itlist CONJ asms)
                            }
                        let! newth = tryfind (Choice.toOption << Choice.map Choice.result << follow_horn) useful 
                                     |> Option.toChoiceWithError "tryfind"
                        return! exhaust_inhabitations ths (newth :: sofar)
                }
        
            let! ithms = exhaust_inhabitations imps bases
            let! exths = Choice.List.map (fun p -> 
                            Choice.List.tryFind (fun th -> 
                                        choice {
                                            let! th = th
                                            return fst(strip_comb(concl th)) = p
                                        }) ithms
                            |> Choice.bind (Option.toChoiceWithError "find")) preds
        
            return exths
        }

    (* ----------------------------------------------------------------------- *)
    (* Makes a type definition for one of the defined subsets.                 *)
    (* ----------------------------------------------------------------------- *)
    let define_inductive_type cdefs exth = 
        choice {
            let! exth = exth
            let extm = concl exth
            let epred = fst(strip_comb extm)
            let! (ename, _) = dest_var epred
            let! tm1 = 
                Choice.List.tryFind (fun eq -> 
                    choice {
                        let! tm = lhand eq
                        return tm = epred
                    }) (hyp exth)
                |> Choice.bind (Option.toChoiceWithError "find")
            let th1 = ASSUME tm1
            let! tm2 = Choice.bind (rand << concl) th1
            let th2 = TRANS th1 (SUBS_CONV cdefs tm2)
            let! tm3 = rand extm
            let th3 = EQ_MP (AP_THM th2 tm3) (Choice.result exth)
            let! tms4 = Choice.map hyp th3
            let! th4, _ = Choice.List.fold (fun acc x -> SCRUB_EQUATION x acc) (th3, []) tms4
            let mkname = "_mk_" + ename
            let destname = "_dest_" + ename
            let bij1, bij2 = new_basic_type_definition ename (mkname, destname) th4
            let! tm5 = Choice.bind (rand << concl) bij2
            let! tm6 = rand tm5
            let bij2a = AP_THM th2 tm6
            let bij2b = TRANS bij2a bij2
            return bij1, bij2b
        }

    (* ----------------------------------------------------------------------- *)
    (* Defines a type constructor corresponding to current pseudo-constructor. *)
    (* ----------------------------------------------------------------------- *)

    let define_inductive_type_constructor defs consindex th = 
        choice {
            let! th = th
            let avs, bod = strip_forall(concl th)
            let! asms, conc = 
                choice {
                    if is_imp bod
                    then
                        let! tm1 = lhand bod
                        let! tm2 = rand bod
                        return conjuncts tm1, tm2
                    else 
                        return [], bod
                }

            let! asmlist = Choice.List.map dest_comb asms
            let! cpred, cterm = dest_comb conc
            let oldcon, oldargs = strip_comb cterm
            let modify_arg v = 
                choice { 
                    let! dest =
                        choice {
                            // TODO : Give this variable a better name.
                            let! x =
                                rev_assoc v asmlist
                                |> Option.toChoiceWithError "find"
                            return!
                                assoc x consindex
                                |> Option.toChoiceWithError "find"
                                |> Choice.map snd
                        }
                    let! ty1 = type_of dest
                    let! (_, ty2) = dest_type ty1
                    let ty' = hd ty2
                    let! (ty3, _) = dest_var v
                    let v' = mk_var(ty3, ty')
                    let! tm1 = mk_comb(dest, v')
                    return tm1, v'
                }
                |> Choice.fill (v, v)

            let newrights, newargs = unzip(map modify_arg oldargs)
            let! retmk =
                assoc cpred consindex
                |> Option.toChoiceWithError "find"
                |> Choice.map fst
            let! defbod = mk_comb(retmk, list_mk_comb(oldcon, newrights))
            let defrt = list_mk_abs(newargs, defbod)
            let! expth = 
                Choice.List.tryFind (fun th -> 
                    choice {
                        let! th = th
                        let! tm1 = lhand(concl th)
                        return tm1 = oldcon
                    }) defs
                |> Choice.bind (Option.toChoiceWithError "find")

            let rexpth = SUBS_CONV [expth] defrt
            let! (ty1, _) = dest_var oldcon
            let! ty2 = type_of defrt
            let deflf = mk_var(ty1, ty2)
            let! tm1 = Choice.bind (rand << concl) rexpth
            let! tm2 = mk_eq(deflf, tm1)
            let defth = new_definition tm2
            return! TRANS defth (SYM rexpth)
        }

    (* ----------------------------------------------------------------------- *)
    (* Instantiate the induction theorem on the representatives to transfer    *)
    (* it to the new type(s). Uses "\x. rep-pred(x) /\ P(mk x)" for "P".       *)
    (* ----------------------------------------------------------------------- *)

    let instantiate_induction_theorem consindex ith = 
        choice {
            let! ith = ith
            let avs, bod = strip_forall(concl ith)
            let! tm1 = rand bod
            let! corlist = 
                Choice.List.map (Choice.map (repeat (Choice.toOption << rator) ||>> repeat (Choice.toOption << rator)) 
                                 << Choice.bind dest_imp << Choice.bind body << rand) (conjuncts tm1)

            let! consindex' = 
                Choice.List.map (fun v -> 
                    choice {
                        let! w =
                            rev_assoc v corlist
                            |> Option.toChoiceWithError "find"
                        // TODO : Give this variable a better name.
                        let! x =
                            assoc w consindex
                            |> Option.toChoiceWithError "find"
                        return w, x
                    }) avs

            let! recty = (Choice.map hd << Choice.map snd << Choice.bind dest_type << type_of << fst << snd << hd) consindex
            let! newtys = Choice.List.map (Choice.map hd << Choice.map snd << Choice.bind dest_type << type_of << snd << snd) consindex'

            let ptypes = map (C (fun ty -> Choice.get << mk_fun_ty ty) bool_ty) newtys

            let! preds = make_args "P" [] ptypes
            let! args = make_args "x" [] (map (K recty) preds)
            let! lambs = 
                Choice.List.map2 
                    (fun (r, (m, d)) (p, a) ->
                        choice {
                            let! tm0 = mk_comb(r, a)
                            let! tm1 = mk_comb(m, a)
                            let! tm2 = mk_comb(p, tm1)
                            let! tm3 = mk_conj(tm0, tm2)
                            return! mk_abs (a, tm3)
                        }) consindex' (zip preds args)

            return! SPECL lambs (Choice.result ith)
        }

    (* ----------------------------------------------------------------------- *)
    (* Reduce a single clause of the postulated induction theorem (old_ver) ba *)
    (* to the kind wanted for the new type (new_ver); |- new_ver ==> old_ver   *)
    (* ----------------------------------------------------------------------- *)

    let pullback_induction_clause tybijpairs conthms = 
        let PRERULE = GEN_REWRITE_RULE (funpow 3 RAND_CONV) (map SYM conthms)
        let IPRULE = SYM << GEN_REWRITE_RULE I (map snd tybijpairs)
        fun rthm tm -> 
            choice {
                let avs, bimp = strip_forall tm
                if is_imp bimp then 
                    let! ant, con = dest_imp bimp
                    let ths = map (CONV_RULE BETA_CONV) (CONJUNCTS(ASSUME ant))
                    let tths, pths = unzip(map CONJ_PAIR ths)
                    let tth = MATCH_MP (SPEC_ALL rthm) (end_itlist CONJ tths)
                    let mths = map IPRULE (tth :: tths)
                    let! conth1 = BETA_CONV con
                    let! contm1 = rand(concl conth1)
                    let! tm1 = rator contm1
                    let! tm2 = rand contm1
                    let conth2 = TRANS (Choice.result conth1) (AP_TERM tm1 (SUBS_CONV (tl mths) tm2))

                    let conth3 = PRERULE conth2
                    let! lctms = Choice.List.map (Choice.map concl) pths
                    let! tm3 = (Choice.bind rand << Choice.bind rand << Choice.map concl) conth3
                    let! asmin = mk_imp(list_mk_conj lctms, tm3)
                    let! tm4 = lhand asmin
                    let! argsin = Choice.List.map rand (conjuncts tm4)
                    let! argsgen = 
                        Choice.List.map (fun tm -> 
                            choice {
                                let! tm1 = rand tm
                                let! (s, _) = dest_var tm1
                                let! ty1 = type_of tm
                                return mk_var(s, ty1)
                            }) argsin

                    let! asmgen = subst (zip argsgen argsin) asmin

                    let asmquant = list_mk_forall(snd(strip_comb(Choice.get <| rand(Choice.get <| rand asmgen))), asmgen)

                    let th1 = INST (zip argsin argsgen) (SPEC_ALL(ASSUME asmquant))
                    let th2 = MP th1 (end_itlist CONJ pths)
                    let th3 = EQ_MP (SYM conth3) (CONJ tth th2)
                    return! DISCH asmquant (GENL avs (DISCH ant th3))
                else 
                    let con = bimp
                    let conth2 = BETA_CONV con
                    let tth = PART_MATCH Choice.result rthm (Choice.get <| lhand(Choice.get <| rand(concl <| Choice.get conth2)))
                    let conth3 = PRERULE conth2
                    let! tm1 = Choice.bind (rand << concl) conth3
                    let! asmgen = rand tm1
                    let asmquant = list_mk_forall(snd(strip_comb(Choice.get <| rand asmgen)), asmgen)
                    let th2 = SPEC_ALL(ASSUME asmquant)
                    let th3 = EQ_MP (SYM conth3) (CONJ tth th2)
                    return! DISCH asmquant (GENL avs th3)
            }

    (* ----------------------------------------------------------------------- *)
    (* Finish off a consequence of the induction theorem.                      *)
    (* ----------------------------------------------------------------------- *)

    let finish_induction_conclusion consindex tybijpairs = 
        let tybij1, tybij2 = unzip tybijpairs
        let PRERULE = 
            GEN_REWRITE_RULE (LAND_CONV << LAND_CONV << RAND_CONV) tybij1 
            << GEN_REWRITE_RULE LAND_CONV tybij2
        let FINRULE = GEN_REWRITE_RULE RAND_CONV tybij1
        fun th -> 
            choice {
                let! th = th
                let! av, bimp = dest_forall(concl th)
                let! tm1 = rand bimp
                let! tm2 = rator tm1
                let! tm3 = body tm2
                let! pv = lhand tm3
                let! p, v = dest_comb pv
                let! mk, dest = 
                    assoc p consindex 
                    |> Option.toChoiceWithError "find"
                let! ty1 = type_of dest
                let! (_, tys2) = dest_type ty1
                let ty = hd tys2
                let! (s, _) = dest_var v
                let v' = mk_var(s, ty)
                let! dv = mk_comb(dest, v')
                let! th1 = PRERULE(SPEC dv (Choice.result th))
                let! tm4 = lhand(concl th1)
                let! tm5 = rand tm4
                let th2 = MP (Choice.result th1) (REFL tm5)
                let th3 = CONV_RULE BETA_CONV th2
                return! GEN v' (FINRULE(CONJUNCT2 th3))
            }

    (* ----------------------------------------------------------------------- *)
    (* Derive the induction theorem.                                           *)
    (* ----------------------------------------------------------------------- *)

    let derive_induction_theorem consindex tybijpairs conthms iith rth = 
        choice {
            let! iith = iith
            let! tm1 = lhand(concl iith)
            let bths = map2 (pullback_induction_clause tybijpairs conthms) (CONJUNCTS rth) (conjuncts tm1)
            let! tms = Choice.List.map (Choice.bind lhand << Choice.map concl) bths
            let asm = list_mk_conj tms
            let ths = map2 MP bths (CONJUNCTS(ASSUME asm))
            let th1 = MP (Choice.result iith) (end_itlist CONJ ths)
            let th2 = end_itlist CONJ (map (finish_induction_conclusion consindex tybijpairs) (CONJUNCTS th1))
            let! th3 = DISCH asm th2

            let! tm2 = rand(concl th3)
            let! preds = Choice.List.map (Choice.bind rator << Choice.bind body << rand) (conjuncts tm2)

            let th4 = GENL preds (Choice.result th3)
            let! tms1 = Choice.map hyp th4
            let! pasms = Choice.List.filter (Choice.map (C mem (map fst consindex)) << lhand) tms1

            let th5 = itlist DISCH pasms th4
            let! tms2 = Choice.map hyp th5
            let! th6, _ = Choice.List.fold (fun acc x -> SCRUB_EQUATION x acc) (th5, []) tms2
            let th7 = UNDISCH_ALL th6
            let! tms3 = Choice.map hyp th7
            let! (th8, _) = Choice.List.fold (fun acc x -> SCRUB_EQUATION x acc) (th7, []) tms3
            return! th8
        }

    (* ----------------------------------------------------------------------- *)
    (* Create the recursive functions and eliminate pseudo-constructors.       *)
    (* (These are kept just long enough to derive the key property.)           *)
    (* ----------------------------------------------------------------------- *)

    let create_recursive_functions tybijpairs consindex conthms rth = 
        choice {
            let! domtys = Choice.List.map (Choice.map hd << Choice.map snd << Choice.bind dest_type << type_of << snd << snd) consindex
            let! recty = (Choice.map hd << Choice.map snd << Choice.bind dest_type << type_of << fst << snd << hd) consindex
            let ranty = mk_vartype "Z"
            let! ty1 = mk_fun_ty recty ranty
            let fn = mk_var("fn", ty1)
            let! fns = make_args "fn" [] (map (C (fun ty -> Choice.get << mk_fun_ty ty) ranty) domtys)
            let! args = make_args "a" [] domtys

            let! rights = 
                Choice.List.map2 (fun (_, (_, d)) a -> 
                    choice {
                        let! tm1 = mk_comb(d, a)
                        let! tm2 = mk_comb(fn, tm1)
                        return! mk_abs(a, tm2)
                    }) consindex args

            let! eqs = Choice.List.map2 (curry mk_eq) fns rights

            let fdefs = map ASSUME eqs

            let fxths1 = 
                map (fun th1 -> tryfind (fun th2 -> Choice.toOption <| MK_COMB(th2, th1)) fdefs
                                |> Option.toChoiceWithError "tryfind") conthms

            let! fxths2 = Choice.List.map (fun th -> 
                            choice {
                                let! tm1 = Choice.bind (rand << concl) th 
                                return TRANS th (BETA_CONV tm1)
                            }) fxths1

            let mk_tybijcons(th1, th2) = 
                choice {
                    let! tm1 = Choice.bind (lhand << concl) th1
                    let! tm2 = rand tm1
                    let! tm3 = Choice.bind (lhand << concl) th2
                    let! tm4 = rand tm3
                    let th3 = INST [tm2, tm4] th2
                    let! tm4 = Choice.bind (rand << concl) th2
                    let! tm5 = lhand tm4
                    let! tm6 = rator tm5
                    let th4 = AP_TERM tm6 th1
                    return! EQ_MP (SYM th3) th4
                }

            let SCONV = GEN_REWRITE_CONV I (map mk_tybijcons tybijpairs)
            let ERULE = GEN_REWRITE_RULE I (map snd tybijpairs)

            let simplify_fxthm rthm fxth = 
                choice {
                    let! fxth = fxth
                    let! pat = Choice.funpow 4 rand (concl fxth)
                    let! rthm = rthm
                    if is_imp(repeat (Choice.toOption << Choice.map snd << dest_forall) (concl rthm)) then 
                        let! th1 = PART_MATCH (Choice.bind rand << rand) (Choice.result rthm) pat
                        let! tm1 = lhand(concl th1)
                        let tms1 = conjuncts tm1
                        let ths2 = map (fun t -> EQ_MP (SYM(SCONV t)) TRUTH) tms1
                        return! ERULE(MP (Choice.result th1) (end_itlist CONJ ths2))
                    else 
                        return! ERULE(PART_MATCH rand (Choice.result rthm) pat)
                }

            let fxths3 = map2 simplify_fxthm (CONJUNCTS rth) fxths2
            let fxths4 = map2 (fun th1 -> TRANS th1 << AP_TERM fn) fxths2 fxths3

            let cleanup_fxthm cth fxth = 
                choice {
                    let! fxth = fxth
                    let! tm1 = rand(concl fxth)
                    let! tm2 = rand tm1
                    let tms = snd(strip_comb(tm2))
                    let! cth = cth
                    let kth = RIGHT_BETAS tms (ASSUME(hd(hyp cth)))
                    return! TRANS (Choice.result fxth) (AP_TERM fn kth)
                }

            let fxth5 = end_itlist CONJ (map2 cleanup_fxthm conthms fxths4)
            let! tm1 = Choice.map hyp fxth5
            let! pasms = Choice.List.filter (Choice.map (C mem (map fst consindex)) << lhand) tm1
            let fxth6 = itlist DISCH pasms fxth5
            let tms1 = itlist (union << hyp << Choice.get) conthms []
            let! fxth7, _ = Choice.List.fold (fun acc x -> SCRUB_EQUATION x acc) (fxth6, []) tms1
            let! fxth8 = UNDISCH_ALL fxth7
            let! th1, _ = Choice.List.fold (fun acc x -> SCRUB_EQUATION x acc) (Choice.result fxth8, []) (subtract (hyp fxth8) eqs)
            return! th1
        }

    (* ----------------------------------------------------------------------- *)
    (* Create a function for recursion clause.                                 *)
    (* ----------------------------------------------------------------------- *)

    let create_recursion_iso_constructor = 
        let s = (parse_term @"s:num->Z")
        let zty = (parse_type @"Z")
        let numty = (parse_type @"num")
        let rec extract_arg tup v = 
            choice {
                if v = tup then 
                    return! REFL tup
                else 
                    let! t1, t2 = dest_pair tup
                    let! PAIR_th = 
                        ISPECL [t1; t2] (if free_in v t1 then FST else SND)
                    let! tup' = rand(concl PAIR_th)
                    if tup' = v then 
                        return PAIR_th
                    else 
                        let! tm1 = rand(concl PAIR_th)
                        let th = extract_arg tm1 v
                        return! SUBS [SYM <| Choice.result PAIR_th] th
            }

        fun consindex -> 
            let v =
                choice {
                    let! ty1 = type_of(fst(hd consindex))
                    let! (_, tys2) = dest_type(ty1)
                    let recty = hd(tys2)
                    let! (_, tys3) = dest_type recty
                    let domty = hd(tys3)
                    let i = mk_var("i", domty)
                    let! ty2 = mk_fun_ty numty recty
                    let r = mk_var("r", ty2)
                    let mks = map (fst << snd) consindex
                    let! mkindex = 
                        Choice.List.map (fun t -> 
                            choice {
                                let! ty1 = type_of t
                                let! (_, tys2) = dest_type ty1
                                return hd(tl tys2), t
                            }) mks
                    return (i, r, mkindex)
                }

            fun cth -> 
                choice {
                    let! (i, r, mkindex) = v

                    let! cth = cth

                    let! tm1 = rand(concl cth)
                    let! tm2 = rand(tm1)
                    let artms = snd(strip_comb(tm2))
                    let artys = mapfilter (Choice.toOption << Choice.bind type_of << rand) artms
                    let! tm3 = rand(hd(hyp cth))
                    let args, bod = strip_abs tm3
                    let! ccitm, rtm = dest_comb bod
                    let! cctm, itm = dest_comb ccitm
                    let rargs, iargs = partition (C free_in rtm) args
                    let xths = map (extract_arg itm) iargs
                    let! cargs' = Choice.List.map (Choice.bind (subst [i, itm]) << Choice.bind lhand << Choice.map concl) xths
                    let! indices = Choice.List.map sucivate (0 -- (length rargs - 1))
                    let! rindexed = Choice.List.map (curry mk_comb r) indices
                    let! rargs' = 
                        Choice.List.map2 (fun a rx -> 
                            choice {
                                let! tm = assoc a mkindex |> Option.toChoiceWithError "find"
                                return! mk_comb(tm, rx)
                            }) artys rindexed

                    let! sargs' = Choice.List.map (curry mk_comb s) indices
                    let allargs = cargs' @ rargs' @ sargs'

                    let! funty = Choice.List.fold (fun acc ty -> type_of ty |> Choice.bind (fun ty -> mk_fun_ty ty acc)) zty allargs
                    
                    let! tm4 = lhand(concl cth)
                    let! (name, _) = dest_const(repeat (Choice.toOption << rator) tm4)
                    let funname = name + "'"
                    let funarg = mk_var(funname, funty)
                    return list_mk_abs([i; r; s], list_mk_comb(funarg, allargs))
                }

    (* ----------------------------------------------------------------------- *)
    (* Derive the recursion theorem.                                           *)
    (* ----------------------------------------------------------------------- *)

    let derive_recursion_theorem = 
        let CCONV = funpow 3 RATOR_CONV (REPEATC(GEN_REWRITE_CONV I [FCONS]))
        fun tybijpairs consindex conthms rath -> 
            choice {
                let! isocons = Choice.List.map (create_recursion_iso_constructor consindex) conthms
                let! ty = type_of(hd isocons)
                let! fcons = mk_const("FCONS", [ty, aty])
                let! fnil = mk_const("FNIL", [ty, aty])

                let! bigfun = Choice.List.fold (fun acc tm -> mk_binop fcons tm acc) fnil isocons
                let! eth = ISPEC bigfun CONSTR_REC

                let! rath = rath
                let! tm1 = rand(hd(conjuncts(concl rath)))
                let! fn = rator tm1
                let! betm = 
                    choice {
                        let! tm1 = rand(concl eth)
                        let! v, bod = dest_abs tm1
                        return! vsubst [fn, v] bod
                    }

                let LCONV = REWR_CONV(ASSUME betm)
                let! fnths = Choice.List.map (fun t -> 
                                choice {
                                    let! tm1 = rand t
                                    let! tm2 = bndvar tm1
                                    return RIGHT_BETAS [tm2] (ASSUME t)
                                }) (hyp rath)

                let SIMPER = 
                    PURE_REWRITE_RULE
                        (map SYM fnths 
                         @ map fst tybijpairs @ [FST; SND; FCONS; BETA_THM])

                let hackdown_rath th = 
                    choice {
                        let! th = th

                        let! ltm, rtm = dest_eq(concl th)
                        let! tm1 = rand ltm
                        let wargs = snd(strip_comb tm1)
                        let! th1 = TRANS (Choice.result th) (LCONV rtm)
                        let! tm2 = rand(concl th1)
                        let! th2 = TRANS (Choice.result th1) (CCONV tm2)
                        let! tm3 = rand(concl th2)
                        let! th3 = TRANS (Choice.result th2) (funpow 2 RATOR_CONV BETA_CONV tm3)
                        let! tm4 = rand(concl th3)
                        let! th4 = TRANS (Choice.result th3) (RATOR_CONV BETA_CONV tm4)
                        let! tm5 = rand(concl th4)
                        let th5 = TRANS (Choice.result th4) (BETA_CONV tm5)
                        return! GENL wargs (SIMPER th5)
                    }

                let! rthm = end_itlist CONJ (map hackdown_rath (CONJUNCTS (Choice.result rath)))
                let! seqs = 
                    choice {
                        let unseqs = filter is_eq (hyp rthm)
                        let! tys = Choice.List.map (Choice.map (hd << snd) << Choice.bind dest_type << type_of << snd << snd) consindex
                        return!
                          Choice.List.map 
                            (fun ty -> 
                                Choice.List.tryFind (fun t -> 
                                    choice {
                                        let! tm1 = lhand t
                                        let! ty1 = type_of tm1
                                        let! (_, tys2) = dest_type ty1
                                        return hd(tys2) = ty
                                    }) unseqs
                                |> Choice.bind (Option.toChoiceWithError "find")) tys
                    }

                let rethm = itlist EXISTS_EQUATION seqs (Choice.result rthm)
                let fethm = CHOOSE (fn, Choice.result eth) rethm
                let! pcons = 
                    Choice.List.map (Choice.map (repeat (Choice.toOption << rator)) << rand 
                                     << repeat (Choice.toOption << Choice.map snd << dest_forall)) (conjuncts(concl rthm))

                return! GENL pcons fethm
            }

    fun def -> 
        choice {
            (* ----------------------------------------------------------------------- *)
            (* Basic function: returns induction and recursion separately. No parser.  *)
            (* ----------------------------------------------------------------------- *)

            let! defs, rth, ith = justify_inductive_type_model def
            let! neths = prove_model_inhabitation rth
            let! tybijpairs = Choice.List.map (define_inductive_type defs) neths
            let! preds = Choice.List.map (Choice.map (repeat (Choice.toOption << rator) << concl)) neths
            let! mkdests = 
                Choice.List.map (fun (th, _) -> 
                    choice {
                        let! th = th
                        let! tm = lhand(concl th)
                        let! tm1 = rand tm
                        let! tm2 = rator tm1
                        let! tm3 = rator tm
                        return tm3, tm2
                    }) tybijpairs

            let consindex = zip preds mkdests
            let condefs = map (define_inductive_type_constructor defs consindex) (CONJUNCTS rth)
            let! conthms = 
                Choice.List.map (fun th -> 
                    choice {
                        let! th = th
                        let! tm1 = rand(concl th)
                        let args = fst(strip_abs tm1)
                        return RIGHT_BETAS args (Choice.result th)
                    }) condefs

            let iith = instantiate_induction_theorem consindex ith
            let fth = derive_induction_theorem consindex tybijpairs conthms iith rth
            let rath = create_recursive_functions tybijpairs consindex conthms rth
            let kth = derive_recursion_theorem tybijpairs consindex conthms rath
            return fth, kth
        }
        |> Choice.getOrFailure2 "define_type_raw_001"

(* ------------------------------------------------------------------------- *)
(* Parser to present a nice interface a la Melham.                           *)
(* ------------------------------------------------------------------------- *)

/// Parses the specification for an inductive type into a structured format.
let parse_inductive_type_specification = 
    let parse_type_loc src = 
        let pty, rst = parse_pretype src
        Choice.get <| type_of_pretype pty, rst

    let parse_type_conapp src = 
        let cn, sps = 
            match src with
            | (Ident cn) :: sps -> cn, sps
            | _ -> fail()
        let tys, rst = many parse_type_loc sps
        (cn, tys), rst

    let parse_type_clause src = 
        let tn, sps = 
            match src with
            | (Ident tn) :: sps -> tn, sps
            | _ -> fail()
        let tys, rst = 
            (a(Ident "=") 
             .>>. listof parse_type_conapp (a(Resword "|")) 
                      "type definition clauses" |>> snd) sps
        (mk_vartype tn, tys), rst

    let parse_type_definition = 
        listof parse_type_clause (a(Resword ";")) "type definition"

    fun s -> 
        let spec, rst = (parse_type_definition << lex << explode) s
        if rst = [] then spec
        else failwith "parse_inductive_type_specification: junk after def"

(* ------------------------------------------------------------------------- *)
(* Use this temporary version to define the sum type.                        *)
(* ------------------------------------------------------------------------- *)

let sum_INDUCT, sum_RECURSION = 
    define_type_raw_001
        (parse_inductive_type_specification "sum = INL A | INR B")

let OUTL = 
    new_recursive_definition sum_RECURSION (parse_term @"OUTL (INL x :A+B) = x")

let OUTR = 
    new_recursive_definition sum_RECURSION (parse_term @"OUTR (INR y :A+B) = y");;

(* ------------------------------------------------------------------------- *)
(* Generalize the recursion theorem to multiple domain types.                *)
(* (We needed to use a single type to justify it via a proforma theorem.)    *)
(*                                                                           *)
(* NB! Before this is called nontrivially (i.e. more than one new type)      *)
(*     the type constructor ":sum", used internally, must have been defined. *)
(* ------------------------------------------------------------------------- *)

let define_type_raw_002 = 
    let generalize_recursion_theorem = 
        let ELIM_OUTCOMBS = GEN_REWRITE_RULE TOP_DEPTH_CONV [OUTL; OUTR]
        let rec mk_sum tys = 
            choice {
            let k = length tys
            if k = 1 then 
                return hd tys
            else 
                let tys1, tys2 = chop_list (k / 2) tys
                let! s1 = mk_sum tys1
                let! s2 = mk_sum tys2
                return! mk_type("sum", [s1; s2])
            }

        let mk_inls = 
            let rec mk_inls ty = 
                choice {
                    if is_vartype ty then 
                        return [mk_var("x", ty)]
                    else
                        let! ty1 = dest_type ty
                        match ty1 with
                        | _, [ty1; ty2] -> 
                            let! inls1 = mk_inls ty1
                            let! inls2 = mk_inls ty2
                            let! inl = mk_const("INL", [ty1, aty;
                                                        ty2, bty])
                            let! inr = mk_const("INR", [ty1, aty;
                                                        ty2, bty])
                            let! tms1 = Choice.List.map (curry mk_comb inl) inls1
                            let! tms2 = Choice.List.map (curry mk_comb inr) inls2
                            return tms1 @ tms2
                        | _ -> 
                            return! Choice.failwith "mk_inls: Unhandled case."
                }

            fun ty -> 
                choice {
                    let! bods = mk_inls ty
                    return! Choice.List.map (fun t -> 
                        choice {
                            let! tm1 = find_term is_var t
                            return! mk_abs(tm1, t)
                        }) bods
                }

        let mk_outls = 
            let rec mk_inls sof ty = 
                choice {
                    if is_vartype ty then 
                        return [sof]
                    else 
                        let! ty' = dest_type ty
                        match ty' with
                        | _, [ty1; ty2] ->
                            let! outl = mk_const("OUTL", [ty1, aty; ty2, bty])
                            let! outr = mk_const("OUTR", [ty1, aty; ty2, bty])
                            let! tm1 = mk_comb(outl, sof)
                            let! l = mk_inls tm1 ty1 
                            let! tm2 = mk_comb(outr, sof)
                            let! r = mk_inls tm2 ty2
                            return l @ r
                        | _ -> 
                            return! Choice.failwith "mk_outls: Unhandled case."
                }

            fun ty -> 
                choice {
                    let x = mk_var("x", ty)
                    let! tms = mk_inls x ty
                    return! Choice.List.map (curry mk_abs x) tms
                }

        let mk_newfun fn outl = 
            choice {
                let! s, ty = dest_var fn
                let! (_, tys1) = dest_type ty
                let dty = hd(tys1)
                let x = mk_var("x", dty)
                let! y, bod = dest_abs outl
                let! tm1 = mk_comb(fn, x)
                let! tm2 = vsubst [tm1, y] bod
                let! r = mk_abs(x, tm2)
                let! ty2 = type_of r
                let l = mk_var(s, ty2)
                let! tm3 = mk_eq(l, r)
                let th1 = ASSUME tm3
                return! RIGHT_BETAS [x] th1
            }

        fun th -> 
            choice {
                let! th = th

                let avs, ebod = strip_forall(concl th)
                let evs, bod = strip_exists ebod
                let n = length evs
                if n = 1 then 
                    return th
                else 
                    let tys = map (fun i -> mk_vartype("Z" + (string i))) (0 -- (n - 1))
                    let! sty = mk_sum tys
                    let! inls = mk_inls sty
                    let! outls = mk_outls sty
                    let! tm1 = rand(snd(strip_forall(hd(conjuncts bod))))
                    let! zty = type_of tm1
                    let! ith = INST_TYPE [sty, zty] (Choice.result th)
                    let avs, ebod = strip_forall(concl ith)
                    let evs, bod = strip_exists ebod
                    let fns' = map2 mk_newfun evs outls
                    let! tms = Choice.List.map (Choice.bind rator << Choice.bind lhs << Choice.map concl) fns'
                    let fnalist = zip evs tms
                    let inlalist = zip evs inls
                    let outlalist = zip evs outls

                    let hack_clause tm = 
                        choice {
                            let avs, bod = strip_forall tm
                            let! l, r = dest_eq bod
                            let fn, args = strip_comb r
                            let! pargs = 
                                Choice.List.map (fun a -> 
                                    choice {
                                        let! tya = type_of a
                                        let g = genvar tya
                                        if is_var a then 
                                            return g, g
                                        else 
                                            let! tm0 = rator a
                                            let! outl = assoc tm0 outlalist |> Option.toChoiceWithError "find"
                                            let! tm1 = mk_comb(outl, g)
                                            return tm1, g
                                    }) args
                        
                            let args', args'' = unzip pargs
                            let! tm1 = rator l
                            let! inl = assoc tm1 inlalist |> Option.toChoiceWithError "find"
                            let! ty1 = type_of inl
                            let! (_, tys2) = dest_type ty1
                            let rty = hd(tys2)
                            let nty = itlist ((fun ty -> Choice.get << mk_fun_ty ty) << Choice.get << type_of) args' rty
                            let! (s, _) = dest_var fn
                            let fn' = mk_var(s, nty)
                            let! tm2 = mk_comb(inl, list_mk_comb(fn', args'))
                            let r' = list_mk_abs (args'', tm2)
                            return r', fn
                        }

                    let! defs = Choice.List.map hack_clause (conjuncts bod)
                    let! jth = BETA_RULE(SPECL (map fst defs) (Choice.result ith))
                    let bth = ASSUME(snd(strip_exists(concl jth)))

                    let finish_clause th = 
                        choice {
                            let! th = th

                            let avs, bod = strip_forall(concl th)
                            let! tm1 = lhand bod
                            let! tm2 = rator tm1
                            let! outl = assoc tm1 outlalist |> Option.toChoiceWithError "find"
                            return! GENL avs (BETA_RULE(AP_TERM outl (SPECL avs (Choice.result th))))
                        }

                    let cth = end_itlist CONJ (map finish_clause (CONJUNCTS bth))
                    let dth = ELIM_OUTCOMBS cth
                    let! eth = GEN_REWRITE_RULE ONCE_DEPTH_CONV (map SYM fns') dth
                    let fth = itlist SIMPLE_EXISTS (map snd fnalist) (Choice.result eth)
                    let! dtms = Choice.List.map (Choice.map (hd << hyp)) fns'
                    let! gth = 
                        Choice.List.fold (fun th e -> 
                            choice {
                                let l, r = Choice.get <| dest_eq e
                                return MP (INST [r, l] (DISCH e th)) (REFL r)
                            }) fth dtms 

                    let hth = PROVE_HYP (Choice.result jth) (itlist SIMPLE_CHOOSE evs gth)
                    let! xvs = 
                        Choice.List.map (Choice.map (fst << strip_comb) << rand << snd << strip_forall) (conjuncts(concl eth))
                    return! GENL xvs hth
            }
    fun def ->
        let ith, rth = define_type_raw_001 def
        ith, generalize_recursion_theorem rth;;

(* ------------------------------------------------------------------------- *)
(* Set up options and lists.                                                 *)
(* ------------------------------------------------------------------------- *)

let option_INDUCT, option_RECURSION = 
    define_type_raw_002
        (parse_inductive_type_specification "option = NONE | SOME A")

let list_INDUCT, list_RECURSION = 
    define_type_raw_002
        (parse_inductive_type_specification "list = NIL | CONS A list")

(* ------------------------------------------------------------------------- *)
(* Tools for proving injectivity and distinctness of constructors.           *)
(* ------------------------------------------------------------------------- *)

/// Proves that the constructors of an automatically-defined concrete type are injective.
let prove_constructors_injective = 
    let DEPAIR = GEN_REWRITE_RULE TOP_SWEEP_CONV [PAIR_EQ]

    let prove_distinctness ax pat = 
        choice {
            let f, args = strip_comb pat
            let! rt = Choice.List.reduceBack (curry mk_pair) args
            let! ty = mk_fun_ty (Choice.get <| type_of pat) (Choice.get <| type_of rt)
            let fn = genvar ty
            let! dtm = mk_eq(Choice.get <| mk_comb(fn, pat), rt)
            let eth = prove_recursive_functions_exist ax (list_mk_forall(args, dtm))
            let! args' = variants args args
            let! atm = mk_eq(pat, list_mk_comb(f, args'))
            let ath = ASSUME atm
            let bth = AP_TERM fn ath
            let cth1 = SPECL args (ASSUME(snd(Choice.get <| dest_exists(concl <| Choice.get eth))))
            let cth2 = INST (zip args' args) cth1
            let pth = TRANS (TRANS (SYM cth1) bth) cth2
            let qth = DEPAIR pth
            let qtm = concl <| Choice.get qth
            let rth = rev_itlist (C(curry MK_COMB)) (CONJUNCTS(ASSUME qtm)) (REFL f)
            let tth = IMP_ANTISYM_RULE (DISCH atm qth) (DISCH qtm rth)
            let uth = GENL args (GENL args' tth)
            return! PROVE_HYP eth (SIMPLE_CHOOSE fn uth)
        }

    fun ax -> 
        choice {
            let! ax = ax
            let cls = conjuncts(snd(strip_exists(snd(strip_forall(concl ax)))))
            let pats = map (Choice.get << rand << Choice.get << lhand << snd << strip_forall) cls
            let tms = mapfilter (Choice.toOption << Choice.map Choice.result << prove_distinctness (Choice.result ax)) pats
            return! end_itlist CONJ tms
        };;

/// Proves that the constructors of an automatically-dened concrete type yield distinct values.
let prove_constructors_distinct = 
    let num_ty = (parse_type @"num")
    let rec allopairs f l m = 
        if l = [] then []
        else map (f(hd l)) (tl m) @ allopairs f (tl l) (tl m)
    let NEGATE = 
        GEN_ALL << CONV_RULE(REWR_CONV(TAUT(parse_term @"a ==> F <=> ~a")))

    let prove_distinct ax pat = 
        choice {
            let! nums = Choice.List.map mk_small_numeral (0 -- (length pat - 1))
            let! ty1 = type_of(hd pat)
            let! ty2 = mk_type("fun", [ty1; num_ty])
            let fn = genvar ty2
            let! ls = Choice.List.map (curry mk_comb fn) pat
            let! defs = 
                Choice.List.map2 (fun l r -> 
                    choice {
                        let! tm1 = rand l
                        let! tm2 = mk_eq(l, r)
                        return list_mk_forall(frees tm1, tm2)
                    }) ls nums

            let! eth = prove_recursive_functions_exist ax (list_mk_conj defs)
            let! ev, bod = dest_exists(concl eth)
            let REWRITE = GEN_REWRITE_RULE ONCE_DEPTH_CONV (CONJUNCTS(ASSUME bod))
            let! pat' = 
                Choice.List.map (fun t -> 
                    choice {
                        let f, args = 
                            if is_numeral t then t, []
                            else strip_comb t
                        let! tms = variants args args
                        return list_mk_comb(f, tms)
                    }) pat

            let pairs = allopairs (curry (Choice.get << mk_eq)) pat pat'
            let nths = map (REWRITE << AP_TERM fn << ASSUME) pairs
            let fths = map2 (fun t th -> NEGATE(DISCH t (CONV_RULE NUM_EQ_CONV th))) pairs nths
            return CONJUNCTS(PROVE_HYP (Choice.result eth) (SIMPLE_CHOOSE ev (end_itlist CONJ fths)))
        }

    fun ax -> 
        choice {
            let! ax = ax

            let cls = conjuncts(snd(strip_exists(snd(strip_forall(concl ax)))))
            let! lefts = Choice.List.map (Choice.bind dest_comb << lhand << snd << strip_forall) cls
            let fns = itlist (insert << fst) lefts []
            let pats = map (fun f -> map snd (filter ((=) f << fst) lefts)) fns
            return! end_itlist CONJ (end_itlist (@) (mapfilter (Choice.toOption << prove_distinct (Choice.result ax)) pats))
        };;

(* ------------------------------------------------------------------------- *)
(* Automatically prove the case analysis theorems.                           *)
(* ------------------------------------------------------------------------- *)

/// Proves a structural cases theorem for an automatically-defined concrete type.
let prove_cases_thm = 
    let mk_exclauses x rpats = 
        let xts = map (fun t -> list_mk_exists(frees t, Choice.get <| mk_eq(x, t))) rpats
        Choice.get <| mk_abs(x, list_mk_disj xts)
    let prove_triv tm = 
        let evs, bod = strip_exists tm
        let l, r = Choice.get <| dest_eq bod
        if l = r
        then REFL l
        else 
            let lf, largs = strip_comb l
            let rf, rargs = strip_comb r
            if lf = rf
            then 
                let ths = map (ASSUME << Choice.get << mk_eq) (zip rargs largs)
                let th1 = rev_itlist (C(curry MK_COMB)) ths (REFL lf)
                itlist EXISTS_EQUATION (map (concl << Choice.get) ths) (SYM th1)
            else failwith "prove_triv"
    let rec prove_disj tm = 
        if is_disj tm
        then 
            let l, r = Choice.get <| dest_disj tm
            try 
                DISJ1 (prove_triv l) r
            with
            | Failure _ -> DISJ2 l (prove_disj r)
        else prove_triv tm
    let prove_eclause tm = 
        let avs, bod = strip_forall tm
        let ctm = 
            if is_imp bod
            then Choice.get <| rand bod
            else bod
        let cth = prove_disj ctm
        let dth = 
            if is_imp bod
            then DISCH (Choice.get <| lhand bod) cth
            else cth
        GENL avs dth
    fun th -> 
        let avs, bod = strip_forall(concl <| Choice.get th)
        let cls = map (snd << strip_forall) (conjuncts(Choice.get <| lhand bod))
        let pats = 
            map (fun t -> 
                    if is_imp t
                    then Choice.get <| rand t
                    else t) cls
        let spats = map (Choice.get << dest_comb) pats
        let preds = itlist (insert << fst) spats []
        let rpatlist = 
            map (fun pr -> map snd (filter (fun (p, x) -> p = pr) spats)) preds
        let xs = Choice.get <| make_args "x" (freesl pats) (map (Choice.get << type_of << hd) rpatlist)
        let xpreds = map2 mk_exclauses xs rpatlist
        let ith = BETA_RULE(INST (zip xpreds preds) (SPEC_ALL th))
        let eclauses = conjuncts(fst(Choice.get <| dest_imp(concl <| Choice.get ith)))
        MP ith (end_itlist CONJ (map prove_eclause eclauses));;

inductive_type_store 
:= ["list", (2, list_INDUCT, list_RECURSION)
    "option", (2, option_INDUCT, option_RECURSION)
    "sum", (2, sum_INDUCT, sum_RECURSION)] @ (!inductive_type_store);;

(* ------------------------------------------------------------------------- *)
(* Now deal with nested recursion. Need a store of previous theorems.        *)
(* ------------------------------------------------------------------------- *)

(* ------------------------------------------------------------------------- *)
(* Also add a cached rewrite of distinctness and injectivity theorems. Since *)
(* there can be quadratically many distinctness clauses, it would really be  *)
(* preferable to have a conversion, but this seems OK up 100 constructors.   *)
(* ------------------------------------------------------------------------- *)

/// Net of injectivity and distinctness properties for recursive type constructors.
let basic_rectype_net = ref empty_net

/// Internal theorem list of distinctness theorems.
let distinctness_store = ref ["bool", TAUT(parse_term @"(T <=> F) <=> F")]

/// Internal theorem list of injectivity theorems.
let injectivity_store = ref []

/// Extends internal store of distinctness and injectivity theorems for a new inductive type.
let extend_rectype_net(tyname, (_, _, rth)) = 
    let ths1 = 
        try 
            [prove_constructors_distinct rth]
        with
        | Failure _ -> []
    let ths2 = 
        try 
            [prove_constructors_injective rth]
        with
        | Failure _ -> []
    let canon_thl = itlist (fun x y -> Choice.get <| mk_rewrites false x y) (ths1 @ ths2) []
    distinctness_store := map (fun th -> tyname, th) ths1 @ (!distinctness_store)
    injectivity_store := map (fun th -> tyname, th) ths2 @ (!injectivity_store)
    basic_rectype_net := itlist (fun x y -> Choice.get <| net_of_thm true x y) canon_thl (!basic_rectype_net)

do_list extend_rectype_net (!inductive_type_store);;

(* ------------------------------------------------------------------------- *)
(* Return distinctness and injectivity for a type by simple lookup.          *)
(* ------------------------------------------------------------------------- *)

/// Produce distinctness theorem for an inductive type.
let distinctness ty =
    assoc ty (!distinctness_store)
    |> Option.getOrFailWith "find"

/// Produce injectivity theorem for an inductive type.
let injectivity ty =
    assoc ty (!injectivity_store)
    |> Option.getOrFailWith "find"

/// Produce cases theorem for an inductive type.
let cases ty = 
    if ty = "num"
    then num_CASES
    else 
        let _, ith, _ =
            assoc ty (!inductive_type_store)
            |> Option.getOrFailWith "find"
        prove_cases_thm ith

(* ------------------------------------------------------------------------- *)
(* Convenient definitions for type isomorphism.                              *)
(* ------------------------------------------------------------------------- *)

let ISO = 
    new_definition
        (parse_term @"ISO (f:A->B) (g:B->A) <=> (!x. f(g x) = x) /\ (!y. g(f y) = y)");;

let ISO_REFL = prove((parse_term @"ISO (\x:A. x) (\x. x)"), REWRITE_TAC [ISO])

let ISO_FUN = 
    prove
        ((parse_term @"ISO (f:A->A') f' /\ ISO (g:B->B') g'
     ==> ISO (\h a'. g(h(f' a'))) (\h a. g'(h(f a)))"),
         REWRITE_TAC [ISO; FUN_EQ_THM]
         |> THEN <| MESON_TAC [])

let ISO_USAGE = prove((parse_term @"ISO f g
   ==> (!P. (!x. P x) <=> (!x. P(g x))) /\
       (!P. (?x. P x) <=> (?x. P(g x))) /\
       (!a b. (a = g b) <=> (f a = b))"), REWRITE_TAC [ISO; FUN_EQ_THM]
                                          |> THEN <| MESON_TAC [])

(* ------------------------------------------------------------------------- *)
(* Hence extend type definition to nested types.                             *)
(* ------------------------------------------------------------------------- *)

/// <summary>
/// Like <see cref="define_type"/> but from a more structured representation than a string.
/// </summary>
let define_type_raw = 
    (* ----------------------------------------------------------------------- *)
    (* Dispose of trivial antecedent.                                          *)
    (* ----------------------------------------------------------------------- *)

    let TRIV_ANTE_RULE = 
        let TRIV_IMP_CONV tm = 
            let avs, bod = strip_forall tm
            let bth = 
                if is_eq bod
                then REFL(Choice.get <| rand bod)
                else 
                    let ant, con = Choice.get <| dest_imp bod
                    let ith = SUBS_CONV (CONJUNCTS(ASSUME ant)) (Choice.get <| lhs con)
                    DISCH ant ith
            GENL avs bth
        fun th -> 
            let tm = concl <| Choice.get th
            if is_imp tm
            then 
                let ant, con = Choice.get <| dest_imp(concl <| Choice.get th)
                let cjs = conjuncts ant
                let cths = map TRIV_IMP_CONV cjs
                MP th (end_itlist CONJ cths)
            else th

    (* ----------------------------------------------------------------------- *)
    (* Lift type bijections to "arbitrary" (well, free rec or function) type.  *)
    (* ----------------------------------------------------------------------- *)

    let ISO_EXPAND_CONV = PURE_ONCE_REWRITE_CONV [ISO]

    let rec lift_type_bijections iths cty = 
        let itys = 
            map (hd << snd << Choice.get << dest_type << Choice.get << type_of << Choice.get << lhand << concl <<Choice.get) iths

        match assoc cty (zip itys iths) with
        | Some x -> x
        | None ->
            if not(exists (C occurs_in cty) itys)
            then INST_TYPE [cty, aty] ISO_REFL
            else 
                let tycon, isotys = Choice.get <| dest_type cty
                if tycon = "fun"
                then 
                    MATCH_MP ISO_FUN 
                        (end_itlist CONJ 
                             (map (lift_type_bijections iths) isotys))
                else 
                    failwith
                        ("lift_type_bijections: Unexpected type operator \"" 
                         + tycon + "\"")

    (* ----------------------------------------------------------------------- *)
    (* Prove isomorphism of nested types where former is the smaller.          *)
    (* ----------------------------------------------------------------------- *)

    let DE_EXISTENTIALIZE_RULE = 
        let pth = 
            prove
                ((parse_term @"(?) P ==> (c = (@)P) ==> P c"), 
                 GEN_REWRITE_TAC (LAND_CONV << RAND_CONV) [GSYM ETA_AX]
                 |> THEN <| DISCH_TAC
                 |> THEN <| DISCH_THEN SUBST1_TAC
                 |> THEN <| MATCH_MP_TAC SELECT_AX
                 |> THEN <| POP_ASSUM ACCEPT_TAC)
        let USE_PTH = MATCH_MP pth
        let rec DE_EXISTENTIALIZE_RULE th = 
            if not(is_exists(concl <| Choice.get th))
            then [], th
            else 
                let th1 = USE_PTH th
                let v1 = Choice.get <| rand(Choice.get <| rand(concl <| Choice.get th1))
                let gv = genvar(Choice.get <| type_of v1)
                let th2 = CONV_RULE BETA_CONV (UNDISCH(INST [gv, v1] th1))
                let vs, th3 = DE_EXISTENTIALIZE_RULE th2
                gv :: vs, th3
        DE_EXISTENTIALIZE_RULE
    let grab_type = Choice.get << type_of << Choice.get << rand << Choice.get << lhand << snd << strip_forall

    let clause_corresponds cl0 = 
        let f0, ctm0 = Choice.get <| dest_comb(Choice.get <| lhs cl0)
        let c0 = fst(Choice.get <| dest_const(fst(strip_comb ctm0)))
        let dty0, rty0 = Choice.get <| dest_fun_ty(Choice.get <| type_of f0)
        fun cl1 -> 
            let f1, ctm1 = Choice.get <| dest_comb(Choice.get <| lhs cl1)
            let c1 = fst(Choice.get <| dest_const(fst(strip_comb ctm1)))
            let dty1, rty1 = Choice.get <| dest_fun_ty(Choice.get <| type_of f1)
            c0 = c1 && dty0 = rty1 && rty0 = dty1

    let prove_inductive_types_isomorphic n k (ith0, rth0) (ith1, rth1) = 
        let sth0 = SPEC_ALL rth0
        let sth1 = SPEC_ALL rth1
        let t_tm = concl <| Choice.get TRUTH
        let pevs0, pbod0 = strip_exists(concl <| Choice.get sth0)
        let pevs1, pbod1 = strip_exists(concl <| Choice.get sth1)
        let pcjs0, qcjs0 = chop_list k (conjuncts pbod0)
        let pcjs1, qcjs1 = chop_list k (snd(chop_list n (conjuncts pbod1)))
        let tyal0 = setify(zip (map grab_type pcjs1) (map grab_type pcjs0))
        let tyal1 = map (fun (a, b) -> (b, a)) tyal0
        let tyins0 = 
            map (fun f -> 
                    let domty, ranty = Choice.get <| dest_fun_ty(Choice.get <| type_of f)
                    Choice.get <| tysubst tyal0 domty, ranty) pevs0
        let tyins1 = 
            map (fun f -> 
                    let domty, ranty = Choice.get <| dest_fun_ty(Choice.get <| type_of f)
                    Choice.get <| tysubst tyal1 domty, ranty) pevs1
        let tth0 = INST_TYPE tyins0 sth0
        let tth1 = INST_TYPE tyins1 sth1
        let evs0, bod0 = strip_exists(concl <| Choice.get tth0)
        let evs1, bod1 = strip_exists(concl <| Choice.get tth1)
        let lcjs0, rcjs0 = 
            chop_list k (map (snd << strip_forall) (conjuncts bod0))
        let lcjs1, rcjsx = 
            chop_list k 
                (map (snd << strip_forall) (snd(chop_list n (conjuncts bod1))))
        let rcjs1 = map (fun t -> Option.get <| find (clause_corresponds t) rcjsx) rcjs0
        let proc_clause tm0 tm1 = 
            let l0, r0 = Choice.get <| dest_eq tm0
            let l1, r1 = Choice.get <| dest_eq tm1
            let vc0, wargs0 = strip_comb r0
            let con0, vargs0 = strip_comb(Choice.get <| rand l0)
            let gargs0 = map (genvar << Choice.get << type_of) wargs0
            let nestf0 =
                map 
                    (fun a -> 
                        Option.isSome <| find(fun t -> is_comb t && Choice.get <| rand t = a) wargs0) 
                    vargs0
            let targs0 = 
                map2 (fun a f -> 
                        if f
                        then Option.get <| find (fun t -> is_comb t && Choice.get <| rand t = a) wargs0
                        else a) vargs0 nestf0
            let gvlist0 = zip wargs0 gargs0
            let xargs =
                targs0
                |> map (fun v ->
                    assoc v gvlist0
                    |> Option.getOrFailWith "find")
            let inst0 = 
                list_mk_abs
                    (gargs0, list_mk_comb(fst(strip_comb(Choice.get <| rand l1)), xargs)), vc0
            let vc1, wargs1 = strip_comb r1
            let con1, vargs1 = strip_comb(Choice.get <| rand l1)
            let gargs1 = map (genvar << Choice.get << type_of) wargs1
            let targs1 = 
                map2 (fun a f -> 
                        if f
                        then Option.get <| find (fun t -> is_comb t && Choice.get <| rand t = a) wargs1
                        else a) vargs1 nestf0
            let gvlist1 = zip wargs1 gargs1
            let xargs =
                targs1
                |> map (fun v ->
                    assoc v gvlist1
                    |> Option.getOrFailWith "find")
            let inst1 = 
                list_mk_abs
                    (gargs1, list_mk_comb(fst(strip_comb(Choice.get <| rand l0)), xargs)), vc1
            inst0, inst1
        let insts0, insts1 = 
            unzip(map2 proc_clause (lcjs0 @ rcjs0) (lcjs1 @ rcjs1))
        let uth0 = BETA_RULE(INST insts0 tth0)
        let uth1 = BETA_RULE(INST insts1 tth1)
        let efvs0, sth0 = DE_EXISTENTIALIZE_RULE uth0
        let efvs1, sth1 = DE_EXISTENTIALIZE_RULE uth1
        let efvs2 = 
            map 
                (fun t1 -> 
                    Option.get <| find 
                        (fun t2 -> 
                            hd(tl(snd(Choice.get <| dest_type(Choice.get <| type_of t1)))) = hd
                                                                     (snd
                                                                          (Choice.get <| dest_type
                                                                               (Choice.get <| type_of 
                                                                                    t2)))) 
                        efvs1) efvs0
        let isotms = 
            map2 (fun ff gg -> Choice.get <| list_mk_icomb "ISO" [ff; gg]) efvs0 efvs2
        let ctm = list_mk_conj isotms
        let cth1 = ISO_EXPAND_CONV ctm
        let ctm1 = Choice.get <| rand(concl <| Choice.get cth1)
        let cjs = conjuncts ctm1
        let eee = map (fun n -> n mod 2 = 0) (0 -- (length cjs - 1))
        let cjs1, cjs2 = partition fst (zip eee cjs)
        let ctm2 = 
            Choice.get <| mk_conj(list_mk_conj(map snd cjs1), list_mk_conj(map snd cjs2))
        let DETRIV_RULE = TRIV_ANTE_RULE << REWRITE_RULE [sth0; sth1]
        let jth0 = 
            let itha = SPEC_ALL ith0
            let icjs = conjuncts(Choice.get <| rand(concl <| Choice.get itha))
            let cinsts = 
                map (fun tm -> tryfind (fun vtm -> Choice.toOption <| term_match [] vtm tm) icjs
                               |> Option.getOrFailWith "tryfind") 
                    (conjuncts(Choice.get <| rand ctm2))
            let tvs = 
                subtract (fst(strip_forall(concl <| Choice.get ith0))) 
                    (itlist (fun (_, x, _) -> union(map snd x)) cinsts [])
            let ctvs = 
                map (fun p -> 
                        let x = mk_var("x", hd(snd(Choice.get <| dest_type(Choice.get <| type_of p))))
                        Choice.get <| mk_abs(x, t_tm), p) tvs
            DETRIV_RULE(INST ctvs (itlist INSTANTIATE cinsts itha))
        let jth1 = 
            let itha = SPEC_ALL ith1
            let icjs = conjuncts(Choice.get <| rand(concl <| Choice.get itha))
            let cinsts = 
                map (fun tm -> tryfind (fun vtm -> Choice.toOption <| term_match [] vtm tm) icjs
                               |> Option.getOrFailWith "tryfind") 
                    (conjuncts(Choice.get <| lhand ctm2))
            let tvs = 
                subtract (fst(strip_forall(concl <| Choice.get ith1))) 
                    (itlist (fun (_, x, _) -> union(map snd x)) cinsts [])
            let ctvs = 
                map (fun p -> 
                        let x = mk_var("x", hd(snd(Choice.get <| dest_type(Choice.get <| type_of p))))
                        Choice.get <| mk_abs(x, t_tm), p) tvs
            DETRIV_RULE(INST ctvs (itlist INSTANTIATE cinsts itha))
        let cths4 = map2 CONJ (CONJUNCTS jth0) (CONJUNCTS jth1)
        let cths5 = map (PURE_ONCE_REWRITE_RULE [GSYM ISO]) cths4
        let cth6 = end_itlist CONJ cths5
        cth6, CONJ sth0 sth1

    (* ----------------------------------------------------------------------- *)
    (* Define nested type by doing a 1-level unwinding.                        *)
    (* ----------------------------------------------------------------------- *)

    let SCRUB_ASSUMPTION th = 
        let hyps = hyp <| Choice.get th
        let eqn = 
            Option.get <| find (fun t -> 
                    let x = Choice.get <| lhs t
                    forall (fun u -> not(free_in x (Choice.get <| rand u))) hyps) hyps
        let l, r = Choice.get <| dest_eq eqn
        MP (INST [r, l] (DISCH eqn th)) (REFL r)
    let define_type_basecase def = 
        let add_id s = fst(Choice.get <| dest_var(genvar bool_ty))
        let def' = map (I ||>> (map(add_id ||>> I))) def
        define_type_raw_002 def'
    let SIMPLE_BETA_RULE = GSYM << PURE_REWRITE_RULE [BETA_THM; FUN_EQ_THM]
    let ISO_USAGE_RULE = MATCH_MP ISO_USAGE
    let SIMPLE_ISO_EXPAND_RULE = CONV_RULE(REWR_CONV ISO)
    let REWRITE_FUN_EQ_RULE = 
        let ths = itlist (fun x y -> Choice.get <| mk_rewrites false x y) [FUN_EQ_THM] []
        let net = itlist (fun x y -> Choice.get <| net_of_thm false x y) ths (basic_net())
        CONV_RULE << GENERAL_REWRITE_CONV true TOP_DEPTH_CONV net
    let is_nested vs ty = 
        not(is_vartype ty) && not(intersect (tyvars ty) vs = [])
    let rec modify_type alist ty =
        match rev_assoc ty alist with
        | Some x -> x
        | None ->
            try 
                let tycon, tyargs = Choice.get <| dest_type ty
                Choice.get <| mk_type(tycon, map (modify_type alist) tyargs)
            with
            | Failure _ -> ty
    let modify_item alist (s, l) = s, map (modify_type alist) l
    let modify_clause alist (l, lis) = l, map (modify_item alist) lis
    let recover_clause id tm = 
        let con, args = strip_comb tm
        fst(Choice.get <| dest_const con) + id, map (Choice.get << type_of) args

    let rec create_auxiliary_clauses nty = 
        let id = fst(Choice.get <| dest_var(genvar bool_ty))
        let tycon, tyargs = Choice.get <| dest_type nty
        let k, ith, rth =
            match assoc tycon !inductive_type_store with
            | Some x -> x
            | None ->
                failwith("Can't Option.get <| find definition for nested type: " + tycon)
        let evs, bod = strip_exists(snd(strip_forall(concl <| Choice.get rth)))
        let cjs = map (Choice.get << lhand << snd << strip_forall) (conjuncts bod)
        let rtys = map (hd << snd << Choice.get << dest_type << Choice.get << type_of) evs
        let tyins = tryfind (fun vty -> Choice.toOption <| type_match vty nty []) rtys |> Option.getOrFailWith "tryfind"
        let cjs' = map (Choice.get << inst tyins << Choice.get << rand) (fst(chop_list k cjs))
        let mtys = itlist (insert << Choice.get << type_of) cjs' []
        let pcons = map (fun ty -> filter (fun t -> Choice.get <| type_of t = ty) cjs') mtys
        let cls' = zip mtys (map (map(recover_clause id)) pcons)
        let tyal = map (fun ty -> mk_vartype(fst(Choice.get <| dest_type ty) + id), ty) mtys
        let cls'' = map (modify_type tyal ||>> map(modify_item tyal)) cls'
        k, tyal, cls'', INST_TYPE tyins ith, INST_TYPE tyins rth

    let rec define_type_nested def = 
        let n = length(itlist (@) (map (map fst << snd) def) [])
        let newtys = map fst def
        let utys = unions(itlist (union << map snd << snd) def [])
        let rectys = filter (is_nested newtys) utys
        if rectys = []
        then 
            let th1, th2 = define_type_basecase def
            n, th1, th2
        else 
            let nty = hd(sort (fun t1 t2 -> occurs_in t2 t1) rectys)
            let k, tyal, ncls, ith, rth = create_auxiliary_clauses nty
            let cls = map (modify_clause tyal) def @ ncls
            let _, ith1, rth1 = define_type_nested cls
            let xnewtys = 
                map (hd << snd << Choice.get << dest_type << Choice.get << type_of) 
                    (fst(strip_exists(snd(strip_forall(concl <| Choice.get rth1)))))
            let xtyal = 
                map (fun ty -> 
                        let s = Choice.get <| dest_vartype ty
                        Option.get <| find (fun t -> fst(Choice.get <| dest_type t) = s) xnewtys, ty) 
                    (map fst cls)
            let ith0 = INST_TYPE xtyal ith
            let rth0 = INST_TYPE xtyal rth
            let isoth, rclauses = 
                prove_inductive_types_isomorphic n k (ith0, rth0) (ith1, rth1)
            let irth3 = CONJ ith1 rth1
            let vtylist = itlist (insert << Choice.get << type_of) (Choice.get <| variables(concl <| Choice.get irth3)) []
            let isoths = CONJUNCTS isoth
            let isotys = 
                map (hd << snd << Choice.get << dest_type << Choice.get << type_of << Choice.get << lhand << concl <<Choice.get) isoths
            let ctylist = 
                filter (fun ty -> exists (fun t -> occurs_in t ty) isotys) 
                    vtylist
            let atylist = itlist (union << striplist (Choice.toOption << dest_fun_ty)) ctylist []
            let isoths' = 
                map (lift_type_bijections isoths) 
                    (filter (fun ty -> exists (fun t -> occurs_in t ty) isotys) 
                         atylist)
            let cisoths = 
                map (BETA_RULE << lift_type_bijections isoths') ctylist
            let uisoths = map ISO_USAGE_RULE cisoths
            let visoths = map (ASSUME << concl <<Choice.get) uisoths
            let irth4 = 
                itlist PROVE_HYP uisoths (REWRITE_FUN_EQ_RULE visoths irth3)
            let irth5 = 
                REWRITE_RULE (rclauses :: map SIMPLE_ISO_EXPAND_RULE isoths') 
                    irth4
            let irth6 = repeat (Choice.toOption << Choice.result << SCRUB_ASSUMPTION) irth5
            let ncjs = 
                filter 
                    (fun t -> 
                        exists (fun v -> not(is_var v)) 
                            (snd(strip_comb(Choice.get <| rand(Choice.get <| lhs(snd(strip_forall t))))))) 
                    (conjuncts
                         (snd
                              (strip_exists
                                   (snd(strip_forall(Choice.get <| rand(concl <| Choice.get irth6)))))))
            let mk_newcon tm = 
                let vs, bod = strip_forall tm
                let rdeb = Choice.get <| rand(Choice.get <| lhs bod)
                let rdef = list_mk_abs(vs, rdeb)
                let newname = fst(Choice.get <| dest_var(genvar bool_ty))
                let def = Choice.get <| mk_eq(mk_var(newname, Choice.get <| type_of rdef), rdef)
                let dth = new_definition def
                SIMPLE_BETA_RULE dth
            let dths = map mk_newcon ncjs
            let ith6, rth6 = CONJ_PAIR(PURE_REWRITE_RULE dths irth6)
            n, ith6, rth6

    fun def -> 
        let newtys = map fst def
        let truecons = itlist (@) (map (map fst << snd) def) []
        let (p, ith0, rth0) = define_type_nested def
        let avs, etm = strip_forall(concl <| Choice.get rth0)
        let allcls = conjuncts(snd(strip_exists etm))
        let relcls = fst(chop_list (length truecons) allcls)
        let gencons = 
            map (repeat (Choice.toOption << rator) << Choice.get << rand << Choice.get << lhand << snd << strip_forall) relcls
        let cdefs = 
            map2 
                (fun s r -> SYM(new_definition(Choice.get <| mk_eq(mk_var(s, Choice.get <| type_of r), r)))) 
                truecons gencons
        let tavs = Choice.get <| make_args "f" [] (map (Choice.get << type_of) avs)
        let ith1 = SUBS cdefs ith0
        let rth1 = GENL tavs (SUBS cdefs (SPECL tavs rth0))
        let retval = p, ith1, rth1
        let newentries = map (fun s -> Choice.get <| dest_vartype s, retval) newtys
        (inductive_type_store := newentries @ (!inductive_type_store)
         do_list extend_rectype_net newentries
         ith1, rth1)

(* ----------------------------------------------------------------------- *)
(* The overall function, with rather crude string-based benignity.         *)
(* ----------------------------------------------------------------------- *)

/// List of previously declared inductive types.
let the_inductive_types = 
    ref ["list = NIL | CONS A list", (list_INDUCT, list_RECURSION)
         "option = NONE | SOME A", (option_INDUCT, option_RECURSION)
         "sum = INL A | INR B", (sum_INDUCT, sum_RECURSION)];;

/// Automatically define user-specified inductive data types.
let define_type s = 
    choice {
        match assoc s !the_inductive_types with
        | Some retval -> 
            warn true "Benign redefinition of inductive type"
            return retval
        | None -> 
            let defspec = parse_inductive_type_specification s
            let newtypes = map fst defspec
            let constructors = itlist ((@) << map fst) (map snd defspec) []
            if not(length(setify newtypes) = length newtypes) then 
                return! Choice.failwith "define_type: multiple definitions of a type"
            elif not(length(setify constructors) = length constructors) then 
                return! Choice.failwith "define_type: multiple instances of a constructor"
            elif exists (Choice.isResult << Choice.bind get_type_arity << dest_vartype) newtypes then
                let! ss = Choice.List.map dest_vartype newtypes
                let! t = find (Choice.isResult << get_type_arity) ss
                         |> Option.toChoiceWithError "find"
                return! Choice.failwith("define_type: type :" + t + " already defined")
            elif exists (Choice.isResult << get_const_type) constructors then 
                let! t = find (Choice.isResult << get_const_type) constructors
                         |> Option.toChoiceWithError "find"
                return! Choice.failwith("define_type: constant " + t + " already defined")
            else 
                let retval = define_type_raw_002 defspec
                the_inductive_types := (s, retval) :: (!the_inductive_types)
                return retval
    }
    |> Choice.getOrFailure2 "define_type"

(* ------------------------------------------------------------------------- *)
(* Unwinding, and application of patterns. Add easy cases to default net.    *)
(* ------------------------------------------------------------------------- *)

// UNWIND_CONV: Eliminates existentially quantified variables that are equated to something.
// MATCH_CONV: Expands application of pattern-matching construct to particular case.
let UNWIND_CONV, MATCH_CONV = 
    let pth_0 = 
      prove((parse_term @"(if ?!x. x = a /\ p then @x. x = a /\ p else @x. F) =
      (if p then a else @x. F)"),
        BOOL_CASES_TAC(parse_term @"p:bool")
        |> THEN <| ASM_REWRITE_TAC [COND_ID]
        |> THEN <| MESON_TAC [])

    let pth_1 = 
      prove((parse_term @"_MATCH x (_SEQPATTERN r s) =
      (if ?y. r x y then _MATCH x r else _MATCH x s) /\
      _FUNCTION (_SEQPATTERN r s) x =
      (if ?y. r x y then _FUNCTION r x else _FUNCTION s x)"),
        REWRITE_TAC [_MATCH; _SEQPATTERN; _FUNCTION]
        |> THEN <| MESON_TAC [])

    let pth_2 = 
        prove
            ((parse_term @"((?y. _UNGUARDED_PATTERN (GEQ s t) (GEQ u y)) <=> s = t) /\
     ((?y. _GUARDED_PATTERN (GEQ s t) p (GEQ u y)) <=> s = t /\ p)"), 
             REWRITE_TAC [_UNGUARDED_PATTERN; _GUARDED_PATTERN; GEQ_DEF]
             |> THEN <| MESON_TAC [])

    let pth_3 = 
        prove
            ((parse_term @"(_MATCH x (\y z. P y z) = if ?!z. P x z then @z. P x z else @x. F) /\
     (_FUNCTION (\y z. P y z) x = if ?!z. P x z then @z. P x z else @x. F)"), 
             REWRITE_TAC [_MATCH; _FUNCTION])

    let pth_4 = 
        prove
            ((parse_term @"(_UNGUARDED_PATTERN (GEQ s t) (GEQ u y) <=> y = u /\ s = t) /\
     (_GUARDED_PATTERN (GEQ s t) p (GEQ u y) <=> y = u /\ s = t /\ p)"), 
             REWRITE_TAC [_UNGUARDED_PATTERN; _GUARDED_PATTERN; GEQ_DEF]
             |> THEN <| MESON_TAC [])

    let pth_5 = 
        prove
            ((parse_term @"(if ?!z. z = k then @z. z = k else @x. F) = k"), 
             MESON_TAC [])

    let rec INSIDE_EXISTS_CONV conv tm = 
        if is_exists tm then BINDER_CONV (INSIDE_EXISTS_CONV conv) tm
        else conv tm

    let PUSH_EXISTS_CONV = 
        let econv = REWR_CONV SWAP_EXISTS_THM
        let rec conv bc tm = 
            choice { 
                return! (econv |> THENC <| BINDER_CONV(conv bc)) tm
            }
            |> Choice.bindError (fun _ -> bc tm)
        conv

    let BREAK_CONS_CONV = 
        let conv2 = GEN_REWRITE_CONV DEPTH_CONV [AND_CLAUSES; OR_CLAUSES]
                    |> THENC <| ASSOC_CONV CONJ_ASSOC
        fun tm -> 
            let conv0 = TOP_SWEEP_CONV(REWRITES_CONV(!basic_rectype_net))
            let conv1 = 
                if is_conj tm then LAND_CONV conv0
                else conv0
            (conv1
             |> THENC <| conv2) tm

    let UNWIND_CONV = 
        let baseconv = 
            GEN_REWRITE_CONV I [UNWIND_THM1
                                UNWIND_THM2
                                EQT_INTRO(SPEC_ALL EXISTS_REFL)
                                EQT_INTRO(GSYM(SPEC_ALL EXISTS_REFL))]

        let rec UNWIND_CONV tm = 
            let evs, bod = strip_exists tm
            let eqs = conjuncts bod
            choice { 
                let! eq = 
                    Choice.List.tryFind (fun tm -> 
                        choice {
                            let! l, r = dest_eq tm
                            return
                                is_eq tm &&
                                (mem l evs && not(free_in l r)) || 
                                (mem r evs && not(free_in r l))
                        }) eqs
                    |> Choice.bind (Option.toChoiceWithError "find")

                let! l, r = dest_eq eq
                let v = 
                    if mem l evs && not(free_in l r)
                    then l
                    else r
                let cjs' = eq :: (subtract eqs [eq])
                let n = length evs - (1 + index v (rev evs))
                let! tm1 = mk_eq(bod, list_mk_conj cjs')
                let th1 = CONJ_ACI_RULE tm1
                let th2 = itlist MK_EXISTS evs th1
                let! tm2 = Choice.bind (rand << concl) th2 
                let th3 = funpow n BINDER_CONV (PUSH_EXISTS_CONV baseconv) tm2
                return! CONV_RULE (RAND_CONV UNWIND_CONV) (TRANS th2 th3)
            }
            |> Choice.bindError (fun _ -> REFL tm)
        UNWIND_CONV

    let MATCH_SEQPATTERN_CONV = 
        GEN_REWRITE_CONV I [pth_1]
        |> THENC <| RATOR_CONV
                       (LAND_CONV(BINDER_CONV(RATOR_CONV BETA_CONV
                                              |> THENC <| BETA_CONV)
                                  |> THENC <| PUSH_EXISTS_CONV(GEN_REWRITE_CONV I [pth_2]
                                                               |> THENC <| BREAK_CONS_CONV)
                                  |> THENC <| UNWIND_CONV
                                  |> THENC <| GEN_REWRITE_CONV DEPTH_CONV [EQT_INTRO
                                                                               (SPEC_ALL 
                                                                                    EQ_REFL)
                                                                           AND_CLAUSES]
                                  |> THENC <| GEN_REWRITE_CONV DEPTH_CONV [EXISTS_SIMP]))

    let MATCH_ONEPATTERN_CONV tm = 
        choice {
        let! th1 = GEN_REWRITE_CONV I [pth_3] tm
        let! tm1 = rand(concl th1)
        let! tm2 = lhand tm1
        let! tm3 = rand tm2
        let! tm' = body tm3
        let th2 = 
            (INSIDE_EXISTS_CONV(GEN_REWRITE_CONV I [pth_4]
                                |> THENC <| RAND_CONV BREAK_CONS_CONV)
             |> THENC <| UNWIND_CONV
             |> THENC <| GEN_REWRITE_CONV DEPTH_CONV [EQT_INTRO
                                                          (SPEC_ALL EQ_REFL)
                                                      AND_CLAUSES]
             |> THENC <| GEN_REWRITE_CONV DEPTH_CONV [EXISTS_SIMP]) tm'

        let conv tm = 
            choice {
                let! tm1 = Choice.bind (lhand << concl) th2
                if tm = tm1
                then return! th2
                else return! Choice.fail()
            }

        return!
            CONV_RULE 
                (RAND_CONV
                     (RATOR_CONV
                          (COMB2_CONV (RAND_CONV(BINDER_CONV conv)) 
                               (BINDER_CONV conv)))) (Choice.result th1)
        }

    let MATCH_SEQPATTERN_CONV_TRIV = 
        MATCH_SEQPATTERN_CONV
        |> THENC <| GEN_REWRITE_CONV I [COND_CLAUSES]

    let MATCH_SEQPATTERN_CONV_GEN = 
        MATCH_SEQPATTERN_CONV
        |> THENC <| GEN_REWRITE_CONV TRY_CONV [COND_CLAUSES]

    let MATCH_ONEPATTERN_CONV_TRIV = 
        MATCH_ONEPATTERN_CONV
        |> THENC <| GEN_REWRITE_CONV I [pth_5]

    let MATCH_ONEPATTERN_CONV_GEN = 
        MATCH_ONEPATTERN_CONV
        |> THENC <| GEN_REWRITE_CONV TRY_CONV [pth_0; pth_5]

    do_list (ignore << extend_basic_convs)
        ["MATCH_SEQPATTERN_CONV", 
         ((parse_term @"_MATCH x (_SEQPATTERN r s)"), MATCH_SEQPATTERN_CONV_TRIV)
         "FUN_SEQPATTERN_CONV", 
         ((parse_term @"_FUNCTION (_SEQPATTERN r s) x"), 
          MATCH_SEQPATTERN_CONV_TRIV)
         "MATCH_ONEPATTERN_CONV", 
         ((parse_term @"_MATCH x (\y z. P y z)"), MATCH_ONEPATTERN_CONV_TRIV)
         "FUN_ONEPATTERN_CONV", 
         ((parse_term @"_FUNCTION (\y z. P y z) x"), MATCH_ONEPATTERN_CONV_TRIV)]
    (CHANGED_CONV UNWIND_CONV, (MATCH_SEQPATTERN_CONV_GEN
                                |> ORELSEC <| MATCH_ONEPATTERN_CONV_GEN))

/// Eliminates universally quantified variables that are equated to something.
let FORALL_UNWIND_CONV = 
    let PUSH_FORALL_CONV = 
        let econv = REWR_CONV SWAP_FORALL_THM
        let rec conv bc tm = 
            choice { 
                return! (econv |> THENC <| BINDER_CONV(conv bc)) tm
            }
            |> Choice.bindError (fun _ -> bc tm)
        conv

    let baseconv = 
        GEN_REWRITE_CONV I [MESON [] 
                                (parse_term @"(!x. x = a /\ p x ==> q x) <=> (p a ==> q a)")
                            MESON [] 
                                (parse_term @"(!x. a = x /\ p x ==> q x) <=> (p a ==> q a)")
                            MESON [] (parse_term @"(!x. x = a ==> q x) <=> q a")
                            MESON [] (parse_term @"(!x. a = x ==> q x) <=> q a")]

    let rec FORALL_UNWIND_CONV tm = 
        choice { 
            let avs, bod = strip_forall tm
            let! ant, con = dest_imp bod
            let eqs = conjuncts ant
            let! eq = 
                Choice.List.tryFind (fun tm -> 
                    choice {
                        let! l, r = dest_eq tm
                        return
                            is_eq tm && 
                            (mem l avs && not(free_in l r)) || 
                            (mem r avs && not(free_in r l))
                    }) eqs
                |> Choice.bind (Option.toChoiceWithError "find")

            let! l, r = dest_eq eq
            let v = 
                if mem l avs && not(free_in l r) then l
                else r

            let cjs' = eq :: (subtract eqs [eq])
            let n = length avs - (1 + index v (rev avs))
            let! tm1 =  mk_eq(ant, list_mk_conj cjs')
            let th1 = CONJ_ACI_RULE tm1
            let! tm2 = rator bod
            let! tm3 = rator tm2
            let th2 = AP_THM (AP_TERM tm3 th1) con
            let th3 = itlist MK_FORALL avs th2
            let! tm4 = Choice.bind (rand << concl) th3
            let th4 = funpow n BINDER_CONV (PUSH_FORALL_CONV baseconv) tm4
            return! CONV_RULE (RAND_CONV FORALL_UNWIND_CONV) (TRANS th3 th4)
        }
        |> Choice.bindError (fun _ -> REFL tm)
    FORALL_UNWIND_CONV
