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
        [StructuresField("mprofile_link")] public int MidProfileLink;

        [StructuresField("Lsprof")] public string LastProfile;
        [StructuresField("mat3")] public string LastMaterial;
        [StructuresField("ClassLast")] public string ClassLast;
        [StructuresField("partname3")] public string LastPartName;
        [StructuresField("lprofile_link")] public int LastProfileLink;
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
                var pts = picker.PickPoints(Picker.PickPointEnum.PICK_POLYGON, "Pick path points and middle-click to finish");
                inputList.Add(new InputDefinition(pts));
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

                if (Input.Count < 1) return false;

                var pts = (ArrayList)Input[0].GetInput();
                var pathPoints = new List<Point>();
                
                var workPlaneHandler = _Model.GetWorkPlaneHandler();
                var currentPlane = workPlaneHandler.GetCurrentTransformationPlane();
                var toGlobal = currentPlane.TransformationMatrixToGlobal;
                
                foreach (Point pt in pts)
                {
                    pathPoints.Add(toGlobal.Transform(pt));
                }
                
                workPlaneHandler.SetCurrentTransformationPlane(new TransformationPlane());

                int postsInserted = 0;
                try
                {
                    var builder = new StanchionsBuilder(_Model, _Data, pathPoints);
                    postsInserted = builder.Build();
                }
                finally
                {
                    workPlaneHandler.SetCurrentTransformationPlane(currentPlane);
                }

                return postsInserted > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            if (IsDefaultValue(_Data.HeightMode)) _Data.HeightMode = 0;
            
            if (IsDefaultValue(_Data.FirstProfile)) _Data.FirstProfile = "PIPE1-1/4SCH40";
            if (IsDefaultValue(_Data.FirstMaterial)) _Data.FirstMaterial = "A53-GR.B";
            if (IsDefaultValue(_Data.ClassFirst)) _Data.ClassFirst = "11";
            if (IsDefaultValue(_Data.FirstPartName)) _Data.FirstPartName = "POST";
            
            if (IsDefaultValue(_Data.MidProfile)) _Data.MidProfile = "PIPE1-1/4SCH40";
            if (IsDefaultValue(_Data.MidMaterial)) _Data.MidMaterial = "A53-GR.B";
            if (IsDefaultValue(_Data.ClassMid)) _Data.ClassMid = "11";
            if (IsDefaultValue(_Data.MidPartName)) _Data.MidPartName = "POST";
            if (IsDefaultValue(_Data.MidProfileLink)) _Data.MidProfileLink = 1;

            if (IsDefaultValue(_Data.LastProfile)) _Data.LastProfile = "PIPE1-1/4SCH40";
            if (IsDefaultValue(_Data.LastMaterial)) _Data.LastMaterial = "A53-GR.B";
            if (IsDefaultValue(_Data.ClassLast)) _Data.ClassLast = "11";
            if (IsDefaultValue(_Data.LastPartName)) _Data.LastPartName = "POST";
            if (IsDefaultValue(_Data.LastProfileLink)) _Data.LastProfileLink = 1;
        }
    }
}
