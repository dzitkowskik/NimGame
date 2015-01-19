module Game

open System
open System.Threading

val getMatches: Async<int list>

val makeAiMove : int list -> (int * int) * bool

val checkMove : heap:int * num:int -> matches: int list -> bool

val checkWin : int list -> bool

val applyMove : heap:int * num:int -> matches: int list -> int list

val getHeapsText : 'a list -> string