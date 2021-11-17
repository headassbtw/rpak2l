using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace RPAK2L.Headers
{
    public static class HeaderInterface
    {
        public static Dictionary<int,Dictionary<string,byte[]>> DdsHeaders;

        private static int[] _resolutions = new int[]{2048};
        private static string[] _compressions = new string[]{"DXT1","BC4U","BC5U","BC7U"};

        public static void Init()
        {
            System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
            
            #if DEBUG
            Console.WriteLine("Embedded Resources:");
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
                            Console.WriteLine($"Reading embedded DDS header for {resolution} {Compression}");
                            byte[] ba = new byte[resFilestream.Length];
                            resFilestream.Read(ba, 0, ba.Length);
                            resFilestream.Close();
                            if(!Compressions.ContainsKey(Compression))
                                Compressions.Add(Compression, ba);
                        }
                        else
                        {
                            Console.WriteLine($"No header found for \"RPAK2L.Headers.{resolution}.DDS_{Compression}.bin\"!");
                        }

                    }
                }
            }
        }
        public static byte[] Get(int res, string compression)
        {
            return DdsHeaders[res][compression];
        }
    }
}