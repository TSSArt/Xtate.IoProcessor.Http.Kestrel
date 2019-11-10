﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class StateMachineController : IService, IExternalCommunication, ILogger, INotifyStateChanged, IDisposable, IAsyncDisposable
	{
		private readonly TaskCompletionSource<object>         _acceptedTcs = new TaskCompletionSource<object>();
		private readonly Channel<IEvent>                      _channel;
		private readonly TaskCompletionSource<DataModelValue> _completedTcs = new TaskCompletionSource<DataModelValue>();
		private readonly InterpreterOptions                   _defaultOptions;
		private readonly CancellationTokenSource              _destroyTokenSource = new CancellationTokenSource();
		private readonly TimeSpan                             _idlePeriod;
		private readonly IoProcessor                          _ioProcessor;
		private readonly HashSet<ScheduledEvent>              _scheduledEvents = new HashSet<ScheduledEvent>();
		private readonly IStateMachine                        _stateMachine;
		private readonly ConcurrentQueue<ScheduledEvent>      _toDelete = new ConcurrentQueue<ScheduledEvent>();
		private          CancellationTokenSource              _suspendOnIdleTokenSource;

		public StateMachineController(string sessionId, IStateMachine stateMachine, IoProcessor ioProcessor, TimeSpan idlePeriod, in InterpreterOptions defaultOptions)
		{
			SessionId = sessionId;
			_stateMachine = stateMachine;
			_ioProcessor = ioProcessor;
			_defaultOptions = defaultOptions;
			_idlePeriod = idlePeriod;
			_channel = Channel.CreateUnbounded<IEvent>();
		}

		public string SessionId { get; }

		public virtual ValueTask DisposeAsync()
		{
			_destroyTokenSource.Dispose();
			_suspendOnIdleTokenSource?.Dispose();

			return default;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		IReadOnlyList<IEventProcessor> IExternalCommunication.GetIoProcessors() => _ioProcessor.GetIoProcessors();

		async ValueTask<SendStatus> IExternalCommunication.TrySendEvent(IOutgoingEvent @event, CancellationToken token)
		{
			var sendStatus = await _ioProcessor.DispatchEvent(SessionId, @event, token).ConfigureAwait(false);

			if (sendStatus == SendStatus.ToSchedule)
			{
				await ScheduleEvent(@event, token).ConfigureAwait(false);

				return SendStatus.Sent;
			}

			return sendStatus;
		}

		async ValueTask IExternalCommunication.CancelEvent(string sendId, CancellationToken token)
		{
			foreach (var @event in _scheduledEvents)
			{
				if (@event.Event.SendId == sendId)
				{
					await DisposeEvent(@event, token).ConfigureAwait(false);
				}
			}

			CleanScheduledEvents();
		}

		ValueTask IExternalCommunication.StartInvoke(string invokeId, Uri type, Uri source, DataModelValue content, DataModelValue parameters, CancellationToken token) =>
				_ioProcessor.StartInvoke(SessionId, invokeId, type, source, content, parameters, token);

		ValueTask IExternalCommunication.CancelInvoke(string invokeId, CancellationToken token) => _ioProcessor.CancelInvoke(SessionId, invokeId, token);

		bool IExternalCommunication.IsInvokeActive(string invokeId) => _ioProcessor.IsInvokeActive(SessionId, invokeId);

		ValueTask IExternalCommunication.ForwardEvent(IEvent @event, string invokeId, CancellationToken token) => _ioProcessor.ForwardEvent(SessionId, @event, invokeId, token);

		ValueTask ILogger.Log(string stateMachineName, string label, DataModelValue data, CancellationToken token) => _ioProcessor.Log(SessionId, stateMachineName, label, data, token);

		ValueTask ILogger.Error(ErrorType errorType, string stateMachineName, string sourceEntityId, Exception exception, CancellationToken token) =>
				_ioProcessor.Error(SessionId, errorType, stateMachineName, sourceEntityId, exception, token);

		ValueTask INotifyStateChanged.OnChanged(StateMachineInterpreterState state)
		{
			if (state == StateMachineInterpreterState.Accepted)
			{
				_acceptedTcs.TrySetResult(null);
			}
			else if (state == StateMachineInterpreterState.Waiting)
			{
				_suspendOnIdleTokenSource?.CancelAfter(_idlePeriod);
			}

			return default;
		}

		public ValueTask Send(IEvent @event, CancellationToken token) => _channel.Writer.WriteAsync(@event, token);

		ValueTask IService.Destroy(CancellationToken token)
		{
			_destroyTokenSource.Cancel();
			_channel.Writer.Complete();
			return default;
		}

		public ValueTask<DataModelValue> Result => new ValueTask<DataModelValue>(_completedTcs.Task);

		protected virtual void Dispose(bool dispose)
		{
			if (dispose)
			{
				_destroyTokenSource.Dispose();
				_suspendOnIdleTokenSource?.Dispose();
			}
		}

		public ValueTask StartAsync(CancellationToken token)
		{
			token.Register(() => _acceptedTcs.TrySetCanceled(token));

			var _ = RunAsync(false);

			return new ValueTask(_acceptedTcs.Task);
		}

		private void FillOptions(out InterpreterOptions options)
		{
			options = _defaultOptions;

			options.ExternalCommunication = this;
			options.Logger = this;
			options.StorageProvider = this as IStorageProvider;
			options.NotifyStateChanged = this;

			if (_idlePeriod > TimeSpan.Zero)
			{
				_suspendOnIdleTokenSource = new CancellationTokenSource(_idlePeriod);

				options.SuspendToken = options.SuspendToken.CanBeCanceled
						? CancellationTokenSource.CreateLinkedTokenSource(options.SuspendToken, _suspendOnIdleTokenSource.Token).Token
						: _suspendOnIdleTokenSource.Token;
			}

			options.DestroyToken = options.DestroyToken.CanBeCanceled
					? CancellationTokenSource.CreateLinkedTokenSource(options.DestroyToken, _destroyTokenSource.Token).Token
					: _destroyTokenSource.Token;
		}

		public async ValueTask<DataModelValue> ExecuteAsync() => (await RunAsync(true).ConfigureAwait(false)).Result;

		protected virtual ValueTask Initialize() => default;

		private async ValueTask<StateMachineResult> RunAsync(bool throwOnError)
		{
			var exitStatus = StateMachineExitStatus.Unknown;
			var initialized = false;
			while (true)
			{
				try
				{
					if (!initialized)
					{
						initialized = true;

						await Initialize().ConfigureAwait(false);
					}

					FillOptions(out var options);
					var result = await StateMachineInterpreter.RunAsync(SessionId, _stateMachine, _channel.Reader, options).ConfigureAwait(false);
					exitStatus = result.Status;

					_acceptedTcs.TrySetResult(null);

					switch (result.Status)
					{
						case StateMachineExitStatus.Completed:
							_completedTcs.TrySetResult(result.Result);
							return new StateMachineResult(StateMachineExitStatus.Completed, result.Result);

						case StateMachineExitStatus.Suspended:
							break;

						case StateMachineExitStatus.Destroyed:
							var exception = new OperationCanceledException(options.DestroyToken);
							if (throwOnError)
							{
								throw exception;
							}

							_completedTcs.TrySetCanceled(options.DestroyToken);
							return new StateMachineResult(result.Status, exception);

						case StateMachineExitStatus.QueueClosed:
						case StateMachineExitStatus.LiveLockAbort:
							if (throwOnError)
							{
								throw result.Exception;
							}

							_completedTcs.TrySetException(result.Exception);
							return new StateMachineResult(result.Status, result.Exception);

						default: throw new ArgumentOutOfRangeException();
					}

					if (!await _channel.Reader.WaitToReadAsync().ConfigureAwait(false))
					{
						exitStatus = StateMachineExitStatus.QueueClosed;
						await _channel.Reader.ReadAsync().ConfigureAwait(false);
					}
				}
				catch (Exception ex)
				{
					if (ex is OperationCanceledException operationCanceledException)
					{
						_acceptedTcs.TrySetCanceled(operationCanceledException.CancellationToken);
						_completedTcs.TrySetCanceled(operationCanceledException.CancellationToken);
					}
					else
					{
						_acceptedTcs.TrySetException(ex);
						_completedTcs.TrySetException(ex);
					}

					if (throwOnError)
					{
						throw;
					}

					return new StateMachineResult(exitStatus, ex);
				}
			}
		}

		protected virtual ValueTask ScheduleEvent(IOutgoingEvent @event, CancellationToken token)
		{
			var scheduledEvent = new ScheduledEvent(@event);

			_scheduledEvents.Add(scheduledEvent);

			var _ = DelayedFire(scheduledEvent, @event.DelayMs);

			CleanScheduledEvents();

			return default;
		}

		private void CleanScheduledEvents()
		{
			while (_toDelete.TryDequeue(out var scheduledEvent))
			{
				_scheduledEvents.Remove(scheduledEvent);
			}
		}

		protected async ValueTask DelayedFire(ScheduledEvent scheduledEvent, int delayMs)
		{
			await Task.Delay(delayMs).ConfigureAwait(false);

			if (scheduledEvent.IsDisposed)
			{
				return;
			}

			await DisposeEvent(scheduledEvent, token: default).ConfigureAwait(false);

			try
			{
				await _ioProcessor.DispatchEvent(SessionId, scheduledEvent.Event, CancellationToken.None).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				await _ioProcessor.Error(SessionId, ErrorType.Communication, _stateMachine.Name, scheduledEvent.Event.SendId, ex, token: default).ConfigureAwait(false);
			}
		}

		protected virtual ValueTask DisposeEvent(ScheduledEvent scheduledEvent, CancellationToken token)
		{
			scheduledEvent.Dispose();
			_toDelete.Enqueue(scheduledEvent);

			return default;
		}

		protected class ScheduledEvent
		{
			public ScheduledEvent(IOutgoingEvent @event) => Event = @event;

			public IOutgoingEvent Event { get; }

			public bool IsDisposed { get; private set; }

			public void Dispose() => IsDisposed = true;
		}
	}
}