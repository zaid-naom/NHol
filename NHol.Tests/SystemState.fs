﻿(*

Copyright 2013 Jack Pappas

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

namespace Tests.NHol

/// Functions for manipulating the NHol system state to facilitate unit testing.
[<RequireQualifiedAccess>]
module internal SystemState =
    open System
    open System.Reflection
    open System.Runtime.CompilerServices


    /// Initializes NHol modules (F# modules), given a reference to the NHol assembly
    /// and a list of module names to initialize.
    let rec private initializeNHolModules (nholAssembly : Assembly) moduleNames =
        match moduleNames with
        | [] -> ()
        | moduleName :: moduleNames ->
            /// The full name (namespace + name) of the type containing the startup (initialization)
            /// code for the current NHol module (F# module).
            let moduleStartupTypeName = "<StartupCode$NHol>.$NHol." + moduleName

            // Try to get the F# module type from the map containing the types in the assembly.
            match nholAssembly.GetType (moduleStartupTypeName, false) |> Option.ofNull with
            | None ->
                // If the module's startup type couldn't be found, emit a message to the NUnit console/log.
                Console.WriteLine (
                    "Unable to initialize the '{0}' module because it could not be found in the NHol assembly. (ModuleStartupTypeName = {1})",
                    moduleName, moduleStartupTypeName)

            | Some moduleType ->
                // Emit a message to the NUnit console/log so we know which module we're initializing
                // in case something goes wrong during the initialization.
                Console.WriteLine ("Initializing the '{0}' module.", moduleName)

                // Execute the static constructor (class constructor) for the class
                // representing the specified F# module.
                RuntimeHelpers.RunClassConstructor moduleType.TypeHandle

                // Emit a message to the NUnit console/log stating that the module was initialized.
                Console.WriteLine ("Initialized the '{0}' module.", moduleName)

            // Initialize the remaining modules.
            initializeNHolModules nholAssembly moduleNames

    /// Initializes the NHol modules in the correct order.
    /// Used to avoid issues with out-of-order initialization when running unit tests.
    let initialize () =
        // Emit a message to the NUnit console/log to record that this function was called.
        Console.WriteLine "Initializing NHol modules prior to running unit tests."

        /// The NHol assembly.
        let nholAssembly = typeof<NHol.fusion.Hol_kernel.hol_type>.Assembly

        // First initialize the module itself.
        // This isn't really necessary, since C# normally doesn't add anything to the
        // module constructor, but it doesn't hurt to be sure.
        Console.WriteLine "Running the NHol module (.dll) constructor."
        RuntimeHelpers.RunModuleConstructor nholAssembly.ManifestModule.ModuleHandle
        
        // Now, initialize each of the F# modules in the NHol assembly by executing
        // the static constructor (.cctor) for the static class representing the module.
        // We use the same ordering as when loading the files into F# interactive,
        // so we should get the same results.
        initializeNHolModules nholAssembly
           ["lib";
            "fusion";
            "basics";
            "nets";
            "printer";
            "preterm";
            "parser";
            "equal";
            "bool";
            "drule";
            "tactics";
            "itab";
            "simp";
            "theorems";
            "ind_defs";
            "class";
            "trivia";
//            "canon";
//            "meson";
//            "quot";
//            "pair";
//            "nums";
//            "recursion";
//            "arith";
//            "wf";
//            "calc_num";
//            "normalizer";
//            "grobner";
//            "ind_types";
//            "lists";
//            "realax";
//            "calc_int";
//            "realarith";
//            "real";
//            "calc_rat";
//            "int";
//            "sets";
//            "iterate";
//            "cart";
//            "define";
//            "help";
//            "database";
            ]


#if SKIP_MODULE_INIT
#else
/// Global setup/teardown for test fixtures in this project.
[<Sealed>]
[<NUnit.Framework.SetUpFixture>]
type NHolTestSetupFixture () =
    /// Forces the modules in the NHol assembly to be initialized in the correct order
    /// if they haven't been already. NUnit calls this method once prior to each test run.
    [<NUnit.Framework.SetUp>]
    member __.NHolTestsInit () =
        // Initialize the NHol modules in the correct order before running any tests.
        SystemState.initialize ()
#endif


/// Functions for resetting mutable state values within each
/// NHol module (F# module) to facilitate unit testing.
[<RequireQualifiedAccess>]
module ModuleReset =
    open System
    open NHol


    /// Emits a message to the NUnit log/console before and after executing an action
    /// function to reset the mutable state within a module.
    let inline private logModuleResetAction (moduleName : string) action =
        // Emit a message to the NUnit console/log to record that we're resetting this module.
        Console.WriteLine ("Resetting the mutable state in the '{0}' module.", moduleName)

        // Execute the action to reset the module.
        action ()

        // Emit another message to the NUnit console/log to record we've finished resetting this module.
        Console.WriteLine ("Finished resetting the mutable state in the '{0}' module.", moduleName)
    
    /// Resets the mutable state values (if any) in the 'lib' module.
    let lib () =
        logModuleResetAction "lib" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'lib' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'fusion' module.
    let fusion () =
        logModuleResetAction "fusion" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'fusion' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'basics' module.
    let basics () =
        logModuleResetAction "basics" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'basics' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'nets' module.
    let nets () =
        logModuleResetAction "nets" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'nets' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'printer' module.
    let printer () =
        logModuleResetAction "printer" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'printer' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'preterm' module.
    let preterm () =
        logModuleResetAction "preterm" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'preterm' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'parser' module.
    let parser () =
        logModuleResetAction "parser" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'parser' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'equal' module.
    let equal () =
        logModuleResetAction "equal" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'equal' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'bool' module.
    let bool () =
        logModuleResetAction "bool" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'bool' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'drule' module.
    let drule () =
        logModuleResetAction "drule" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'drule' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'tactics' module.
    let tactics () =
        logModuleResetAction "tactics" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'tactics' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'itab' module.
    let itab () =
        logModuleResetAction "itab" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'itab' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'simp' module.
    let simp () =
        logModuleResetAction "simp" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'simp' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'theorems' module.
    let theorems () =
        logModuleResetAction "theorems" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'theorems' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'ind_defs' module.
    let ind_defs () =
        logModuleResetAction "ind_defs" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'ind_defs' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'class' module.
    let ``class`` () =
        logModuleResetAction "class" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'class' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'trivia' module.
    let trivia () =
        logModuleResetAction "trivia" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'trivia' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'canon' module.
    let canon () =
        logModuleResetAction "canon" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'canon' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'meson' module.
    let meson () =
        logModuleResetAction "meson" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'meson' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'quot' module.
    let quot () =
        logModuleResetAction "quot" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'quot' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'pair' module.
    let pair () =
        logModuleResetAction "pair" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'pair' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'nums' module.
    let nums () =
        logModuleResetAction "nums" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'nums' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'recursion' module.
    let recursion () =
        logModuleResetAction "recursion" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'recursion' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'arith' module.
    let arith () =
        logModuleResetAction "arith" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'arith' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'wf' module.
    let wf () =
        logModuleResetAction "wf" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'wf' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'calc_num' module.
    let calc_num () =
        logModuleResetAction "calc_num" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'calc_num' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'normalizer' module.
    let normalizer () =
        logModuleResetAction "normalizer" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'normalizer' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'grobner' module.
    let grobner () =
        logModuleResetAction "grobner" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'grobner' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'ind_types' module.
    let ind_types () =
        logModuleResetAction "ind_types" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'ind_types' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'lists' module.
    let lists () =
        logModuleResetAction "lists" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'lists' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'realax' module.
    let realax () =
        logModuleResetAction "realax" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'realax' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'calc_int' module.
    let calc_int () =
        logModuleResetAction "calc_int" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'calc_int' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'realarith' module.
    let realarith () =
        logModuleResetAction "realarith" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'realarith' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'real' module.
    let real () =
        logModuleResetAction "real" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'real' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'calc_rat' module.
    let calc_rat () =
        logModuleResetAction "calc_rat" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'calc_rat' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'int' module.
    let int () =
        logModuleResetAction "int" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'int' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'sets' module.
    let sets () =
        logModuleResetAction "sets" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'sets' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'iterate' module.
    let iterate () =
        logModuleResetAction "iterate" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'iterate' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'cart' module.
    let cart () =
        logModuleResetAction "cart" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'cart' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'define' module.
    let define () =
        logModuleResetAction "define" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'define' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'help' module.
    let help () =
        logModuleResetAction "help" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'help' module has not been implemented."

    /// Resets the mutable state values (if any) in the 'database' module.
    let database () =
        logModuleResetAction "database" <| fun () ->
            // TODO
            Console.WriteLine "Warning : The resetting action for the 'database' module has not been implemented."


/// Helper functions for implementing setup functions for fixtures and tests.
[<RequireQualifiedAccess>]
module SetupHelpers =
    open System

    /// Emits a message to the NUnit console/log stating that the test fixture setup function
    /// is empty (i.e., does nothing), given the name of the NHol module tested by the fixture.
    let emitEmptyTestFixtureSetupMessage (moduleName : string) =
        Console.WriteLine (
            "Info : The test fixture setup function for the '{0}' module is empty.", moduleName)

    /// Emits a message to the NUnit console/log stating that the mutable state will be reset
    /// in modules up to and including the specified module.
    let emitTestSetupModuleResetMessage (moduleName : string) =
        Console.WriteLine (
            "Resetting mutable state in modules up to and including the '{0}' module.", moduleName)
