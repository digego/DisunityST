/***

THE DISUNITY SYNTHESIZER TOOLKIT

Copyright 2020 Andrew Sorensen

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

***/

using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace DST {
    public enum WAVEFORM { SINE, SAW, PULSE }; 

    public class Oscillator : AudioUnit
    {
        public AudioUnit frqInput;
        public AudioUnit pwInput;

        public WAVEFORM waveForm;
        public double gain = 0.2;
        private double phase = 0.0; // phase between 0.0-twopi (for sin)
        private double phase1 = 0.0; // phase betwee 0.0-1.0 (for saw, pulse)
        [Range(0.0F, 4.0F)]
        public double frqOffset = 1.0;
        private double SampleRate = 0.0;
        private int Channels = 0;
        private double TWOPI = 2.0 * 3.141592;
        private double Omega;
        private double[] optionalOscState = { 0.0, 0.0, 0.0, 0.0, 0.0 };
        private float[] frqData;
        private float[] pwData;
  
        //polynomial bandlimited step functions
        private static float polyBlep(double pos, double inc, double height, Boolean risingEdge) {
            double output = 0.0;
            double t = 0.0;
            // leftside of discontinuity
            if (pos > (1.0 - inc)) {
                t = (pos - 1.0) / inc;
                output = height * ((t * t) + (2.0 * t) + 1.0);
            } else if(pos < inc) { // rightside of discontinunity
                t = (pos / inc);
                output = height * ((2.0 * t) - (t * t) - 1.0);
            }
            if (!risingEdge) {
                output = -1.0 * output;
            }
            return (float) output;
        }

        private float calcSin(double frq) {
            if (phase > TWOPI) { phase -= TWOPI; }
            var output = System.Math.Sin(phase); 
            phase += frq * Omega;
            return (float) output;
        }

        // for calcSaw phase is between 0.0 & 1.0
        private float calcSaw(double frq) {
            var inc = frq / SampleRate;
            if (phase1 > 1.0) { phase1 = 0.0; } // if switching from sine
            if (inc > 0.0 && phase1 >= 1.0) { phase1 -= 1.0; }
            else if (inc < 0.0 && phase1 <= 0.0) { phase1 += 1.0; } // neg frqs!
            var blep = polyBlep(phase1, Math.Abs(inc), 1.0, false); 
            var output = (phase1 * 2.0) - 1.0; 
            phase1 += inc;
            return 0.25F * (float) (output + blep);
        }

        // bandlimited using sum of saws
        private float calcPulse(double frq, double pulseWidth) {
            var pw = pulseWidth > 0.95 ? 0.95 : pulseWidth < 0.05 ? 0.05 : pulseWidth;
            var inc = frq / SampleRate;
            if (inc > 0.0 && phase1 >= 1.0) { phase1 -= 1.0; }
            else if (inc < 0.0 && phase1 <= 0.0) { phase1 += 1.0; } // neg frqs!
            // saw 1
            var blep = polyBlep(phase1, Math.Abs(inc), 1.0, false);
            var saw1 = blep + ((2.0 * phase1) - 1.0);
            // calc offset for saw2
            var phase2 = inc > 0.0 ? phase1 + pw : phase1 - pw;
            if (inc > 0.0 && phase2 >= 1.0) { phase2 -= 1.0; }
            else if (inc < 0.0 && phase2 <= 0.0) { phase2 += 1.0; } // neg frqs!
            // saw 2
            blep = polyBlep(phase2, Math.Abs(inc), 1.0, false);
            var saw2 = blep + ((2.0 * phase2) - 1.0);
            var output = (0.5 * saw1) - (0.5 * saw2);
            // dc correct
            var dcorr = 1.0 / pw;
            if (pw < 0.5) {
                dcorr = 1.0 / (1.0 - pw);
            }
            output *= dcorr;
            phase1 += inc;
            return 0.25F * (float) output;
        }

        void Start() {
            SampleRate = AudioSettings.outputSampleRate;
            Omega = (1.0 / AudioSettings.outputSampleRate) * TWOPI;
            int buflength, numbufs;
            AudioSettings.GetDSPBufferSize(out buflength, out numbufs);
            Channels = AudioUnit.speakerModeToChannels(AudioSettings.speakerMode);
            frqData = new float[buflength * Channels];
            pwData = new float[buflength * Channels];
        }

        public override void ProcessAudio(float[] data, long sampleNum, int channels ) {
            // input streams?
            if (frqInput != null) { frqInput.ProcessAudio(frqData, sampleNum, channels); }
            if (pwInput != null) { pwInput.ProcessAudio(pwData, sampleNum, channels); }
            // output stream
            for(int i = 0; i < data.Length; i += channels) {
                float frq = (frqInput ? frqData[i] : 220.0F) * (float) frqOffset; // this is frqInput
                float pw = pwData[i];
                data[i] = (waveForm == WAVEFORM.PULSE) 
                    ? calcPulse(frq, pwInput ? pwData[i] : 0.5)
                    : (waveForm == WAVEFORM.SAW) 
                        ? calcSaw(frq)
                        : calcSin(frq);
                data[i] *= (float) gain;
                for (int j = 1; j < channels; j++) {
                    data[i+j] = data[i]; // i.e. mono
                }
            }
            return;
        }
    }
}