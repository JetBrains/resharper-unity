using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using JetBrains.Util;
using Mono.Debugging.Backend.Values.Render.ChildrenRenderers;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.Values;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.MetadataLite.API;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Values.Render.ChildrenRenderers
{
    public abstract class FilteredObjectChildrenRendererBase<TValue> : ChildrenRendererBase<TValue, IObjectValueRole<TValue>>
        where TValue : class
    {
        [NotNull]
        protected override IEnumerable<IValueEntity> GetChildren([NotNull] IObjectValueRole<TValue> valueRole,
                                                                 IMetadataTypeLite instanceType,
                                                                 IPresentationOptions options,
                                                                 IUserDataHolder dataHolder,
                                                                 CancellationToken token)
        {
            var references = EnumerateChildren(valueRole, options, token);
            references = FilterChildren(references);
            references = SortChildren(references);
            return RenderChildren(valueRole, references, options, token);
        }

        [NotNull]
        protected IEnumerable<IValueReference<TValue>> EnumerateChildren([NotNull] IObjectValueRole<TValue> valueRole,
                                                                         IPresentationOptions options,
                                                                         CancellationToken token)
        {
            // This is essentially the same as ChildrenRenderingUtil.EnumerateMembersFlat and EnumerateMembersWithBaseNode
            // but allows us to split enumerating, sorting, and rendering into separate steps so we can also insert
            // filtering.
            return options.FlattenHierarchy
                ? ChildrenRenderingUtil.CollectMembersByOverridingRules(valueRole, options, token)
                : GetPropertiesAndFields(valueRole);
        }

        private IEnumerable<IValueReference<TValue>> FilterChildren(IEnumerable<IValueReference<TValue>> references)
        {
            return references.Where(ShouldInclude);
        }

        protected virtual bool ShouldInclude(IValueReference<TValue> reference) => true;

        protected IEnumerable<IValueReference<TValue>> SortChildren(IEnumerable<IValueReference<TValue>> references)
        {
            return references.OrderBy(IdentityFunc<IValueReference<TValue>>.Instance,
                ByNameReferenceComparer<TValue>.Instance);
        }

        protected IEnumerable<IValueEntity> RenderChildren(IObjectValueRole<TValue> valueRole,
                                                           IEnumerable<IValueReference<TValue>> references,
                                                           IPresentationOptions options, CancellationToken token)
        {
            if (!options.FlattenHierarchy)
            {
                // Add when rendering to avoid sorting issues
                var baseRole = FindNextBaseRoleWithVisibleMembers(valueRole);
                if (baseRole != null)
                {
                    yield return new ConcreteObjectRoleReference<TValue>(baseRole, "base", false, ValueOriginKind.Base,
                        ValueFlags.None).ToValue(ValueServices);
                }
            }

            foreach (var memberValue in ChildrenRenderingUtil.RenderReferencesWithVisibilityGroups(references, options,
                token, ValueServices))
            {
                yield return memberValue;
            }

            foreach (var staticMember in ChildrenRenderingUtil.EnumerateStaticMembersIfNeeded(valueRole, options,
                token, ValueServices))
            {
                yield return staticMember;
            }
        }

        [CanBeNull]
        private static IObjectValueRole<TValue> FindNextBaseRoleWithVisibleMembers(IObjectValueRole<TValue> role)
        {
            var baseRole = role.Base;
            while (baseRole != null && baseRole.Type.IsVisibleType())
            {
                var baseRoleType = baseRole.Type;
                if (baseRoleType.GetProperties().Any(ChildrenRenderingUtil.IsVisibleGetterProperty) ||
                    baseRoleType.GetFields().Any(ChildrenRenderingUtil.IsVisibleField))
                {
                    return baseRole;
                }

                baseRole = baseRole.Base;
            }

            return null;
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