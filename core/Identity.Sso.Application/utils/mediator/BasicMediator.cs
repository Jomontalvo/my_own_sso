using System.Reflection;
using System.Runtime.ExceptionServices;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Identity.Sso.Application.Exceptions;

namespace Identity.Sso.Application.Utils.Mediator;

public class BasicMediator(IServiceProvider serviceProvider) : IMediator
{
    /// <summary>
    /// Sends a request and returns a response. 
    /// It performs validation if a validator exists for the request type, and then invokes the appropriate handler to process the request. 
    /// If any errors occur during processing, a MediatorException is thrown with details about the error.
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            // Execute validation if a validator exists for the request
            await ExecuteValidationAsync(request, cancellationToken);

            // Determine the type of the use case handler with reflection
            var useCaseType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));

            // Get instance of the use case handler from the service provider
            var useCaseHandler = serviceProvider.GetRequiredService(useCaseType)
                ?? throw new MediatorException($"Handler for request type {request.GetType().Name} not found.");

            // Invoke the Handle method on the handler
            var handleMethod = useCaseType.GetMethod("Handle") ?? throw new MediatorException("Handle method not found on use case handler.");

            // Call the Handle method and return the result
            return await (Task<TResponse>)handleMethod.Invoke(useCaseHandler, [request, cancellationToken])!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            // Surface the real handler failure instead of the reflection wrapper.
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentNullException)
        {
            throw new MediatorException(
                $"An error occurred while processing the request: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Sends a request without a cancellation token.
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
        => await SendAsync(request, CancellationToken.None);

    /// <summary>
    /// Sends a request without expecting a response.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await ExecuteValidationAsync(request, cancellationToken);

        // Determine the type of the use case handler with reflection
        var useCaseType = typeof(IRequestHandler<>).MakeGenericType(request.GetType());

        // Get instance of the use case handler from the service provider
        var useCaseHandler = serviceProvider.GetRequiredService(useCaseType)
            ?? throw new MediatorException($"Handler for request type {request.GetType().Name} not found.");

        var handleMethod = useCaseType.GetMethod("Handle")
            ?? throw new MediatorException("Handle method not found on use case handler.");

        // Call the Handle method not expecting a response
        await (Task)handleMethod.Invoke(useCaseHandler, [request, cancellationToken])!;
    }

    /// <summary>
    /// Executes validation for the given request if a validator exists.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="MediatorException"></exception>
    /// <exception cref="ApplicationValidationException"></exception>
    private async Task ExecuteValidationAsync(object request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate the request if a validator exists
        var validatorType = typeof(IValidator<>).MakeGenericType(request.GetType());
        var validator = serviceProvider.GetService(validatorType) as IValidator;
        if (validator is not null)
        {
            var validationMethod = validatorType.GetMethod("ValidateAsync")
                ?? throw new MediatorException("ValidateAsync method not found on validator.");
            var validationTask = (Task)validationMethod.Invoke(validator, [request, cancellationToken])!;
            await validationTask.ConfigureAwait(false);

            var result = validationTask.GetType().GetProperty("Result");
            var validationResult = (ValidationResult)result?.GetValue(validationTask)!;
            if (!validationResult.IsValid)
            {
                throw new ApplicationValidationException(validationResult);
            }
        }
    }
}