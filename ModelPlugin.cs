using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;

using Tekla.Structures.Geometry3d;
using Tekla.Structures.Model;
using Tekla.Structures.Model.UI;
using Tekla.Structures.Plugins;

namespace Stanchions
{
    public class PluginData
    {
        [StructuresField("startOffset")] public double StartOffset;
        [StructuresField("endOffset")] public double EndOffset;
        [StructuresField("space_scheme")] public int SpaceScheme;
        [StructuresField("span")] public double Span;
        
        [StructuresField("soffset")] public double Offset;
        
        [StructuresField("sheight")] public double Height;
        [StructuresField("nosing_offset")] public double NosingOffset;
        [StructuresField("height_above_nosing")] public double HeightAboveNosing;
        [StructuresField("height_mode")] public int HeightMode;
        [StructuresField("level_start_mode")] public int LevelStartMode;
        [StructuresField("level_start_height")] public double LevelStartHeight;
        [StructuresField("level_end_mode")] public int LevelEndMode;
        [StructuresField("level_end_height")] public double LevelEndHeight;

        [StructuresField("Fsprof")] public string FirstProfile;
        [StructuresField("mat2")] public string FirstMaterial;
        [StructuresField("ClassFirst")] public string ClassFirst;
        [StructuresField("partname2")] public string FirstPartName;
        
        [StructuresField("Msprof")] public string MidProfile;
        [StructuresField("mat1")] public string MidMaterial;
        [StructuresField("ClassMid")] public string ClassMid;
        [StructuresField("partname1")] public string MidPartName;

        [StructuresField("Lsprof")] public string LastProfile;
        [StructuresField("mat3")] public string LastMaterial;
        [StructuresField("ClassLast")] public string ClassLast;
        [StructuresField("partname3")] public string LastPartName;
    }

    [Plugin("Stanchions")]
    [PluginUserInterface("Stanchions.MainWindow")]
    public class Stanchions : PluginBase
    {
        private readonly Model _Model;
        private readonly PluginData _Data;

        public Stanchions(PluginData data)
        {
            _Model = new Model();
            _Data = data;
        }

        public override List<InputDefinition> DefineInput()
        {
            var inputList = new List<InputDefinition>();
            var picker = new Picker();

            try
            {
                var pts = picker.PickPoints(Picker.PickPointEnum.PICK_TWO_POINTS, "Pick run start and end points");
                inputList.Add(new InputDefinition(pts));

                var parts = picker.PickObjects(Picker.PickObjectsEnum.PICK_N_PARTS, "Pick base parts and middle-click to finish");
                var identifiers = new ArrayList();
                var enumerator = parts.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current is Part part)
                    {
                        identifiers.Add(part.Identifier);
                    }
                }
                inputList.Add(new InputDefinition(identifiers));
            }
            catch { }

