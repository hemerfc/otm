using Otm.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Otm.Client.ViewModel
{
	public record LogMessagesViewModel
	{
		public IEnumerable<LogMessage> LogMessages { get; init; }
	}
}
