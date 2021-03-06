﻿(*

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

/// Tests for functions in the NHol.preterm module.
module Tests.NHol.preterm

open NUnit.Framework

#if SKIP_MODULE_INIT
#else
/// Performs setup for this test fixture.
/// Executed once prior to running any tests in this fixture.
[<TestFixtureSetUp>]
let fixtureSetup () : unit =
    // TEMP : Until any "real" code is added here (if ever), just emit a message
    // to the NUnit console/log so we'll know this function has been executed.
    SetupHelpers.emitEmptyTestFixtureSetupMessage "preterm"

/// Performs setup for each unit test.
/// Executed once prior to running each unit test in this fixture.
[<SetUp>]
let testSetup () : unit =
    // Emit a message to the NUnit console/log to record when this function is called.
    SetupHelpers.emitTestSetupModuleResetMessage "preterm"

    // Reset mutable state for this module and those proceeding it before running each unit test.
    // This helps avoid issues with mutable state which arise because unit tests can run in any order.
    ModuleReset.lib ()
    ModuleReset.fusion ()
    ModuleReset.basics ()
    ModuleReset.nets ()
    ModuleReset.printer ()
    ModuleReset.preterm ()
#endif

(* make_overloadable  tests *)

(* remove_interface  tests *)

(* reduce_interface  tests *)

(* override_interface  tests *)

(* overload_interface  tests *)

(* prioritize_overload  tests *)

(* new_type_abbrev  tests *)

(* remove_type_abbrev  tests *)

(* type_abbrevs  tests *)

(* hide_constant  tests *)

(* unhide_constant  tests *)

(* is_hidden  tests *)

(* dpty  tests *)

(* pretype_of_type  tests *)

(* preterm_of_term  tests *)

(* type_of_pretype  tests *)

(* term_of_preterm  tests *)

(* retypecheck  tests *)
