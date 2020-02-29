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

    public enum ENVSTATE {
        IDLE,
        ATTACK,
        DECAY,
        SUSTAIN,
        RELEASE
    }

    public class Envelope : AudioUnit
    {
        public AudioUnit audioInput;
        public AudioUnit gateInput;
        public double attack = 50.0;
        public double decay = 50.0;
        public double sustain = 0.5;
        public double release = 200.0;

        // internal state
        private double SampleRate = 0.0;
        private double attackOld = -1.0;
        private double decayOld = -1.0;
        private double sustainOld = -1.0;
        private double releaseOld = -1.0;
        private double gateOld = 0.0;

        private bool retrigger = false;
        private double attackRate;
        private double attackCoef;
        private double attackBase;

        private double decayRate;
        private double decayCoef;
        private double decayBase;

        private double releaseRate;
        private double releaseCoef;
        private double releaseBase;

        private double sustainLevel;
        private double output = 0.0;

        private float[] audioData;
        private float[] gateData;

        private const double targetRatioA = 0.3;
        private const double targetRatioDR = 0.0001;

        private ENVSTATE state;

        private double calcCoef (double rate, double targetRatio) {
            return Math.Exp((-1.0 * Math.Log((1.0 + targetRatio) / targetRatio)) / rate);
        }

        void Start() {
            SampleRate = AudioSettings.outputSampleRate;
            int buflength, numbufs;
            AudioSettings.GetDSPBufferSize(out buflength, out numbufs);
            var channels = AudioUnit.speakerModeToChannels(AudioSettings.speakerMode);
            audioData = new float[buflength * channels];
            gateData = new float[buflength * channels];
        }

        private double processSample(double gate) {

            if (retrigger) {
                retrigger = false;
                state = ENVSTATE.ATTACK;
            }

            if (attack != attackOld) {
                attackOld = attack;
                attackRate = attack * SampleRate * 0.001;
                attackCoef = calcCoef(attackRate, targetRatioA);
                attackBase = (1.0 + targetRatioA) * (1.0 - attackCoef);
            }

            if (decay != decayOld) {
                decayOld = decay;
                decayRate = decay * SampleRate * 0.001;
                decayCoef = calcCoef(decayRate, targetRatioDR);
                decayBase = (sustainLevel - targetRatioDR) * (1.0 - decayCoef);
            }

            if (sustain != sustainOld) {
                sustainOld = sustain;
                sustainLevel = sustain;
                decayBase = (sustainLevel - targetRatioDR) * (1.0 - decayCoef);
            }

            if (release != releaseOld) {
                releaseOld = release;
                releaseRate = release * SampleRate * 0.001;
                releaseCoef = calcCoef(releaseRate, targetRatioDR);
                releaseBase = -1.0 * targetRatioDR * (1.0 - releaseCoef);
            }

            if (gate != gateOld) {
                gateOld = gate;
                if (gate > 0.1) {
                    if (attack > 0.00001) {
                        state = ENVSTATE.ATTACK;
                    } else if (decay > 0.00001) {
                        output = 1.0;
                        state = ENVSTATE.DECAY;
                    } else {
                        output = sustainLevel;
                        state = ENVSTATE.SUSTAIN;
                    }
                } else {
                    state = ENVSTATE.RELEASE;
                }
            }

            switch (state) {
                case ENVSTATE.ATTACK: {
                    output = attackBase + (output * attackCoef);
                    if (output >= 1.0) {
                        output = 1.0;
                        state = ENVSTATE.DECAY;
                    }
                    return output;
                } 
                case ENVSTATE.DECAY: {
                    output = decayBase + (output * decayCoef);
                    if (output <= sustainLevel) {
                        output = sustainLevel;
                        state = ENVSTATE.SUSTAIN;
                    }
                    return output;
                }
                case ENVSTATE.SUSTAIN: return output;
                case ENVSTATE.RELEASE: {
                    output = releaseBase + (output * releaseCoef);
                    if (output <= 0.0) {
                        output = 0.0;
                        state = ENVSTATE.IDLE;
                    }
                    return output;
                }  
                default: return output;
            }
        }

        public override void ProcessAudio(float[] data, long sampleNum, int channels) {
            // input streams
            if (audioInput != null) {  audioInput.ProcessAudio(audioData, sampleNum, channels); }
            if (gateInput != null) { gateInput.ProcessAudio(gateData, sampleNum, channels);}
            // output stream
            for(int i = 0; i < data.Length; i+=channels) {
                data[i] = audioData[i] * (float) processSample((float) gateData[i]);
                for (int j = 1; j < channels; j++) {
                    data[i+j] = data[i]; // i.e. mono
                }
            }
            return;
        }


    }
}