using System;
using System.Collections.Generic;
using System.IO;

namespace BlobCopyRate
{
    class Program
    {
        static void Main(string[] args)
        {
            // System Document
            var span = new TimeSpan(1, 52, 50);

            var files = 112700;

            var seconds = span.TotalSeconds / files;

            // Family
            span = new TimeSpan(1, 36, 29);

            files = 50400;

            seconds = span.TotalSeconds / files;
        }
    }
}
