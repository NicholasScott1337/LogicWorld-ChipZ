using JimmysUnityUtilities;
using LogicAPI.Data.BuildingRequests;
using LogicWorld.BuildingManagement;
using LogicWorld.ClientCode.LabelAlignment;
using LogicWorld.ClientCode;
using LogicWorld.ClientCode.Resizing;
using LogicWorld.Interfaces;
using LogicWorld.Interfaces.Building;
using LogicWorld.Rendering.Components;
using LogicWorld.Rendering.Dynamics;
using LogicWorld.SharedCode.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using LogicWorld.References;
using LogicWorld.Rendering.Chunks;
using static LogicWorld.Building.WorldOutliner;
using LogicAPI.Data;
using LICC;

namespace Chipz.client
{
    public abstract class ResizableChip : ComponentClientCode<ResizableChip.IData>, IComponentClientCode, IResizableX, IResizableZ, IResizableCallbackReciever
    {
        // Converts a given string of digits and letters to their superscript equivalents
        public static string Superscript(string text)
        {
            // Define the superscript digits and letters
            const string SuperscriptDigits = "\u2070\u00b9\u00b2\u00b3\u2074\u2075\u2076\u2077\u2078\u2079";
            const string SuperscriptLetters = "\u1D2C\u1D2E\u1D9C\u1D30\u1D31\u1DA0\u1D33\u1D34\u1D35\u1D36\u1D37\u1D38\u1D39\u1D3A\u1D3C\u1D3E\uA7F4\u1D3F\u02E2\u1D40\u1D41\u2C7D\u1D42\u02E3\u02B8\u1DBB";

            // Convert the input text into an array of characters
            char[] inputChars = text.ToUpper().ToCharArray();

            // Initialize a StringBuilder to store the superscript text
            StringBuilder superscriptBuilder = new StringBuilder();

            // Iterate through each character in the input text
            for (int i = 0; i < inputChars.Length; i++)
            {
                // Check if the character is a digit (0-9)
                if (char.IsDigit(inputChars[i]))
                {
                    // Append the corresponding superscript digit to the StringBuilder
                    superscriptBuilder.Append(SuperscriptDigits[inputChars[i] - '0']);
                }
                else if (char.IsUpper(inputChars[i]))
                {
                    // Disregard the character 'Q'
                    if (inputChars[i] == 'Q')
                    {
                        continue;
                    }

                    // Append the corresponding superscript capital letter to the StringBuilder
                    superscriptBuilder.Append(SuperscriptLetters[inputChars[i] - 'A']);
                }
            }

            // Convert the modified StringBuilder back to a string
            string superscript = superscriptBuilder.ToString();

            return superscript;
        }

        public interface IData
        {
            int SizeX { get; set; }
            int SizeZ { get; set; }
        }

        public int SizeX { get { return Data.SizeX; } set { Data.SizeX = value; } }
        public int SizeZ { get { return Data.SizeZ; } set { Data.SizeZ = value; } }

        public int MinX => MinSizeX;
        public int MaxX => MaxSizeX;

        public int MinZ => MinSizeX;
        public int MaxZ => MaxSizeX;

        public float GridIntervalX => 1;
        public float GridIntervalZ => 1;

        internal bool Resizing = false;
        internal int LastSizeX = 0;
        internal int LastSizeZ = 0;
        internal Color24 ResizingColor = Color24.CyanBlueAzure;
        internal GpuColor ResizingColorOld;

        public abstract ColoredString ChipTitle { get; }
        public abstract int MinSizeX { get; }
        public abstract int MinSizeZ { get; }
        public abstract int MaxSizeX { get; }
        public abstract int MaxSizeZ { get; }

