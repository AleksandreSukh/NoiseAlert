using System;
using System.IO;
using System.Linq;
using System.Threading;
using CSCore;
using CSCore.Codecs.WAV;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using CSCore.Streams;

namespace NoiseAlert
{
    public class NoiseInfo
    {
        private readonly double _calibrateAdd;
        private readonly double _calibratescale;
        private readonly double _calibrateRange;

        public event EventHandler<NoiseInfoEventArgs> OnNoiseData;
        public event EventHandler<EventArgs> OnStopped;
        public event EventHandler<EventArgs> OnStarted;

        public NoiseInfo(double calibrateAdd, double calibratescale, double calibrateRange)
        {
            _calibrateAdd = calibrateAdd;
            _calibratescale = calibratescale;
            _calibrateRange = calibrateRange;
        }

        readonly object _stopLocker = new object();
        public void Start(TimeSpan time)
        {

            int sampleRate = 48000;
            int bitsPerSample = 24;

            MMDeviceCollection devices;
            while (!(devices = MMDeviceEnumerator.EnumerateDevices(DataFlow.Capture, DeviceState.Active)).Any())
            {
                Thread.Sleep(2000);
            }
            var device = devices.FirstOrDefault();

            //create a new soundIn instance
            using (WasapiCapture soundIn = new WasapiCapture())
            {
                soundIn.Device = device;
                //initialize the soundIn instance
                soundIn.Initialize();

                //create a SoundSource around the the soundIn instance
                SoundInSource soundInSource = new SoundInSource(soundIn) { FillWithZeros = false };

                //create a source, that converts the data provided by the soundInSource to any other format

                IWaveSource convertedSource = soundInSource
                    .ChangeSampleRate(sampleRate) // sample rate
                    .ToSampleSource()
                    .ToWaveSource(bitsPerSample); //bits per sample

                using (var stream = new MemoryStream())
                {
                    var readBufferLength = convertedSource.WaveFormat.BytesPerSecond / 2;
                    //channels...
                    using (convertedSource = convertedSource.ToStereo())
                    {
                        //create a new wavefile
                        using (WaveWriter waveWriter = new WaveWriter(stream, convertedSource.WaveFormat))
                        {
                            //register an event handler for the DataAvailable event of the soundInSource
                            soundInSource.DataAvailable += (s, e) =>
                            {
                                //read data from the converedSource
                                byte[] buffer = new byte[readBufferLength];
                                int read;

                                //keep reading as long as we still get some data
                                while ((read = convertedSource.Read(buffer, 0, buffer.Length)) > 0)
                                {

                                    var decibelsCalibrated = (int)Math.Round(GetSoundLevel(buffer, _calibrateAdd, _calibratescale, _calibrateRange));
                                    if (decibelsCalibrated < 0)
                                        decibelsCalibrated = 0;
                                    OnNoiseData?.Invoke(null, new NoiseInfoEventArgs() { Decibels = decibelsCalibrated });
                                    //write the read data to a file
                                    waveWriter.Write(buffer, 0, read);
                                }
                            };
                            soundIn.Stopped += (e, args) =>
                            {

                                OnStopped?.Invoke(null, null);
                                lock (_stopLocker)
                                    Monitor.PulseAll(_stopLocker);
                            };

                            var tm = new Timer(state => soundIn?.Stop(), null, time, time);

                            //start recording
                            soundIn.Start();
                            OnStarted?.Invoke(null, null);
                            Monitor.Enter(_stopLocker);
                            {
                                Monitor.PulseAll(_stopLocker);
                                Monitor.Wait(_stopLocker);
                            }
                            //stop recording
                            soundIn.Stop();
                        }
                    }
                }
            }
        }

        public static double GetSoundLevel(byte[] playBuffer, double calibrateAdd, double calibratescale, double calibrateRange)
        {
            try
            {
                double sum = 0;
                for (var i = 0; i < playBuffer.Length; i = i + 2)
                {
                    double sample = BitConverter.ToInt16(playBuffer, i) / 32768.0;
                    sum += (sample * sample);
                }

                double rms = Math.Sqrt(sum / (playBuffer.Length / 2));
                var soundLevel = 20 * Math.Log10(rms);
                soundLevel += calibrateAdd;
                soundLevel *= calibratescale;
                soundLevel -= calibrateAdd;
                if (soundLevel < 0) soundLevel = 0;
                if (soundLevel > calibrateRange) soundLevel = calibrateRange;
                return soundLevel / calibrateRange * 100;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }

    public class NoiseInfoEventArgs : EventArgs
    {
        public int Decibels { get; set; }
    }

}