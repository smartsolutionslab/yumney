using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartSolutionsLab.Yumney.Shared.Common;

public interface IStaplesProvider
{
	Task<IReadOnlySet<string>> GetStapleNamesAsync(string ownerId, CancellationToken cancellationToken = default);
}
