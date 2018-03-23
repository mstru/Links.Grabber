namespace Test.Client
{
    class Settings
    {
        public Settings(MainForm _form)
        {
            MainFormInstance = _form;
        }

        public MainForm MainFormInstance
        {
            get;
            set;
        }
        public int Depth
        {
            get;
            set;
        }
        public bool ResolveRelativePaths
        {
            get;
            set;
        }
        public string ReportDestinationFolder
        {
            get;
            set;
        }
        public string ReportId
        {
            get;
            set;
        }
        public string URL
        {
            get;
            set;
        }
        public bool IsValid
        {
            get;
            set;
        }
        public string UserName
        {
            get;
            set;
        }
        public string Password
        {
            get;
            set;
        }       
    }
}
