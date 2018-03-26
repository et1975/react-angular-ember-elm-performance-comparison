(**
 - title: Todo MVC
 - tagline: The famous todo mvc ported from elm-todomvc
 - app-style: width:800px; margin:20px auto 50px auto;
 - intro: Todo MVC implemented to show a more realistic example.
*)
module Todomvc

open Fable.Core
open Fable.Import
open Elmish

let [<Literal>] ESC_KEY = 27.
let [<Literal>] ENTER_KEY = 13.
let [<Literal>] ALL_TODOS = "all"
let [<Literal>] ACTIVE_TODOS = "active"
let [<Literal>] COMPLETED_TODOS = "completed"

let styles: obj =
    JsInterop.importDefault "../css/index.css"

// Local storage interface
module S =
    let private STORAGE_KEY = "elmish-react-todomvc"
    let load<'T> (): 'T option =
        Browser.localStorage.getItem(STORAGE_KEY)
        |> unbox 
        |> Core.Option.map (JS.JSON.parse >> unbox<'T>)

    let save<'T> (model: 'T) =
        Browser.localStorage.setItem(STORAGE_KEY, JS.JSON.stringify model)


// MODEL
type Entry = {
    description : string
    completed : bool
    editing : bool
    id : int
}

// The full application state of our todo app.
type Model = {
    entries : Entry list
    field : string
    uid : int
    visibility : string
}

let emptyModel = 
    { entries = []
      visibility = ALL_TODOS
      field = ""
      uid = 0 }

let newEntry desc id =
  { description = desc
    completed = false
    editing = false
    id = id }


let init = function
  | Some savedModel -> savedModel, []
  | _ -> emptyModel, []


// UPDATE


(** Users of our app can trigger messages by clicking and typing. These
messages are fed into the `update` function as they occur, letting us react
to them.
*)
type Msg = 
    | UpdateField of string
    | EditingEntry of int*bool
    | UpdateEntry of int*string
    | Add
    | Delete of int
    | DeleteComplete
    | Check of int*bool
    | CheckAll of bool
    | ChangeVisibility of string
    | Failed of exn



// How we update our Model on a given Msg?
let update (model:Model) = function
    | Failed _ ->
        model, []

    | Add ->
        let xs = if System.String.IsNullOrEmpty model.field then
                    model.entries
                 else
                    model.entries @ [newEntry model.field model.uid]
        { model with
            uid = model.uid + 1
            field = ""
            entries = xs }, []

    | UpdateField str ->
      { model with field = str }, []

    | EditingEntry (id,isEditing) ->
        let updateEntry t =
          if t.id = id then { t with editing = isEditing } else t
        { model with entries = List.map updateEntry model.entries }, []

    | UpdateEntry (id,task) ->
        let updateEntry t =
          if t.id = id then { t with description = task } else t
        { model with entries = List.map updateEntry model.entries }, []

    | Delete id ->
        { model with entries = List.filter (fun t -> t.id <> id) model.entries }, []

    | DeleteComplete ->
        { model with entries = List.filter (fun t -> not t.completed) model.entries }, []

    | Check (id,isCompleted) ->
        let updateEntry t =
          if t.id = id then { t with completed = isCompleted } else t
        { model with entries = List.map updateEntry model.entries }, []

    | CheckAll isCompleted ->
        let updateEntry t = { t with completed = isCompleted }
        { model with entries = List.map updateEntry model.entries }, []

    | ChangeVisibility visibility ->
        { model with visibility = visibility }, []

let setStorage (model:Model) : Cmd<Msg> =
    Cmd.attemptFunc S.save model Failed

let updateWithStorage (model:Model) msg =
  let (newModel, cmds) = update model msg
  newModel, Cmd.batch [ setStorage newModel; cmds ]

// rendering views with React
module R = Fable.Helpers.React
open Fable.Core.JsInterop
open Fable.Helpers.React.Props
open Elmish.React
open Fable.Import.React

let internal onEnter dispatch = 
    fun msg ->
        function
        | (ev:React.KeyboardEvent) when ev.keyCode = ENTER_KEY ->
            ev.preventDefault() 
            dispatch msg
        | _ -> ()

let viewInput dispatch =
    let onInput (ev:React.FormEvent) = !!ev.target?value |> UpdateField |> dispatch
    let onEnter = onEnter dispatch Add |> OnKeyDown
    fun (model:string) ->
        R.header [ ClassName "header" ] [
            R.h1 [] [ R.str "todos" ]
            R.input [
                ClassName "new-todo"
                Placeholder "What needs to be done?"
                Value model
                onEnter
                OnInput onInput
                AutoFocus true
            ]
        ]

let internal classList classes =
    classes 
    |> List.fold (fun complete -> function | (name,true) -> complete + " " + name | _ -> complete) ""
    |> ClassName