        public struct ColoredString { public Color24 Color; public string Text; }
        public virtual ColoredString GetInputPinShortLabel(int i)
        {
            return new ColoredString()
            {
                Color = Color24.MiddleGreen,
                Text = i.ToString()
            };
        }
        public virtual ColoredString GetInputPinLabel(int i)
        {
            return new ColoredString()
            {
                Color = Color24.LightGreen,
                Text = "INPUT"
            };
        }
        public virtual ColoredString GetOutputPinShortLabel(int i)
        {
            return new ColoredString()
            {
                Color = Color24.MiddleBlue,
                Text = i.ToString()
            };
        }
        public virtual ColoredString GetOutputPinLabel(int i)
        {
            return new ColoredString()
            {
                Color = Color24.LightBlue,
                Text = "OUTPUT"
            };
        }
        internal struct DataObj : LogicWorld.ClientCode.Label.IData
        {
            public string LabelText { get; set; }
            public Color24 LabelColor { get; set; }
            public bool LabelMonospace { get; set; }
            public float LabelFontSizeMax { get; set; }
            public LabelAlignmentHorizontal HorizontalAlignment { get; set; }
            public LabelAlignmentVertical VerticalAlignment { get; set; }
            public int SizeX { get; set; }
            public int SizeZ { get; set; }
        }

        internal List<LabelTextManager> TextManagers = new List<LabelTextManager>();

        protected override void SetDataDefaultValues()
        {
            Data.SizeX = MinX;
            Data.SizeZ = MinZ;
        }
        protected override void DataUpdate()
        {
            // This is where we need to update the size of our main block.
            base.SetBlockScale(0, new Vector3((float)SizeX + (Resizing ? 1 : 0), 1, (float)SizeZ + (Resizing ? 1 : 0)));
            base.SetBlockPosition(0, new Vector3((float)SizeX / 2 - 0.5f, 0, (float)SizeZ / 2 - 0.5f));

            if (!Resizing && (SizeX != this.InputCount / 2 || SizeZ != this.OutputCount / 2))
            {
                RequestPinCountChange(SizeX * 2, SizeZ * 2);
            }
        }
        protected override ChildPlacementInfo GenerateChildPlacementInfo()
        {
            List<FixedPlacingPoint> Points = new List<FixedPlacingPoint>();

            for (var i = 0; i < SizeX; i++)
            {
                for (var k = 0; k < SizeZ; k++)
                {
                    Points.Add(new FixedPlacingPoint()
                    {
                        Position = new Vector3(i, 1, k)
                    });
                }
            }

            // Generate placements for our size.
            return new ChildPlacementInfo()
            {
                Points = Points.ToArray()
            };
        }

        public void RequestPinCountChange(int SizeX, int SizeZ)
        {
            // This is where we need to change our pin count!
            BuildRequestManager.SendBuildRequestWithoutAddingToUndoStack(new BuildRequest_ChangeDynamicComponentPegCounts(this.Address, SizeX, SizeZ));
            // We also need to make our placements dirty, so we recalculate those
            MarkChildPlacementInfoDirty();
        }
        public void OnResizingBegin()
        {
            Resizing = true;

            ResizingColorOld = base.GetBlockEntity(0).Color;
            base.SetBlockColor(ResizingColor.ToGpuColor(), 0);
            base.SetBlockScale(0, new Vector3((float)SizeX + (Resizing ? 1 : 0), 1, (float)SizeZ + (Resizing ? 1 : 0)));
        }
        public void OnResizingEnd()
        {
            Resizing = false;

            base.SetBlockColor(ResizingColorOld, 0);

            RequestPinCountChange(SizeX * 2, SizeZ * 2);
        }

