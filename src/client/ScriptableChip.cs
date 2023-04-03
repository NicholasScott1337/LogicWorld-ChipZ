using Chipz.client;
using JimmysUnityUtilities;
using LogicWorld.Interfaces;
using LogicWorld.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chipz.client
{
    public class ScriptableChipVariantInfo : ResizableChipVariantInfo
    {
        public override int MinSizeX => 4;

        public override int MinSizeZ => 4;

        public override Color24 ChipColor => Color24.Black;

        public override string ComponentID => "CHZ.ScriptableChip";
    }
    public class ScriptableChip : ResizableChip
    {
        public override ColoredString ChipTitle => new ColoredString() { Color = Color24.White, Text = "ScriptableChip" };

        public override int MinSizeX => 4;

        public override int MinSizeZ => 4;

        public override int MaxSizeX => 20;

        public override int MaxSizeZ => 20;

        public override ColoredString GetInputPinLabel(int i)
        {
            return new ColoredString()
            {
                Color = Color24.MiddleGreen,
                Text = i.ToString() + Superscript("IN")
            };
        }
        public override ColoredString GetOutputPinLabel(int i)
        {
            return new ColoredString()
            {
                Color = Color24.MiddleBlue,
                Text = i.ToString() + Superscript("OUT")
            };
        }
    }
}
