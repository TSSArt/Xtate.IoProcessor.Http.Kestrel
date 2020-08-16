﻿#region Copyright © 2019-2020 Sergii Artemenko

// This file is part of the Xtate project. <https://xtate.net/>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Xtate
{
	public sealed partial class StateMachineInterpreter : ILoggerContext
	{
	#region Interface ILoggerContext

		DataModelObject ILoggerContext.GetDataModel() => _context.DataModel.AsConstant();

		DataModelArray ILoggerContext.GetActiveStates()
		{
			var list = new DataModelArray();

			foreach (var node in _context.Configuration)
			{
				list.Add(node.Id.Value);
			}

			list.MakeDeepConstant();

			return list;
		}

		SessionId ILoggerContext.SessionId => _sessionId;

		string? ILoggerContext.StateMachineName => _model.Root.Name;

	#endregion

		private bool IsPlatformError(Exception exception)
		{
			for (var ex = exception; ex is { }; ex = ex.InnerException)
			{
				if (ex is PlatformException platformException && platformException.SessionId == _sessionId)
				{
					return true;
				}
			}

			return false;
		}

		private async ValueTask LogInformation(string? label, DataModelValue data, CancellationToken token)
		{
			try
			{
				await _logger.ExecuteLog(this, label, data, token).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				throw new PlatformException(ex, _sessionId);
			}
		}

		private async ValueTask LogError(ErrorType errorType, string? sourceEntityId, Exception exception, CancellationToken token)
		{
			try
			{
				await _logger.LogError(this, errorType, exception, sourceEntityId, token).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				throw new PlatformException(ex, _sessionId);
			}
		}

		private void LogProcessingEvent(IEvent evt)
		{
			if (_logger.IsTracingEnabled)
			{
				_logger.TraceProcessingEvent(this, evt);
			}
		}

		private void LogEnteringState(StateEntityNode state)
		{
			if (_logger.IsTracingEnabled)
			{
				_logger.TraceEnteringState(this, state.Id);
			}
		}

		private void LogEnteredState(StateEntityNode state)
		{
			if (_logger.IsTracingEnabled)
			{
				_logger.TraceEnteredState(this, state.Id);
			}
		}

		private void LogExitingState(StateEntityNode state)
		{
			if (_logger.IsTracingEnabled)
			{
				_logger.TraceExitingState(this, state.Id);
			}
		}

		private void LogExitedState(StateEntityNode state)
		{
			if (_logger.IsTracingEnabled)
			{
				_logger.TraceExitedState(this, state.Id);
			}
		}

		private void LogPerformingTransition(TransitionNode transition)
		{
			if (_logger.IsTracingEnabled)
			{
				_logger.TracePerformingTransition(this, transition.Type, EventDescriptorToString(transition.EventDescriptors), TargetToString(transition.Target));
			}
		}

		private void LogPerformedTransition(TransitionNode transition)
		{
			if (_logger.IsTracingEnabled)
			{
				_logger.TracePerformedTransition(this, transition.Type, EventDescriptorToString(transition.EventDescriptors), TargetToString(transition.Target));
			}
		}

		private static string? TargetToString(ImmutableArray<IIdentifier> list)
		{
			if (list.IsDefault)
			{
				return null;
			}

			return string.Join(separator: @" ", list.Select(id => id.Value));
		}

		private static string? EventDescriptorToString(ImmutableArray<IEventDescriptor> list)
		{
			if (list.IsDefault)
			{
				return null;
			}

			return string.Join(separator: @" ", list.Select(id => id.Value));
		}

		private void LogInterpreterState(StateMachineInterpreterState state)
		{
			if (_logger.IsTracingEnabled)
			{
				_logger.TraceInterpreterState(this, state);
			}
		}
	}
}