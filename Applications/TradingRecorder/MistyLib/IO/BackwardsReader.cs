using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Misty.Lib.IO.Xml
{
    /// <summary>
    /// http://codepaste.net/nkb9hp  by Ken Wiebke
    /// </summary>
    public class BackwardReader : IDisposable
    {
        private string path;
        private FileStream fs = null;

        public BackwardReader(string path)
        {
            this.path = path;
            fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            fs.Seek(0, SeekOrigin.End);
        }

        public string Readline()
        {
            byte[] line;
            byte[] text = new byte[1];
            long position = 0;
            int count;
            fs.Seek(0, SeekOrigin.Current);
            position = fs.Position;

            //do we have trailing \r\n?
            if (fs.Length > 1)
            {
                byte[] vagnretur = new byte[2];
                fs.Seek(-2, SeekOrigin.Current);
                fs.Read(vagnretur, 0, 2);
                if (ASCIIEncoding.ASCII.GetString(vagnretur).Equals("\r\n"))
                {
                    //move it back
                    fs.Seek(-2, SeekOrigin.Current);
                    position = fs.Position;
                }
            }

            while (fs.Position > 0)
            {
                text.Initialize();
                //read one char
                fs.Read(text, 0, 1);
                string asciiText = ASCIIEncoding.ASCII.GetString(text);
                //moveback to the charachter before
                fs.Seek(-2, SeekOrigin.Current);
                if (asciiText.Equals("\n"))
                {
                    fs.Read(text, 0, 1);
                    asciiText = ASCIIEncoding.ASCII.GetString(text);
                    if (asciiText.Equals("\r"))
                    {
                        fs.Seek(1, SeekOrigin.Current);
                        break;
                    }
                }
            }
            count = int.Parse((position - fs.Position).ToString());
            line = new byte[count];
            fs.Read(line, 0, count);
            fs.Seek(-count, SeekOrigin.Current);
            return ASCIIEncoding.ASCII.GetString(line);
        }

        public bool SOF
        {
            get
            {
                return fs.Position == 0;
            }

        }
        public void Close()
        {
            fs.Close();
        }


        #region IDisposable Members

        public void Dispose()
        {
            fs.Close();
        }

        #endregion
    }
}