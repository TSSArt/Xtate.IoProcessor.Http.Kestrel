﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class CustomActionBase : ICustomActionExecutor
	{
		internal static readonly ICustomActionExecutor NoExecutorInstance = new CustomActionBase();

	#region Interface ICustomActionExecutor

		public virtual ValueTask Execute(IExecutionContext context, CancellationToken token) => throw new NotSupportedException(Resources.Exception_CustomActionDoesNotSupported);

	#endregion
	}
}