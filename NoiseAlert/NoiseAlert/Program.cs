using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
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

    public class NoiseMonitor
    {
        public static void Start(Action<int, int> NoiseLevelLogger)
        {
            var thresholdedLevel = 53;
            var calibratescale = 15;
            var calibrateRange = 130;
            var calibrateAdd = 30;


            var circularBuffer = new ConcuurentCircularBuffer<double>(500);

            bool ShouldScream()
            {
                var currentBuffer = circularBuffer.Read();
                var avg = currentBuffer.Average();
                NoiseLevelLogger?.Invoke((int)avg, thresholdedLevel);

                var result = avg > thresholdedLevel;
                return result;
            }

            void UpdateState(double current)
            {
                //If already reached threshold stop adding 
                circularBuffer.Put(ShouldScream() ? (thresholdedLevel / 1.1) : current);
            }

            var th = new Thread(() =>
            {
                while (true)
                {
                    //TODO:Pass data as parameter
                    Player.Play("aaa.mp3", () => !ShouldScream());
                    Thread.Sleep(1000);
                }
            });

            th.Start();

            var noiseInfo = new NoiseInfo(calibrateAdd, calibratescale, calibrateRange);
            var noiseState = Observable.FromEventPattern<NoiseInfoEventArgs>(
                    h => noiseInfo.OnNoiseData += h,
                    h => noiseInfo.OnNoiseData -= h)
                .Select(e => e.EventArgs.Decibels);
            noiseState
                //.Buffer(50).Select(b => b.Average())
                .Subscribe(averageSoundLevel =>
                {
                    UpdateState(averageSoundLevel);
                });
            noiseInfo.OnStopped += (es, e) =>
            {
                Console.WriteLine("Stopped");

                noiseInfo.Start(TimeSpan.FromSeconds(20));
            };
            noiseInfo.Start(TimeSpan.FromSeconds(20));
            th.Join();
        }
    }
}
