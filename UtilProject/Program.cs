using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace UtilProject
{
    class Program
    {
        static void Main(string[] args)
        {
            //var f = new UtilProject.Formatter();
            //var lines = File.ReadLines("posts.sql");
            //var newLines = new List<string>();
            //foreach (var line in lines)
            //{
            //    newLines.Add(Formatter.Format(line));
            //}
            //Console.WriteLine("Done");
            //File.WriteAllLines("output.sql", newLines);
            List<string> storage = new List<string>();
            foreach(var file in Directory.GetFiles(@"C:\temp\rr"))
            {

                string txt = File.ReadAllText(file);
                Regex reg_exp = new Regex("[^a-zA-Z0-9]");
                txt = reg_exp.Replace(txt, " ");
                storage.AddRange(txt.Split(
                new char[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries));
                if (txt.Contains("sharing"))
                {
                    Console.WriteLine(file);
                }
            }

            // Use regular expressions to replace characters
            // that are not letters or numbers with spaces.

            // Split the text into words.
            string[] words = storage.ToArray();

            // Use LINQ to get the unique words.
            var word_query =
                (from string word in words
                 orderby word
                 select word).Distinct();

            // Display the result.
            string[] result = word_query.ToArray();
            File.WriteAllLines(@"C:\temp\out.txt", result);
        }
    }
}
