#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Daemon.UsageChecking.SwaExtension;
using JetBrains.Serialization;
using JetBrains.Util.Collections;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.SerializeReference
{
    [PolymorphicMarshaller(6)]
    internal class UnitySerializationReferenceElementInfo : ISwaExtensionInfo
    {
        public static readonly IUnsafeMarshaller<UnitySerializationReferenceElementInfo> SerializeRefInfoMarshaller = new UniversalMarshaller<UnitySerializationReferenceElementInfo>(
            reader => (UnitySerializationReferenceElementInfo)Read(reader), Write
            );

        
        [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        [UsedImplicitly] public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) =>
            Write(w ?? throw new ArgumentNullException(nameof(w)), (UnitySerializationReferenceElementInfo)o.NotNull());


        public static readonly UnitySerializationReferenceElementInfo EMPTY = new();

        public UnitySerializationReferenceElementInfo(
            UnitySerializationReferenceElementData unitySerializationReferenceElementData)
        {
            TypeToInterfaces =
                new ClassMetaInfoDictionary(unitySerializationReferenceElementData.ClassInfoDictionary);

            TypeParameterResolves =
                new CountingSet<TypeParameterResolve>(unitySerializationReferenceElementData.TypeParameterResolves);
        }


        public UnitySerializationReferenceElementInfo()
        {
            TypeToInterfaces = new ClassMetaInfoDictionary();
            TypeParameterResolves = new CountingSet<TypeParameterResolve>();
        }

        public ClassMetaInfoDictionary TypeToInterfaces { get; }
        public CountingSet<TypeParameterResolve> TypeParameterResolves { get; }

        public void Dump(StreamWriter writer, ISolution solution)
        {
            // throw new NotImplementedException();
        }

        private static object Read(UnsafeReader reader)
        {
            var result = new UnitySerializationReferenceElementInfo();
            var typeToInterfaceCount = reader.ReadInt();
            for (var i = 0; i < typeToInterfaceCount; i++)
            {
                var classId = ElementId.ReadFrom(reader);
                var metaInfo = ReadMetaInfo(reader);
                result.TypeToInterfaces.Add(classId, metaInfo);
            }

            var resolvesCount = reader.ReadInt();
            for (var i = 0; i < resolvesCount; i++)
            {
                var resolve = ReadResolve(reader);
                var count = reader.ReadInt();
                result.TypeParameterResolves.Add(resolve, count);
            }

            return result;
        }

        private static TypeParameterResolve ReadResolve(UnsafeReader reader)
        {
            var resolutionString = reader.ReadString();
            var openTypeId = ElementId.ReadFrom(reader);
            var resolutionTypeId = ElementId.ReadFrom(reader);
            return new TypeParameterResolve(resolutionString!, openTypeId, resolutionTypeId);
        }

        private static ClassMetaInfo ReadMetaInfo(UnsafeReader reader)
        {
            var className = reader.ReadString();
            var superClasses = ReadCountingSet(reader);
            var serializeReferenceHolders = ReadCountingSet(reader);
            var typeParameters = ReadTypeParametersList(reader);

            return new ClassMetaInfo(className!, superClasses, serializeReferenceHolders, typeParameters);
        }

        private static Dictionary<ElementId, TypeParameter> ReadTypeParametersList(UnsafeReader reader)
        {
            var result = new Dictionary<ElementId, TypeParameter>();
            var count = reader.ReadInt();
            for (var i = 0; i < count; i++)
            {
                var typeParameterInfo = ReadTypeParameter(reader);
                result.Add(typeParameterInfo.ElementId, typeParameterInfo);
            }

            return result;
        }

        private static TypeParameter ReadTypeParameter(UnsafeReader reader)
        {
            var elementId = ElementId.ReadFrom(reader);
            var name = reader.ReadString();
            var index = reader.ReadInt();
            var typeParameters = ReadCountingSet(reader);
            return new TypeParameter(elementId, name!, index, typeParameters);
        }

        private static CountingSet<ElementId> ReadCountingSet(UnsafeReader reader)
        {
            var result = new CountingSet<ElementId>();

            var superClassesCount = reader.ReadInt();
            for (var i = 0; i < superClassesCount; i++)
            {
                var elementId = ElementId.ReadFrom(reader);
                var count = reader.ReadInt();
                result.Add(elementId, count);
            }

            return result;
        }

        private static void Write(UnsafeWriter writer, UnitySerializationReferenceElementInfo value)
        {
            writer.Write(value.TypeToInterfaces.Count);
            foreach (var (classId, classMetaInfo) in value.TypeToInterfaces)
            {
                classId.WriteTo(writer);
                WriteMetaInfo(writer, classMetaInfo);
            }

            writer.Write(value.TypeParameterResolves.Count);
            foreach (var (key, count) in value.TypeParameterResolves)
            {
                WriteTypeResolve(writer, key);
                writer.Write(count);
            }
        }

        private static void WriteTypeResolve(UnsafeWriter writer, TypeParameterResolve resolve)
        {
            writer.Write(resolve.ResolutionString);
            resolve.OpenTypeId.WriteTo(writer);
            resolve.ResolvedTypeId.WriteTo(writer);
        }

        private static void WriteMetaInfo(UnsafeWriter writer, ClassMetaInfo classMetaInfo)
        {
            writer.Write(classMetaInfo.ClassName);
            WriteCountingSet(writer, classMetaInfo.SuperClasses);
            WriteCountingSet(writer, classMetaInfo.SerializeReferenceHolders);
            WriteTypeParametersSet(writer, classMetaInfo.TypeParameters);
        }

        private static void WriteTypeParametersSet(UnsafeWriter writer,
            Dictionary<ElementId, TypeParameter> typeParameters)
        {
            writer.Write(typeParameters.Count);
            foreach (var (_, typeParameter) in typeParameters)
                WriteTypeParameter(writer, typeParameter);
        }

        private static void WriteTypeParameter(UnsafeWriter writer, TypeParameter typeParameter)
        {
            typeParameter.ElementId.WriteTo(writer);
            writer.Write(typeParameter.Name);
            writer.Write(typeParameter.Index);
            WriteCountingSet(writer, typeParameter.SerializeReferenceHolders);
        }

        private static void WriteCountingSet(UnsafeWriter writer, CountingSet<ElementId> countingSet)
        {
            writer.Write(countingSet.Count);
            foreach (var pair in countingSet)
            {
                pair.Key.WriteTo(writer);
                writer.Write(pair.Value);
            }
        }

        public bool IsEmpty()
        {
            return TypeToInterfaces.Count == 0 && TypeParameterResolves.Count == 0;
        }
    }
}