/***

THE DISUNITY SYNTHESIZER TOOLKIT

Copyright 2020 Andrew Sorensen

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

***/

using UnityEngine;
using System;

namespace DST {
    public class Filter : AudioUnit
    {
        public enum FilterType {
            LOW,
            HIGH,
            BAND,
            NOTCH,
            PEAK,
            ALLPASS
        }

        public AudioUnit input;
        public AudioUnit cutoffInput;
        [Range(50.0F, 20000.0F)]
        public double cutoff = 800.0;

        [Range(0.0F, 0.995F)]
        public double resonance = 0.1;
        public FilterType filterType = FilterType.LOW;

        private double g = 0.0;
        private double k = 0.0;
        private double a1 = 0.0;
        private double a2 = 0.0;
        private double v1 = 0.0;
        private double v2 = 0.0;
        private double ic1eq = 0.0;
        private double ic2eq = 0.0;

        private float[] cutoffData;
        private double oldCutFrq = -1.0;
        private double oldRes = -1.0;
        private double SampleRate; 

        void Start() {
            SampleRate = AudioSettings.outputSampleRate;
            int buflength, numbufs;
            AudioSettings.GetDSPBufferSize(out buflength, out numbufs);
            var channels = AudioUnit.speakerModeToChannels(AudioSettings.speakerMode);
            cutoffData = new float[buflength * channels];
        }

        public override void ProcessAudio(float[] data, AudioUnit caller, long sampleNum, int channels) {
            if (cutoffInput != null) {
                cutoffInput.ProcessAudio(cutoffData, this, sampleNum, channels);
            }
            if (input == null) { throw new Exception("Must define an AudioInput"); }
            input.ProcessAudio(data, this, sampleNum, channels);

            for (int i=0;i<data.Length;i++ ) {
                var cutFrq = cutoffInput == null ? cutoff : (double) cutoffData[i] + cutoff;
                if (oldCutFrq != cutFrq || oldRes != resonance) {
                    cutFrq = cutFrq < 50.0 ? 50.0 : cutFrq;
                    oldCutFrq = cutFrq;
                    oldRes = resonance;
                    g = Math.Tan(3.141592 * (cutFrq / SampleRate));
                    k = 2.0 - (2.0 * resonance);
                    a1 = (1.0 / (1.0 + (g * (g + k))));
                    a2 = g * a1;
                }
                v1 = (a1 * ic1eq) + (a2 * (data[i] - ic2eq));
                v2 = ic2eq + (g * v1);
                ic1eq = (2.0 * v1) - ic1eq;
                ic2eq = (2.0 * v2) - ic2eq;
                switch (filterType) {
                    case FilterType.LOW: { data[i] = (float) v2; break; }
                    case FilterType.BAND: { data[i] = (float) v1; break; }
                    case FilterType.HIGH: { 
                        data[i] = (float) ((data[i] - (k * v1)) - v2);
                        break;
                    }
                    case FilterType.NOTCH: {
                        data[i] = (float) (data[i] - (k * v1));
                        break;
                    }
                    case FilterType.PEAK: {
                        data[i] = (float) ((data[i] - (k * v1)) - (2.0 * v2));
                        break;
                    }
                    case FilterType.ALLPASS: {
                        data[i] = (float) (data[i] - (2.0 * k * v1));
                        break;
                    }
                }
            }
            return;
        } 
    }
}
