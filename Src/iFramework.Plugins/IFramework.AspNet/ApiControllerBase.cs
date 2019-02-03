using IFramework.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace IFramework.AspNet
{
    public class ApiControllerBase : Controller
    {
        public ApiControllerBase(IConcurrencyProcessor concurrencyProcessor)
        {
            ConcurrencyProcessor = concurrencyProcessor;
        }

        protected IConcurrencyProcessor ConcurrencyProcessor { get; }

        protected virtual string GetModelErrorMessage(ModelStateDictionary modelState)
        {
            return string.Join(";", modelState.Where(m => (m.Value?.Errors?.Count ?? 0) > 0)
                                              .Select(m => $"{m.Key}:{string.Join(",", m.Value.Errors.Select(e => e.ErrorMessage + e.Exception?.Message))}"));
        }

        #region process wrapping

        protected virtual T Process<T>(Func<T> func,
                                       bool needRetry = true,
                                       Func<ModelStateDictionary, string> getModelErrorMessage = null,
                                       string[] uniqueConstrainNames = null)
        {
            if (ModelState.IsValid)
            {
                var apiResult = needRetry ? ConcurrencyProcessor.Process(func, uniqueConstrainNames) : func();
                return apiResult;
            }
            getModelErrorMessage = getModelErrorMessage ?? GetModelErrorMessage;
            throw new DomainException(ErrorCode.InvalidParameters, getModelErrorMessage(ModelState));
        }

        protected virtual void Process(Action action,
                                       bool needRetry = true,
                                       Func<ModelStateDictionary, string> getModelErrorMessage = null,
                                       string[] uniqueConstrainNames = null)
        {
            if (ModelState.IsValid)
            {
                if (needRetry)
                {
                    ConcurrencyProcessor.Process(action, uniqueConstrainNames);
                }
                else
                {
                    action();
                }
                return;
            }
            getModelErrorMessage = getModelErrorMessage ?? GetModelErrorMessage;
            throw new DomainException(ErrorCode.InvalidParameters, getModelErrorMessage(ModelState));
        }

        protected virtual async Task ProcessAsync(Func<Task> func,
                                                  bool needRetry = true,
                                                  Func<ModelStateDictionary, string> getModelErrorMessage = null,
                                                  string[] uniqueConstrainNames = null)
        {
            if (ModelState.IsValid)
            {
                if (needRetry)
                {
                    await ConcurrencyProcessor.ProcessAsync(func, uniqueConstrainNames)
                                              .ConfigureAwait(false);
                }
                else
                {
                    await func().ConfigureAwait(false);
                }
                return;
            }
            getModelErrorMessage = getModelErrorMessage ?? GetModelErrorMessage;
            throw new DomainException(ErrorCode.InvalidParameters, getModelErrorMessage(ModelState));
        }

        protected virtual async Task<T> ProcessAsync<T>(Func<Task<T>> func,
                                                        bool needRetry = true,
                                                        Func<ModelStateDictionary, string> getModelErrorMessage = null,
                                                        string[] uniqueConstrainNames = null)
        {
            if (ModelState.IsValid)
            {
                return needRetry
                           ? await ConcurrencyProcessor.ProcessAsync(func, uniqueConstrainNames)
                                                       .ConfigureAwait(false)
                           : await func().ConfigureAwait(false);
            }
            getModelErrorMessage = getModelErrorMessage ?? GetModelErrorMessage;
            throw new DomainException(ErrorCode.InvalidParameters, getModelErrorMessage(ModelState));
        }

        #endregion
    }
}
