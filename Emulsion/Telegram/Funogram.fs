module Emulsion.Telegram.Funogram

open Funogram
open Funogram.Bot
open Funogram.Api
open Funogram.Types

open System.Threading
open Emulsion
open Emulsion.Settings

let private processResultWithValue (result: Result<'a, ApiResponseError>) =
    match result with
    | Ok v -> Some v
    | Error e ->
        printfn "Error: %s" e.Description
        None

let private processResult (result: Result<'a, ApiResponseError>) =
    processResultWithValue result |> ignore

let internal convertMessage (message : Funogram.Types.Message) =
    let authorName =
        match message.From with
        | None -> "[UNKNOWN USER]"
        | Some user ->
            match user.Username with
            | Some username -> sprintf "@%s" username
            | None ->
                match user.LastName with
                | Some lastName -> sprintf "%s %s" user.FirstName lastName
                | None -> user.FirstName
    let text = Option.defaultValue "[DATA UNRECOGNIZED]" message.Text
    { author = authorName; text = text }

let private updateArrived onMessage (ctx : UpdateContext) =
    processCommands ctx [
        fun (msg, _) -> onMessage (convertMessage msg); true
    ] |> ignore

let internal prepareHtmlMessage { author = author; text = text } : string =
    sprintf "<b>%s</b>\n%s" (Html.escape author) (Html.escape text)

let send (settings : TelegramSettings) (OutgoingMessage content) : Async<unit> =
    let sendHtmlMessage groupId text =
        sendMessageBase groupId text (Some ParseMode.HTML) None None None None

    let groupId = Int (int64 settings.groupId)
    let message = prepareHtmlMessage content
    async {
        let! result = api settings.token (sendHtmlMessage groupId message)
        return processResult result
    }

let run (settings: TelegramSettings)
        (cancellationToken: CancellationToken)
        (onMessage: Emulsion.Message -> unit) : unit =
    // TODO[F]: Update Funogram and don't ignore the cancellation token here.
    let config = { defaultConfig with Token = settings.token }
    Bot.startBot config (updateArrived onMessage) None
