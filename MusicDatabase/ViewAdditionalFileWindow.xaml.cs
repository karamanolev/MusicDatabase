using System;
using System.IO;
using System.Linq;
using System.Text;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for ViewAdditionalFileWindow.xaml
    /// </summary>
    public partial class ViewAdditionalFileWindow : MusicDatabaseWindow
    {
        public override bool SaveWindowSettings
        {
            get { return false; }
        }

        public ReleaseAdditionalFile AdditionalFile
        {
            get { return null; }
            set
            {
                this.textType.Text = value.Type.ToString();
                this.textTitle.Text = Path.GetFileName(value.OriginalFilename);

                Encoding encoding;
                if (value.File[0] == 255 && value.File[1] == 254)
                {
                    encoding = Encoding.Unicode;
                }
                else
                {
                    encoding = Encoding.UTF8;
                }

                this.textContent.Text = encoding.GetString(value.File);
            }
        }

        public ViewAdditionalFileWindow()
        {
            InitializeComponent();
        }
    }
}
