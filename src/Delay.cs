/***

THE DISUNITY SYNTHESIZER TOOLKIT

Copyright 2020 Andrew Sorensen

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

***/


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DST {
    public class Delay : AudioUnit
    {
        public AudioUnit input;
        [Range(10, 9999)]
        public long delayTimeLeft = 500;
        [Range(10, 9999)]
        public long delayTimeRight = 700;
        public float feedback = 0.25F;
        public float mixIn = 0.75F;
        public float mixOut = 0.75F;
        private float[] delayLineLeft;
        private float[] delayLineRight;
        private long playHeadLeft;
        private long playHeadRight;

        private double SampleRate;


        void Start() {
            SampleRate = AudioSettings.outputSampleRate;
            delayLineLeft = new float[(long)SampleRate * 10]; 
            delayLineRight = new float[(long)SampleRate * 10]; 
        }

        public override void ProcessAudio(float[] data, AudioUnit caller, long sampleNum, int channels) {
            if (input != null) {
                input.ProcessAudio(data, this, sampleNum, channels);
            }
            for (int i=0;i<data.Length;i+=channels ) {
                var leftd = delayLineLeft[playHeadLeft];
                var leftout = data[i] + ((float)mixOut * leftd);
                delayLineLeft[playHeadLeft] = (data[i] * mixIn) + (leftd * feedback);
                playHeadLeft = (playHeadLeft + 1) % (long)(delayTimeLeft * 0.001 * SampleRate);
                data[i] = leftout;
                if (channels > 1) {
                    var rightd = delayLineRight[playHeadRight];
                    var rightout = data[i+1] + ((float)mixOut * rightd);
                    delayLineRight[playHeadRight] = (data[i+1] * mixIn) + (rightd * feedback);
                    playHeadRight = (playHeadRight + 1) % (long)(delayTimeRight * 0.001 * SampleRate);
                    data[i+1] = rightout;
                }
            }
            return;
        } 
    }
}
