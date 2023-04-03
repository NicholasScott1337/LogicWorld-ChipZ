using LICC;
using LogicAPI.Server.Components;
using LogicScript;
using LogicScript.Data;
using LogicScript.Parsing;
using LogicWorld.Building.Overhaul;
using LogicWorld.SharedCode.Modding;
using LogicWorld.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security;

namespace Chipz.server
{
    public class ScriptableChip : LogicComponent,  IUpdatableMachine, IMachine
    {
        public string testScript = @"// Top-level declarations
input'4 inputDataA
input'4 inputDataB
output'8 outputData

assign outputData = ((inputDataB << 4) | inputDataA)'8";

        int IMachine.InputCount => base.Inputs.Count;

        int IMachine.OutputCount => base.Outputs.Count;

        private Script OurScript;

        internal Script LoadScript(string source)
        {
            var s_ = Script.Parse(source);

            if (s_.Script == null)
            {
                var x = LConsole.BeginLine()
                    .Write("[ScriptableChip]", CColor.Blue)
                    .Write(" (" + this.Address + ")", CColor.Green);
                foreach (var err in s_.Errors)
                {
                    x.Write("\t[" + err.Severity.ToString() + "] ", err.Severity == Severity.Error ? CColor.Red : CColor.Yellow);
                    x.Write("(" + err.Span.Start.Line + "-" + err.Span.End.Line + ")");
                    x.WriteLine(err.Message);
                }
            }
            return s_.Script;
        }

        protected override void Initialize()
        {
            OurScript = LoadScript(testScript);
        }

        internal bool hasRunStartup = false;
        protected override void DoLogicUpdate()
        {
            if (Component.ChildCount > 0)
            {
                foreach (var x in Component.EnumerateChildren())
                {
                    LConsole.WriteLine(x.GetType());
                }
            }
            if (OurScript != null)
            {
                LConsole.WriteLine("Running script");
                OurScript.Run(this, !hasRunStartup, false);
                if (!hasRunStartup)
                    hasRunStartup = true;
            }
        }

        private void UpdateScript(string newScript)
        {
            OurScript = LoadScript(newScript);
            hasRunStartup = false;
        }

        private BitsValue[] Registers = Array.Empty<BitsValue>();
        void IMachine.AllocateRegisters(int count)
        {
            if (Registers.Length != count)
            {
                Array.Resize<BitsValue>(ref Registers, count);
            }
        }

        void IMachine.Print(string msg)
        {
            LConsole.BeginLine()
                .Write("[ScriptableChip]", CColor.Blue)
                .Write(" (" + this.Address + ") ", CColor.Green)
                .Write(msg, CColor.Yellow).End();
        }

        void IMachine.ReadInput(Span<bool> values)
        {
            LConsole.WriteLine("reading from inputs");
            for (int i = 0; i < Inputs.Count; i++)
            {
                values[i] = Inputs[i].On;
            }
        }

        BitsValue IMachine.ReadRegister(int index)
        {
            return Registers[index];
        }

        void IMachine.WriteOutput(int startIndex, Span<bool> value)
        {
            for (int i = startIndex; i < Outputs.Count && i < startIndex + value.Length; i++)
            {
                Outputs[i].On = value[i];
            }
        }

        void IMachine.WriteRegister(int index, BitsValue value)
        {
            Registers[index] = value;
        }

        void IUpdatableMachine.QueueUpdate()
        {
            QueueLogicUpdate();
        }
    }
}