            return inputList;
        }

        public override bool Run(List<InputDefinition> Input)
        {
            try
            {
                ApplyDefaults();

                if (string.IsNullOrWhiteSpace(_Data.FirstProfile) || 
                    string.IsNullOrWhiteSpace(_Data.MidProfile) || 
                    string.IsNullOrWhiteSpace(_Data.LastProfile) ||
                    string.IsNullOrWhiteSpace(_Data.FirstMaterial) ||
                    string.IsNullOrWhiteSpace(_Data.MidMaterial) ||
                    string.IsNullOrWhiteSpace(_Data.LastMaterial))
                {
                    MessageBox.Show("One or more Profile or Material fields are empty. Please fill in all fields before applying.", "Missing Values", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                if (_Data.Height <= 0)
                {
                    MessageBox.Show("Post height must be greater than zero.", "Invalid Height", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                if (Input.Count < 2) return false;

                var pts = (ArrayList)Input[0].GetInput();
                var start = pts[0] as Point;
                var end = pts[1] as Point;

                var identifiers = (ArrayList)Input[1].GetInput();
                var parts = new List<Part>();
                foreach (Tekla.Structures.Identifier id in identifiers)
                {
                    var obj = _Model.SelectModelObject(id);
                    if (obj is Part p) parts.Add(p);
                }

                if (parts.Count == 0)
                {
                    MessageBox.Show("No base parts were selected. Posts cannot be placed.", "Missing Parts", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                var workPlaneHandler = _Model.GetWorkPlaneHandler();
                var currentPlane = workPlaneHandler.GetCurrentTransformationPlane();
                var toGlobal = currentPlane.TransformationMatrixToGlobal;
                start = toGlobal.Transform(start);
                end = toGlobal.Transform(end);
                workPlaneHandler.SetCurrentTransformationPlane(new TransformationPlane());

                int postsInserted = 0;
                try
                {
                    var builder = new StanchionsBuilder(_Model, _Data, start, end, parts);
                    postsInserted = builder.Build();
                }
                finally
                {
                    workPlaneHandler.SetCurrentTransformationPlane(currentPlane);
                }

                if (postsInserted == 0)
                {
                    MessageBox.Show("No posts were inserted. This usually means the ray-cast did not hit any of the selected base parts. Ensure your start and end points are directly above the selected stringers or slabs.", "No Posts Inserted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                _Model.CommitChanges();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return false;
            }
        }

        private void ApplyDefaults()
        {
            if (IsDefaultValue(_Data.StartOffset)) _Data.StartOffset = 50.8;
            if (IsDefaultValue(_Data.EndOffset)) _Data.EndOffset = 50.8;
            if (IsDefaultValue(_Data.SpaceScheme)) _Data.SpaceScheme = 1;
            if (IsDefaultValue(_Data.Span)) _Data.Span = 1219.2;
            
            if (IsDefaultValue(_Data.Offset)) _Data.Offset = 0.0;
            
            if (IsDefaultValue(_Data.Height)) _Data.Height = 1066.8;
            if (IsDefaultValue(_Data.NosingOffset)) _Data.NosingOffset = 0.0;
            if (IsDefaultValue(_Data.HeightAboveNosing)) _Data.HeightAboveNosing = 1100.0;
            if (IsDefaultValue(_Data.HeightMode)) _Data.HeightMode = 0;
            if (IsDefaultValue(_Data.LevelStartMode)) _Data.LevelStartMode = 0;
            if (IsDefaultValue(_Data.LevelStartHeight)) _Data.LevelStartHeight = 1100.0;
            if (IsDefaultValue(_Data.LevelEndMode)) _Data.LevelEndMode = 0;
            if (IsDefaultValue(_Data.LevelEndHeight)) _Data.LevelEndHeight = 1100.0;

            if (IsDefaultValue(_Data.FirstProfile)) _Data.FirstProfile = "PIPE1-1/4SCH40";
            if (IsDefaultValue(_Data.FirstMaterial)) _Data.FirstMaterial = "A53-GR.B";
            if (IsDefaultValue(_Data.ClassFirst)) _Data.ClassFirst = "11";
            if (IsDefaultValue(_Data.FirstPartName)) _Data.FirstPartName = "POST";

            if (IsDefaultValue(_Data.MidProfile)) _Data.MidProfile = "PIPE1-1/4SCH40";
            if (IsDefaultValue(_Data.MidMaterial)) _Data.MidMaterial = "A53-GR.B";
            if (IsDefaultValue(_Data.ClassMid)) _Data.ClassMid = "11";
            if (IsDefaultValue(_Data.MidPartName)) _Data.MidPartName = "POST";

            if (IsDefaultValue(_Data.LastProfile)) _Data.LastProfile = "PIPE1-1/4SCH40";
            if (IsDefaultValue(_Data.LastMaterial)) _Data.LastMaterial = "A53-GR.B";
            if (IsDefaultValue(_Data.ClassLast)) _Data.ClassLast = "11";
            if (IsDefaultValue(_Data.LastPartName)) _Data.LastPartName = "POST";
        }
    }
}

