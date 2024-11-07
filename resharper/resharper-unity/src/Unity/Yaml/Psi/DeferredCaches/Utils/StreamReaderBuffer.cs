using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JetBrains.Diagnostics;
using JetBrains.Text;
using JetBrains.Util;
using JetBrains.Util.DataStructures.Specialized;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils
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

        public int GetFNVHashCode(int prefixSeed, TextRange range)
        {
            var hashCode = prefixSeed;

            for (var index = range.StartOffset; index < range.EndOffset; index++)
            {
                hashCode = unchecked((hashCode ^ this[index]) * StringTable.FnvPrime);
            }

            return hashCode;
        }

        public void CopyTo(int sourceIndex, char[] destinationArray, int destinationIndex, int length)
        {
            throw new NotImplementedException();
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

            bool isAdditionalFragmentRequested = false;
            if (!myIsEof)
            {
                isAdditionalFragmentRequested = true;
                charIndex += myBufferSize; // read next fragment to detect EOF
            }
            
            while (charIndex >= lastLoaded)
            {
                lastLoaded += myBufferSize;
                ReadFragment();
                if (isAdditionalFragmentRequested && myIsEof)
                    break;
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

            var builder = new StringBuilder();
            
            // replace '\r\n' to '\n'
            // replace '\r' to '\n'
            while (builder.Length != myBufferSize)
            {
                char[] buffer = new char[myBufferSize];
                var queryRead = myBufferSize - builder.Length;
                var readCount = myStreamReader.ReadBlock(buffer, 0, queryRead);
                
                for (int i = 0; i < readCount; i++)
                {
                    var c = buffer[i];
                    if (c == '\r')
                    {
                        // next character is not '\n'
                        if (i == readCount - 1 && myStreamReader.Peek() != '\n' || i != readCount - 1 && buffer[i + 1] != '\n')
                            builder.Append('\n');
                    }
                    else
                    {
                        builder.Append(c);
                    }
                }
                
                if (readCount != queryRead)
                {
                    myLength = myReadFragmentsCount * myBufferSize + builder.Length;
                    myIsEof = true;
                    break;
                }
            }
                        
            if (builder.Length == 0)
                return;

            myFragments.Add(new BufferFragment(myReadFragmentsCount * myBufferSize, builder));
            myReadFragmentsCount++;
        }

        public int Length => myLength;

        public void DropFragments(int count = 3)
        {
            var length = myFragments.Count;
            
            var newFragments = new List<BufferFragment>();
            
            /*
             * [BLOCK1]
             * x : 0
             * -[BLOCK1][BLOCK2]--  [BlOCK2][BLOCK3]
             *  eof[BLOCK3]
             */
            
            for (int i = Math.Max(0, length - count); i < length; i++)
                newFragments.Add(myFragments[i]);

            myFragments = newFragments;
        }
        
        private class BufferFragment
        {
            private readonly int myCharIndex;
            private readonly StringBuilder myBuffer;

            public BufferFragment(int charIndex, StringBuilder buffer)
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