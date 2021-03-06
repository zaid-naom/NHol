﻿(*

Copyright 1998 University of Cambridge
Copyright 1998-2007 John Harrison
Copyright 2013 Jack Pappas, Eric Taucher, Domenico Masini

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
/// Polynomial normalizer for rings and semirings.
module NHol.normalizer

open FSharp.Compatibility.OCaml
open FSharp.Compatibility.OCaml.Num

open ExtCore.Control

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
#endif

infof "Entering normalizer.fs"


/// Produces normalizer functions over a ring or even a semiring.
let SEMIRING_NORMALIZERS_CONV = 
    let SEMIRING_PTHS = 
        prove
            ((parse_term @"(!x:A y z. add x (add y z) = add (add x y) z) /\
     (!x y. add x y = add y x) /\
     (!x. add r0 x = x) /\
     (!x y z. mul x (mul y z) = mul (mul x y) z) /\
     (!x y. mul x y = mul y x) /\
     (!x. mul r1 x = x) /\
     (!x. mul r0 x = r0) /\
     (!x y z. mul x (add y z) = add (mul x y) (mul x z)) /\
     (!x. pwr x 0 = r1) /\
     (!x n. pwr x (SUC n) = mul x (pwr x n))
     ==> (mul r1 x = x) /\
         (add (mul a m) (mul b m) = mul (add a b) m) /\
         (add (mul a m) m = mul (add a r1) m) /\
         (add m (mul a m) = mul (add a r1) m) /\
         (add m m = mul (add r1 r1) m) /\
         (mul r0 m = r0) /\
         (add r0 a = a) /\
         (add a r0 = a) /\
         (mul a b = mul b a) /\
         (mul (add a b) c = add (mul a c) (mul b c)) /\
         (mul r0 a = r0) /\
         (mul a r0 = r0) /\
         (mul r1 a = a) /\
         (mul a r1 = a) /\
         (mul (mul lx ly) (mul rx ry) = mul (mul lx rx) (mul ly ry)) /\
         (mul (mul lx ly) (mul rx ry) = mul lx (mul ly (mul rx ry))) /\
         (mul (mul lx ly) (mul rx ry) = mul rx (mul (mul lx ly) ry)) /\
         (mul (mul lx ly) rx = mul (mul lx rx) ly) /\
         (mul (mul lx ly) rx = mul lx (mul ly rx)) /\
         (mul lx rx = mul rx lx) /\
         (mul lx (mul rx ry) = mul (mul lx rx) ry) /\
         (mul lx (mul rx ry) = mul rx (mul lx ry)) /\
         (add (add a b) (add c d) = add (add a c) (add b d)) /\
         (add (add a b) c = add a (add b c)) /\
         (add a (add c d) = add c (add a d)) /\
         (add (add a b) c = add (add a c) b) /\
         (add a c = add c a) /\
         (add a (add c d) = add (add a c) d) /\
         (mul (pwr x p) (pwr x q) = pwr x (p + q)) /\
         (mul x (pwr x q) = pwr x (SUC q)) /\
         (mul (pwr x q) x = pwr x (SUC q)) /\
         (mul x x = pwr x 2) /\
         (pwr (mul x y) q = mul (pwr x q) (pwr y q)) /\
         (pwr (pwr x p) q = pwr x (p * q)) /\
         (pwr x 0 = r1) /\
         (pwr x 1 = x) /\
         (mul x (add y z) = add (mul x y) (mul x z)) /\
         (pwr x (SUC q) = mul x (pwr x q))"), 
             STRIP_TAC
             |> THEN <| SUBGOAL_THEN (parse_term @"(!m:A n. add m n = add n m) /\
      (!m n p. add (add m n) p = add m (add n p)) /\
      (!m n p. add m (add n p) = add n (add m p)) /\
      (!x. add x r0 = x) /\
      (!m n. mul m n = mul n m) /\
      (!m n p. mul (mul m n) p = mul m (mul n p)) /\
      (!m n p. mul m (mul n p) = mul n (mul m p)) /\
      (!m n p. mul (add m n) p = add (mul m p) (mul n p)) /\
      (!x. mul x r1 = x) /\
      (!x. mul x r0 = r0)") MP_TAC
             |> THENL <| [ASM_MESON_TAC []
                          MAP_EVERY (fun t -> UNDISCH_THEN t (K ALL_TAC)) 
                              [(parse_term @"!x:A y z. add x (add y z) = add (add x y) z")
                               (parse_term @"!x:A y. add x y :A = add y x")
                               (parse_term @"!x:A y z. mul x (mul y z) = mul (mul x y) z")
                               (parse_term @"!x:A y. mul x y :A = mul y x")]
                          |> THEN <| STRIP_TAC]
             |> THEN <| ASM_REWRITE_TAC [num_CONV(parse_term @"2")
                                         num_CONV(parse_term @"1")]
             |> THEN  <| SUBGOAL_THEN 
                            (parse_term @"!m n:num x:A. pwr x (m + n) :A = mul (pwr x m) (pwr x n)") 
                            ASSUME_TAC
             |> THENL <| [GEN_TAC
                          |> THEN <| INDUCT_TAC
                          |> THEN <| ASM_REWRITE_TAC [ADD_CLAUSES]
                          ALL_TAC]
             |> THEN  <| SUBGOAL_THEN 
                            (parse_term @"!x:A y:A n:num. pwr (mul x y) n = mul (pwr x n) (pwr y n)") 
                            ASSUME_TAC
             |> THENL <| [GEN_TAC
                          |> THEN <| GEN_TAC
                          |> THEN <| INDUCT_TAC
                          |> THEN <| ASM_REWRITE_TAC []
                          ALL_TAC]
             |> THEN  <| SUBGOAL_THEN 
                            (parse_term @"!x:A m:num n. pwr (pwr x m) n = pwr x (m * n)") 
                            (fun th -> ASM_MESON_TAC [th])
             |> THEN <| GEN_TAC
             |> THEN <| GEN_TAC
             |> THEN <| INDUCT_TAC
             |> THEN <| ASM_REWRITE_TAC [MULT_CLAUSES])

    let true_tm = Choice.map concl TRUTH

    fun sth rth (is_semiring_constant, SEMIRING_ADD_CONV, SEMIRING_MUL_CONV, SEMIRING_POW_CONV) -> 
      choice {
        let! pthm_01, pthm_02, pthm_03, pthm_04, pthm_05, pthm_06, pthm_07, pthm_08, pthm_09, pthm_10, pthm_11, pthm_12, pthm_13, pthm_14, pthm_15, pthm_16, pthm_17, pthm_18, pthm_19, pthm_20, pthm_21, pthm_22, pthm_23, pthm_24, pthm_25, pthm_26, pthm_27, pthm_28, pthm_29, pthm_30, pthm_31, pthm_32, pthm_33, pthm_34, pthm_35, pthm_36, pthm_37, pthm_38 =
          choice {  
            let pthFuncs = CONJUNCTS(MATCH_MP SEMIRING_PTHS sth)
            match pthFuncs with
            | [pthm_01; pthm_02; pthm_03; pthm_04; pthm_05; pthm_06; pthm_07; pthm_08; pthm_09; pthm_10; pthm_11; pthm_12; pthm_13; pthm_14; pthm_15; pthm_16; pthm_17; pthm_18; pthm_19; pthm_20; pthm_21; pthm_22; pthm_23; pthm_24; pthm_25; pthm_26; pthm_27; pthm_28; pthm_29; pthm_30; pthm_31; pthm_32; pthm_33; pthm_34; pthm_35; pthm_36; pthm_37; pthm_38] ->
                return pthm_01, pthm_02, pthm_03, pthm_04, pthm_05, pthm_06, pthm_07, pthm_08, pthm_09, pthm_10, pthm_11, pthm_12, pthm_13, pthm_14, pthm_15, pthm_16, pthm_17, pthm_18, pthm_19, pthm_20, pthm_21, pthm_22, pthm_23, pthm_24, pthm_25, pthm_26, pthm_27, pthm_28, pthm_29, pthm_30, pthm_31, pthm_32, pthm_33, pthm_34, pthm_35, pthm_36, pthm_37, pthm_38
            | _ -> 
                return! Choice.failwith "pthFuncs: Unhandled case."
          }

        let! add_tm = (Choice.bind rator << Choice.bind rator << Choice.bind lhand << Choice.map concl) pthm_07
        let! mul_tm = (Choice.bind rator << Choice.bind rator << Choice.bind lhand << Choice.map concl)  pthm_13
        let! pow_tm = (Choice.bind rator << Choice.bind rator << Choice.bind rand << Choice.map concl)  pthm_32
        let! zero_tm = Choice.bind (rand << concl) pthm_06
        let! tm1 = Choice.bind (lhand << concl) pthm_14
        let! one_tm = rand tm1
        let! tm2 = Choice.bind (rand << concl) pthm_01
        let! ty = type_of tm2
        let p_tm = (parse_term @"p:num")
        let q_tm = (parse_term @"q:num")
        let zeron_tm = (parse_term @"0")
        let onen_tm = (parse_term @"1")
        let a_tm = mk_var("a", ty)
        let b_tm = mk_var("b", ty)
        let c_tm = mk_var("c", ty)
        let d_tm = mk_var("d", ty)
        let lx_tm = mk_var("lx", ty)
        let ly_tm = mk_var("ly", ty)
        let m_tm = mk_var("m", ty)
        let rx_tm = mk_var("rx", ty)
        let ry_tm = mk_var("ry", ty)
        let x_tm = mk_var("x", ty)
        let y_tm = mk_var("y", ty)
        let z_tm = mk_var("z", ty)
        let dest_add = dest_binop add_tm
        let dest_mul = dest_binop mul_tm

        let dest_pow tm = 
            choice {
                let! l, r = dest_binop pow_tm tm
                if is_numeral r then 
                    return l, r
                else 
                    return! Choice.failwith "dest_pow"
            }

        let is_add = is_binop add_tm
        let is_mul = is_binop mul_tm

        let! nthm_1, nthm_2, sub_tm, neg_tm, dest_sub, is_sub = 
            choice {
                let! true_tm = true_tm
                let! rtm = Choice.map concl rth
                if rtm = true_tm then 
                    return rth, rth, true_tm, true_tm, (fun t -> t, t), K false
                else 
                    let nthm_1 = SPEC x_tm (CONJUNCT1 rth)
                    let nthm_2 = SPECL [x_tm; y_tm] (CONJUNCT2 rth)
                    let! tm1 = (Choice.bind rator << Choice.bind lhand << Choice.map concl) nthm_2
                    let! sub_tm = rator tm1
                    let! tm2 = Choice.bind (lhand << concl) nthm_1
                    let! neg_tm = rator tm2
                    let dest_sub = Choice.get << dest_binop sub_tm
                    let is_sub = is_binop sub_tm
                    return (nthm_1, nthm_2, sub_tm, neg_tm, dest_sub, is_sub)
            }
        
        // It's weird to return a function value like this
        return
          fun variable_order -> 

            (* ------------------------------------------------------------------------- *)
            (* Conversion for "x^n * x^m", with either x^n = x and/or x^m = x possible.  *)
            (* Also deals with "const * const", but both terms must involve powers of    *)
            (* the same variable, or both be constants, or behaviour may be incorrect.   *)
            (* ------------------------------------------------------------------------- *)

            let POWVAR_MUL_CONV tm = 
                choice {
                    let! l, r = dest_mul tm
                    if is_semiring_constant l && is_semiring_constant r then 
                        return! SEMIRING_MUL_CONV tm
                    else
                      return! 
                        choice { 
                            let! lx, ln = dest_pow l
                            return!
                                choice { 
                                    let! rx, rn = dest_pow r
                                    let th1 = 
                                        INST [lx, x_tm;
                                              ln, p_tm;
                                              rn, q_tm] pthm_29
                                    let! tm1' = Choice.bind (rand << concl) th1
                                    let! tm1, tm2 = dest_comb tm1'
                                    return! TRANS th1 (AP_TERM tm1 (NUM_ADD_CONV tm2))
                                }
                                |> Choice.bindError (fun _ -> 
                                    choice {
                                        let th1 = 
                                            INST [lx, x_tm;
                                                  ln, q_tm] pthm_31
                                        let! tm1' = Choice.bind (rand << concl) th1
                                        let! tm1, tm2 = dest_comb tm1'
                                        return! TRANS th1 (AP_TERM tm1 (NUM_SUC_CONV tm2))
                                    })
                        }
                        |> Choice.bindError (fun _ -> 
                            choice { 
                                let! rx, rn = dest_pow r
                                let th1 = 
                                    INST [rx, x_tm;
                                          rn, q_tm] pthm_30
                                let! tm1' = Choice.bind (rand << concl) th1
                                let! tm1, tm2 = dest_comb tm1'
                                return! TRANS th1 (AP_TERM tm1 (NUM_SUC_CONV tm2))
                            }
                            |> Choice.bindError (fun _ -> INST [l, x_tm] pthm_32))
                }

            (* ------------------------------------------------------------------------- *)
            (* Remove "1 * m" from a monomial, and just leave m.                         *)
            (* ------------------------------------------------------------------------- *)

            let MONOMIAL_DEONE th = 
                choice { 
                    let! tm1 = Choice.bind (rand << concl) th
                    let! l, r = dest_mul tm1
                    if l = one_tm then 
                        return! TRANS th (INST [r, x_tm] pthm_01)
                    else 
                        return! th
                }
                |> Choice.bindError (fun _ -> th)

            (* ------------------------------------------------------------------------- *)
            (* Conversion for "(monomial)^n", where n is a numeral.                      *)
            (* ------------------------------------------------------------------------- *)

            let MONOMIAL_POW_CONV = 
                let rec MONOMIAL_POW tm bod ntm = 
                    choice {
                        if not(is_comb bod) then 
                            return! REFL tm
                        elif is_semiring_constant bod then 
                            return! SEMIRING_POW_CONV tm
                        else 
                            let! lop, r = dest_comb bod
                            if not(is_comb lop) then 
                                return! REFL tm
                            else 
                                let! op, l = dest_comb lop
                                if op = pow_tm && is_numeral r then 
                                    let th1 = 
                                        INST [l, x_tm;
                                              r, p_tm;
                                              ntm, q_tm] pthm_34
                                    let! tm1 = Choice.bind (rand << concl) th1
                                    let! l, r = dest_comb tm1
                                    return! TRANS th1 (AP_TERM l (NUM_MULT_CONV r))
                                elif op = mul_tm then 
                                    let th1 = 
                                        INST [l, x_tm;
                                              r, y_tm;
                                              ntm, q_tm] pthm_33
                                    let! tm1 = Choice.bind (rand << concl) th1
                                    let! xy, z = dest_comb tm1
                                    let! x, y = dest_comb xy
                                    let thl = MONOMIAL_POW y l ntm
                                    let thr = MONOMIAL_POW z r ntm
                                    return! TRANS th1 (MK_COMB(AP_TERM x thl, thr))
                                else 
                                    return! REFL tm
                    }

                fun tm -> 
                    choice {
                        let! lop, r = dest_comb tm
                        let! op, l = dest_comb lop
                    
                        if op <> pow_tm || not(is_numeral r) then 
                            return! Choice.failwith "MONOMIAL_POW_CONV"
                        elif r = zeron_tm then 
                            return! INST [l, x_tm] pthm_35
                        elif r = onen_tm then 
                            return! INST [l, x_tm] pthm_36
                        else 
                            return! MONOMIAL_DEONE(MONOMIAL_POW tm l r)
                    }

            (* ------------------------------------------------------------------------- *)
            (* Multiplication of canonical monomials.                                    *)
            (* ------------------------------------------------------------------------- *)

            let MONOMIAL_MUL_CONV = 
                let powvar tm = 
                    choice {
                        if is_semiring_constant tm then 
                            return one_tm
                        else
                            return!  
                                choice { 
                                    let! lop, r = dest_comb tm
                                    let! op, l = dest_comb lop
                                    if op = pow_tm && is_numeral r then 
                                        return l
                                    else 
                                        return! Choice.failwith ""
                                }
                                |> Choice.bindError (fun _ -> Choice.result tm)
                    }

                let vorder x y = 
                    if x = y then 0
                    elif x = one_tm then -1
                    elif y = one_tm then 1
                    elif variable_order x y then -1
                    else 1

                let rec MONOMIAL_MUL tm l r = 
                    choice { 
                      let! lx, ly = dest_mul l
                      let! vl = powvar lx
                      return!
                        choice { 
                            let! rx, ry = dest_mul r
                            let! vr = powvar rx
                            let ord = vorder vl vr
                            if ord = 0 then 
                                let th1 = 
                                    INST [lx, lx_tm;
                                          ly, ly_tm;
                                          rx, rx_tm;
                                          ry, ry_tm] pthm_15
                                let! tm1' = Choice.bind (rand << concl) th1
                                let! tm1, tm2 = dest_comb tm1'
                                let! tm3, tm4 = dest_comb tm1
                                let th2 = AP_THM (AP_TERM tm3 (POWVAR_MUL_CONV tm4)) tm2
                                let th3 = TRANS th1 th2
                                let! tm2' = Choice.bind (rand << concl) th3
                                let! tm5, tm6 = dest_comb tm2'
                                let! tm7, tm8 = dest_comb tm6
                                let! tm3' = rand tm7
                                let th4 = MONOMIAL_MUL tm6 tm3' tm8
                                return! TRANS th3 (AP_TERM tm5 th4)
                            else 
                                let th0 = 
                                    if ord < 0 then pthm_16
                                    else pthm_17
                                let th1 = 
                                    INST [lx, lx_tm;
                                          ly, ly_tm;
                                          rx, rx_tm;
                                          ry, ry_tm] th0
                                let! tm1' = Choice.bind (rand << concl) th1
                                let! tm1, tm2 = dest_comb tm1'
                                let! tm3, tm4 = dest_comb tm2
                                let! tm2' = rand tm3
                                return! TRANS th1 (AP_TERM tm1 (MONOMIAL_MUL tm2 tm2' tm4))
                        }
                        |> Choice.bindError (fun _ -> 
                            choice {
                                let! vr = powvar r
                                let ord = vorder vl vr
                                if ord = 0 then 
                                    let th1 = 
                                        INST [lx, lx_tm;
                                              ly, ly_tm;
                                              r, rx_tm] pthm_18
                                    let! tm1' = Choice.bind (rand << concl) th1
                                    let! tm1, tm2 = dest_comb tm1'
                                    let! tm3, tm4 = dest_comb tm1
                                    let th2 = AP_THM (AP_TERM tm3 (POWVAR_MUL_CONV tm4)) tm2
                                    return! TRANS th1 th2
                                elif ord < 0 then 
                                    let th1 = 
                                        INST [lx, lx_tm;
                                              ly, ly_tm;
                                              r, rx_tm] pthm_19
                                    let! tm1' = Choice.bind (rand << concl) th1
                                    let! tm1, tm2 = dest_comb tm1'
                                    let! tm3, tm4 = dest_comb tm2
                                    let! tm2' = rand tm3
                                    return! TRANS th1 (AP_TERM tm1 (MONOMIAL_MUL tm2 tm2' tm4))
                                else 
                                    return! INST [l, lx_tm; r, rx_tm] pthm_20})
                    }
                    |> Choice.bindError (fun _ -> 
                        let vl = powvar l
                        choice { 
                            let! vl = vl
                            let! rx, ry = dest_mul r
                            let! vr = powvar rx
                            let ord = vorder vl vr
                            if ord = 0 then 
                                let th1 = 
                                    INST [l, lx_tm;
                                          rx, rx_tm;
                                          ry, ry_tm] pthm_21
                                let! tm1' = Choice.bind (rand << concl) th1
                                let! tm1, tm2 = dest_comb tm1'
                                let! tm3, tm4 = dest_comb tm1
                                return! TRANS th1 (AP_THM (AP_TERM tm3 (POWVAR_MUL_CONV tm4)) tm2)
                            elif ord > 0 then 
                                let th1 = 
                                    INST [l, lx_tm;
                                          rx, rx_tm;
                                          ry, ry_tm] pthm_22
                                let! tm1' = Choice.bind (rand << concl) th1
                                let! tm1, tm2 = dest_comb tm1'
                                let! tm3, tm4 = dest_comb tm2
                                let! tm2' = rand tm3
                                return! TRANS th1 (AP_TERM tm1 (MONOMIAL_MUL tm2 tm2' tm4))
                            else 
                                return! REFL tm
                        }
                        |> Choice.bindError (fun _ -> 
                            choice {
                                let! vl = vl
                                let! vr = powvar r
                                let ord = vorder vl vr
                                if ord = 0 then 
                                    return! POWVAR_MUL_CONV tm
                                elif ord > 0 then 
                                    return! INST [l, lx_tm; r, rx_tm] pthm_20
                                else 
                                    return! REFL tm
                            }))

                fun tm -> 
                    choice {
                        let! l, r = dest_mul tm
                        return! MONOMIAL_DEONE(MONOMIAL_MUL tm l r)
                    }

            (* ------------------------------------------------------------------------- *)
            (* Multiplication by monomial of a polynomial.                               *)
            (* ------------------------------------------------------------------------- *)

            let POLYNOMIAL_MONOMIAL_MUL_CONV = 
                let rec PMM_CONV tm = 
                    let lr = dest_mul tm
                    choice { 
                        let! l, r = lr
                        let! y, z = dest_add r
                        let th1 = 
                            INST [l, x_tm; y, y_tm; z, z_tm] pthm_37
                        let! tm1' = Choice.bind (rand << concl) th1
                        let! tm1, tm2 = dest_comb tm1'
                        let! tm3, tm4 = dest_comb tm1
                        let th2 = MK_COMB (AP_TERM tm3 (MONOMIAL_MUL_CONV tm4), PMM_CONV tm2)
                        return! TRANS th1 th2
                    }
                    |> Choice.bindError (fun _ -> 
                        choice {
                            let! l, r = lr
                            return! MONOMIAL_MUL_CONV tm
                        })
                PMM_CONV

            (* ------------------------------------------------------------------------- *)
            (* Addition of two monomials identical except for constant multiples.        *)
            (* ------------------------------------------------------------------------- *)

            let MONOMIAL_ADD_CONV tm = 
                choice {
                    let! l, r = dest_add tm
                    if is_semiring_constant l && is_semiring_constant r then 
                        return! SEMIRING_ADD_CONV tm
                    else 
                        let th1 = 
                            choice {
                                let! l' = lhand l
                                if is_mul l && is_semiring_constant l' then 
                                    let! r' = lhand r
                                    if is_mul r && is_semiring_constant r' then
                                        let! tm1 = rand r
                                        return! INST [l', a_tm; r', b_tm; tm1, m_tm] pthm_02
                                    else 
                                        return! INST [l', a_tm; r, m_tm] pthm_03
                                else
                                    let! r' = lhand r
                                    if is_mul r && is_semiring_constant r' then 
                                        return! INST [r', a_tm; l, m_tm] pthm_04
                                    else 
                                        return! INST [r, m_tm] pthm_05
                            }

                        let! tm1' = Choice.bind (rand << concl) th1
                        let! tm1, tm2 = dest_comb tm1'
                        let! tm3, tm4 = dest_comb tm1
                        let th2 = AP_TERM tm3 (SEMIRING_ADD_CONV tm4)
                        let th3 = TRANS th1 (AP_THM th2 tm2)
                        let! tm5 = Choice.bind (rand << concl) th3
                        let! tm2' = lhand tm5
                        if tm2' = zero_tm then 
                            let! tm3' = rand tm5
                            return! TRANS th3 (INST [tm3', m_tm] pthm_06)
                        else 
                            return! MONOMIAL_DEONE th3
                }

            (* ------------------------------------------------------------------------- *)
            (* Ordering on monomials.                                                    *)
            (* ------------------------------------------------------------------------- *)

            let powervars tm = 
                let ptms = striplist (Choice.toOption << dest_mul) tm
                
                if is_semiring_constant(hd ptms) then 
                    tl ptms
                else 
                    ptms

            let dest_varpow tm = 
                choice { 
                    let! x, n = dest_pow tm
                    let! n' = dest_numeral n
                    return (x, n')
                }
                |> Choice.fill(
                    (tm, if is_semiring_constant tm then num_0 else num_1))

            let morder = 
                let rec lexorder l1 l2 = 
                    match (l1, l2) with
                    | [], [] -> 0
                    | vps, [] -> -1
                    | [], vps -> 1
                    | ((x1, n1) :: vs1), ((x2, n2) :: vs2) -> 
                        if variable_order x1 x2 then 1
                        elif variable_order x2 x1 then -1
                        elif n1 < n2 then -1
                        elif n2 < n1 then 1
                        else lexorder vs1 vs2

                fun tm1 tm2 -> 
                    let vdegs1 = map dest_varpow (powervars tm1)
                    let vdegs2 = map dest_varpow (powervars tm2)
                    let deg1 = itlist ((+) << snd) vdegs1 num_0
                    let deg2 = itlist ((+) << snd) vdegs2 num_0
                    
                    if deg1 < deg2 then -1
                    elif deg1 > deg2 then 1
                    else lexorder vdegs1 vdegs2

            (* ------------------------------------------------------------------------- *)
            (* Addition of two polynomials.                                              *)
            (* ------------------------------------------------------------------------- *)

            let POLYNOMIAL_ADD_CONV = 
                let DEZERO_RULE th = 
                    choice {
                        let! tm = Choice.bind (rand << concl) th
                        if not(is_add tm) then 
                            return! th
                        else 
                            let! lop, r = dest_comb tm
                            let! l = rand lop
                            if l = zero_tm then 
                                return! TRANS th (INST [r, a_tm] pthm_07)
                            elif r = zero_tm then 
                                return! TRANS th (INST [l, a_tm] pthm_08)
                            else 
                                return! th
                    }

                let rec PADD tm = 
                    choice {
                        let! l, r = dest_add tm
                        if l = zero_tm then 
                            return! INST [r, a_tm] pthm_07
                        elif r = zero_tm then 
                            return! INST [l, a_tm] pthm_08
                        elif is_add l then 
                            let! a, b = dest_add l
                            if is_add r then 
                                let! c, d = dest_add r
                                let ord = morder a c
                                if ord = 0 then 
                                    let th1 = 
                                        INST [a, a_tm;
                                              b, b_tm;
                                              c, c_tm;
                                              d, d_tm] pthm_23
                                    let! tm1' = Choice.bind (rand << concl) th1
                                    let! tm1, tm2 = dest_comb tm1'
                                    let! tm3, tm4 = dest_comb tm1
                                    let th2 = AP_TERM tm3 (MONOMIAL_ADD_CONV tm4)
                                    return! DEZERO_RULE(TRANS th1 (MK_COMB(th2, PADD tm2)))
                                else 
                                    let th1 = 
                                        if ord > 0 then 
                                            INST [a, a_tm;
                                                  b, b_tm;
                                                  r, c_tm] pthm_24
                                        else 
                                            INST [l, a_tm;
                                                  c, c_tm;
                                                  d, d_tm] pthm_25
                                    let! tm1' = Choice.bind (rand << concl) th1
                                    let! tm1, tm2 = dest_comb tm1'
                                    return! DEZERO_RULE(TRANS th1 (AP_TERM tm1 (PADD tm2)))
                            else 
                                let ord = morder a r
                                if ord = 0 then 
                                    let th1 = 
                                        INST [a, a_tm;
                                              b, b_tm;
                                              r, c_tm] pthm_26
                                    let! tm1' = Choice.bind (rand << concl) th1
                                    let! tm1, tm2 = dest_comb tm1'
                                    let! tm3, tm4 = dest_comb tm1
                                    let th2 = AP_THM (AP_TERM tm3 (MONOMIAL_ADD_CONV tm4)) tm2
                                    return! DEZERO_RULE(TRANS th1 th2)
                                elif ord > 0 then 
                                    let th1 = 
                                        INST [a, a_tm;
                                              b, b_tm;
                                              r, c_tm] pthm_24
                                    let! tm1' = Choice.bind (rand << concl) th1
                                    let! tm1, tm2 = dest_comb tm1'
                                    return! DEZERO_RULE(TRANS th1 (AP_TERM tm1 (PADD tm2)))
                                else 
                                    return! DEZERO_RULE(INST [l, a_tm; r, c_tm] pthm_27)
                        elif is_add r then 
                            let! c, d = dest_add r
                            let ord = morder l c
                            if ord = 0 then 
                                let th1 = 
                                    INST [l, a_tm;
                                          c, c_tm;
                                          d, d_tm] pthm_28
                                let! tm1' = Choice.bind (rand << concl) th1
                                let! tm1, tm2 = dest_comb tm1'
                                let! tm3, tm4 = dest_comb tm1
                                let th2 = AP_THM (AP_TERM tm3 (MONOMIAL_ADD_CONV tm4)) tm2
                                return! DEZERO_RULE(TRANS th1 th2)
                            elif ord > 0 then 
                                return! REFL tm
                            else 
                                let th1 = 
                                    INST [l, a_tm;
                                          c, c_tm;
                                          d, d_tm] pthm_25
                                let! tm1' = Choice.bind (rand << concl) th1
                                let! tm1, tm2 = dest_comb tm1'
                                return! DEZERO_RULE(TRANS th1 (AP_TERM tm1 (PADD tm2)))
                        else 
                            let ord = morder l r
                            if ord = 0 then 
                                return! MONOMIAL_ADD_CONV tm
                            elif ord > 0 then 
                                return! DEZERO_RULE(REFL tm)
                            else 
                                return! DEZERO_RULE(INST [l, a_tm; r, c_tm] pthm_27)
                    }

                PADD

            (* ------------------------------------------------------------------------- *)
            (* Multiplication of two polynomials.                                        *)
            (* ------------------------------------------------------------------------- *)

            let POLYNOMIAL_MUL_CONV = 
                let rec PMUL tm = 
                    choice {
                        let! l, r = dest_mul tm
                        if not(is_add l) then 
                            return! POLYNOMIAL_MONOMIAL_MUL_CONV tm
                        elif not(is_add r) then 
                            let th1 = 
                                INST [l, a_tm;
                                      r, b_tm] pthm_09
                            let! tm1' = Choice.bind (rand << concl) th1
                            return! TRANS th1 (POLYNOMIAL_MONOMIAL_MUL_CONV tm1')
                        else 
                            let! a, b = dest_add l
                            let th1 = 
                                INST [a, a_tm;
                                      b, b_tm;
                                      r, c_tm] pthm_10
                            let! tm1' = Choice.bind (rand << concl) th1
                            let! tm1, tm2 = dest_comb tm1'
                            let! tm3, tm4 = dest_comb tm1
                            let th2 = AP_TERM tm3 (POLYNOMIAL_MONOMIAL_MUL_CONV tm4)
                            let th3 = TRANS th1 (MK_COMB(th2, PMUL tm2))
                            let! tm2' = Choice.bind (rand << concl) th3
                            return! TRANS th3 (POLYNOMIAL_ADD_CONV tm2')
                    }

                fun tm -> 
                    choice {
                        let! l, r = dest_mul tm
                        if l = zero_tm then 
                            return! INST [r, a_tm] pthm_11
                        elif r = zero_tm then 
                            return! INST [l, a_tm] pthm_12
                        elif l = one_tm then 
                            return! INST [r, a_tm] pthm_13
                        elif r = one_tm then 
                            return! INST [l, a_tm] pthm_14
                        else 
                            return! PMUL tm
                    }

            (* ------------------------------------------------------------------------- *)
            (* Power of polynomial (optimized for the monomial and trivial cases).       *)
            (* ------------------------------------------------------------------------- *)

            let POLYNOMIAL_POW_CONV = 
                let rec PPOW tm = 
                    choice {
                        let! l, n = dest_pow tm
                    
                        if n = zeron_tm then 
                            return! INST [l, x_tm] pthm_35
                        elif n = onen_tm then 
                            return! INST [l, x_tm] pthm_36
                        else 
                            let th1 = num_CONV n
                            let! tm1' = Choice.bind (rand << concl) th1
                            let! tm2' = rand tm1'
                            let th2 = 
                                INST [l, x_tm;
                                      tm2', q_tm] pthm_38
                            let! tm3' = Choice.bind (rand << concl) th2
                            let! tm1, tm2 = dest_comb tm3'
                            let th3 = TRANS th2 (AP_TERM tm1 (PPOW tm2))
                            let! tm4' = rator tm
                            let th4 = TRANS (AP_TERM tm4' th1) th3
                            let! tm5' = Choice.bind (rand << concl) th4
                            return! TRANS th4 (POLYNOMIAL_MUL_CONV tm5')
                    }
                fun tm -> 
                    choice {
                        let! tm1 = lhand tm
                        if is_add tm1 then 
                            return! PPOW tm
                        else 
                            return! MONOMIAL_POW_CONV tm
                    }

            (* ------------------------------------------------------------------------- *)
            (* Negation.                                                                 *)
            (* ------------------------------------------------------------------------- *)

            let POLYNOMIAL_NEG_CONV = 
                fun tm -> 
                    choice {
                        let! l, r = dest_comb tm
                        if l <> neg_tm
                        then 
                            return! failwith "POLYNOMIAL_NEG_CONV"
                        else 
                            let th1 = INST [r, x_tm] nthm_1
                            let! tm1' = Choice.bind (rand << concl) th1
                            return! TRANS th1 (POLYNOMIAL_MONOMIAL_MUL_CONV tm1')
                    }

            (* ------------------------------------------------------------------------- *)
            (* Subtraction.                                                              *)
            (* ------------------------------------------------------------------------- *)

            let POLYNOMIAL_SUB_CONV = 
                fun tm -> 
                    choice {
                        let l, r = dest_sub tm
                    
                        let th1 = 
                            INST [l, x_tm;
                                  r, y_tm] nthm_2
                        let! tm1' = Choice.bind (rand << concl) th1
                        let! tm1, tm2 = dest_comb tm1'
                        let th2 = AP_TERM tm1 (POLYNOMIAL_MONOMIAL_MUL_CONV tm2)
                        let! tm2' = Choice.bind (rand << concl) th2
                        return! TRANS th1 (TRANS th2 (POLYNOMIAL_ADD_CONV tm2'))
                    }

            (* ------------------------------------------------------------------------- *)
            (* Conversion from HOL term.                                                 *)
            (* ------------------------------------------------------------------------- *)

            let rec POLYNOMIAL_CONV tm = 
                choice {
                    if not(is_comb tm) || is_semiring_constant tm then 
                        return! REFL tm
                    else 
                        let! lop, r = dest_comb tm
                        if lop = neg_tm then 
                            let th1 = AP_TERM lop (POLYNOMIAL_CONV r)
                            let! tm1' = Choice.bind (rand << concl) th1
                            return! TRANS th1 (POLYNOMIAL_NEG_CONV tm1')
                        elif not(is_comb lop) then 
                            return! REFL tm
                        else 
                            let! op, l = dest_comb lop
                            if op = pow_tm && is_numeral r then 
                                let th1 = AP_THM (AP_TERM op (POLYNOMIAL_CONV l)) r
                                let! tm1' = Choice.bind (rand << concl) th1
                                return! TRANS th1 (POLYNOMIAL_POW_CONV tm1')
                            elif op = add_tm || op = mul_tm || op = sub_tm then 
                                let th1 = MK_COMB(AP_TERM op (POLYNOMIAL_CONV l), POLYNOMIAL_CONV r)
                                let fn = 
                                    if op = add_tm then POLYNOMIAL_ADD_CONV
                                    elif op = mul_tm then POLYNOMIAL_MUL_CONV
                                    else POLYNOMIAL_SUB_CONV
                                let! tm1' = Choice.bind (rand << concl) th1
                                return! TRANS th1 (fn tm1')
                            else 
                                return! REFL tm
                }

            POLYNOMIAL_NEG_CONV, POLYNOMIAL_ADD_CONV, 
            POLYNOMIAL_SUB_CONV, POLYNOMIAL_MUL_CONV, 
            POLYNOMIAL_POW_CONV, POLYNOMIAL_CONV
      }
      // NOTE: we fill up this by 6 erroneous theorems
      |> Choice.fill (            
            let errorFunc = fun _ -> Choice.failwith "SEMIRING_NORMALIZERS_CONV"
            fun _ -> errorFunc, errorFunc, errorFunc, errorFunc, errorFunc, errorFunc)

(* ------------------------------------------------------------------------- *)
(* Instantiate it to the natural numbers.                                    *)
(* ------------------------------------------------------------------------- *)

/// Puts natural number expressions built using addition, multiplication and powers in canonical
/// polynomial form.
let NUM_NORMALIZE_CONV = 
    let sth = 
        prove
            ((parse_term @"(!x y z. x + (y + z) = (x + y) + z) /\
     (!x y. x + y = y + x) /\
     (!x. 0 + x = x) /\
     (!x y z. x * (y * z) = (x * y) * z) /\
     (!x y. x * y = y * x) /\
     (!x. 1 * x = x) /\
     (!x. 0 * x = 0) /\
     (!x y z. x * (y + z) = x * y + x * z) /\
     (!x. x EXP 0 = 1) /\
     (!x n. x EXP (SUC n) = x * x EXP n)"), 
             REWRITE_TAC [EXP; MULT_CLAUSES; ADD_CLAUSES; LEFT_ADD_DISTRIB]
             |> THEN <| REWRITE_TAC [ADD_AC; MULT_AC])
    let rth = TRUTH
    let is_semiring_constant = is_numeral
    let SEMIRING_ADD_CONV = NUM_ADD_CONV
    let SEMIRING_MUL_CONV = NUM_MULT_CONV
    let SEMIRING_POW_CONV = NUM_EXP_CONV
    let _, _, _, _, _, NUM_NORMALIZE_CONV = 
        SEMIRING_NORMALIZERS_CONV sth rth 
            (is_semiring_constant, SEMIRING_ADD_CONV, SEMIRING_MUL_CONV, 
             SEMIRING_POW_CONV) (<)
    NUM_NORMALIZE_CONV
