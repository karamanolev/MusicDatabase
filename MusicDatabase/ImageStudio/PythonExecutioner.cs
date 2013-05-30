using System;
using System.Linq;
using System.Text;
using System.Windows;
using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using NImageMagick;

namespace MusicDatabase.ImageStudio
{
    class PythonExecutioner
    {
        private Action<Image> saveCallback;
        private ScriptEngine engine;

        public PythonExecutioner(Action<Image> saveCallback)
        {
            this.engine = Python.CreateEngine();
            this.saveCallback = saveCallback;
        }

        private string GetFunctionCode(string rawCode)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(@"import clr
clr.AddReference(""NImageMagick"")
import NImageMagick
clr.ImportExtensions(NImageMagick.Extensions.ImageExtensions)

clr.AddReference(""MusicDatabase"")
from MusicDatabase.ImageStudio import ImageExtensions
clr.ImportExtensions(ImageExtensions)
");
            builder.AppendLine("def ProcessImage(i):");
            builder.AppendLine("  pass");
            foreach (string line in rawCode.Replace("\r", "").Split('\n'))
            {
                builder.Append("  ");
                builder.AppendLine(line);
            }
            return builder.ToString();
        }

        public void RunRawCode(InputImage[] images, string rawCode)
        {
            string pythonCode = this.GetFunctionCode(rawCode);

            dynamic scope = engine.CreateScope();

            try
            {
                engine.Execute(pythonCode, scope);
            }
            catch (SyntaxErrorException ex)
            {
                engine = null;
                scope = null;

                ExceptionOperations eo = engine.GetService<ExceptionOperations>();
                MessageBox.Show(eo.FormatException(ex));
            }

            if (engine == null || scope == null)
            {
                throw new Exception("Cannot create Python engine!");
            }

            ImageExtensions.SaveAction = saveCallback;

            foreach (var inputImage in images)
            {
                Image imageCopy = new Image(inputImage.SourceImage);

                try
                {
                    scope.ProcessImage(imageCopy);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error processing " + inputImage.SourceImage.Filename + ": " + ex.Message);
                }
            }
        }
    }
}
