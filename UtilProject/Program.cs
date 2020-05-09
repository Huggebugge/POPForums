using System;
using System.Collections.Generic;
using System.IO;

namespace UtilProject
{
    class Program
    {
        static void Main(string[] args)
        {
            var f = new UtilProject.Formatter();
            var lines = File.ReadLines("posts.sql");
            var newLines = new List<string>();
            foreach (var line in lines)
            {
                newLines.Add(Formatter.Format(line));
            }
            Console.WriteLine("Done");
            File.WriteAllLines("output.sql", newLines);
        }
    }
}
