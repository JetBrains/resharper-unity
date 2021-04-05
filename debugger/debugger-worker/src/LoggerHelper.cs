using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Evaluation;
using JetBrains.Util;
using Mono.Debugging.Backend.Values;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Evaluation;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger
{
    public static class LoggerHelper
    {
        [CanBeNull]
        public static T CatchEvaluatorException<TValue, T>(this ILogger logger, [InstantHandle] Func<T> action,
                                                           Action<EvaluatorExceptionThrownException<TValue>> onEvaluatorException)
            where TValue : class
        {
            try
            {
                return action();
            }
            catch (EvaluatorExceptionThrownException<TValue> e)
            {
                // The code being evaluated threw an exception. This might be expected, might not
                onEvaluatorException(e);
            }
            catch (Exception e)
            {
                // We're not expecting this exception, log it as an error so we can fix it
                logger.LogException(e);
            }

            return default;
        }

        // Extract the message from the thrown exception and log it. If it's a UnityException, treat it as expected and
        // log silently (it usually means Unity doesn't like us calling a method at the current location). If it's any
        // other exception, log it as an error so we can fix it.
        public static void LogThrownUnityException<TValue>(this ILogger logger,
                                                           EvaluatorExceptionThrownException<TValue> exception,
                                                           IStackFrame frame,
                                                           IValueServicesFacade<TValue> valueServices,
                                                           IValueFetchOptions valueFetchOptions)
            where TValue : class
        {
            var message = EvaluatorExceptionThrownExceptionHelper.GetThrownExceptionMessage(exception, frame,
                valueServices, valueFetchOptions, logger);
            if (exception.ExceptionTypeName == "UnityEngine.UnityException")
            {
                // We're expecting this if we e.g. call GetActiveScene from a MonoBehaviour constructor. Log the
                logger.Verbose(exception, comment: message);
            }
            else
            {
                // Not expecting this, log it as an error so we can fix it
                logger.Error(exception, "Exception thrown by evaluated code: {0}", message);
            }
        }
    }
}