let viewEntry dispatch =
    let onCheck todo _ =
        Check (todo.id,(not todo.completed)) |> dispatch
    let onEditing todo _ =
        EditingEntry (todo.id,true) |> dispatch    
    let onNotEditing todo _ =
        EditingEntry (todo.id,false) |> dispatch    
    let onInput todo (ev:FormEvent) =
        UpdateEntry (todo.id, !!ev.target?value) |> dispatch    
    let onEnter todo =    
        EditingEntry (todo.id,false) |> onEnter dispatch
    fun todo ->
      R.li
        [ classList [ ("completed", todo.completed); ("editing", todo.editing) ]]
        [ R.div
            [ ClassName "view" ]
            [ R.input
                [ ClassName "toggle"
                  Type "checkbox"
                  Checked todo.completed
                  OnChange (onCheck todo)]
              R.label
                [ OnDoubleClick (onEditing todo) ]
                [ unbox todo.description ]
              R.button
                [ ClassName "destroy"
                  OnClick (fun _-> Delete todo.id |> dispatch) ]
                []
            ]
          R.input
            [ ClassName "edit"
              Value todo.description
              Name "title"
              Id ("todo-" + (string todo.id))
              OnInput (onInput todo)
              OnBlur (onNotEditing todo)
              OnKeyDown (onEnter todo) ]
        ]

let viewEntries dispatch =
    let viewEntry = viewEntry dispatch
    let onCheckAll allCompleted = fun _ -> CheckAll (not allCompleted) |> dispatch
    fun visibility entries ->
        let isVisible todo =
            match visibility with
            | COMPLETED_TODOS -> todo.completed
            | ACTIVE_TODOS -> not todo.completed
            | _ -> true

        let allCompleted =
            List.forall (fun t -> t.completed) entries

        let cssVisibility =
            if List.isEmpty entries then "hidden" else "visible"


        R.section
          [ ClassName "main"
            Style [ Visibility cssVisibility ]]
          [ R.input
              [ ClassName "toggle-all"
                Type "checkbox"
                Name "toggle"
                Checked allCompleted
                OnChange (onCheckAll allCompleted)]
            R.label
              [ HtmlFor "toggle-all" ]
              [ unbox "Mark all as complete" ]
            R.ul 
              [ ClassName "todo-list" ]
              (entries
               |> List.filter isVisible  
               |> List.map viewEntry) ]

// VIEW CONTROLS AND FOOTER
let viewControls dispatch =
    let visibilitySwap uri visibility actualVisibility =
      R.li
        [ OnClick (fun _ -> ChangeVisibility visibility |> dispatch) ]
        [ R.a [ Href uri
                classList ["selected", visibility = actualVisibility] ]
              [ unbox visibility ] ]

    let viewControlsFilters visibility =
      R.ul
        [ ClassName "filters" ]
        [ visibilitySwap "#/" ALL_TODOS visibility 
          unbox " "
          visibilitySwap "#/active" ACTIVE_TODOS visibility 
          unbox " "
          visibilitySwap "#/completed" COMPLETED_TODOS visibility ]

    let viewControlsCount entriesLeft =
      let item =
          if entriesLeft = 1 then " item" else " items"

      R.span
          [ ClassName "todo-count" ]
          [ R.strong [] [ unbox (string entriesLeft) ]
            unbox (item + " left") ]

    let viewControlsClear entriesCompleted =
      R.button
        [ ClassName "clear-completed"
          Hidden (entriesCompleted = 0)
          OnClick (fun _ -> DeleteComplete |> dispatch)]
        [ unbox ("Clear completed (" + (string entriesCompleted) + ")") ]

    fun visibility entries ->
      let entriesCompleted =
          entries
          |> List.filter (fun t -> t.completed) 
          |> List.length

      let entriesLeft =
          List.length entries - entriesCompleted

      R.footer
          [ ClassName "footer"
            Hidden (List.isEmpty entries) ]
          [ viewControlsCount entriesLeft
            viewControlsFilters visibility 
            viewControlsClear entriesCompleted ]


let infoFooter =
  R.footer [ ClassName "info" ]
    [ R.p [] 
        [ unbox "Double-click to edit a todo" ]
      R.p []
        [ unbox "Ported from Elm by "
          R.a [ Href "https://github.com/et1975" ] [ unbox "Eugene Tolmachev" ]]
      R.p []
        [ unbox "Part of "
          R.a [ Href "http://todomvc.com" ] [ unbox "TodoMVC" ]]
    ]

let view dispatch =
    let viewInput = viewInput dispatch
    let viewEntries = viewEntries dispatch
    let viewControls = viewControls dispatch
    fun model ->
      R.div
        [ ClassName "todomvc-wrapper"]
        [ R.section
            [ ClassName "todoapp" ]
            [ viewInput model.field 
              viewEntries model.visibility model.entries 
              viewControls model.visibility model.entries ]
          infoFooter ]

// App
Program.mkProgram init update view
//|> Program.withConsoleTrace
|> Program.withReact "todoapp"
|> Program.runWith None
