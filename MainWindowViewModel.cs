using System.ComponentModel;
using Tekla.Structures.Dialog;
using TD = Tekla.Structures.Datatype;

namespace Stanchions
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private TD.Distance startOffset = new TD.Distance(50.8);
        private TD.Distance endOffset = new TD.Distance(50.8);
        private int spaceScheme = 1;
        private TD.Distance span = new TD.Distance(1219.2);
        
        private TD.Distance offset = new TD.Distance(0);
        
        private TD.Distance height = new TD.Distance(1066.8);
        private TD.Distance nosingOffset = new TD.Distance(0);
        private int heightMode = 0;
        private int levelStartMode = 0;
        private TD.Distance levelStartHeight = new TD.Distance(1100);
        private int levelEndMode = 0;
        private TD.Distance levelEndHeight = new TD.Distance(1100);

        private string firstProfile = "PIPE1-1/4SCH40";
        private string firstMaterial = "A53-GR.B";
        private string classFirst = "11";
        private string firstPartName = "POST";

        private string midProfile = "PIPE1-1/4SCH40";
        private string midMaterial = "A53-GR.B";
        private string classMid = "11";
        private string midPartName = "POST";
        private int midProfileLink = 1;

        private string lastProfile = "PIPE1-1/4SCH40";
        private string lastMaterial = "A53-GR.B";
        private string classLast = "11";
        private string lastPartName = "POST";
        private int lastProfileLink = 1;


        [StructuresDialog("startOffset", typeof(TD.Distance))]
        public TD.Distance StartOffset { get { return startOffset; } set { startOffset = value; OnPropertyChanged("StartOffset"); } }

        [StructuresDialog("endOffset", typeof(TD.Distance))]
        public TD.Distance EndOffset { get { return endOffset; } set { endOffset = value; OnPropertyChanged("EndOffset"); } }

        [StructuresDialog("space_scheme", typeof(TD.Integer))]
        public int SpaceScheme { get { return spaceScheme; } set { spaceScheme = value; OnPropertyChanged("SpaceScheme"); } }

        [StructuresDialog("span", typeof(TD.Distance))]
        public TD.Distance Span { get { return span; } set { span = value; OnPropertyChanged("Span"); } }

        
        

        [StructuresDialog("soffset", typeof(TD.Distance))]
        public TD.Distance Offset { get { return offset; } set { offset = value; OnPropertyChanged("Offset"); } }

        
        

        [StructuresDialog("sheight", typeof(TD.Distance))]
        public TD.Distance Height { get { return height; } set { height = value; OnPropertyChanged("Height"); } }

        [StructuresDialog("nosing_offset", typeof(TD.Distance))]
        public TD.Distance NosingOffset { get { return nosingOffset; } set { nosingOffset = value; OnPropertyChanged("NosingOffset"); } }

        [StructuresDialog("height_mode", typeof(TD.Integer))]
        public int HeightMode { get { return heightMode; } set { heightMode = value; OnPropertyChanged("HeightMode"); } }

        [StructuresDialog("level_start_mode", typeof(TD.Integer))]
        public int LevelStartMode { get { return levelStartMode; } set { levelStartMode = value; OnPropertyChanged("LevelStartMode"); } }

        [StructuresDialog("level_start_height", typeof(TD.Distance))]
        public TD.Distance LevelStartHeight { get { return levelStartHeight; } set { levelStartHeight = value; OnPropertyChanged("LevelStartHeight"); } }

        [StructuresDialog("level_end_mode", typeof(TD.Integer))]
        public int LevelEndMode { get { return levelEndMode; } set { levelEndMode = value; OnPropertyChanged("LevelEndMode"); } }

        [StructuresDialog("level_end_height", typeof(TD.Distance))]
        public TD.Distance LevelEndHeight { get { return levelEndHeight; } set { levelEndHeight = value; OnPropertyChanged("LevelEndHeight"); } }


        [StructuresDialog("Fsprof", typeof(TD.String))]
        public string FirstProfile { get { return firstProfile; } set { firstProfile = value; OnPropertyChanged("FirstProfile"); } }

        [StructuresDialog("mat2", typeof(TD.String))]
        public string FirstMaterial { get { return firstMaterial; } set { firstMaterial = value; OnPropertyChanged("FirstMaterial"); } }

        [StructuresDialog("ClassFirst", typeof(TD.String))]
        public string ClassFirst { get { return classFirst; } set { classFirst = value; OnPropertyChanged("ClassFirst"); } }

        [StructuresDialog("partname2", typeof(TD.String))]
        public string FirstPartName { get { return firstPartName; } set { firstPartName = value; OnPropertyChanged("FirstPartName"); } }


        [StructuresDialog("Msprof", typeof(TD.String))]
        public string MidProfile { get { return midProfile; } set { midProfile = value; OnPropertyChanged("MidProfile"); } }

        [StructuresDialog("mat1", typeof(TD.String))]
        public string MidMaterial { get { return midMaterial; } set { midMaterial = value; OnPropertyChanged("MidMaterial"); } }

        [StructuresDialog("ClassMid", typeof(TD.String))]
        public string ClassMid { get { return classMid; } set { classMid = value; OnPropertyChanged("ClassMid"); } }

        [StructuresDialog("partname1", typeof(TD.String))]
        public string MidPartName { get { return midPartName; } set { midPartName = value; OnPropertyChanged("MidPartName"); } }

        [StructuresDialog("mprofile_link", typeof(TD.Integer))]
        public int MidProfileLink { get { return midProfileLink; } set { midProfileLink = value; OnPropertyChanged("MidProfileLink"); } }


        [StructuresDialog("Lsprof", typeof(TD.String))]
        public string LastProfile { get { return lastProfile; } set { lastProfile = value; OnPropertyChanged("LastProfile"); } }

        [StructuresDialog("mat3", typeof(TD.String))]
        public string LastMaterial { get { return lastMaterial; } set { lastMaterial = value; OnPropertyChanged("LastMaterial"); } }

        [StructuresDialog("ClassLast", typeof(TD.String))]
        public string ClassLast { get { return classLast; } set { classLast = value; OnPropertyChanged("ClassLast"); } }

        [StructuresDialog("partname3", typeof(TD.String))]
        public string LastPartName { get { return lastPartName; } set { lastPartName = value; OnPropertyChanged("LastPartName"); } }

        [StructuresDialog("lprofile_link", typeof(TD.Integer))]
        public int LastProfileLink { get { return lastProfileLink; } set { lastProfileLink = value; OnPropertyChanged("LastProfileLink"); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