        internal GameObject CreateTextLabel(DataObj data)
        {
            GameObject GO = UnityEngine.Object.Instantiate<GameObject>(Prefabs.ComponentDecorations.LabelText);
            LabelTextManager TM = GO.GetComponent<LabelTextManager>();
            TextManagers.Add(TM);
            TM.DataUpdate(data);
            return GO;
        }
        protected override IList<IDecoration> GenerateDecorations()
        {
            int width = SizeX;
            int height = SizeZ;
            float halfWidth = width / 2f;
            float halfHeight = height / 2f;

            var decorations = new List<IDecoration>();

            for (var i = 0; i < width * 2; i++)
            {
                bool FirstSide = i < width;
                int iModulated = i % width;
                Vector3 PosToPlace = new Vector3(
                    iModulated + (FirstSide ? -0.5f : 0.5f),
                    0.91f,
                    0 + (FirstSide ? -0.7f : height - 0.3f) + (FirstSide ? -0.5f : 0.5f));
                Quaternion RotToSet = Quaternion.Euler(90f, (FirstSide ? 0f : 180f), 0f);

                // Add Input Pin #
                var data = GetInputPinShortLabel(i + 1);
                decorations.Add(new Decoration()
                {
                    LocalPosition = PosToPlace * 0.3f,
                    LocalRotation = RotToSet,
                    IncludeInModels = false,
                    DecorationObject = CreateTextLabel(new DataObj()
                    {
                        HorizontalAlignment = LabelAlignmentHorizontal.Center,
                        VerticalAlignment = LabelAlignmentVertical.Middle,
                        LabelColor = data.Color,
                        LabelMonospace = true,
                        LabelFontSizeMax = 0.8f,
                        LabelText = data.Text,
                        SizeX = 1,
                        SizeZ = 1
                    })
                });

                PosToPlace.x = iModulated + (FirstSide ? -0.5f : 0.5f);
                PosToPlace.y = 0.2f;
                PosToPlace.z = FirstSide ? -0.96f : height - 0.04f;
                RotToSet = Quaternion.Euler(0f, (FirstSide ? 0f : 180f), 0f);
                // Add Input Pin Text
                data = GetInputPinLabel(i + 1);
                decorations.Add(new Decoration()
                {
                    LocalPosition = PosToPlace * 0.3f,
                    LocalRotation = RotToSet,
                    IncludeInModels = false,
                    DecorationObject = CreateTextLabel(new DataObj()
                    {
                        HorizontalAlignment = LabelAlignmentHorizontal.Center,
                        VerticalAlignment = LabelAlignmentVertical.Middle,
                        LabelColor = data.Color,
                        LabelMonospace = true,
                        LabelFontSizeMax = 0.35f,
                        LabelText = data.Text,
                        SizeX = 1,
                        SizeZ = 1
                    })
                });
            }

            for (var i = 0; i < height * 2; i++)
            {
                bool FirstSide = i < height;
                int iModulated = i % height;
                Vector3 PosToPlace = new Vector3(
                    (FirstSide ? -0.7f : width - 0.3f) + (FirstSide ? -0.5f : 0.5f),
                    0.91f,
                    iModulated + (FirstSide ? 0.5f : -0.5f));
                Quaternion RotToSet = Quaternion.Euler(90f, (FirstSide ? 90f : -90f), 0f);
                // Add Output Pin #
                var data = GetOutputPinShortLabel(i + 1);
                decorations.Add(new Decoration()
                {
                    LocalPosition = PosToPlace * 0.3f,
                    LocalRotation = RotToSet,
                    IncludeInModels = false,
                    DecorationObject = CreateTextLabel(new DataObj()
                    {
                        HorizontalAlignment = LabelAlignmentHorizontal.Center,
                        VerticalAlignment = LabelAlignmentVertical.Middle,
                        LabelColor = data.Color,
                        LabelMonospace = true,
                        LabelFontSizeMax = 0.8f,
                        LabelText = data.Text,
                        SizeX = 1,
                        SizeZ = 1
                    })
                });

                PosToPlace.x = (FirstSide ? -0.96f : width - 0.04f);
                PosToPlace.y = 0.2f;
                PosToPlace.z = iModulated + (FirstSide ? 0.5f : -0.5f);
                RotToSet = Quaternion.Euler(0f, (FirstSide ? 90f : -90f), 0f);
                // Add Output Pin A Text
                data = GetOutputPinLabel(i + 1);
                decorations.Add(new Decoration()
                {
                    LocalPosition = PosToPlace * 0.3f,
                    LocalRotation = RotToSet,
                    IncludeInModels = false,
                    DecorationObject = CreateTextLabel(new DataObj()
                    {
                        HorizontalAlignment = LabelAlignmentHorizontal.Center,
                        VerticalAlignment = LabelAlignmentVertical.Middle,
                        LabelColor = data.Color,
                        LabelMonospace = true,
                        LabelFontSizeMax = 0.35f,
                        LabelText = data.Text,
                        SizeX = 1,
                        SizeZ = 1
                    })
                });
            }

            decorations.Add(new Decoration()
            {
                DecorationObject = CreateTextLabel(new DataObj()
                {
                    HorizontalAlignment = LabelAlignmentHorizontal.Center,
                    VerticalAlignment = LabelAlignmentVertical.Middle,
                    LabelColor = ChipTitle.Color,
                    LabelFontSizeMax = 1f,
                    LabelMonospace = true,
                    LabelText = ChipTitle.Text,
                    SizeX = 4,
                    SizeZ = 4
                }),
                LocalPosition = new Vector3(halfWidth - 2.5f, 1.01f, halfHeight + 1.5f) * 0.3f,
                LocalRotation = Quaternion.Euler(90, 90, 0),
                IncludeInModels = true
            });
            return decorations;
        }
    }

