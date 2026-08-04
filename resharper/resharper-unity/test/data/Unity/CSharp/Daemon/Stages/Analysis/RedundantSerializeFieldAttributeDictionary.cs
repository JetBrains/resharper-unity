using System;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField] private Dictionary<string, int> NotRedundantSimple;
    [SerializeField] private Dictionary<string, List<int>> NotRedundantListValue;
    [SerializeField] private Dictionary<int, GameObject> NotRedundantObjectValue;
    [SerializeField] private Dictionary<string, Dictionary<string, int>> RedundantNestedDictionary;
    [SerializeField] private List<Dictionary<string, int>> RedundantDictionaryInList;
    [SerializeField] private Dictionary<string, int>[] RedundantDictionaryArray;
    [SerializeField] private Dictionary<List<int>, int> RedundantCollectionKey;
    [SerializeField] private readonly Dictionary<string, int> RedundantReadonly;
}
