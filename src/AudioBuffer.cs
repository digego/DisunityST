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
    public class AudioBuffer : AudioUnit {
        public AudioClip audioFile;
        public AudioUnit gateInput;
        public AudioUnit loopStartInput;
        public float gate = 1.0F;
        public bool gateRetrigger = true;
        public double loopStart = 0.0;
        public double loopDuration = 0.0;
        private long playHead = 0;

        private double SampleRate;
        private float[] gateData;
        private float[] loopStartData;
        private long clipSamples = -1;
        private long clipChannels = -1;
        private float[] audioBuffer; 
        private float gateState = 0.0F;

        // not using this yet
        private float hermite_interp_local(float fractional, float y1, float x0, float x1, float x2) {
            var c = 0.5F * (x1 - y1);
            var v = x0 - x1;
            var w = c + v;
            var a = w + v + ((x2 - x0) * 0.5F);
            var b = w + a;
            var res = (((((a * fractional) - b) - fractional) * c) * fractional) + x0;
            return res;
        }

        // not using this yet
        private float midi2frq(float pitch) {
            return (pitch <= 0.0F) ? 0.0F : 440.0F * Mathf.Pow(2.0F, (pitch - 69.0F) / 12.0F);
        }

        void Awake() {
        }

        void Start() {
            SampleRate = AudioSettings.outputSampleRate;
            int buflength, numbufs;
            AudioSettings.GetDSPBufferSize(out buflength, out numbufs);
            var channels = AudioUnit.speakerModeToChannels(AudioSettings.speakerMode);
            gateData = new float[buflength * channels];
            loopStartData = new float[buflength * channels];
            if (audioFile) {
                clipSamples = audioFile.samples;
                clipChannels = audioFile.channels;
                audioBuffer = new float[clipSamples * clipChannels];
                audioFile.GetData(audioBuffer, 0);                
            }
        }

        public override void ProcessAudio(float[] data, AudioUnit caller, long sampleNum, int channels ) {
            if (audioFile == null || clipSamples < 0) { 
                return;
            } 
            if (gateInput != null) {
                gateInput.ProcessAudio(gateData, this, sampleNum, channels);
            }
            if (loopStartInput != null) {
                loopStartInput.ProcessAudio(loopStartData, this, sampleNum, channels);
            }
            if ((long)(loopStart * SampleRate) < 0 || (loopStart * SampleRate) > clipSamples) { loopStart = 0.0; }
            
            for (int i=0;i<data.Length;i+=channels) {
                var g = ((gateInput != null) ? gateData[i] : gate) > 0.25 ? 1.0F : 0.0F;
                var ls = loopStartInput ? (long)((loopStartData[i] + loopStart) * SampleRate) : (long)(loopStart * SampleRate);
                if (playHead > clipSamples) {
                    Debug.Log("playHead " + playHead.ToString() + " file " +  clipSamples);
                }
                data[i] = audioBuffer[playHead * clipChannels] * g;
                if (clipChannels > 1) {
                    data[i+1] = audioBuffer[(playHead * clipChannels) + 1] * g;
                }
                if (gateState < 0.5 && g >= 0.5) {
                    if (gateRetrigger) {
                        playHead = Math.Abs(ls);
                    }
                }
                gateState = g;
                if (g > 0.5) {
                    playHead++;
                    if (playHead >= ((loopDuration > 0.0) ? (long)(loopDuration * SampleRate) + ls : clipSamples)) {
                        playHead = Math.Abs(ls);
                    }
                    if (playHead >= clipSamples) {
                        playHead = 0;
                    }
                }
            }
        }
    }
}