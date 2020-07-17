using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.Commands;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;

namespace EmasaRhinoPlugin
{
    [Guid("2914f630-548e-4870-9bb7-2020ca83fd2f"), CommandStyle(Style.None)]
    public class EMSBogusCommand : Command
    {
        public override string EnglishName => "EMSBogusCommand";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            RhinoApp.WriteLine($"EMSInterfaceRhinoPlugin is available! Built at {GetLinkerTimestampUtc(Assembly.GetExecutingAssembly())}");
            return Result.Success;
        }

        public static DateTime GetLinkerTimestampUtc(Assembly assembly)
        {
            var location = assembly.Location;
            return GetLinkerTimestampUtc(location);
        }

        public static DateTime GetLinkerTimestampUtc(string filePath)
        {
            const int peHeaderOffset = 60;
            const int linkerTimestampOffset = 8;
            var bytes = new byte[2048];

            using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                file.Read(bytes, 0, bytes.Length);
            }

            var headerPos = BitConverter.ToInt32(bytes, peHeaderOffset);
            var secondsSince1970 = BitConverter.ToInt32(bytes, headerPos + linkerTimestampOffset);
            var dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return dt.AddSeconds(secondsSince1970);
        }
    }
}
