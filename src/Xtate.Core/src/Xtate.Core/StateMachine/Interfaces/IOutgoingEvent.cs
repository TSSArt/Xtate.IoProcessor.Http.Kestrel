﻿using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public interface IOutgoingEvent : IEntity
	{
		SendId?                     SendId    { get; }
		ImmutableArray<IIdentifier> NameParts { get; }
		Uri?                        Target    { get; }
		Uri?                        Type      { get; }
		int                         DelayMs   { get; }
		DataModelValue              Data      { get; }
	}
}