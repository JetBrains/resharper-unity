using System;
using JetBrains.Util;
using Mono.Debugging.Backend.Values;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Evaluation;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Evaluation
{
    public static class EvaluatorExceptionThrownExceptionHelper
    {
        public static string? GetThrownExceptionMessage<TValue>(EvaluatorExceptionThrownException<TValue> exception,
                                                                IStackFrame frame,
                                                                IValueServicesFacade<TValue> valueServices,
                                                                IValueFetchOptions valueFetchOptions,
                                                                ILogger logger)
            where TValue : class
        {
            try
            {
                var reference = new SimpleValueReference<TValue>(exception.Exception, frame, valueServices.RoleFactory);
                return reference.AsObjectSafe(valueFetchOptions)
                    ?.GetInstancePropertyReference("Message", true)
                    ?.AsStringSafe(valueFetchOptions)?.GetString();
            }
            catch (Exception e)
            {
                // Argh! Exception thrown while trying to get information about a thrown exception!
                logger.LogExceptionSilently(e);
                return null;
            }
        }
    }
}