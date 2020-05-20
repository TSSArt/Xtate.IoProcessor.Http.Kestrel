﻿namespace TSSArt.StateMachine
{
	public interface IAssign : IExecutableEntity
	{
		ILocationExpression? Location      { get; }
		IValueExpression?    Expression    { get; }
		string?              InlineContent { get; }
	}
}