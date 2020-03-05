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
    public class Mixer : AudioUnit
    {
        // input channels for mixer
        public AudioUnit[] inputs;
        // gains for each input channel
        // missing 'channels' will be gain 1.0;
        public AudioUnit[] gains;
        private long Channels;
        private float[] audioData;
        private float[] amplitudeData;

        void Start() {
            // SampleRate = AudioSettings.outputSampleRate;
            int buflength, numbufs;
            AudioSettings.GetDSPBufferSize(out buflength, out numbufs);
            Channels = AudioUnit.speakerModeToChannels(AudioSettings.speakerMode);
            audioData = new float[buflength * Channels];
            amplitudeData = new float[buflength * Channels];
        }

        private void scalarToArray(float scalar, float[] arr) {
            for(int i = 0; i < arr.Length; i++) { arr[i] = scalar; }
        } 

        public override void ProcessAudio(float[] data, AudioUnit caller, long sampleNum, int channels) {
            scalarToArray(0.0F, data);
            for(int j = 0; j < inputs.Length; j++) {
                inputs[j].ProcessAudio(audioData, this, sampleNum, channels);
                if (gains.Length > j) {
                    if (gains[j] != null) {
                        gains[j].ProcessAudio(amplitudeData, this, sampleNum, channels);
                    } else {
                        scalarToArray(1.0F, amplitudeData);
                    }
                } else {
                    scalarToArray(1.0F, amplitudeData);
                }
                for (int i=0;i<data.Length;i++ ) {
                    data[i] += amplitudeData[i] * audioData[i]; 
                }
                
            }

            return;
        }
    }
}
