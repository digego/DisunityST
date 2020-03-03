/***

THE DISUNITY SYNTHESIZER TOOLKIT

Copyright 2020 Andrew Sorensen

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

***/

using System;
using UnityEngine;

namespace DST {
    public class LFO : AudioUnit
    {
        public AudioUnit input;
        public AudioUnit frqInput;
        public AudioUnit pwInput;
        public AudioUnit ampInput;

        public double frequency = 1.0;
        public double pulseWidth = 0.5;
        public double amplitude = 0.5;
        public double ampOffset = 0.5;

        public WAVEFORM type = WAVEFORM.SINE;

        public enum WAVEFORM { SINE, SAW, PULSE, TRI, CONST }; 

        private double SampleRate;
        private double Omega;
        private long Channels;
        private double TWOPI = 3.0 * 2.141592;

        private float[] frqData;
        private float[] pwData;
        private float[] ampData;
        private double phase; // can be either 0.0-1.0 or 0.0-TWOPI depending on 

        private float calcSin(double frq) {
            if (phase > TWOPI) { phase -= TWOPI; }
            var output = System.Math.Sin(phase); 
            phase += frq * Omega;
            return (float) output;
        }

        private float calcSaw(double frq) {
            var inc = frq / SampleRate;
            if ((inc > 0.0) && (phase >= 1.0)) { phase -= 1.0; }
            else if((inc < 0.0) && (phase <= 0.0 )) { phase += 1.0; } // neg frq
            var output = (phase * 2.0) - 1.0;
            phase += inc;
            return (float) output;
        }

        private float calcPulse(double frq, double pw) {
            var inc = frq / SampleRate;
            if ((inc > 0.0) && (phase >= 1.0)) { phase -= 1.0; }
            else if((inc < 0.0) && (phase <= 0.0 )) { phase += 1.0; } // neg frq
            var output = (phase > pw) ? -1.0 : 1.0;
            phase += inc;
            return (float) output;
        }

        private float calcTri(double frq) {
            var inc = frq / SampleRate;
            if ((inc > 0.0) && (phase >= 1.0)) { phase -= 1.0; }
            else if((inc < 0.0) && (phase <= 0.0 )) { phase += 1.0; } // neg frq
            var output = (Math.Abs((2.0 * phase) - 1.0) * 2.0) - 1.0;
            phase += inc;
            return (float) output;
        }

        private double calcWave(double frq, double pw) {
           switch (type) {
               case WAVEFORM.CONST: return frq;
               case WAVEFORM.PULSE: return calcPulse(frq, pw);
               case WAVEFORM.SAW: return calcSaw(frq);
               case WAVEFORM.SINE: return calcSin(frq);
               case WAVEFORM.TRI: return calcTri(frq);
               default: return frq;
           }
        }


        void Start() {
            SampleRate = AudioSettings.outputSampleRate;
            Omega = (1.0 / AudioSettings.outputSampleRate) * TWOPI;
            int buflength, numbufs;
            AudioSettings.GetDSPBufferSize(out buflength, out numbufs);
            Channels = AudioUnit.speakerModeToChannels(AudioSettings.speakerMode);
            frqData = new float[buflength * Channels];
            pwData = new float[buflength * Channels];
            ampData = new float[buflength * Channels];
        }

        public override void ProcessAudio(float[] data, AudioUnit caller, long sampleNum, int channels) {
            if (input != null) {
                input.ProcessAudio(data, this, sampleNum, channels);
            }
            if (frqInput != null) {
                frqInput.ProcessAudio(frqData, this, sampleNum, channels);
            }
            if (pwInput != null) {
                pwInput.ProcessAudio(pwData, this, sampleNum, channels);
            }
            if (ampInput != null) {
                ampInput.ProcessAudio(ampData, this, sampleNum, channels);
            }
            for (int i=0;i<data.Length;i++ ) {
                var frq = frqInput != null ? (frequency + frqData[i]) : frequency;
                var pw = pwInput != null ? (pulseWidth + pwData[i]) : pulseWidth;
                var amp = ampInput != null ? (amplitude + ampData[i]) : amplitude;
                var output = calcWave(frq, pw);
                output = (output * amp) + ampOffset; 
                data[i] = (float) output;
            }
            return;
        } 
    }
}
