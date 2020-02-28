﻿using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public abstract class EventProcessorBase : IEventProcessor
	{
		private readonly Uri _eventProcessorAliasId;

		private readonly IEventConsumer _eventConsumer;

		protected EventProcessorBase(IEventConsumer eventConsumer)
		{
			_eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));

			var eventProcessorAttribute = GetType().GetCustomAttribute<EventProcessorAttribute>();

			if (eventProcessorAttribute == null)
			{
				throw new InvalidOperationException("EventProcessorAttribute did not provided for type " + GetType());
			}

			EventProcessorId = new Uri(eventProcessorAttribute.Type, UriKind.RelativeOrAbsolute);
			_eventProcessorAliasId = eventProcessorAttribute.Alias != null ? new Uri(eventProcessorAttribute.Alias, UriKind.RelativeOrAbsolute) : null;
		}

		protected Uri EventProcessorId { get; }

		Uri IEventProcessor.Id => EventProcessorId;

		Uri IEventProcessor.AliasId => _eventProcessorAliasId;

		Uri IEventProcessor.GetTarget(string sessionId) => GetTarget(sessionId);

		ValueTask IEventProcessor.Dispatch(string sessionId, IOutgoingEvent @event, CancellationToken token) => OutgoingEvent(sessionId, @event, token);

		protected abstract Uri GetTarget(string sessionId);

		protected abstract ValueTask OutgoingEvent(string sessionId, IOutgoingEvent @event, CancellationToken token);

		protected ValueTask IncomingEvent(string sessionId, IEvent @event, CancellationToken token) => _eventConsumer.Dispatch(sessionId, @event, token);
	}
}