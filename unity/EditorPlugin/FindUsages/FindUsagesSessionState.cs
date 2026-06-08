using System;
using System.IO;
using JetBrains.Diagnostics;
using JetBrains.Rd;
using JetBrains.Rd.Impl;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Serialization;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.FindUsages
{
  internal interface IStringStore
  {
    string GetString(string key, string defaultValue);
    void SetString(string key, string value);
    void EraseString(string key);
  }

  internal sealed class UnitySessionStateStore : IStringStore
  {
    public string GetString(string key, string defaultValue) => SessionState.GetString(key, defaultValue);
    public void SetString(string key, string value) => SessionState.SetString(key, value);
    public void EraseString(string key) => SessionState.EraseString(key);
  }

  internal static class FindUsagesSessionState
  {
    private static readonly ILog ourLogger = Log.GetLog("FindUsages.SessionState");
    private const string StateKey = "JetBrains.Rider.FindUsages.v1.State";

    // Header validated before UnsafeReader runs: it has no bounds checks, so a bad payload would
    // read past the buffer and crash via an uncatchable AccessViolationException. Bump FormatVersion on schema change.
    private const int FormatMagic = 0x52464755; // 'RFGU'
    private const int FormatVersion = 1;

    private static readonly SerializationCtx ourSerializationCtx = CreateSerializationContext();

    internal static IStringStore Store { get; set; } = new UnitySessionStateStore();

    public static void Save(FindUsagesSessionResult result)
    {
      try
      {
        Store.SetString(StateKey, SerializeToBase64(result));
      }
      catch (Exception e)
      {
        ourLogger.Warn($"FindUsages save failed: {e}");
      }
    }

    public static bool HasSavedState() =>
      !string.IsNullOrEmpty(Store.GetString(StateKey, ""));

    public static bool TryLoad(out FindUsagesSessionResult result)
    {
      var savedBase64 = Store.GetString(StateKey, "");
      if (string.IsNullOrEmpty(savedBase64))
      {
        result = null;
        return false;
      }

      Store.EraseString(StateKey); // consume once; a corrupt payload must not be retried on next reload
      try
      {
        result = DeserializeFromBase64(savedBase64);
        return true;
      }
      catch (Exception)
      {
        result = null;
        return false;
      }
    }

    private static string SerializeToBase64(FindUsagesSessionResult result)
    {
      byte[] payload;
      using (var cookie = UnsafeWriter.NewThreadLocalWriter())
      {
        FindUsagesSessionResult.Write(ourSerializationCtx, cookie.Writer, result);
        payload = cookie.CloneData();
      }

      using var stream = new MemoryStream();
      using var writer = new BinaryWriter(stream);
      writer.Write(FormatMagic);
      writer.Write(FormatVersion);
      writer.Write(payload.Length);
      writer.Write(payload);
      return Convert.ToBase64String(stream.ToArray());
    }

    private static FindUsagesSessionResult DeserializeFromBase64(string base64)
    {
      using var stream = new MemoryStream(Convert.FromBase64String(base64));
      using var reader = new BinaryReader(stream);
      // BinaryReader throws cleanly (EndOfStream/OutOfMemory) on truncated or oversized input,
      // so a malformed buffer never reaches the bounds-unchecked UnsafeReader.
      if (reader.ReadInt32() != FormatMagic || reader.ReadInt32() != FormatVersion)
        throw new FormatException("Unrecognized FindUsages session state payload");

      var length = reader.ReadInt32();
      var payload = reader.ReadBytes(length);
      if (payload.Length != length)
        throw new FormatException("Truncated FindUsages session state payload");

      FindUsagesSessionResult result = null;
      UnsafeReader.With(payload, r => result = FindUsagesSessionResult.Read(ourSerializationCtx, r));
      return result;
    }

    // Standalone Rd context; JsonUtility can't serialize collections from this assembly.
    private static SerializationCtx CreateSerializationContext()
    {
      var serializers = new Serializers();
      BackendUnityModel.RegisterDeclaredTypesSerializers(serializers);
      return new SerializationCtx(serializers, new SequentialIdentities(IdKind.Client));
    }
  }
}
