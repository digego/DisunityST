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
    public class Constant : AudioUnit
    {
        public AudioUnit input;
        public double value = 0.0;

        public override void ProcessAudio(float[] data, AudioUnit caller, long sampleNum, int channels) {
            if (input != null) {
                input.ProcessAudio(data, this, sampleNum, channels);
            }
            for (int i=0;i<data.Length;i++ ) {
                var offset = input != null ? data[i] : 0.0;
                data[i] = (float)value + (float)offset;
            }
            return;
        } 
    }
}
