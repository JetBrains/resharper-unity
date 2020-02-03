using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JetBrains.Diagnostics;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.Utils
{
    public class StreamReaderBuffer : IBuffer
    {
        private readonly StreamReader myStreamReader;
        private readonly int myBufferSize;
        private int myReadFragmentsCount = 0;
        private bool myIsEof = false;

        private List<BufferFragment> myFragments = new List<BufferFragment>();

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
                    ReadFragment();
                return myFragments[0].CharIndex;
            }
        }

        private void EnsureFragmentIsLoaded(int charIndex)
        {
            var lastLoaded = myReadFragmentsCount * myBufferSize;

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
                myIsEof = true;
            
            myFragments.Add(new BufferFragment(myReadFragmentsCount * myBufferSize, buffer));
            myReadFragmentsCount++;
        }

        public int Length => (int)myStreamReader.BaseStream.Length;

        public void DropFragments(int saveCount = 2)
        {
            var length = myFragments.Count;
            if (length < 2)
                return;
            
            var newFragments = new List<BufferFragment>();
            newFragments.Add(myFragments[length - 2]);
            newFragments.Add(myFragments[length - 1]);

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