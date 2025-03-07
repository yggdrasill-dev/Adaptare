﻿using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Adaptare.RabbitMQ.Configuration;

internal class MessageDataInfo
{
	public BasicDeliverEventArgs Args { get; init; } = default!;

	public CancellationToken CancellationToken { get; init; }

	public IChannel Channel { get; init; } = default!;

	public ILogger Logger { get; init; } = default!;

	public IServiceProvider ServiceProvider { get; init; } = default!;
}