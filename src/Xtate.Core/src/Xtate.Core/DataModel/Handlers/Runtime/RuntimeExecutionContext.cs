﻿#region Copyright © 2019-2023 Sergii Artemenko

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

namespace Xtate.DataModel.Runtime;

public class RuntimeExecutionContext
{
<<<<<<< Updated upstream
	public required IInStateController?   InStateController   { get; init; }
	public required ILogController?       LogController       { get; init; }
	public required IEventController?     EventController     { get; init; }
	public required IInvokeController?    InvokeController    { get; init; }
	public required IDataModelController? DataModelController { get; init; }
=======
	public required IInStateController?   InStateController   { get; [UsedImplicitly] init; }
	public required ILogController?       LogController       { get; [UsedImplicitly] init; }
	public required IEventController?     EventController     { get; [UsedImplicitly] init; }
	public required IInvokeController?    InvokeController    { get; [UsedImplicitly] init; }
	public required IDataModelController? DataModelController { get; [UsedImplicitly] init; }
>>>>>>> Stashed changes
}