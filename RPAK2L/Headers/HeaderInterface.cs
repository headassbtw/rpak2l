using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using RPAK2L.Dialogs;

namespace RPAK2L.Headers
{
    public class HeaderInterface : ProgressableTask
    {
        public Dictionary<int,Dictionary<string,byte[]>> DdsHeaders;

        private int[] _resolutions = new int[]{2048};
        private string[] _compressions = new string[]{"DXT1","BC4U","BC5U","BC7U"};

        public override void Run()
        {
            TotalItems = _resolutions.Length * _compressions.Length;
            System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
            
            #if DEBUG || EXTREME_DEBUG
            Logger.Log.Debug("Embedded Resources:");
            foreach (string res in a.GetManifestResourceNames())
            {
                Console.WriteLine(res);
            }
            #endif
            DdsHeaders = new Dictionary<int,Dictionary<string,byte[]>>();
            for(int r = 0; r < _resolutions.Length; r++)
            {
                int resolution = _resolutions[r];
                if(!DdsHeaders.ContainsKey(resolution))
                    DdsHeaders.Add(resolution, new Dictionary<string,byte[]>());
                Dictionary<string,byte[]> Compressions = DdsHeaders[resolution];
                for(int c = 0; c < _compressions.Length; c++)
                {
                    string Compression = _compressions[c];
                    
                    //why the fuck is there an underscore before the res directory
                    using (Stream resFilestream = a.GetManifestResourceStream($"RPAK2L.Headers._{resolution}.DDS_{Compression}.bin"))
                    {
                        if (resFilestream != null)
                        {
                            Logger.Log.Debug($"Reading embedded DDS header for {resolution} {Compression}");
                            byte[] ba = new byte[resFilestream.Length];
                            resFilestream.Read(ba, 0, ba.Length);
                            resFilestream.Close();
                            if(!Compressions.ContainsKey(Compression))
                                Compressions.Add(Compression, ba);
                            IncrementProgress();
                            Thread.Sleep(10);
                        }
                        else
                        {
                            Logger.Log.Error($"No header found for \"RPAK2L.Headers.{resolution}.DDS_{Compression}.bin\"!");
                        }

                    }
                }
            }
        }

        public byte[] GetCustomRes(uint width, uint height, string compression)
        {
            byte[] tmp = DdsHeaders[2048][compression];

            var widthBytes = BitConverter.GetBytes(width);
            var heightBytes = BitConverter.GetBytes(height);

            tmp[12] = heightBytes[0];
            tmp[13] = heightBytes[1];
            tmp[14] = heightBytes[2];
            tmp[15] = heightBytes[3];
            tmp[16] = widthBytes[0];
            tmp[17] = widthBytes[1];
            tmp[18] = widthBytes[2];
            tmp[19] = widthBytes[3];
            

            return tmp;
        }
        
        
        public byte[] Get(int res, string compression)
        {
            return DdsHeaders.TryGetValue(res , out Dictionary<string,byte[]> Compressions) ? Compressions.TryGetValue(compression, out byte[] ba) ? ba : null : null;
        }
    }
}
