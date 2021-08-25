using System;
using JetBrains.Annotations;
using JetBrains.Debugger.Worker.Plugins.Unity.Evaluation;
using JetBrains.Util;
using Mono.Debugging.Backend.Values;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Evaluation;

namespace JetBrains.Debugger.Worker.Plugins.Unity
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
            catch (EvaluatorAbortedException e)
            {
                // Evaluation has been aborted, e.g. the user has continued before evaluation has completed
                logger.LogExceptionSilently(e);
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
                valueServices, valueFetchOptions.WithOverridden(o => o.AllowTargetInvoke = true), logger);
            if (exception.ExceptionTypeName == "UnityEngine.UnityException"
                || exception.ExceptionTypeName == "UnityEngine.MissingComponentException"
                || exception.ExceptionTypeName == "UnityEngine.MissingReferenceException"
                || exception.ExceptionTypeName == "UnityEngine.UnassignedReferenceException")
            {
                // These exceptions are possible while we evaluate our extra data. They are (mostly) expected and can be
                // handled gracefully. Log silently and fall back.
                //
                // Note that several of these exceptions are thrown from Unity's "null" Object instances - a valid C#
                // instance that isn't bound to a native object. When a Unity API is called on these instances, they
                // will through the appropriate exception. Stepping into a method on these instances will try to
                // evaluate our extra data with the "null" instance as `this` and we can see `this`, `this.gameObject`
                // or something like `gameObject.transform` throw the exception, which can be very confusing.
                //
                // * UnityException. General purpose, usually means we've called an API when it's not valid
                //   e.g. calling GetActiveScene from the ctor of a MonoBehaviour
                // * MissingComponentException. Calling GetComponent<T> for a Component that isn't attached to the
                //   GameObject will return a "null" instance which throws MissingComponentException when accessing APIs
                // * MissingReferenceException. Accessing a Unity Object that is still a valid C# object but has been
                //   destroyed
                // * UnassignedReferenceException. A serialised field that is not bound to a native object is given a
                //   "null" instance that throws UnassignedReferenceException when an API is called.
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