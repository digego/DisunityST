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

    public class Sequencer : AudioUnit
    {
        public AudioUnit frqOutput;
        public AudioUnit gateOutput;


        public double[] pitches = { 60, 63, 67, 72 };
        public double[] durations = { 1.0, 1.0, 0.5, 1.5 };
        public double[] gates = { 0.5 };

        public double BPM = 120.0;

        private double SampleRate; 
        private long countDown = 0; // this counts down in samples until next step
        private long stepNumber = -1;

        private long stepDuration = -1;
        private long processRunNum = 0;

        private double midi2Frq(double pitch) {
            return 440.0 * Math.Pow(2.0, (pitch - 69.0) / 12.0);
        }

        void Start() {
            SampleRate = AudioSettings.outputSampleRate;
        }

        public override void ProcessAudio(float[] data, AudioUnit caller, long sampleNum, int channels ) {
            if (BPM < 40.0) { BPM = 40.0; }
            var tmpCountDown = countDown;
            var tmpStepNumber = stepNumber;
            var tmpStepDuration = stepDuration;
            for(int i = 0; i < data.Length; i += channels) {
                if (tmpCountDown < 1) {
                    tmpStepNumber++; // increment the stepcount!
                    // set the countdown until the next step increment
                    tmpCountDown = (long) ((SampleRate / (BPM / 60.0)) * durations[tmpStepNumber % durations.Length]);
                    tmpStepDuration = tmpCountDown;
                }
                if (caller == gateOutput) {
                    var gateLength = (float) gates[tmpStepNumber % gates.Length];
                    data[i] = ((tmpStepDuration - tmpCountDown) > (long)(gateLength * tmpStepDuration)) ? 0.0F : 1.0F; 
                    // data[i] = (sampleNum % stepDuration) < ((long) ((SampleRate / (BPM / 60.0)) * gateLength)) ? 1.0F : 0.0F;
                } else {
                    data[i] = (float) midi2Frq(pitches[tmpStepNumber % pitches.Length]);
                }
                for (int j = 1; j < channels; j++) {
                    data[i+j] = data[i]; // i.e. mono
                }
                tmpCountDown--;
            }
             if ((processRunNum % 2) == 1)
             {
                 countDown = tmpCountDown;
                 stepNumber = tmpStepNumber;
                 stepDuration = tmpStepDuration;
             }
            processRunNum++;
            return;
        }
    }
}
