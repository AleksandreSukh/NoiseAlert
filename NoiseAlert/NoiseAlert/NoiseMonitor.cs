using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;

namespace NoiseAlert
{
    public class NoiseMonitor
    {
        public static void Start(Action<int, int> noiseLevelLogger)
        {
            var thresholdedLevel = 53;
            var calibratescale = 15;
            var calibrateRange = 130;
            var calibrateAdd = 30;
            var lifeSpan = TimeSpan.FromSeconds(20);

            var circularBuffer = new ConcuurentCircularBuffer<double>(500);

            bool ShouldScream()
            {
                var currentBuffer = circularBuffer.Read();
                var avg = currentBuffer.Average();
                noiseLevelLogger?.Invoke((int)avg, thresholdedLevel);

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
                noiseInfo.Start(lifeSpan);
            };

            noiseInfo.Start(lifeSpan);
            th.Join();
        }
    }
}