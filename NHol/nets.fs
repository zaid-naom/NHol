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
/// Term nets: reasonably fast lookup based on term matchability.
module NHol.nets

open FSharp.Compatibility.OCaml
open FSharp.Compatibility.OCaml.Num

open NHol
open lib
open fusion
open fusion.Hol_kernel
open basics
#endif

(* ------------------------------------------------------------------------- *)
(* Term nets are a finitely branching tree structure; at each level we       *)
(* have a set of branches and a set of "values". Linearization is            *)
(* performed from the left of a combination; even in iterated                *)
(* combinations we look at the head first. This is probably fastest, and     *)
(* anyway it's useful to allow our restricted second order matches: if       *)
(* the head is a variable then then whole term is treated as a variable.     *)
(* ------------------------------------------------------------------------- *)

type term_label = 
    | Vnet                      (* variable (instantiable)   *)
    | Lcnet of (string * int)   (* local constant            *)
    | Cnet of (string * int)    (* constant                  *)
    | Lnet of int               (* lambda term (abstraction) *)
    override this.ToString() = 
        match this with
        | Vnet -> "Vnet"
        | Lcnet(s, i) -> "Lcnet (\"" + s + "\", " + i.ToString() + ")"
        | Cnet(s, i) -> "Cnet (\"" + s + "\", " + i.ToString() + ")"
        | Lnet i -> "Lnet (" + i.ToString() + ")"

type net<'a> = 
    | Netnode of (term_label * 'a net) list * 'a list
    override this.ToString() = 
        match this with
        | Netnode(tlList, aList) -> 
            "Netnode (" + tlList.ToString() + ", " + aList.ToString() + ")"

(* ------------------------------------------------------------------------- *)
(* The empty net.                                                            *)
(* ------------------------------------------------------------------------- *)

/// The empty net.
let empty_net = Netnode([], [])

(* ------------------------------------------------------------------------- *)
(* Insert a new element into a net.                                          *)
(* ------------------------------------------------------------------------- *)

/// Insert a new element into a net.
let enter = 
    let label_to_store lconsts tm = 
        let op, args = strip_comb tm
        if is_const op then Cnet(fst(Choice.get <| dest_const op), length args), args
        elif is_abs op then 
            let bv, bod = Choice.get <| dest_abs op
            let bod' = 
                if mem bv lconsts then Choice.get <| vsubst [genvar(Choice.get <| type_of bv), bv] bod
                else bod
            Lnet(length args), bod' :: args
        elif mem op lconsts then Lcnet(fst(Choice.get <| dest_var op), length args), args
        else Vnet, []
    let rec canon_eq x y = 
        try 
            Unchecked.compare x y = 0
        with
        | Failure _ -> false
    and canon_lt x y = 
        try 
            Unchecked.compare x y < 0
        with
        | Failure _ -> false
    let rec sinsert x l = 
        match l with
        | [] -> Choice.succeed [x]
        | hd :: tl -> 
            if canon_eq hd x then Choice.failwith "sinsert"
            elif canon_lt x hd then Choice.succeed (x :: l)
            else Choice.succeed (hd :: (Choice.get <| sinsert x tl))
    let set_insert x l = 
        try 
            Choice.get <|sinsert x l
        with
        | Failure "sinsert" -> l
    let rec net_update lconsts (elem, tms, Netnode(edges, tips)) = 
        match tms with
        | [] -> Netnode(edges, set_insert elem tips)
        | (tm :: rtms) -> 
            let label, ntms = label_to_store lconsts tm
            let child, others = 
                match (remove (fun (x, y) -> x = label) edges) with
                | Some x -> (snd ||>> I) x
                | None -> (empty_net, edges)
            let new_child = net_update lconsts (elem, ntms @ rtms, child)
            Netnode((label, new_child) :: others, tips)
    fun lconsts (tm, elem) net -> net_update lconsts (elem, [tm], net)

(* ------------------------------------------------------------------------- *)
(* Look up a term in a net and return possible matches.                      *)
(* ------------------------------------------------------------------------- *)

/// Look up a term in a net and return possible matches.
let lookup = 
    let label_for_lookup tm = 
        let op, args = strip_comb tm
        if is_const op then Cnet(fst(Choice.get <| dest_const op), length args), args
        elif is_abs op then Lnet(length args), (Choice.get <| body op) :: args
        else Lcnet(fst(Choice.get <| dest_var op), length args), args
    let rec follow(tms, Netnode(edges, tips)) = 
        match tms with
        | [] -> tips
        | (tm :: rtms) -> 
            let label, ntms = label_for_lookup tm
            let collection =
                // OPTIMIZE : Use Option.map and Option.fill to replace the 'match' statement.
                match assoc label edges with
                | None -> []
                | Some child ->
                    follow(ntms @ rtms, child)

            if label = Vnet then collection
            else
                // OPTIMIZE : Use Option.map and Option.fill to replace the 'match' statement.
                match assoc Vnet edges with
                | None -> collection
                | Some x ->
                    collection @ follow(rtms, x)

    fun tm net -> follow([tm], net)

(* ------------------------------------------------------------------------- *)
(* Function to merge two nets (code from Don Syme's hol-lite).               *)
(* ------------------------------------------------------------------------- *)

/// Function to merge two nets.
let merge_nets = 
    let rec canon_eq x y = 
        try 
            compare x y = 0
        with
        | Failure _ -> false
    and canon_lt x y = 
        try 
            compare x y < 0
        with
        | Failure _ -> false
    let rec set_merge l1 l2 = 
        if l1 = [] then l2
        elif l2 = [] then l1
        else 
            let h1 = hd l1
            let t1 = tl l1
            let h2 = hd l2
            let t2 = tl l2
            if canon_eq h1 h2 then h1 :: (set_merge t1 t2)
            elif canon_lt h1 h2 then h1 :: (set_merge t1 l2)
            else h2 :: (set_merge l1 t2)
    let rec merge_nets(Netnode(l1, data1), Netnode(l2, data2)) = 
        let add_node ((lab, net) as p) l = 
            match remove (fun (x, y) -> x = lab) l with
            | Some ((lab', net'), rest) ->
                (lab', merge_nets(net, net')) :: rest
            | None -> p :: l
        Netnode(itlist add_node l2 (itlist add_node l1 []), set_merge data1 data2)
    merge_nets
