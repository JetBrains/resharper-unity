using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace JetBrains.Rider.Unity.Editor.NonUnity
{
  public class BoundSynchronizedQueue<T> where T : class
  {
    private readonly int myMaxSize;
    private readonly LinkedList<T> myLinkedList;
    private readonly object myLockObject = new object();
    public BoundSynchronizedQueue(int maxSize)
    {
      if (maxSize < 0)         
        throw new ArgumentOutOfRangeException(nameof (maxSize), "ArgumentOutOfRange_NeedNonNegNum");
      
      myMaxSize = maxSize;
      myLinkedList = new LinkedList<T>();
    }

    [CanBeNull]
    public T Dequeue()
    {
      lock (myLockObject)
      {
        if (myLinkedList.First == null) 
          return null;
        var firstValue = myLinkedList.First.Value;
        myLinkedList.RemoveFirst();
        return firstValue;
      }
    }

    public void Enqueue([NotNull]T input)
    {
      lock (myLockObject)
      {
        myLinkedList.AddLast(input);
        if (myLinkedList.Count >= myMaxSize)
          myLinkedList.RemoveFirst(); // limit max size  
      }
    }
  }
}