    public abstract class ResizableChipVariantInfo : PrefabVariantInfo
    {
        public override string ComponentTextID => ComponentID;

        private Color24 blockColor => ChipColor;
        private static Color24 fakePinColor = new Color24(25, 23, 23);

        public virtual Vector2Int BaseSize => new Vector2Int(MinSizeX, MinSizeZ);
        public abstract int MinSizeX { get; }
        public abstract int MinSizeZ { get; }
        public abstract Color24 ChipColor { get; }
        public abstract string ComponentID { get; }

        public override ComponentVariant GenerateVariant(PrefabVariantIdentifier identifier)
        {
            int inputCount = identifier.InputCount;
            int outputCount = identifier.OutputCount;
            float width = inputCount / 2f;
            float height = outputCount / 2f;
            float halfWidth = inputCount / 4f;
            float halfHeight = outputCount / 4f;


            PlacingRules placingRules = new PlacingRules
            {
                OffsetDimensions = new Vector2Int((int)width, (int)height),
                DefaultOffset = new Vector2Int((int)halfWidth, (int)halfHeight),
                GridPlacingDimensions = new Vector2Int((int)width, (int)height),
                AllowFineRotation = false,
                PrimaryGridPositions = new Vector2[]
                {
                    new Vector2(0.5f, 0.5f)
                }
            };


            var prefabBlocks = new List<Block>();
            var prefabInputs = new List<ComponentInput>();
            var prefabOutputs = new List<ComponentOutput>();

            prefabBlocks.Add(new Block()
            {
                RawColor = blockColor,
                Scale = new Vector3(width, 1, height),
                Position = new Vector3(halfWidth - 0.5f, 0, halfHeight - 0.5f)
            });

            for (var i = 0; i < width * 2; i++)
            {
                bool firstPass = i < width;
                int iModulated = i % (int)width;
                prefabInputs.Add(new ComponentInput()
                {
                    Length = 0.6f,
                    Position = new Vector3(iModulated, 0.6f, firstPass ? -0.7f : height - 0.3f),
                    Rotation = new Vector3(180f, 0, 0)
                });
                prefabBlocks.Add(new Block()
                {
                    RawColor = fakePinColor,
                    Scale = new Vector3(0.5f, 0.5f, 0.4f),
                    Position = new Vector3(iModulated, 0.4f, firstPass ? -0.7f : height - 0.3f)
                });
            }

            for (var i = 0; i < height * 2; i++)
            {
                bool firstPass = i < height;
                int iModulated = i % (int)height;
                prefabOutputs.Add(new ComponentOutput()
                {
                    StartOn = false,
                    Position = new Vector3(firstPass ? -0.5f : width - 0.5f, 0.65f, iModulated),
                    Rotation = new Vector3(0, 0, firstPass ? 90f : -90f)
                });
                prefabBlocks.Add(new Block()
                {
                    RawColor = fakePinColor,
                    Scale = new Vector3(0.332f, 0.6f, 0.332f),
                    Position = new Vector3(firstPass ? -0.7f : width - 0.3f, 0f, iModulated)
                });
            }

            return new ComponentVariant()
            {
                VariantPlacingRules = placingRules,
                VariantPrefab = new Prefab()
                {
                    Blocks = prefabBlocks.ToArray(),
                    Inputs = prefabInputs.ToArray(),
                    Outputs = prefabOutputs.ToArray()
                }
            };
        }

        public override PrefabVariantIdentifier GetDefaultComponentVariant()
        {
            return new PrefabVariantIdentifier(BaseSize.x * 2, BaseSize.y * 2);
        }
    }
}
