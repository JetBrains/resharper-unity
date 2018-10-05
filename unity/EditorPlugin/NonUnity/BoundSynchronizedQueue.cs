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

    public void Queue([NotNull]T input)
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