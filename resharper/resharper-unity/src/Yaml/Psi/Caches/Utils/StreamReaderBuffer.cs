using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JetBrains.Diagnostics;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.Utils
{
    /// <summary>
    /// NOTE: bufferSize must be larger than number of look ahead characters
    /// </summary>
    public class StreamReaderBuffer : IBuffer
    {
        private readonly StreamReader myStreamReader;
        private readonly int myBufferSize;
        private int myReadFragmentsCount = 0;
        private bool myIsEof = false;

        private List<BufferFragment> myFragments = new List<BufferFragment>();
        private int myLength = int.MaxValue; // We don't know the real number of chars in large file, we will read bufferSize chars ahead to detect eof

        public StreamReaderBuffer(StreamReader streamReader, int bufferSize)
        {
            myStreamReader = streamReader;
            myBufferSize = bufferSize;
        }
        
        [Obsolete]
        public string GetText()
        {
            throw new NotImplementedException("This operation is very expensive and will be never implemented");
        }

        public string GetText(TextRange range)
        {
            var sb = new StringBuilder();

            AppendTextTo(sb, range);

            return sb.ToString();
        }

        public void AppendTextTo(StringBuilder builder, TextRange range)
        {
            for (int i = range.StartOffset; i < range.EndOffset; i++)
            {
                builder.Append(this[i]);
            }
        }

        public void CopyTo(int sourceIndex, char[] destinationArray, int destinationIndex, int length)
        {
            throw new System.NotImplementedException();
        }

        public char this[int index]
        {
            get
            {
                var fragment = GetFragmentByIndex(index);
                return fragment.Get(index);
            }
        }

        private int FirstLoadedCharIndex
        {
            get
            {
                if (myFragments.Count == 0)
                    return 0;
                return myFragments[0].CharIndex;
            }
        }

        private void EnsureFragmentIsLoaded(int charIndex)
        {
            var lastLoaded = myReadFragmentsCount * myBufferSize;

            if (!myIsEof)
            {
                charIndex += myBufferSize; // read next fragment to detect EOF
            }
            
            while (charIndex >= lastLoaded)
            {
                lastLoaded += myBufferSize;
                ReadFragment();
            }


        }

        private BufferFragment GetFragmentByIndex(int charIndex)
        {
            if (charIndex < FirstLoadedCharIndex)
                throw new InvalidOperationException($"Data for {charIndex} was already disposed");
            EnsureFragmentIsLoaded(charIndex);

            return myFragments[(charIndex - FirstLoadedCharIndex) / myBufferSize];
        }
        
        private void ReadFragment()
        {
            Assertion.Assert(!myIsEof, "!myIsEof");
            char[] buffer = new char[myBufferSize];
            var readCount = myStreamReader.ReadBlock(buffer, 0, myBufferSize);
            if (readCount != myBufferSize)
            {

                myLength = myReadFragmentsCount * myBufferSize + readCount;
                myIsEof = true;
            }
            if (readCount == 0)
                return;

            myFragments.Add(new BufferFragment(myReadFragmentsCount * myBufferSize, buffer));
            myReadFragmentsCount++;
        }

        public int Length => myLength;

        public void DropFragments()
        {
            var length = myFragments.Count;
            if (length < 2)
                return;
            
            var newFragments = new List<BufferFragment>();
            
            /*
             * [BLOCK1]
             * x : 0
             * -[BLOCK1][BLOCK2]--  [BlOCK2][BLOCK3]
             *  eof[BLOCK3]
             */
            
            newFragments.Add(myFragments[length - 3]); // could contain first part of yaml document (the first minus char)
            newFragments.Add(myFragments[length - 2]); // the rest part of yaml document
            newFragments.Add(myFragments[length - 1]); // the part which was loaded for eof check

            myFragments = newFragments;
        }
        
        private class BufferFragment
        {
            private readonly int myCharIndex;
            private readonly char[] myBuffer;

            public BufferFragment(int charIndex, char[] buffer)
            {
                myCharIndex = charIndex;
                myBuffer = buffer;
            }

            public int CharIndex => myCharIndex;
            public char Get(int index)
            {
                return myBuffer[index - myCharIndex];
            }
        }
    }
}