﻿using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using FluentlyHttpClient.Middleware;

namespace FluentlyHttpClient.Middleware
{
	/// <summary>
	/// Logger HTTP middleware options.
	/// </summary>
	public class LoggerHttpMiddlewareOptions
	{
		/// <summary>
		/// Gets or sets whether the request log should be detailed e.g. include body. Note: This should only be enabled for development or as needed,
		/// as it will reduce performance.
		/// </summary>
		public bool? ShouldLogDetailedRequest { get; set; }

		/// <summary>
		/// Gets or sets whether the response log should be detailed e.g. include body. Note: This should only be enabled for development or as needed,
		/// as it will reduce performance.
		/// </summary>
		public bool? ShouldLogDetailedResponse { get; set; }
	}

	/// <summary>
	/// Logging middleware for HTTP client.
	/// </summary>
	public class LoggerHttpMiddleware : IFluentHttpMiddleware
	{
		private readonly FluentHttpRequestDelegate _next;
		private readonly LoggerHttpMiddlewareOptions _options;
		private readonly ILogger _logger;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		public LoggerHttpMiddleware(FluentHttpRequestDelegate next, LoggerHttpMiddlewareOptions options, ILogger<LoggerHttpMiddleware> logger)
		{
			_next = next;
			_options = options;
			_logger = logger;
		}

		/// <inheritdoc />
		public async Task<FluentHttpResponse> Invoke(FluentHttpRequest request)
		{
			if (!_logger.IsEnabled(LogLevel.Information))
				return await _next(request);

			var options = request.GetLoggingOptions(_options);
			if (request.Message.Content == null || !options.ShouldLogDetailedRequest.GetValueOrDefault(false))
				_logger.LogInformation("Pre-request... {request}", request);
			else
			{
				var requestContent = await request.Message.Content.ReadAsStringAsync();
				_logger.LogInformation("Pre-request... {request}\nContent: {requestContent}", request, requestContent);
			}

			var response = await _next(request);

			if (response.Content == null || !options.ShouldLogDetailedResponse.GetValueOrDefault(false))
			{
				_logger.LogInformation("Post-request... {response}", response);
				return response;
			}

			var responseContent = await response.Content.ReadAsStringAsync();
			_logger.LogInformation("Post-request... {response}\nContent: {responseContent}", response, responseContent);
			return response;
		}
	}
}

namespace FluentlyHttpClient
{
	/// <summary>
	/// Logger HTTP middleware extensions.
	/// </summary>
	public static class LoggerHttpMiddlwareExtensions
	{
		private const string LoggingOptionsKey = "LOGGING_OPTIONS";

		#region Request Extensions
		/// <summary>
		/// Set logging options for the request.
		/// </summary>
		/// <param name="requestBuilder">Request builder instance.</param>
		/// <param name="options">Logging options to set.</param>
		public static FluentHttpRequestBuilder WithLoggingOptions(this FluentHttpRequestBuilder requestBuilder, LoggerHttpMiddlewareOptions options)
		{
			requestBuilder.Items[LoggingOptionsKey] = options;
			return requestBuilder;
		}

		/// <summary>
		/// Get logging option for the request.
		/// </summary>
		/// <param name="request">Request to get options from.</param>
		/// <param name="defaultOptions"></param>
		/// <returns>Returns merged logging options.</returns>
		public static LoggerHttpMiddlewareOptions GetLoggingOptions(this FluentHttpRequest request, LoggerHttpMiddlewareOptions defaultOptions = null)
		{
			if (!request.Items.TryGetValue(LoggingOptionsKey, out var result)) return defaultOptions;
			var options = (LoggerHttpMiddlewareOptions)result;
			if (defaultOptions == null)
				return options;
			options.ShouldLogDetailedRequest = options.ShouldLogDetailedRequest ?? defaultOptions.ShouldLogDetailedRequest;
			options.ShouldLogDetailedResponse = options.ShouldLogDetailedResponse ?? defaultOptions.ShouldLogDetailedResponse;
			return options;
		}
		#endregion

		/// <summary>
		/// Use logger middleware which logs out going requests and incoming responses.
		/// </summary>
		/// <param name="builder">Builder instance</param>
		/// <param name="options"></param>
		public static FluentHttpClientBuilder UseLogging(this FluentHttpClientBuilder builder, LoggerHttpMiddlewareOptions options = null)
			=> builder.UseMiddleware<LoggerHttpMiddleware>(options ?? new LoggerHttpMiddlewareOptions());
	}
}