<<<<<<< Updated upstream
﻿#region Copyright © 2019-2023 Sergii Artemenko

=======
﻿// Copyright © 2019-2023 Sergii Artemenko
// 
>>>>>>> Stashed changes
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

<<<<<<< Updated upstream
#endregion

using System.Collections.Immutable;
using System.Threading.Tasks;
using Xtate.Core;
=======
>>>>>>> Stashed changes
using Xtate.CustomAction;

namespace Xtate.DataModel;

<<<<<<< Updated upstream
public abstract class CustomActionEvaluator : ICustomAction, IExecEvaluator, IAncestorProvider
{
	private readonly ICustomAction _customAction;

	protected CustomActionEvaluator(ICustomAction customAction)
	{
		Infra.Requires(customAction);

		_customAction = customAction;
	}

#region Interface IAncestorProvider

	object IAncestorProvider.Ancestor => _customAction;
=======
public abstract class CustomActionEvaluator(ICustomAction customAction) : ICustomAction, IExecEvaluator, IAncestorProvider
{
#region Interface IAncestorProvider

	object IAncestorProvider.Ancestor => customAction;
>>>>>>> Stashed changes

#endregion

#region Interface ICustomAction

<<<<<<< Updated upstream
	public virtual string?                             XmlNamespace => _customAction.XmlNamespace;
	public virtual string?                             XmlName      => _customAction.XmlName;
	public virtual string?                             Xml          => _customAction.Xml;
	public virtual ImmutableArray<ILocationExpression> Locations    => _customAction.Locations;
	public virtual ImmutableArray<IValueExpression>    Values       => _customAction.Values;
=======
	public virtual string?                             XmlNamespace => customAction.XmlNamespace;
	public virtual string?                             XmlName      => customAction.XmlName;
	public virtual string?                             Xml          => customAction.Xml;
	public virtual ImmutableArray<ILocationExpression> Locations    => customAction.Locations;
	public virtual ImmutableArray<IValueExpression>    Values       => customAction.Values;
>>>>>>> Stashed changes

#endregion

#region Interface IExecEvaluator

	public abstract ValueTask Execute();

<<<<<<< Updated upstream
#endregion 
=======
#endregion
>>>>>>> Stashed changes
}

public class DefaultCustomActionEvaluator : CustomActionEvaluator
{
	private readonly CustomActionContainer? _customActionContainer;

	public DefaultCustomActionEvaluator(ICustomAction customAction) : base(customAction)
	{
		if (customAction.Is(out _customActionContainer))
		{
<<<<<<< Updated upstream
			_customActionContainer.SetEvaluators(customAction.Values, customAction.Locations);
=======
			_customActionContainer.SetEvaluators(base.Values, base.Locations);
>>>>>>> Stashed changes
		}
	}

	public override ValueTask Execute() => _customActionContainer?.Execute() ?? default;
}