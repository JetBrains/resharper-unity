using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using JetBrains.Util;
using MetadataLite.API;
using Mono.Debugging.Backend.Values;
using Mono.Debugging.Backend.Values.Render.ChildrenRenderers;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.Values;
using Mono.Debugging.Client.Values.Render;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values.Render.ChildrenRenderers
{
    public abstract class FilteredObjectChildrenRendererBase<TValue> : ChildrenRendererBase<TValue, IObjectValueRole<TValue>>
        where TValue : class
    {
        protected override IEnumerable<IValueEntity> GetChildren(IObjectValueRole<TValue> valueRole, IMetadataTypeLite instanceType, IPresentationOptions options,
                                                                 IUserDataHolder dataHolder, CancellationToken token)
        {
            return options.FlattenHierarchy
                ? EnumerateMembersFlat(valueRole, options, token, ValueServices)
                : EnumerateMembersWithBaseNode(valueRole, options, token, ValueServices);
        }

        protected abstract bool IsAllowedReference(IValueReference<TValue> reference);

        private IEnumerable<IValueEntity> EnumerateMembersFlat(IObjectValueRole<TValue> valueRole,
                                                               IPresentationOptions options,
                                                               CancellationToken token,
                                                               IValueServicesFacade<TValue> valueServices)
        {
            var sortedReferences = ChildrenRenderingUtil.CollectMembersByOverridingRules(valueRole, token)
                .Where(IsAllowedReference)
                .OrderBy(IdentityFunc<IValueReference<TValue>>.Instance, ByNameReferenceComparer<TValue>.Instance);

            foreach (var memberValue in ChildrenRenderingUtil.RenderReferencesWithVisibilityGroups(sortedReferences, options, token, valueServices))
                yield return memberValue;

            foreach (var staticMember in ChildrenRenderingUtil.EnumerateStaticMembersIfNeeded(valueRole, options, token, valueServices))
                yield return staticMember;
        }

        private IEnumerable<IValueEntity> EnumerateMembersWithBaseNode(IObjectValueRole<TValue> valueRole,
                                                                       IPresentationOptions options,
                                                                       CancellationToken token,
                                                                       IValueServicesFacade<TValue> valueServices)
        {
            var baseRole = FindNextBaseRoleWithVisibleMembers(valueRole);
            if (baseRole != null)
            {
                yield return new ConcreteObjectRoleReference<TValue>(baseRole, "base", false, ValueOriginKind.Base, ValueFlags.None).ToValue(valueServices);
            }

            var propertiesAndFields = GetPropertiesAndFields(valueRole)
                .Where(IsAllowedReference)
                .OrderBy(IdentityFunc<IValueReference<TValue>>.Instance, ByNameReferenceComparer<TValue>.Instance);

            foreach (var member in ChildrenRenderingUtil.RenderReferencesWithVisibilityGroups(propertiesAndFields, options, token, valueServices))
                yield return member;

            foreach (var staticMember in ChildrenRenderingUtil.EnumerateStaticMembersIfNeeded(valueRole, options, token, valueServices))
                yield return staticMember;
        }

        [CanBeNull]
        private static IObjectValueRole<TValue> FindNextBaseRoleWithVisibleMembers(IObjectValueRole<TValue> role)
        {
            var baseRole = role.Base;
            if (baseRole == null || !baseRole.Type.IsVisibleType())
                return null;
            var baseRoleType = baseRole.Type;
            if (baseRoleType.GetProperties().Any(ChildrenRenderingUtil.IsVisibleGetterProperty) ||
                baseRoleType.GetFields().Any(ChildrenRenderingUtil.IsVisibleField))
            {
                return baseRole;
            }

            return FindNextBaseRoleWithVisibleMembers(baseRole);
        }

        private static IReadOnlyList<IValueReference<TValue>> GetPropertiesAndFields(IObjectValueRole<TValue> role)
        {
            var result = new List<IValueReference<TValue>>();
            result.AddRange(role.GetInstancePropertyReferences(ChildrenRenderingUtil.IsVisibleGetterProperty));
            result.AddRange(role.GetInstanceFieldReferences(ChildrenRenderingUtil.IsVisibleField));
            return result;
        }
    }
}