module Emulsion.Actors.Xmpp

open System

open Akka.Actor

open Emulsion
open Emulsion.Settings
open Emulsion.Xmpp

type XmppActor(core : IActorRef, xmpp : XmppModule) as this =
    inherit SyncTaskWatcher()
    do printfn "Starting XMPP actor..."
    do this.Receive<string>(this.OnMessage)
    let robot = xmpp.construct core

    override __.RunInTask() =
        printfn "Starting XMPP connection..."
        xmpp.run robot

    member private __.OnMessage(message : string) : unit =
        xmpp.send robot message

let spawn (xmpp : XmppModule)
          (factory : IActorRefFactory)
          (core : IActorRef)
          (name : string) =
    printfn "Spawning XMPP..."
    let props = Props.Create<XmppActor>(core, xmpp)
    factory.ActorOf(props, name)