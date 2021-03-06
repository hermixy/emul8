﻿using System;
using System.Collections.Generic;
using Emul8.Core;
using Microsoft.Scripting.Hosting;
using Emul8.Logging;
using Emul8.Time;
using Emul8.Exceptions;
using Emul8.Utilities;

namespace Emul8.UserInterface
{
    public static class MonitorExecutorExtensions
    {
        public static void ExecutePythonEvery(this Machine machine, string name, int milliseconds, string script)
        {
            var engine = new ExecutorPythonEngine(machine, script);
            var clockEntry = new ClockEntry(milliseconds, ClockEntry.FrequencyToRatio(machine, 1000), engine.Action);
            machine.ObtainClockSource().AddClockEntry(clockEntry);

            events.Add(machine, name, engine.Action);
            machine.StateChanged += (m, s) => UnregisterEvent(m, name, s);
        }

        public static void StopPythonExecution(this Machine machine, string name)
        {
            machine.ObtainClockSource().RemoveClockEntry(events.WithdrawAction(machine, name));
            events.Remove(machine, name);
        }

        private static void UnregisterEvent(Machine machine, String name, MachineStateChangedEventArgs state)
        {
            if(state.CurrentState == MachineStateChangedEventArgs.State.Disposed)
            {
                events.Remove(machine, name);
            }
        }

        private static readonly PeriodicEventsRegister events = new PeriodicEventsRegister();

        private sealed class ExecutorPythonEngine : PythonEngine
        {
            public ExecutorPythonEngine(Machine machine, string script)
            {
                Scope.SetVariable("machine", machine);
                Scope.SetVariable("self", machine);

                source = new Lazy<ScriptSource>(() => Engine.CreateScriptSourceFromString(script));
                Action = () => source.Value.Execute(Scope);
            }

            public Action Action { get; private set; }

            private Lazy<ScriptSource> source;
        }

        private sealed class PeriodicEventsRegister
        {
            public void Add(Machine machine, string name, Action action)
            {
                lock(periodicEvents)
                {
                    if(HasEvent(machine, name))
                    {
                        throw new RecoverableException("Periodic event '{0}' already registered in this machine.".FormatWith(name));
                    }
                    periodicEvents.Add(Tuple.Create(machine, name), action);
                }
            }

            public Action WithdrawAction(Machine machine, string name)
            {
                lock(periodicEvents)
                {
                    var action = periodicEvents[Tuple.Create(machine, name)];
                    Remove(machine, name);
                    return action;
                }
            }

            public void Remove(Machine machine, String name)
            {
                lock(periodicEvents)
                {
                    periodicEvents.Remove(Tuple.Create(machine, name));
                }
            }

            public bool HasEvent(Machine machine, String name)
            {
                lock(periodicEvents)
                {
                    return periodicEvents.ContainsKey(Tuple.Create(machine, name));
                }
            }

            private static readonly Dictionary<Tuple<Machine, string>, Action> periodicEvents = new Dictionary<Tuple<Machine, string>, Action>();
        }
    }
}
