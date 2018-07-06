using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NoiseAlert
{
    class Program
    {
        static void Main(string[] args)
        {
            NoiseMonitor.Start(LogToConsole);
        }


        private static void LogToConsole(int currentLevel, int thresholdedLevel)
        {
            Console.Clear();
            var levelString = CreateLongString('=', currentLevel);
            var thresholdString = CreateLongString('-', thresholdedLevel);
            Console.WriteLine(levelString);
            Console.WriteLine(thresholdString);


            //if (levelString.Length > thresholdedLevel)
            //{
            //    var newStringCharArray = levelString.ToCharArray();
            //    newStringCharArray[thresholdedLevel] = '|';
            //    var newString = new string(newStringCharArray);
            //    levelString = newString;
            //}

            //Console.WriteLine(levelString);
        }

        private static string CreateLongString(char fillWith, double currentLevel)
        {
            return new string(fillWith, Console.WindowWidth * (int)currentLevel / 100);
        }
    }
}
